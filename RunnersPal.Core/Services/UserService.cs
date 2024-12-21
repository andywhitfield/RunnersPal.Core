using System.Security.Claims;

namespace RunnersPal.Core.Services;

public class UserService(IHttpContextAccessor httpContextAccessor) : IUserService
{
    public bool IsLoggedIn => !string.IsNullOrEmpty(httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name));
}