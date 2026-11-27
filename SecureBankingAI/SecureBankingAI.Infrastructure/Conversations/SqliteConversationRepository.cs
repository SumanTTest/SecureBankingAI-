
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Conversations;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.Infrastructure.Conversations;

public sealed class SqliteConversationRepository
    : IConversationRepository
{
    private readonly SecureBankingAiDbContext _dbContext;

    public SqliteConversationRepository(
        SecureBankingAiDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ConversationRecord> CreateAsync(
        string userId,
        string? title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (userId.Length > 200)
        {
            throw new ArgumentException(
                "User ID cannot exceed 200 characters.",
                nameof(userId));
        }

        if (title?.Length > 200)
        {
            throw new ArgumentException(
                "Conversation title cannot exceed 200 characters.",
                nameof(title));
        }

        var now = DateTime.UtcNow;

        var conversation = new Conversation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _dbContext.Conversations.Add(conversation);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ConversationRecord(
            conversation.Id,
            conversation.Title,
            conversation.CreatedAtUtc,
            conversation.UpdatedAtUtc);
    }

    public async Task<ConversationHistoryRecord?> GetHistoryAsync(
        string userId,
        Guid conversationId,
        int maxMessages,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation ID is required.",
                nameof(conversationId));
        }

        if (maxMessages is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxMessages),
                "Maximum messages must be between 1 and 100.");
        }

        var conversation = await _dbContext.Conversations
            .AsNoTracking()
            .Where(item =>
                item.Id == conversationId &&
                item.UserId == userId)
            .Select(item => new ConversationRecord(
                item.Id,
                item.Title,
                item.CreatedAtUtc,
                item.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);

        if (conversation is null)
        {
            return null;
        }

        var recentMessages = await _dbContext.ConversationMessages
            .AsNoTracking()
            .Where(message => message.ConversationId == conversationId)
            .OrderByDescending(message => message.CreatedAtUtc)
            .ThenByDescending(message => message.Id)
            .Take(maxMessages)
            .Select(message => new ConversationMessageRecord(
                message.Id,
                message.Role,
                message.Content,
                message.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        recentMessages.Reverse();

        return new ConversationHistoryRecord(
            conversation,
            recentMessages);
    }

    public async Task<bool> AddMessageAsync(
        string userId,
        Guid conversationId,
        string role,
        string content,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException(
                "User ID is required.",
                nameof(userId));
        }

        if (conversationId == Guid.Empty)
        {
            throw new ArgumentException(
                "Conversation ID is required.",
                nameof(conversationId));
        }

        if (role is not ("user" or "assistant"))
        {
            throw new ArgumentException(
                "Message role must be 'user' or 'assistant'.",
                nameof(role));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Message content is required.",
                nameof(content));
        }

        await using var transaction =
        await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var updatedRows = await _dbContext.Conversations
            .Where(item =>
                item.Id == conversationId &&
                item.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    item => item.UpdatedAtUtc,
                    now),
                cancellationToken);

        if (updatedRows == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        var message = new ConversationMessage
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            Role = role,
            Content = content,
            CreatedAtUtc = now
        };

        _dbContext.ConversationMessages.Add(message);

        await _dbContext.SaveChangesAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return true;    
    }
}
