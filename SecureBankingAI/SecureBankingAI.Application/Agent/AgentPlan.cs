namespace SecureBankingAI.Application.Agent;

public sealed record AgentPlan(
    string Intent,
    string ToolName,
    string Question);