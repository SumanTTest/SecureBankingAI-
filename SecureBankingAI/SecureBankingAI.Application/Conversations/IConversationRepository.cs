
namespace SecureBankingAI.Application.Conversations;

public sealed record ConversationRecord(
    Guid Id,
    string? Title,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record ConversationMessageRecord(
    Guid Id,
    string Role,
    string Content,
    DateTime CreatedAtUtc);

public sealed record ConversationHistoryRecord(
    ConversationRecord Conversation,
    IReadOnlyList<ConversationMessageRecord> Messages);

public interface IConversationRepository
{
    Task<ConversationRecord> CreateAsync(
        string userId,
        string? title,
        CancellationToken cancellationToken = default);

    Task<ConversationHistoryRecord?> GetHistoryAsync(
        string userId,
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken = default);

    Task<bool> AddMessageAsync(
        string userId,
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default);
}
