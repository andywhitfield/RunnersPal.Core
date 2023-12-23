namespace RunnersPal.Core.ViewModels;

public class LoginViewModel(string? returnUrl)
{
    public string? ReturnUrl { get; } = returnUrl;
}
