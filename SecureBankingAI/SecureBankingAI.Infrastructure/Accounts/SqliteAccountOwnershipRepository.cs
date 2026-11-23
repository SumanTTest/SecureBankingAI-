
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class SqliteAccountOwnershipRepository
    : IAccountOwnershipRepository
{
    private readonly SecureBankingAiDbContext _dbContext;

    public SqliteAccountOwnershipRepository(
        SecureBankingAiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string?> GetOwnerIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException(
                "Account ID is required.",
                nameof(accountId));
        }

        return await _dbContext.BankAccounts
            .AsNoTracking()
            .Where(account => account.AccountId == accountId)
            .Select(account => account.OwnerId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
