using System.Security.Claims;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public interface IUserAccountRepository
{
    Task<UserAccount> CreateNewUserAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle);
    Task<UserAccount?> GetAsync(int userAccountId);
    Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user);
    Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user);
    Task<UserAccount?> GetUserAccountByEmailAsync(string email);
    IAsyncEnumerable<UserAccountAuthentication> GetUserAccountAuthenticationsAsync(UserAccount user);
    Task<UserAccountAuthentication?> GetUserAccountAuthenticationByUserHandleAsync(byte[] userHandle);
    Task SetSignatureCountAsync(UserAccountAuthentication userAccountAuthentication, uint signatureCount);
    Task UpdateAsync(UserAccount userAccount);
}