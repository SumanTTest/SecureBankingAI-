namespace SecureBankingAI.Application.Agent;

public interface IAgentService
{
    Task<AgentResponse> ProcessAsync(
        AgentRequest request,
        string userId,
        CancellationToken cancellationToken = default);
}