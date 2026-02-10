using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using RunnersPal.Core.Models;

namespace RunnersPal.Core.Repository;

public class UserAccountRepository(ILogger<UserAccountRepository> logger, SqliteDataContext context)
    : IUserAccountRepository
{
    public Task<UserAccount?> GetAsync(int userAccountId) =>
        context.UserAccount.SingleOrDefaultAsync(a => a.Id == userAccountId);

    public async Task<UserAccount> CreateNewUserAsync(string email, byte[] credentialId, byte[] publicKey, byte[] userHandle)
    {
        var newUserAccount = context.UserAccount.Add(new UserAccount { EmailAddress = email, DisplayName = "", OriginalHostAddress = "" });
        context.UserAccountAuthentication.Add(new()
        {
            UserAccount = newUserAccount.Entity,
            CredentialId = credentialId,
            PublicKey = publicKey,
            UserHandle = userHandle
        });
        await context.SaveChangesAsync();
        return newUserAccount.Entity;
    }

    private string? GetEmailFromPrincipal(ClaimsPrincipal user)
    {
        logger.LogTrace("Getting email from user: {Name}: [{Claims}]", user?.Identity?.Name, string.Join(',', user?.Claims.Select(c => $"{c.Type}={c.Value}") ?? []));
        return user?.FindFirstValue(ClaimTypes.Name);
    }

    public async Task<UserAccount> GetUserAccountAsync(ClaimsPrincipal user)
        => (await GetUserAccountOrNullAsync(user)) ?? throw new ArgumentException($"No UserAccount for the user: {GetEmailFromPrincipal(user)}");

    public Task<UserAccount?> GetUserAccountOrNullAsync(ClaimsPrincipal user)
    {
        var email = GetEmailFromPrincipal(user);
        if (string.IsNullOrWhiteSpace(email))
            return Task.FromResult((UserAccount?)null);

        return context.UserAccount.FirstOrDefaultAsync(ua => ua.EmailAddress == email);
    }

    public Task<UserAccount?> GetUserAccountByEmailAsync(string email)
        => context.UserAccount.FirstOrDefaultAsync(a => a.EmailAddress == email);

    public IAsyncEnumerable<UserAccountAuthentication> GetUserAccountAuthenticationsAsync(UserAccount user)
        => context.UserAccountAuthentication.Where(uac => uac.UserAccountId == user.Id).AsAsyncEnumerable();

    public Task<UserAccountAuthentication?> GetUserAccountAuthenticationByUserHandleAsync(byte[] userHandle)
        => context.UserAccountAuthentication.FirstOrDefaultAsync(uac => uac.UserHandle != null && uac.UserHandle.SequenceEqual(userHandle));

    public Task SetSignatureCountAsync(UserAccountAuthentication userAccountAuthentication, uint signatureCount)
    {
        userAccountAuthentication.SignatureCount = signatureCount;
        return context.SaveChangesAsync();
    }

    public Task UpdateAsync(UserAccount userAccount)
    {
        userAccount.LastActivityDate = DateTime.UtcNow;
        return context.SaveChangesAsync();
    }

    public Task<UserAccount> GetAdminUserAccountAsync()
        => context.UserAccount.FirstAsync(ua => ua.DisplayName == "Admin" && ua.UserType == 'A');
}
