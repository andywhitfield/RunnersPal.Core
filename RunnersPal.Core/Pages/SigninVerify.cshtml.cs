using System.Security.Claims;
using System.Text.Json;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages;

public class SigninVerifyModel(ILogger<SigninVerifyModel> logger, IFido2 fido2,
    IUserAccountRepository userAccountRepository)
    : PageModel
{
    public bool IsReturningUser { get; private set; }

    [BindProperty(SupportsGet = true)] public string? Email { get; set; }
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public string? VerifyOptions { get; set; }
    [BindProperty] public string? VerifyResponse { get; set; }

    public async Task OnGet() =>
        IsReturningUser = await userAccountRepository.GetUserAccountByEmailAsync(Email ?? "") != null;

    public async Task<IActionResult> OnPost()
    {
        if (Email == null)
        {
            logger.LogWarning("Missing Email");
            return Page();
        }
        if (VerifyResponse == null)
        {
            logger.LogWarning("Missing VerifyResponse");
            return Page();
        }

        UserAccount? user;
        if ((user = await userAccountRepository.GetUserAccountByEmailAsync(Email)) != null)
        {
            if (!await SigninUserAsync(user))
                return Page();
        }
        else
        {
            user = await CreateNewUserAsync();
            if (user == null)
                return Page();
        }

        List<Claim> claims = [new Claim(ClaimTypes.Name, Email!)];
        ClaimsIdentity claimsIdentity = new(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        AuthenticationProperties authProperties = new() { IsPersistent = true };
        await Request.HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        logger.LogTrace("Signed in: {Email}", Email);

        return Redirect(string.IsNullOrEmpty(ReturnUrl) || !ReturnUrl.StartsWith('/') ? "/" : ReturnUrl);
    }

    private async Task<UserAccount?> CreateNewUserAsync()
    {
        var options = CredentialCreateOptions.FromJson(VerifyOptions ?? "");

        AuthenticatorAttestationRawResponse? authenticatorAttestationRawResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(VerifyResponse ?? "");
        if (authenticatorAttestationRawResponse == null)
        {
            logger.LogWarning("Cannot parse signin verify response: {VerifyResponse}", VerifyResponse);
            return null;
        }

        logger.LogTrace("Successfully parsed response: {VerifyResponse}", VerifyResponse);

        var success = await fido2.MakeNewCredentialAsync(new()
        {
            AttestationResponse = authenticatorAttestationRawResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = (_, _) => Task.FromResult(true)
        });
        logger.LogTrace("Got new credential: {Result}", JsonSerializer.Serialize(success));

        return await userAccountRepository.CreateNewUserAsync(Email!, success.Id,
            success.PublicKey, success.User.Id);
    }

    private async Task<bool> SigninUserAsync(UserAccount user)
    {
        logger.LogTrace("Checking credientials: {VerifyResponse}", VerifyResponse);
        AuthenticatorAssertionRawResponse? authenticatorAssertionRawResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(VerifyResponse!);
        if (authenticatorAssertionRawResponse == null)
        {
            logger.LogWarning("Cannot parse signin assertion verify response: {VerifyResponse}", VerifyResponse);
            return false;
        }
        var options = AssertionOptions.FromJson(VerifyOptions ?? "");
        var userAccountAuthentication = await userAccountRepository.GetUserAccountAuthenticationsAsync(user).FirstOrDefaultAsync(uac => (uac.CredentialId ?? []).SequenceEqual(authenticatorAssertionRawResponse.RawId));
        if (userAccountAuthentication == null)
        {
            logger.LogWarning("No credential id [{Id}] for user [{Email}]", Convert.ToBase64String(authenticatorAssertionRawResponse.RawId), user.EmailAddress);
            return false;
        }

        logger.LogTrace("Making assertion for user [{Email}]", user.EmailAddress);
        var res = await fido2.MakeAssertionAsync(new()
        {
            AssertionResponse = authenticatorAssertionRawResponse,
            OriginalOptions = options,
            StoredPublicKey = userAccountAuthentication.PublicKey ?? [],
            StoredSignatureCounter = userAccountAuthentication.SignatureCount.GetValueOrDefault(),
            IsUserHandleOwnerOfCredentialIdCallback = VerifyExistingUserCredentialAsync
        });
        if (res == null)
        {
            logger.LogWarning("Signin assertion failed");
            return false;
        }

        logger.LogTrace("Signin success, got response: {Res}", JsonSerializer.Serialize(res));
        await userAccountRepository.SetSignatureCountAsync(userAccountAuthentication, res.SignCount);

        return true;
    }

    private async Task<bool> VerifyExistingUserCredentialAsync(IsUserHandleOwnerOfCredentialIdParams credentialIdUserHandleParams, CancellationToken cancellationToken)
    {
        logger.LogInformation("Checking credential {CredentialId} - {UserHandle}", credentialIdUserHandleParams.CredentialId, credentialIdUserHandleParams.UserHandle);
        var userAccountAuthentication = await userAccountRepository.GetUserAccountAuthenticationByUserHandleAsync(credentialIdUserHandleParams.UserHandle);
        return (userAccountAuthentication?.CredentialId ?? []).SequenceEqual(credentialIdUserHandleParams.CredentialId);
    }
}
