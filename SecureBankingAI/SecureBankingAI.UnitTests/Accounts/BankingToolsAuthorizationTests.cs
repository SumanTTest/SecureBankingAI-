
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Moq;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Mcp.Banking.Security;
using SecureBankingAI.Mcp.Banking.Tools;
using SecureBankingAI.Application.Agent.Tools;

namespace SecureBankingAI.UnitTests.Accounts;

public sealed class BankingToolsAuthorizationTests
{
    private const string Issuer = "SecureBankingAI.Tests";
    private const string Audience = "SecureBankingAI.Tests.Client";

    private const string SigningKey =
        "SecureBankingAI-Test-Signing-Key-At-Least-32-Characters";

    private static McpCallerIdentity CreateCallerIdentity(string userId)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience,
                ["Jwt:SigningKey"] = SigningKey
            })
            .Build();

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, userId)
            ],
            notBefore: DateTime.UtcNow.AddMinutes(-1),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        var accessToken = new JwtSecurityTokenHandler()
            .WriteToken(token);

        return new McpCallerIdentity(accessToken, configuration);
    }

    [Fact]
    public async Task GetRecentTransactions_WhenCallerOwnsAccount_ReturnsTransactions()
    {
        var authorizationService =
            new Mock<IAccountAuthorizationService>();

        var transactionRepository =
            new Mock<IAccountTransactionRepository>();

        var callerIdentity = CreateCallerIdentity("dev-user");

        var transactions = new List<AccountTransaction>
        {
            new(
                "TXN-001",
                "ACC-1001",
                "Supermarket",
                -85.50m,
                "AUD",
                "Completed")
        };

        authorizationService
            .Setup(x => x.CanAccessAccountAsync(
                "dev-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        transactionRepository
            .Setup(x => x.GetRecentTransactionsAsync(
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(transactions);

        var result = await BankingTools.GetRecentTransactions(
            "ACC-1001",
            authorizationService.Object,
            transactionRepository.Object,
            callerIdentity);

        Assert.Contains("TXN-001", result);
        Assert.Contains("Supermarket", result);

        authorizationService.Verify(
            x => x.CanAccessAccountAsync(
                "dev-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()),
            Times.Once);

        transactionRepository.Verify(
            x => x.GetRecentTransactionsAsync(
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetRecentTransactions_WhenCallerDoesNotOwnAccount_DeniesAccess()
    {
        var authorizationService =
            new Mock<IAccountAuthorizationService>();

        var transactionRepository =
            new Mock<IAccountTransactionRepository>();

        var callerIdentity = CreateCallerIdentity("another-demo-user");

        authorizationService
            .Setup(x => x.CanAccessAccountAsync(
                "another-demo-user",
                "ACC-1001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await BankingTools.GetRecentTransactions(
            "ACC-1001",
            authorizationService.Object,
            transactionRepository.Object,
            callerIdentity);

        Assert.Equal(
            "Account not found or access denied.",
            result);

        transactionRepository.Verify(
            x => x.GetRecentTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRecentTransactions_WhenAccountIdIsEmpty_RejectsRequest()
    {
        var authorizationService =
            new Mock<IAccountAuthorizationService>();

        var transactionRepository =
            new Mock<IAccountTransactionRepository>();

        var callerIdentity = CreateCallerIdentity("dev-user");

        var result = await BankingTools.GetRecentTransactions(
            "",
            authorizationService.Object,
            transactionRepository.Object,
            callerIdentity);

        Assert.Equal("Account identifier is required.", result);

        authorizationService.Verify(
            x => x.CanAccessAccountAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        transactionRepository.Verify(
            x => x.GetRecentTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetRecentTransactions_WhenMaxResultsExceedsLimit_RejectsRequest()
    {
        var authorizationService =
            new Mock<IAccountAuthorizationService>();

        var transactionRepository =
            new Mock<IAccountTransactionRepository>();

        var callerIdentity = CreateCallerIdentity("dev-user");

        var result = await BankingTools.GetRecentTransactions(
            "ACC-1001",
            authorizationService.Object,
            transactionRepository.Object,
            callerIdentity,
            maxResults: 11);

        Assert.Equal(
            "maxResults must be between 1 and 10.",
            result);

        authorizationService.Verify(
            x => x.CanAccessAccountAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        transactionRepository.Verify(
            x => x.GetRecentTransactionsAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    [Fact]
public async Task GetAccountStatus_UsesAuthenticatedCallerIdentity()
{
    // Arrange
    var accountStatusTool =
        new Mock<IGetAccountStatusTool>();

    var callerIdentity = CreateCallerIdentity("dev-user");

    const string expectedResult =
        "Account ACC-1001: Everyday Account - Active.";

    accountStatusTool
        .Setup(x => x.ExecuteAsync(
            "dev-user",
            It.IsAny<CancellationToken>()))
        .ReturnsAsync(expectedResult);

    // Act
    var result = await BankingTools.GetAccountStatus(
        accountStatusTool.Object,
        callerIdentity,
        CancellationToken.None);

    // Assert
    Assert.Equal(expectedResult, result);

    accountStatusTool.Verify(
        x => x.ExecuteAsync(
            "dev-user",
            It.IsAny<CancellationToken>()),
        Times.Once);
}
}
