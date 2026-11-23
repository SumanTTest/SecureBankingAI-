
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.Accounts;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.UnitTests.Accounts;

public sealed class SqliteAccountRepositoriesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SecureBankingAiDbContext _dbContext;

    private readonly SqliteAccountOwnershipRepository _ownershipRepository;
    private readonly SqliteAccountTransactionRepository _transactionRepository;

    public SqliteAccountRepositoriesTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SecureBankingAiDbContext>()
            .UseSqlite(_connection)
            .Options;

        _dbContext = new SecureBankingAiDbContext(options);
        _dbContext.Database.EnsureCreated();

        _ownershipRepository =
            new SqliteAccountOwnershipRepository(_dbContext);

        _transactionRepository =
            new SqliteAccountTransactionRepository(_dbContext);

        SeedTestData();
    }

    [Fact]
    public async Task GetOwnerIdAsync_WhenAccountExists_ReturnsOwnerId()
    {
        var result = await _ownershipRepository.GetOwnerIdAsync("ACC-1001");

        Assert.Equal("dev-user", result);
    }

    [Fact]
    public async Task GetOwnerIdAsync_WhenAccountDoesNotExist_ReturnsNull()
    {
        var result = await _ownershipRepository.GetOwnerIdAsync("ACC-9999");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetOwnerIdAsync_WhenAccountIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _ownershipRepository.GetOwnerIdAsync(""));
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_ReturnsOnlyTransactionsForRequestedAccount()
    {
        var result = await _transactionRepository.GetRecentTransactionsAsync(
            "ACC-1001",
            10);

        Assert.Equal(2, result.Count);
        Assert.All(result, transaction =>
            Assert.Equal("ACC-1001", transaction.AccountId));
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_WhenAccountHasNoTransactions_ReturnsEmptyList()
    {
        var result = await _transactionRepository.GetRecentTransactionsAsync(
            "ACC-9999",
            5);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_RespectsMaximumResults()
    {
        var result = await _transactionRepository.GetRecentTransactionsAsync(
            "ACC-1001",
            1);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetRecentTransactionsAsync_WhenMaxResultsIsOutsideRange_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _transactionRepository.GetRecentTransactionsAsync(
                "ACC-1001",
                11));
    }

    private void SeedTestData()
    {
        _dbContext.BankAccounts.AddRange(
            new BankAccount
            {
                AccountId = "ACC-1001",
                OwnerId = "dev-user",
                Status = "Active",
                AccountType = "Everyday Account",
                Balance = 5250.75m,
                Currency = "AUD"
            },
            new BankAccount
            {
                AccountId = "ACC-2002",
                OwnerId = "another-demo-user",
                Status = "Restricted",
                AccountType = "Savings Account",
                Balance = 12000m,
                Currency = "AUD"
            });

        _dbContext.BankTransactions.AddRange(
            new BankTransaction
            {
                TransactionId = "TXN-001",
                AccountId = "ACC-1001",
                Description = "Supermarket",
                Amount = -85.50m,
                Currency = "AUD",
                Status = "Completed"
            },
            new BankTransaction
            {
                TransactionId = "TXN-002",
                AccountId = "ACC-1001",
                Description = "Salary",
                Amount = 3500m,
                Currency = "AUD",
                Status = "Completed"
            },
            new BankTransaction
            {
                TransactionId = "TXN-003",
                AccountId = "ACC-2002",
                Description = "Online transfer",
                Amount = -250m,
                Currency = "AUD",
                Status = "Completed"
            });

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        _connection.Dispose();
    }
}
