
namespace SecureBankingAI.Infrastructure.Accounts;

public sealed class BankTransaction
{
    public string TransactionId { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "AUD";

    public string Status { get; set; } = string.Empty;

    public BankAccount Account { get; set; } = null!;
}
