
using Moq;
using SecureBankingAI.Application.Accounts;

namespace SecureBankingAI.UnitTests.Accounts;

public class AccountAuthorizationServiceTests
{
    private readonly Mock<IAccountOwnershipRepository> _repositoryMock;
    private readonly AccountAuthorizationService _service;

    public AccountAuthorizationServiceTests()
    {
        _repositoryMock = new Mock<IAccountOwnershipRepository>();

        _service = new AccountAuthorizationService(
            _repositoryMock.Object);
    }

    [Fact]
    public async Task CanAccessAccountAsync_WhenUserOwnsAccount_ReturnsTrue()
    {
        _repositoryMock
            .Setup(x => x.GetOwnerIdAsync(
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("dev-user");

        var result = await _service.CanAccessAccountAsync(
            "dev-user",
            "ACC-1001");

        Assert.True(result);
    }

    [Fact]
    public async Task CanAccessAccountAsync_WhenUserDoesNotOwnAccount_ReturnsFalse()
    {
        _repositoryMock
            .Setup(x => x.GetOwnerIdAsync(
                "ACC-2002",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("another-demo-user");

        var result = await _service.CanAccessAccountAsync(
            "dev-user",
            "ACC-2002");

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessAccountAsync_WhenAccountDoesNotExist_ReturnsFalse()
    {
        _repositoryMock
            .Setup(x => x.GetOwnerIdAsync(
                "ACC-9999",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var result = await _service.CanAccessAccountAsync(
            "dev-user",
            "ACC-9999");

        Assert.False(result);
    }

    [Fact]
    public async Task CanAccessAccountAsync_WhenUserIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CanAccessAccountAsync(
                "",
                "ACC-1001"));
    }

    [Fact]
    public async Task CanAccessAccountAsync_WhenAccountIdIsEmpty_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CanAccessAccountAsync(
                "dev-user",
                ""));
    }
}
