using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Application.Conversations;
using SecureBankingAI.Infrastructure.Conversations;
using SecureBankingAI.Infrastructure.VectorStore;

namespace SecureBankingAI.IntegrationTests.Conversations;

public sealed class SqliteConversationRepositoryTests
{
    [Fact]
    public async Task CreateAsync_CreatesConversationForUser()
    {
        var (connection, dbContext) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var contextScope = dbContext;

        var repository = new SqliteConversationRepository(dbContext);

        var result = await repository.CreateAsync(
            "customer-a",
            "My banking questions");

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("My banking questions", result.Title);
        Assert.True(result.CreatedAtUtc <= result.UpdatedAtUtc);

        var savedConversation = await dbContext.Conversations
            .SingleAsync();

        Assert.Equal("customer-a", savedConversation.UserId);
        Assert.Equal(result.Id, savedConversation.Id);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsMessagesInChronologicalOrder()
    {
        var (connection, dbContext) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var contextScope = dbContext;

        var repository = new SqliteConversationRepository(dbContext);

        var conversation = await repository.CreateAsync(
            "customer-a",
            "Transaction questions");

        await repository.AddMessageAsync(
            "customer-a",
            conversation.Id,
            "user",
            "Show my transactions.");

        await Task.Delay(10);

        await repository.AddMessageAsync(
            "customer-a",
            conversation.Id,
            "assistant",
            "Here are your transactions.");

        var history = await repository.GetHistoryAsync(
            "customer-a",
            conversation.Id,
            maxMessages: 10);

        Assert.NotNull(history);
        Assert.Equal(conversation.Id, history.Conversation.Id);
        Assert.Equal(2, history.Messages.Count);

        Assert.Equal("user", history.Messages[0].Role);
        Assert.Equal(
            "Show my transactions.",
            history.Messages[0].Content);

        Assert.Equal("assistant", history.Messages[1].Role);
        Assert.Equal(
            "Here are your transactions.",
            history.Messages[1].Content);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsOnlyMostRecentMessages()
    {
        var (connection, dbContext) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var contextScope = dbContext;

        var repository = new SqliteConversationRepository(dbContext);

        var conversation = await repository.CreateAsync(
            "customer-a",
            null);

        await repository.AddMessageAsync(
            "customer-a",
            conversation.Id,
            "user",
            "First message");

        await Task.Delay(10);

        await repository.AddMessageAsync(
            "customer-a",
            conversation.Id,
            "assistant",
            "Second message");

        await Task.Delay(10);

        await repository.AddMessageAsync(
            "customer-a",
            conversation.Id,
            "user",
            "Third message");

        var history = await repository.GetHistoryAsync(
            "customer-a",
            conversation.Id,
            maxMessages: 2);

        Assert.NotNull(history);
        Assert.Equal(2, history.Messages.Count);

        Assert.Equal(
            "Second message",
            history.Messages[0].Content);

        Assert.Equal(
            "Third message",
            history.Messages[1].Content);
    }

    [Fact]
    public async Task GetHistoryAsync_WhenConversationBelongsToAnotherUser_ReturnsNull()
    {
        var (connection, dbContext) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var contextScope = dbContext;

        var repository = new SqliteConversationRepository(dbContext);

        var conversation = await repository.CreateAsync(
            "customer-a",
            "Private conversation");

        var history = await repository.GetHistoryAsync(
            "customer-b",
            conversation.Id,
            maxMessages: 10);

        Assert.Null(history);
    }

    [Fact]
    public async Task AddMessageAsync_WhenConversationBelongsToAnotherUser_ReturnsFalse()
    {
        var (connection, dbContext) = await CreateDatabaseAsync();
        await using var connectionScope = connection;
        await using var contextScope = dbContext;

        var repository = new SqliteConversationRepository(dbContext);

        var conversation = await repository.CreateAsync(
            "customer-a",
            null);

        var result = await repository.AddMessageAsync(
            "customer-b",
            conversation.Id,
            "user",
            "Attempt to access another user's conversation.");

        Assert.False(result);
        Assert.Empty(await dbContext.ConversationMessages.ToListAsync());
    }

    private static async Task<(
        SqliteConnection Connection,
        SecureBankingAiDbContext DbContext)> CreateDatabaseAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<SecureBankingAiDbContext>()
                .UseSqlite(connection)
                .Options;

        var dbContext = new SecureBankingAiDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        return (connection, dbContext);
    }
}