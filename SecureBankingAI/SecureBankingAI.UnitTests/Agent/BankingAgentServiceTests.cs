using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SecureBankingAI.Application.Agent;
using SecureBankingAI.Application.Agent.Tools;
using SecureBankingAI.Application.Chat;
using SecureBankingAI.Infrastructure.Agent;

namespace SecureBankingAI.UnitTests.Agent;

public sealed class BankingAgentServiceTests
{
    private readonly Mock<IChatCompletionService> _chatMock = new();
    private readonly Mock<IGetClosurePolicyTool> _closureMock = new();
    private readonly Mock<IGetAccountStatusTool> _accountStatusMock = new();
    private readonly Mock<IGetRecentTransactionsTool> _transactionsMock = new();

    private BankingAgentService CreateService()
    {
        return new BankingAgentService(
            _chatMock.Object,
            _closureMock.Object,
            _accountStatusMock.Object,
            _transactionsMock.Object,
            NullLogger<BankingAgentService>.Instance);
    }

    [Fact]
    public async Task ProcessAsync_WhenTransactionEnquirySelected_ExecutesTransactionTool()
    {
        _chatMock
            .SetupSequence(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                    "intent": "TransactionEnquiry",
                    "toolName": "GetRecentTransactions",
                    "question": "Show recent transactions",
                    "accountId": "ACC-1001"
                }
                """)
            .ReturnsAsync("Here are your recent transactions.");

        _transactionsMock
            .Setup(x => x.ExecuteAsync(
                "dev-user",
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Transaction TXN-001: Supermarket - AUD 85.50");

        var service = CreateService();

        var result = await service.ProcessAsync(
            new AgentRequest("Show my recent transactions"),
            "dev-user");

        Assert.Equal(
            "Here are your recent transactions.",
            result.Message);

        Assert.Equal("TransactionEnquiry", result.Intent);

        _transactionsMock.Verify(
            x => x.ExecuteAsync(
                "dev-user",
                "ACC-1001",
                5,
                It.IsAny<CancellationToken>()),
            Times.Once);

        _accountStatusMock.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessAsync_WhenTransactionPlanHasNoAccountId_RejectsPlan()
    {
        _chatMock
            .Setup(x => x.CompleteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("""
                {
                    "intent": "TransactionEnquiry",
                    "toolName": "GetRecentTransactions",
                    "question": "Show recent transactions"
                }
                """);

        var service = CreateService();

        var result = await service.ProcessAsync(
            new AgentRequest("Show my recent transactions"),
            "dev-user");

        Assert.Equal("Unknown", result.Intent);

        _transactionsMock.Verify(
            x => x.ExecuteAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
    [Fact]
public async Task ProcessAsync_WhenIntentDoesNotMatchTool_RejectsPlan()
{
    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("""
            {
                "intent": "BankingPolicy",
                "toolName": "GetRecentTransactions",
                "question": "Explain account closure",
                "accountId": "ACC-1001"
            }
            """);

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest("Explain account closure"),
        "dev-user");

    Assert.Equal("Unknown", result.Intent);

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}
[Fact]
public async Task ProcessAsync_WhenPlanningTimesOut_ReturnsFallbackResponse()
{
    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(
            new OperationCanceledException("Simulated timeout"));

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest("Show my recent transactions"),
        "dev-user");

    Assert.Equal("Unknown", result.Intent);

    Assert.Equal(
        "I could not process that request. Please try again.",
        result.Message);

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}
[Fact]
public async Task ProcessAsync_WhenFinalResponseTimesOut_ReturnsFallbackResponse()
{
    _chatMock
        .SetupSequence(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("""
            {
                "intent": "TransactionEnquiry",
                "toolName": "GetRecentTransactions",
                "question": "Show recent transactions",
                "accountId": "ACC-1001"
            }
            """)
        .ThrowsAsync(
            new OperationCanceledException("Simulated timeout"));

    _transactionsMock
        .Setup(x => x.ExecuteAsync(
            "dev-user",
            "ACC-1001",
            5,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("Transaction TXN-001: Supermarket");

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest("Show my recent transactions"),
        "dev-user");

    Assert.Equal("Unknown", result.Intent);

    Assert.Equal(
        "I could not process that request. Please try again.",
        result.Message);

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            "dev-user",
            "ACC-1001",
            5,
            It.IsAny<CancellationToken>()),
        Times.Once);
}
[Fact]
public async Task ProcessAsync_WhenPlanningProviderFails_ReturnsFallbackResponse()
{
    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(
            new InvalidOperationException("Simulated provider failure"));

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest("Show my recent transactions"),
        "dev-user");

    Assert.Equal("Unknown", result.Intent);

    Assert.Equal(
        "I could not process that request. Please try again.",
        result.Message);

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}
[Fact]
public async Task ProcessAsync_WhenFinalResponseProviderFails_ReturnsFallbackResponse()
{
    _chatMock
        .SetupSequence(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("""
            {
                "intent": "TransactionEnquiry",
                "toolName": "GetRecentTransactions",
                "question": "Show recent transactions",
                "accountId": "ACC-1001"
            }
            """)
        .ThrowsAsync(
            new InvalidOperationException("Simulated provider failure"));

    _transactionsMock
        .Setup(x => x.ExecuteAsync(
            "dev-user",
            "ACC-1001",
            5,
            It.IsAny<CancellationToken>()))
        .ReturnsAsync("Transaction TXN-001: Supermarket");

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest("Show my recent transactions"),
        "dev-user");

    Assert.Equal("Unknown", result.Intent);

    Assert.Equal(
        "I could not process that request. Please try again.",
        result.Message);

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            "dev-user",
            "ACC-1001",
            5,
            It.IsAny<CancellationToken>()),
        Times.Once);
}
[Fact]
public async Task ProcessAsync_WhenCallerCancelsRequest_PropagatesCancellation()
{
    using var cancellationCts = new CancellationTokenSource();
    await cancellationCts.CancelAsync();

    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ThrowsAsync(new OperationCanceledException(
            cancellationCts.Token));

    var service = CreateService();

    await Assert.ThrowsAnyAsync<OperationCanceledException>(
        () => service.ProcessAsync(
            new AgentRequest("Show my recent transactions"),
            "dev-user",
            cancellationCts.Token));

    _transactionsMock.Verify(
        x => x.ExecuteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<int>(),
            It.IsAny<CancellationToken>()),
        Times.Never);
}
[Fact]
public async Task ProcessAsync_WhenConversationHistoryProvided_IncludesHistoryInBothPrompts()
{
    var userPrompts = new List<string>();

    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .Callback<string, string, CancellationToken>(
            (_, userPrompt, _) => userPrompts.Add(userPrompt))
        .Returns(() => Task.FromResult(
            userPrompts.Count == 1
                ? """
                  {
                      "intent": "GeneralEnquiry",
                      "toolName": "None",
                      "question": "Recent transactions"
                  }
                  """
                : "I can help you review your recent transactions."));

    var history = new List<AgentConversationMessage>
    {
        new("user", "What is my account status?"),
        new("assistant", "Your account is active.")
    };

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest(
            "What about recent transactions?",
            history),
        "dev-user");

    Assert.Equal(
        "I can help you review your recent transactions.",
        result.Message);

    Assert.Equal(2, userPrompts.Count);

    // Planning prompt receives conversation history.
    Assert.Contains("What is my account status?", userPrompts[0]);
    Assert.Contains("Your account is active.", userPrompts[0]);

    // Final response prompt also receives conversation history.
    Assert.Contains("What is my account status?", userPrompts[1]);
    Assert.Contains("Your account is active.", userPrompts[1]);
}

[Fact]
public async Task ProcessAsync_WhenConversationHistoryExceedsLimit_UsesBoundedRecentHistory()
{
    var capturedPrompts = new List<string>();

    _chatMock
        .Setup(x => x.CompleteAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()))
        .ReturnsAsync((
            string systemPrompt,
            string userPrompt,
            CancellationToken cancellationToken) =>
        {
            capturedPrompts.Add(userPrompt);

            return capturedPrompts.Count == 1
                ? """
                  {
                      "intent": "GeneralEnquiry",
                      "toolName": "None",
                      "question": "General question"
                  }
                  """
                : "Here is your answer.";
        });

    var history = new List<AgentConversationMessage>
    {
        new("user", "OLDEST-MESSAGE " + new string('a', 3985)),
        new("assistant", "SECOND-MESSAGE " + new string('b', 3985)),
        new("user", "THIRD-MESSAGE " + new string('c', 3985)),
        new("assistant", "LATEST-MESSAGE " + new string('d', 3985))
    };

    var service = CreateService();

    var result = await service.ProcessAsync(
        new AgentRequest(
            "Continue our conversation",
            history),
        "dev-user");

    Assert.Equal("Here is your answer.", result.Message);
    Assert.Equal(2, capturedPrompts.Count);

    // Both planning and final prompts must retain recent context.
    foreach (var prompt in capturedPrompts)
    {
        Assert.Contains("LATEST-MESSAGE", prompt);
        Assert.Contains("THIRD-MESSAGE", prompt);

        // The oldest message is outside the retained context.
        Assert.DoesNotContain("OLDEST-MESSAGE", prompt);
    }

    // Retained messages must remain in chronological order.
    Assert.True(
        capturedPrompts[0].IndexOf("THIRD-MESSAGE", StringComparison.Ordinal)
        <
        capturedPrompts[0].IndexOf("LATEST-MESSAGE", StringComparison.Ordinal));
}

}