using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class SqliteAccountTransactionRepository
    : IAccountTransactionRepository
{
    private readonly SecureBankingAiDbContext _dbContext;

    public SqliteAccountTransactionRepository(
        SecureBankingAiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AccountTransaction>>
        GetRecentTransactionsAsync(
            string accountId,
            int maxResults,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException(
                "Account ID is required.",
                nameof(accountId));
        }

        if (maxResults is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                "Maximum results must be between 1 and 10.");
        }

        return await _dbContext.BankTransactions
            .AsNoTracking()
            .Where(transaction => transaction.AccountId == accountId)
            .OrderByDescending(transaction => transaction.TransactionDateUtc)
            .Take(maxResults)
            .Select(transaction => new AccountTransaction(
                transaction.TransactionId,
                transaction.AccountId,
                transaction.Description,
                transaction.Amount,
                transaction.Currency,
                transaction.Status))
            .ToListAsync(cancellationToken);
    }
}