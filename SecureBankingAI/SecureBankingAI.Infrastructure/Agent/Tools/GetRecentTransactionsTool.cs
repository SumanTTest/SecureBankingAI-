using System.Text.Json;
using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Application.Agent.Tools;

namespace SecureBankingAI.Infrastructure.Agent.Tools;

public sealed class GetRecentTransactionsTool
    : IGetRecentTransactionsTool
{
    private readonly IAccountAuthorizationService _authorizationService;
    private readonly IAccountTransactionRepository _transactionRepository;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public GetRecentTransactionsTool(
        IAccountAuthorizationService authorizationService,
        IAccountTransactionRepository transactionRepository)
    {
        _authorizationService = authorizationService;
        _transactionRepository = transactionRepository;
    }

    public async Task<string> ExecuteAsync(
        string userId,
        string accountId,
        int maxResults = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(accountId))
        {
            throw new ArgumentException(
                "Account ID is required.",
                nameof(accountId));
        }

        if (maxResults is < 1 or > 10)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxResults),
                "Maximum results must be between 1 and 10.");
        }

        var canAccess = await _authorizationService.CanAccessAccountAsync(
            userId,
            accountId,
            cancellationToken);

        if (!canAccess)
        {
            return "Account not found or access denied.";
        }

        var transactions =
            await _transactionRepository.GetRecentTransactionsAsync(
                accountId,
                maxResults,
                cancellationToken);

        if (transactions.Count == 0)
        {
            return "No transactions were found for this account.";
        }

        return JsonSerializer.Serialize(
            transactions,
            JsonOptions);
    }
}