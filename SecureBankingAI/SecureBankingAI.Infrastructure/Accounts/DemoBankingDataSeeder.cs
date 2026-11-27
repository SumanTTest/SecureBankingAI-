
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.Infrastructure.Accounts;

public static class DemoBankingDataSeeder
{
    public static async Task SeedAsync(
        SecureBankingAiDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var firstAccount = await EnsureAccountAsync(
            dbContext,
            new BankAccount
            {
                AccountId = "ACC-1001",
                OwnerId = "dev-user",
                Status = "Active",
                AccountType = "Everyday Account",
                Balance = 5250.75m,
                Currency = "AUD"
            },
            cancellationToken);

        var secondAccount = await EnsureAccountAsync(
            dbContext,
            new BankAccount
            {
                AccountId = "ACC-2002",
                OwnerId = "another-demo-user",
                Status = "Restricted",
                AccountType = "Savings Account",
                Balance = 12000.00m,
                Currency = "AUD"
            },
            cancellationToken);

        if (firstAccount)
        {
            await EnsureTransactionAsync(dbContext, new BankTransaction
            {
                TransactionId = "TXN-001",
                AccountId = "ACC-1001",
                Description = "Supermarket",
                Amount = -85.50m,
                Currency = "AUD",
                Status = "Completed",
                TransactionDateUtc = DateTime.UtcNow
            }, cancellationToken);

            await EnsureTransactionAsync(dbContext, new BankTransaction
            {
                TransactionId = "TXN-002",
                AccountId = "ACC-1001",
                Description = "Salary",
                Amount = 3500.00m,
                Currency = "AUD",
                Status = "Completed",
                TransactionDateUtc = DateTime.UtcNow
            }, cancellationToken);

            await EnsureTransactionAsync(dbContext, new BankTransaction
            {
                TransactionId = "TXN-003",
                AccountId = "ACC-1001",
                Description = "Electricity",
                Amount = -120.00m,
                Currency = "AUD",
                Status = "Pending",
                TransactionDateUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        if (secondAccount)
        {
            await EnsureTransactionAsync(dbContext, new BankTransaction
            {
                TransactionId = "TXN-004",
                AccountId = "ACC-2002",
                Description = "Online transfer",
                Amount = -250.00m,
                Currency = "AUD",
                Status = "Completed",
                TransactionDateUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<bool> EnsureAccountAsync(
        SecureBankingAiDbContext dbContext,
        BankAccount account,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.BankAccounts
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AccountId == account.AccountId,
                cancellationToken);

        if (existing is null)
        {
            dbContext.BankAccounts.Add(account);
            return true;
        }

        // Never overwrite an existing account or its ownership.
        return existing.OwnerId == account.OwnerId;
    }

    private static async Task EnsureTransactionAsync(
        SecureBankingAiDbContext dbContext,
        BankTransaction transaction,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.BankTransactions
            .AnyAsync(
                item => item.TransactionId == transaction.TransactionId,
                cancellationToken);

        if (!exists)
        {
            dbContext.BankTransactions.Add(transaction);
        }
    }
}
