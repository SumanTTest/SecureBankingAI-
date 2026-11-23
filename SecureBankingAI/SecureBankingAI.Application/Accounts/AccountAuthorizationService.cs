
namespace SecureBankingAI.Application.Accounts;

public sealed class AccountAuthorizationService
    : IAccountAuthorizationService
{
    private readonly IAccountOwnershipRepository _repository;

    public AccountAuthorizationService(
        IAccountOwnershipRepository repository)
    {
        _repository = repository;
    }

    public async Task<bool> CanAccessAccountAsync(
        string userId,
        string accountId,
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

        var ownerId = await _repository.GetOwnerIdAsync(
            accountId,
            cancellationToken);

        return !string.IsNullOrWhiteSpace(ownerId)
            && string.Equals(
                ownerId,
                userId,
                StringComparison.Ordinal);
    }
}
