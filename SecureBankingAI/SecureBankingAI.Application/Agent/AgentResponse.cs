namespace SecureBankingAI.Application.Agent;

public sealed record AgentResponse(
    string Message,
    string Intent,
    bool RequiresApproval);