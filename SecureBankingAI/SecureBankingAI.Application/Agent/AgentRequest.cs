namespace SecureBankingAI.Application.Agent;

public sealed record AgentConversationMessage(
    string Role,
    string Content);

public sealed record AgentRequest(
    string Message,
    IReadOnlyList<AgentConversationMessage>? ConversationHistory = null);