
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using SecureBankingAI.Infrastructure;
using Microsoft.Extensions.Configuration;
using SecureBankingAI.Mcp.Banking.Security;

var builder = Host.CreateApplicationBuilder(args);

// Locate the solution root from the MCP application's output directory.
var solutionDirectory = Path.GetFullPath(
    Path.Combine(
        AppContext.BaseDirectory,
        "../../../../"));

var apiDirectory = Path.Combine(
    solutionDirectory,
    "SecureBankingAI.Api");

// Load the existing API configuration.
var apiSettingsPath = Path.Combine(
    apiDirectory,
    "appsettings.json");

builder.Configuration.AddJsonFile(
    apiSettingsPath,
    optional: false,
    reloadOnChange: true);

// Load the API project's User Secrets.
// This allows MCP to use the same JWT signing key as the API.
builder.Configuration.AddUserSecrets<Program>(
    optional: false);

// Use the API's actual SQLite database, not a relative path.
var databasePath = Path.Combine(
    apiDirectory,
    "securebankingai.db");

builder.Configuration["ConnectionStrings:SecureBankingAi"] =
    $"Data Source={databasePath}";

// Register the existing application and infrastructure services.
builder.Services.AddInfrastructure(builder.Configuration);

// Require a JWT access token for the MCP process.
var accessToken = builder.Configuration["Mcp:AccessToken"];

if (string.IsNullOrWhiteSpace(accessToken))
{
    throw new InvalidOperationException(
        "MCP access token is missing. Configure Mcp:AccessToken.");
}

// Validate the token before starting the MCP server.
var callerIdentity = new McpCallerIdentity(
    accessToken,
    builder.Configuration);

// Register the authenticated identity for all MCP tools.
builder.Services.AddSingleton(callerIdentity);

// MCP uses stdout for protocol communication.
// Send application logs to stderr instead.
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
