using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RunnersPal.Core.Data;
using RunnersPal.Core.Extensions;

namespace RunnersPal.Core.Controllers;

public class AuthorisationHandler(ILogger<AuthorisationHandler> logger, IConfiguration configuration, IFido2 fido2)
    : IAuthorisationHandler
{
    public (bool IsReturningUser, string VerifyOptions) HandleSigninRequest(string email, CancellationToken cancellationToken)
    {
        dynamic? user;
        string options;
        if ((user = MassiveDB.Current.FindUserByEmailAddress(email)) != null)
        {
            logger.LogTrace($"Found existing user account with email [{email}], creating assertion options");
            options = fido2.GetAssertionOptions(
                MassiveDB.Current
                    .GetUserAccountAuthentications((long)user.Id)
                    .Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))
                    .ToArray(),
                UserVerificationRequirement.Discouraged,
                new AuthenticationExtensionsClientInputs()
                {
                    Extensions = true,
                    UserVerificationMethod = true,
                    AppID = configuration.GetValue<string>("FidoOrigins")
                }
            ).ToJson();
        }
        else
        {
            logger.LogTrace($"Found no user account with email [{email}], creating request new creds options");
            options = fido2.RequestNewCredential(
                new Fido2User() { Id = Encoding.UTF8.GetBytes(email), Name = email, DisplayName = email },
                [],
                AuthenticatorSelection.Default,
                AttestationConveyancePreference.None,
                new()
                {
                    Extensions = true,
                    UserVerificationMethod = true,
                    AppID = configuration.GetValue<string>("FidoOrigins")
                }
            ).ToJson();
        }

        logger.LogTrace($"Created sign in options: {options}");

        return (user != null, options);        
    }

    public async Task<(bool IsValid, string UserType)> HandleSigninVerifyRequest(HttpContext httpContext, string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        dynamic? user;
        if ((user = MassiveDB.Current.FindUserByEmailAddress(email)) != null)
        {
            if (!await SigninUserAsync(user, verifyOptions, verifyResponse, cancellationToken))
                return (false, "");
        }
        else
        {
            user = await CreateNewUserAsync(httpContext, email, verifyOptions, verifyResponse, cancellationToken);
            if (user == null)
                return (false, "");
        }

        // set our own cookie & session state
        long? userId = user.Id;
        httpContext.Session.Set("rp_UserAccount", userId);
        httpContext.Response.Cookies.Append("rp_UserAccount", Secure.EncryptValue(userId.ToString()), new CookieOptions { Expires = DateTime.UtcNow.AddYears(1) });

        List<Claim> claims = [new Claim(ClaimTypes.Name, user.EmailAddress)];
        ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties authProperties = new() { IsPersistent = true };
        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        logger.LogTrace($"Signed in {userId}: {email}");

        return (true, user.UserType);
    }

    private async Task<dynamic?> CreateNewUserAsync(HttpContext httpContext, string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        logger.LogTrace("Creating new user credientials");
        var options = CredentialCreateOptions.FromJson(verifyOptions);

        AuthenticatorAttestationRawResponse? authenticatorAttestationRawResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(verifyResponse);
        if (authenticatorAttestationRawResponse == null)
        {
            logger.LogWarning($"Cannot parse signin verify response: {verifyResponse}");
            return null;
        }

        logger.LogTrace($"Successfully parsed response: {verifyResponse}");

        var success = await fido2.MakeNewCredentialAsync(authenticatorAttestationRawResponse, options, (_, _) => Task.FromResult(true), cancellationToken: cancellationToken);
        logger.LogInformation($"got success status: {success.Status} error: {success.ErrorMessage}");
        if (success.Result == null)
        {
            logger.LogWarning($"Could not create new credential: {success.Status} - {success.ErrorMessage}");
            return null;
        }

        logger.LogTrace($"Got new credential: {JsonSerializer.Serialize(success.Result)}");

        return MassiveDB.Current.CreateUser(email, httpContext.Connection?.RemoteIpAddress?.ToString(), httpContext.UserDistanceUnits(),
            success.Result.CredentialId, success.Result.PublicKey, success.Result.User.Id);
    }

    private async Task<bool> SigninUserAsync(dynamic user, string verifyOptions, string verifyResponse, CancellationToken cancellationToken)
    {
        long userId = user.Id;
        logger.LogTrace($"Checking credientials for user {userId}: {verifyResponse}");
        AuthenticatorAssertionRawResponse? authenticatorAssertionRawResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(verifyResponse);
        if (authenticatorAssertionRawResponse == null)
        {
            logger.LogWarning($"Cannot parse signin assertion verify response: {verifyResponse}");
            return false;
        }
        var options = AssertionOptions.FromJson(verifyOptions);

        dynamic? userAccountCredential = null;
        foreach (dynamic uac in MassiveDB.Current.GetUserAccountAuthentications(userId))
        {
            if (((byte[])uac.CredentialId).SequenceEqual(authenticatorAssertionRawResponse.Id))
            {
                userAccountCredential = uac;
                break;
            }
        }

        if (userAccountCredential == null)
        {
            logger.LogWarning($"No credential id [{Convert.ToBase64String(authenticatorAssertionRawResponse.Id)}] for user [{user.EmailAddress}]");
            return false;
        }
        
        logger.LogTrace($"Making assertion for user [{user.EmailAddress}]");
        var res = await fido2.MakeAssertionAsync(authenticatorAssertionRawResponse, options, (byte[])userAccountCredential.PublicKey, (uint)userAccountCredential.SignatureCount, VerifyExistingUserCredentialAsync, cancellationToken: cancellationToken);
        if (!string.IsNullOrEmpty(res.ErrorMessage))
        {
            logger.LogWarning($"Signin assertion failed: {res.Status} - {res.ErrorMessage}");
            return false;
        }

        logger.LogTrace($"Signin success, got response: {JsonSerializer.Serialize(res)}");
        userAccountCredential.SignatureCount = res.Counter;
        MassiveDB.Current.UpdateUserAuthentication(userAccountCredential);

        return true;
    }

    private Task<bool> VerifyExistingUserCredentialAsync(IsUserHandleOwnerOfCredentialIdParams credentialIdUserHandleParams, CancellationToken cancellationToken)
    {
        logger.LogInformation($"Checking credential {credentialIdUserHandleParams.CredentialId} - {credentialIdUserHandleParams.UserHandle}");
        var userAccountCredentials = MassiveDB.Current.FindUserAccountAuthenticationByUserHandle(credentialIdUserHandleParams.UserHandle);
        return Task.FromResult(userAccountCredentials?.CredentialId.SequenceEqual(credentialIdUserHandleParams.CredentialId) ?? false);
    }
}
