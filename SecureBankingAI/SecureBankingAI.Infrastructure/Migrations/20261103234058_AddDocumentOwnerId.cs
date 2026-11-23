using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecureBankingAI.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentOwnerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OwnerId",
                table: "DocumentChunks",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "system");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "DocumentChunks");
        }
    }
}
