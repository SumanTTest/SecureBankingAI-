
namespace SecureBankingAI.Application.Accounts;

public sealed record AccountStatusRecord(
    string AccountId,
    string AccountType,
    string Status);

public interface IAccountStatusRepository
{
    Task<IReadOnlyList<AccountStatusRecord>> GetAccountsForUserAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
