
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SecureBankingAI.IntegrationTests.Infrastructure;

public sealed class TestAuthenticationHandler
    : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

    public TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Allow tests to simulate an unauthenticated request.
        if (Request.Headers.TryGetValue(
                "X-Test-Unauthenticated", out var unauthenticated)
            && string.Equals(
                unauthenticated.ToString(),
                "true",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        // Default role is Admin so existing integration tests can run.
        var role = Request.Headers["X-Test-Role"].FirstOrDefault()
                   ?? "Admin";

        var userId = Request.Headers["X-Test-User"].FirstOrDefault()
                     ?? "integration-test-user";

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Role, role)
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);

        var ticket = new AuthenticationTicket(
            principal,
            SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
