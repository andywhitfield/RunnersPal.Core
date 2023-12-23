using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RunnersPal.Core.Controllers;

public interface IAuthorisationHandler
{
    (bool IsReturningUser, string VerifyOptions) HandleSigninRequest(string email, CancellationToken cancellationToken);
    Task<(bool IsValid, string UserType)> HandleSigninVerifyRequest(HttpContext httpContext, string email, string verifyOptions, string verifyResponse, CancellationToken cancellationToken);
}
