namespace SecureBankingAI.Application.Accounts;

public interface IAccountTransactionRepository
{
    Task<IReadOnlyList<AccountTransaction>> GetRecentTransactionsAsync(
        string accountId,
        int maxResults,
        CancellationToken cancellationToken = default);
}

public sealed record AccountTransaction(
    string TransactionId,
    string AccountId,
    string Description,
    decimal Amount,
    string Currency,
    string Status);