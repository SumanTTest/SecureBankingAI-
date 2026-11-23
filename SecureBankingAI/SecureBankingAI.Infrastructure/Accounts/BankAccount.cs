
namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class BankAccount
{
    public string AccountId { get; set; } = string.Empty;

    public string OwnerId { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string AccountType { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public string Currency { get; set; } = "AUD";

    public ICollection<BankTransaction> Transactions { get; set; }
        = new List<BankTransaction>();
}
