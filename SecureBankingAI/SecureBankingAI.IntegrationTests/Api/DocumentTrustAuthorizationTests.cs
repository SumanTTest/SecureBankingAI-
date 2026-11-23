using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SecureBankingAI.Infrastructure.VectorStore;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class DocumentTrustAuthorizationTests
    : IClassFixture<JwtWebApplicationFactory>, IAsyncLifetime
{
    private const string TestSigningKey =
        "IntegrationTestsOnly_SigningKey_12345678901234567890";

    private const string Issuer = "SecureBankingAI";
    private const string Audience = "SecureBankingAI.Api";

    private readonly JwtWebApplicationFactory _factory;
    private HttpClient _client = null!;

    public DocumentTrustAuthorizationTests(
        JwtWebApplicationFactory  factory)
    {
        _factory = factory;
    }

    public async Task InitializeAsync()
    {
        await _factory.InitializeDatabaseAsync();
        _client = _factory.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SetTrustStatus_WithoutToken_ReturnsUnauthorized()
    {       
        var response = await _client.PutAsJsonAsync(
            "/api/documents/missing-document/trust",
            new { IsTrusted = true });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task SetTrustStatus_WithUserToken_ReturnsForbidden()
    {
        var token = CreateToken(isAdmin: false);        

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PutAsJsonAsync(
            "/api/documents/missing-document/trust",
            new { IsTrusted = true });

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task SetTrustStatus_WithAdminToken_UpdatesTrustStatus()
    {
        const string documentId = "admin-trust-test-document";

        // Seed a document chunk into the test database.
        using (var scope = _factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider
                .GetRequiredService<SecureBankingAiDbContext>();

            dbContext.Set<StoredDocumentChunk>().Add(
                new StoredDocumentChunk
                {
                    Id = "admin-trust-test-chunk",
                    DocumentId = documentId,
                    ChunkIndex = 0,
                    Content = "Test banking security document.",
                    Source = "test-document.txt",
                    Embedding = [],
                    IsTrusted = false
                });

            await dbContext.SaveChangesAsync();
        }
        var token = CreateToken(isAdmin: true);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);     

        var response = await _client.PutAsJsonAsync(
            $"/api/documents/{documentId}/trust",
            new { IsTrusted = true });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify that the trust status was persisted.
        using var verificationScope = _factory.Services.CreateScope();

        var verificationDb = verificationScope.ServiceProvider
            .GetRequiredService<SecureBankingAiDbContext>();

        var storedChunk = await verificationDb
            .Set<StoredDocumentChunk>()
            .SingleAsync(chunk => chunk.DocumentId == documentId);

        Assert.True(storedChunk.IsTrusted);
    }

    private static string CreateToken(bool isAdmin)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "integration-test-user"),
            new(ClaimTypes.Name, "Integration Test User")
        };

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
        }

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(TestSigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}