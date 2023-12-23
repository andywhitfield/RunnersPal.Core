namespace RunnersPal.Core.ViewModels;

public class LoginVerifyViewModel(string? returnUrl, string email, bool isReturningUser, string verifyOptions)
{
    public string? ReturnUrl { get; } = returnUrl;
    public string Email { get; } = email;
    public bool IsReturningUser { get; } = isReturningUser;
    public string VerifyOptions { get; } = verifyOptions;
}
