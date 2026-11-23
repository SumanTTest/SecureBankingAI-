
using SecureBankingAI.Application.Accounts;

namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class DemoAccountOwnershipRepository
    : IAccountOwnershipRepository
{
    private static readonly Dictionary<string, string> AccountOwners =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ACC-1001"] = "dev-user",
            ["ACC-2002"] = "another-demo-user"
        };

    public Task<string?> GetOwnerIdAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        AccountOwners.TryGetValue(accountId, out var ownerId);

        return Task.FromResult(ownerId);
    }
}
