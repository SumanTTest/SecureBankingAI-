using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureBankingAI.Infrastructure.VectorStore.Migrations
{
    /// <inheritdoc />
    public partial class AddTransactionDateUtc : Migration
    {
        /// <inheritdoc />
 protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<DateTime>(
        name: "TransactionDateUtc",
        table: "BankTransactions",
        type: "TEXT",
        nullable: false,
        defaultValue: new DateTime(
            2026, 11, 24, 0, 0, 0,
            DateTimeKind.Utc));

    migrationBuilder.Sql("""
        UPDATE BankTransactions
        SET TransactionDateUtc = CASE TransactionId
            WHEN 'TXN-001' THEN '2026-11-24 09:30:00'
            WHEN 'TXN-002' THEN '2026-11-23 08:00:00'
            WHEN 'TXN-003' THEN '2026-11-22 14:15:00'
            WHEN 'TXN-004' THEN '2026-11-21 16:45:00'
            ELSE TransactionDateUtc
        END;
        """);
}

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionDateUtc",
                table: "BankTransactions");
        }
    }
}
