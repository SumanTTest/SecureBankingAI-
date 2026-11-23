
using System.ComponentModel;
using ModelContextProtocol.Server;
using SecureBankingAI.Application.Agent.Tools;
using SecureBankingAI.Mcp.Banking.Security;

namespace SecureBankingAI.Mcp.Banking.Tools;

[McpServerToolType]
public sealed class ClosurePolicyTools
{
    private readonly IGetClosurePolicyTool _closurePolicyTool;
    private readonly McpCallerIdentity _callerIdentity;

    public ClosurePolicyTools(
        IGetClosurePolicyTool closurePolicyTool,
        McpCallerIdentity callerIdentity)
    {
        _closurePolicyTool = closurePolicyTool;
        _callerIdentity = callerIdentity;
    }

    [McpServerTool(Name = "get_closure_policy")]
    [Description(
        "Retrieves banking account closure policy information " +
        "using the existing RAG knowledge base. Read-only operation.")]
    public async Task<string> GetClosurePolicy(
        [Description("The question about banking account closure policy.")]
        string question,
        CancellationToken cancellationToken)
    {
        return await _closurePolicyTool.ExecuteAsync(
            _callerIdentity.UserId,
            question,
            cancellationToken);
    }
}
