namespace SecureBankingAI.Application.Agent.Tools;

public interface IGetClosurePolicyTool
{
    Task<string> ExecuteAsync(
        string userId,
        string question,
        CancellationToken cancellationToken = default);
}