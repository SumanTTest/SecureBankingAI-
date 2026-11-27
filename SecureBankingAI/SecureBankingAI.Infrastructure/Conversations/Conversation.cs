
namespace SecureBankingAI.Infrastructure.Conversations;

public sealed class Conversation
{
    public Guid Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string? Title { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ConversationMessage> Messages { get; set; }
        = new List<ConversationMessage>();
}
