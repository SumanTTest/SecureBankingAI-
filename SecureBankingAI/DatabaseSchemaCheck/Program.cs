
using Microsoft.Data.Sqlite;

var databasePath = Path.GetFullPath(
    @"SecureBankingAI.Api\securebankingai.db");

if (!File.Exists(databasePath))
{
    Console.WriteLine($"Database not found: {databasePath}");
    return;
}

await using var connection =
    new SqliteConnection($"Data Source={databasePath}");

await connection.OpenAsync();

using var transaction = connection.BeginTransaction();

await using var command = connection.CreateCommand();
command.Transaction = transaction;

command.CommandText = """
    UPDATE DocumentChunks
    SET IsTrusted = 1
    WHERE DocumentId IN (
        'savings-policy-001',
        'banking-security-001'
    );
    """;

var updatedRows = await command.ExecuteNonQueryAsync();

await transaction.CommitAsync();

Console.WriteLine(
    $"Trusted status updated for {updatedRows} chunk(s).");

await using var verifyCommand = connection.CreateCommand();

verifyCommand.CommandText = """
    SELECT DocumentId, IsTrusted, COUNT(*) AS ChunkCount
    FROM DocumentChunks
    GROUP BY DocumentId, IsTrusted
    ORDER BY DocumentId;
    """;

await using var reader = await verifyCommand.ExecuteReaderAsync();

Console.WriteLine("\nDocument trust status:");

while (await reader.ReadAsync())
{
    Console.WriteLine(
        $"{reader["DocumentId"]}: " +
        $"IsTrusted={reader["IsTrusted"]}, " +
        $"Chunks={reader["ChunkCount"]}");
}
