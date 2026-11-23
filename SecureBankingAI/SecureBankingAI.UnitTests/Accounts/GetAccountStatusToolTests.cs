using Moq;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Infrastructure.Agent.Tools;

namespace SecureBankingAI.UnitTests.Accounts;

public class GetAccountStatusToolTests
{
    private readonly Mock<IAccountStatusRepository> _repositoryMock;
    private readonly GetAccountStatusTool _tool;

    public GetAccountStatusToolTests()
    {
        _repositoryMock = new Mock<IAccountStatusRepository>();

        _tool = new GetAccountStatusTool(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WhenAccountsExist_ReturnsAccountStatus()
    {
        _repositoryMock
            .Setup(x => x.GetAccountsForUserAsync(
                "dev-user",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountStatusRecord>
            {
                new("ACC-1001", "Everyday Account", "Active")
            });

        var result = await _tool.ExecuteAsync("dev-user");

        Assert.Contains("ACC-1001", result);
        Assert.Contains("Everyday Account", result);
        Assert.Contains("Active", result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNoAccountsExist_ReturnsNoRecordMessage()
    {
        _repositoryMock
            .Setup(x => x.GetAccountsForUserAsync(
                "unknown-user",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AccountStatusRecord>());

        var result = await _tool.ExecuteAsync("unknown-user");

        Assert.Equal(
            "No account status record was found for this customer.",
            result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _tool.ExecuteAsync(""));
    }
}