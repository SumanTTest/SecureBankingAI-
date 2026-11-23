
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Infrastructure.Accounts;

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

    }
}
