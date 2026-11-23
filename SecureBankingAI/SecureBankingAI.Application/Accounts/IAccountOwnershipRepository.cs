
namespace SecureBankingAI.Application.Accounts;

public interface IAccountOwnershipRepository
{
    Task<string?> GetOwnerIdAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
