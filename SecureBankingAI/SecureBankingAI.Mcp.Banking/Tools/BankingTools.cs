
using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using SecureBankingAI.Application.Agent.Tools;
using SecureBankingAI.Mcp.Banking.Security;
using SecureBankingAI.Application.Accounts;

namespace SecureBankingAI.Mcp.Banking.Tools;

[McpServerToolType]
public static class BankingTools
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };
    
    [McpServerTool(Name = "get_account_status")]
    [Description("Retrieves account status for the authenticated user. Read-only operation.")]
    public static async Task<string> GetAccountStatus(
        IGetAccountStatusTool accountStatusTool,
        McpCallerIdentity callerIdentity,
        CancellationToken cancellationToken)
    {
        return await accountStatusTool.ExecuteAsync(
            callerIdentity.UserId,
            cancellationToken);
    }

    
[McpServerTool(Name = "get_recent_transactions")]
[Description("Retrieves recent transactions for an account owned by the authenticated user. Read-only operation.")]
public static async Task<string> GetRecentTransactions(
    [Description("The account identifier, for example ACC-1001.")]
    string accountId,
    IAccountAuthorizationService authorizationService,
    IAccountTransactionRepository transactionRepository,
    McpCallerIdentity callerIdentity,
    [Description("Maximum number of transactions to return. Must be between 1 and 10.")]
    int maxResults = 5,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(accountId))
    {
        return "Account identifier is required.";
    }

    if (maxResults is < 1 or > 10)
    {
        return "maxResults must be between 1 and 10.";
    }

    var canAccess = await authorizationService.CanAccessAccountAsync(
        callerIdentity.UserId,
        accountId,
        cancellationToken);

    if (!canAccess)
    {
        return "Account not found or access denied.";
    }

    var results = await transactionRepository.GetRecentTransactionsAsync(
    accountId,
    maxResults,
    cancellationToken);

    return JsonSerializer.Serialize(results, JsonOptions);
}
}
