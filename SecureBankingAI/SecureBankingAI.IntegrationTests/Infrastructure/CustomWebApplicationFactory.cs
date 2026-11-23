
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SecureBankingAI.Infrastructure.VectorStore;
using Microsoft.AspNetCore.Authentication;

namespace SecureBankingAI.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection;

    public CustomWebApplicationFactory()
    {
        _connection = new Microsoft.Data.Sqlite.SqliteConnection(
            "Data Source=:memory:");

        _connection.Open();
    }
    public async Task InitializeDatabaseAsync()
{
    using var scope = Services.CreateScope();

    var dbContext = scope.ServiceProvider
        .GetRequiredService<SecureBankingAiDbContext>();

    await dbContext.Database.EnsureCreatedAsync();
}

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting(
    "Jwt:SigningKey",
    "IntegrationTestsOnly_SigningKey_12345678901234567890");
        builder.ConfigureServices(services =>
    {
            // Register test-only authentication.
            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultAuthenticateScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultChallengeScheme =
                        TestAuthenticationHandler.SchemeName;

                    options.DefaultForbidScheme =
                        TestAuthenticationHandler.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    _ => { });

            // Replace the database with the in-memory SQLite connection.
            services.RemoveAll<DbContextOptions<SecureBankingAiDbContext>>();

            services.AddDbContext<SecureBankingAiDbContext>(options =>
                options.UseSqlite(_connection));
    });
       
   }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
