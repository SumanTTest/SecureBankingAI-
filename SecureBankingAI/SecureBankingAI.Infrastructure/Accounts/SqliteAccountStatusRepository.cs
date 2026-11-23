using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class SqliteAccountStatusRepository
    : IAccountStatusRepository
{
    private readonly SecureBankingAiDbContext _dbContext;

    public SqliteAccountStatusRepository(
        SecureBankingAiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AccountStatusRecord>>
        GetAccountsForUserAsync(
            string userId,
            CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        return await _dbContext.BankAccounts
            .AsNoTracking()
            .Where(account => account.OwnerId == userId)
            .Select(account => new AccountStatusRecord(
                account.AccountId,
                account.AccountType,
                account.Status))
            .ToListAsync(cancellationToken);
    }
}