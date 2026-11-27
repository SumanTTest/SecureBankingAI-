using SecureBankingAI.Application.Accounts;
using SecureBankingAI.Application.Agent.Tools;

namespace SecureBankingAI.Infrastructure.Agent.Tools;

public sealed class GetAccountStatusTool : IGetAccountStatusTool
{
    private readonly IAccountStatusRepository _repository;

    public GetAccountStatusTool(
        IAccountStatusRepository repository)
    {
        _repository = repository;
    }

    public async Task<string> ExecuteAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        var accounts = await _repository.GetAccountsForUserAsync(
            userId,
            cancellationToken);

        if (accounts.Count == 0)
        {
            return "No account status record was found for this customer.";
        }

        return string.Join(
            Environment.NewLine,
            accounts.Select(account =>
                $"Account {account.AccountId}: " +
                $"{account.AccountType} - {account.Status}." +
                $"Balance: {account.Currency} {account.Balance:N2}."));
    }
}