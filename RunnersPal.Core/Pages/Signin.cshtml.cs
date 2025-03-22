using System.Text;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RunnersPal.Core.Models;
using RunnersPal.Core.Repository;

namespace RunnersPal.Core.Pages;

public class SigninModel(ILogger<SigninModel> logger, IFido2 fido2,
    IUserAccountRepository userAccountRepository)
    : PageModel
{
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty] public string? Email { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPost()
    {
        logger.LogInformation("Checking user: {Email} / {ReturnUrl}", Email, ReturnUrl);

        if (string.IsNullOrEmpty(Email))
            return Page();

        UserAccount? user;
        string options;
        if ((user = await userAccountRepository.GetUserAccountByEmailAsync(Email)) != null)
        {
            logger.LogTrace("Found existing user account with email [{Email}], creating assertion options", Email);
            options = fido2.GetAssertionOptions(
                await userAccountRepository
                    .GetUserAccountAuthenticationsAsync(user)
                    .Select(uac => new PublicKeyCredentialDescriptor(uac.CredentialId))
                    .ToArrayAsync(),
                UserVerificationRequirement.Discouraged
            ).ToJson();
        }
        else
        {
            logger.LogTrace("Found no user account with email [{Email}], creating request new creds options", Email);
            options = fido2.RequestNewCredential(
                new Fido2User() { Id = Encoding.UTF8.GetBytes(Email), Name = Email, DisplayName = Email },
                [],
                AuthenticatorSelection.Default,
                AttestationConveyancePreference.None
            ).ToJson();
        }

        logger.LogTrace("Created sign in options for: {Email}: {Options}", Email, options);
        return RedirectToPage("./signinverify", routeValues: new { ReturnUrl, Email, VerifyOptions = options });
    }
}
