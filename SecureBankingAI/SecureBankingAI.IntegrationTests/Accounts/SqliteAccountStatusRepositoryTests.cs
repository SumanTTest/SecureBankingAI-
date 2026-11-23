
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Infrastructure.Accounts;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.IntegrationTests.Accounts;

public sealed class SqliteAccountStatusRepositoryTests
{
    [Fact]
    public async Task GetAccountsForUserAsync_ReturnsOnlyAccountsBelongingToUser()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SecureBankingAiDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new SecureBankingAiDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.BankAccounts.AddRange(
            new BankAccount
            {
                AccountId = "ACC-1001",
                OwnerId = "customer-a",
                AccountType = "Everyday Account",
                Status = "Active"
            },
            new BankAccount
            {
                AccountId = "ACC-1002",
                OwnerId = "customer-a",
                AccountType = "Savings Account",
                Status = "Active"
            },
            new BankAccount
            {
                AccountId = "ACC-2001",
                OwnerId = "customer-b",
                AccountType = "Savings Account",
                Status = "Restricted"
            });

        await dbContext.SaveChangesAsync();

        var repository =
            new SqliteAccountStatusRepository(dbContext);

        var results = await repository.GetAccountsForUserAsync(
            "customer-a");

        Assert.Equal(2, results.Count);

        Assert.All(
            results,
            account => Assert.Contains(
                account.AccountId,
                new[] { "ACC-1001", "ACC-1002" }));

        Assert.DoesNotContain(
            results,
            account => account.AccountId == "ACC-2001");
    }

    [Fact]
    public async Task GetAccountsForUserAsync_WhenUserHasNoAccounts_ReturnsEmptyList()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SecureBankingAiDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new SecureBankingAiDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var repository =
            new SqliteAccountStatusRepository(dbContext);

        var results = await repository.GetAccountsForUserAsync(
            "unknown-user");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetAccountsForUserAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await using var connection =
            new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SecureBankingAiDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new SecureBankingAiDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var repository =
            new SqliteAccountStatusRepository(dbContext);

        await Assert.ThrowsAsync<ArgumentException>(
            () => repository.GetAccountsForUserAsync(""));
    }
}
