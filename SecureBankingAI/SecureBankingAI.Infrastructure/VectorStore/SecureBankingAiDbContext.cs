
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Infrastructure.Accounts;
using SecureBankingAI.Infrastructure.Conversations;

namespace SecureBankingAI.Infrastructure.VectorStore;

/// <summary>
/// EF Core database context for the RAG vector store.
/// </summary>
public sealed class SecureBankingAiDbContext : DbContext
{
    public SecureBankingAiDbContext(
        DbContextOptions<SecureBankingAiDbContext> options)
        : base(options)
    {
    }

    public DbSet<StoredDocumentChunk> DocumentChunks =>
        Set<StoredDocumentChunk>();
    public DbSet<BankAccount> BankAccounts =>
    Set<BankAccount>();
    public DbSet<BankTransaction> BankTransactions =>
    Set<BankTransaction>();
    public DbSet<Conversation> Conversations =>
    Set<Conversation>();

public DbSet<ConversationMessage> ConversationMessages =>
    Set<ConversationMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var entity = modelBuilder.Entity<StoredDocumentChunk>();

        entity.ToTable("DocumentChunks");

        entity.HasKey(chunk => chunk.Id);

        entity.Property(chunk => chunk.Id)
            .HasMaxLength(200)
            .IsRequired();
       
        entity.Property(chunk => chunk.IsTrusted)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(chunk => chunk.DocumentId)
            .HasMaxLength(200)
            .IsRequired();
      
        entity.Property(chunk => chunk.OwnerId)
        .HasMaxLength(200)
        .IsRequired();

        entity.Property(chunk => chunk.Content)
            .IsRequired();

        entity.Property(chunk => chunk.Source)
            .HasMaxLength(1000)
            .IsRequired();

        entity.Property(chunk => chunk.Embedding)
            .IsRequired();

        entity.HasIndex(chunk => new
        {
            chunk.DocumentId,
            chunk.ChunkIndex
        }).IsUnique();
            modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.ToTable("BankAccounts");

            entity.HasKey(account => account.AccountId);

            entity.Property(account => account.AccountId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(account => account.OwnerId)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(account => account.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(account => account.AccountType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(account => account.Balance)
                .HasPrecision(18, 2);

            entity.Property(account => account.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.HasMany(account => account.Transactions)
                .WithOne(transaction => transaction.Account)
                .HasForeignKey(transaction => transaction.AccountId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(account => account.OwnerId);
        });

        modelBuilder.Entity<BankTransaction>(entity =>
        {
            entity.ToTable("BankTransactions");

            entity.HasKey(transaction => transaction.TransactionId);

            entity.Property(transaction => transaction.TransactionId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(transaction => transaction.AccountId)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(transaction => transaction.Description)
                .HasMaxLength(250)
                .IsRequired();

            entity.Property(transaction => transaction.Amount)
                .HasPrecision(18, 2);

            entity.Property(transaction => transaction.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(transaction => transaction.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(transaction => transaction.AccountId);
        });
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.ToTable("Conversations");

            entity.HasKey(conversation => conversation.Id);

            entity.Property(conversation => conversation.UserId)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(conversation => conversation.Title)
                .HasMaxLength(200);

            entity.Property(conversation => conversation.CreatedAtUtc)
                .IsRequired();

            entity.Property(conversation => conversation.UpdatedAtUtc)
                .IsRequired();

            entity.HasMany(conversation => conversation.Messages)
                .WithOne(message => message.Conversation)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(conversation => new
            {
                conversation.UserId,
                conversation.UpdatedAtUtc
            });
        });

        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.ToTable("ConversationMessages");

            entity.HasKey(message => message.Id);

            entity.Property(message => message.Role)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(message => message.Content)
                .IsRequired();

            entity.Property(message => message.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(message => new
            {
                message.ConversationId,
                message.CreatedAtUtc
            });
        });

    }
}
