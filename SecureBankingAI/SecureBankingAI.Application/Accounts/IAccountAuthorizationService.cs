
namespace SecureBankingAI.Application.Accounts;

public interface IAccountAuthorizationService
{
    Task<bool> CanAccessAccountAsync(
        string userId,
        string accountId,
        CancellationToken cancellationToken = default);
}
