using Moq;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.Agent.Tools;

namespace SecureBankingAI.UnitTests.Accounts;

public sealed class GetRecentTransactionsToolTests
{
    private readonly Mock<IAccountAuthorizationService>
        _authorizationMock = new();

    private readonly Mock<IAccountTransactionRepository>
        _repositoryMock = new();

    private readonly GetRecentTransactionsTool _tool;

    public GetRecentTransactionsToolTests()
    {
        _tool = new GetRecentTransactionsTool(
            _authorizationMock.Object,
            _repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserOwnsAccount_ReturnsTransactions()
    {
        _authorizationMock
            .Setup(x => x.CanAccessAccountAsync(
                "dev-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.GetRecentTransactionsAsync(
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountTransaction>
            {
                new(
                    "TXN-001",
                    "ACC-1001",
                    "Supermarket",
                    -85.50m,
                    "AUD",
                    "Completed")
            });

        var result = await _tool.ExecuteAsync(
            "dev-user",
            "ACC-1001");

        Assert.Contains("TXN-001", result);
        Assert.Contains("Supermarket", result);
        Assert.Contains("85.50", result);

        _repositoryMock.Verify(
            x => x.GetRecentTransactionsAsync(
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserDoesNotOwnAccount_DeniesAccess()
    {
        _authorizationMock
            .Setup(x => x.CanAccessAccountAsync(
                "another-demo-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _tool.ExecuteAsync(
            "another-demo-user",
            "ACC-1001");

        Assert.Equal(
            "Account not found or access denied.",
            result);

        _repositoryMock.Verify(
            x => x.GetRecentTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMaxResultsExceedsLimit_ThrowsException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _tool.ExecuteAsync(
                "dev-user",
                "ACC-1001",
                11));

        _authorizationMock.Verify(
            x => x.CanAccessAccountAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoTransactionsExist_ReturnsMessage()
    {
        _authorizationMock
            .Setup(x => x.CanAccessAccountAsync(
                "dev-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _repositoryMock
            .Setup(x => x.GetRecentTransactionsAsync(
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountTransaction>());

        var result = await _tool.ExecuteAsync(
            "dev-user",
            "ACC-1001");

        Assert.Equal(
            "No transactions were found for this account.",
            result);
    }
}