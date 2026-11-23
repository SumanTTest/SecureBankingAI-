namespace SecureBankingAI.Application.Agent.Tools;

public interface IGetAccountStatusTool
{
    Task<string> ExecuteAsync(
        string userId,
        CancellationToken cancellationToken = default);
}