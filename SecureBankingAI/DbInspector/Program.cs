
using Microsoft.Data.Sqlite;

var databasePath = Path.GetFullPath(
    Path.Combine("SecureBankingAI.Api", "securebankingai.db"));

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = databasePath,
    Mode = SqliteOpenMode.ReadOnly
}.ToString();

using var connection = new SqliteConnection(connectionString);
connection.Open();

using var command = connection.CreateCommand();
command.CommandText = """
    SELECT DocumentId, Source, COUNT(*) AS ChunkCount,
           MIN(IsTrusted) AS MinTrust, MAX(IsTrusted) AS MaxTrust
    FROM DocumentChunks
    GROUP BY DocumentId, Source
    ORDER BY DocumentId;
    """;

using var reader = command.ExecuteReader();

Console.WriteLine("=== Existing Documents ===");

while (reader.Read())
{
    Console.WriteLine(
        $"ID: {reader.GetString(0)} | Source: {reader.GetString(1)} | " +
        $"Chunks: {reader.GetInt32(2)} | " +
        $"Trust range: {reader.GetInt32(3)}-{reader.GetInt32(4)}");
}
