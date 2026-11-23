
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SecureBankingAI.Mcp.Banking.Security;

public sealed class McpCallerIdentity
{
    public string UserId { get; }

    public IReadOnlyCollection<string> Roles { get; }

    public ClaimsPrincipal Principal { get; }

    public McpCallerIdentity(
        string accessToken,
        IConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException(
                "MCP access token is missing.");
        }

        var issuer = configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is not configured.");

        var audience = configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is not configured.");

        var signingKey = configuration["Jwt:SigningKey"]
            ?? throw new InvalidOperationException(
                "JWT signing key is not configured.");

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(signingKey));

        var tokenHandler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = true
        };

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,

            ValidateAudience = true,
            ValidAudience = audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,

            ValidateLifetime = true,
            RequireExpirationTime = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            ValidAlgorithms =
            [
                SecurityAlgorithms.HmacSha256
            ],

            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };

        try
        {
            Principal = tokenHandler.ValidateToken(
                accessToken,
                validationParameters,
                out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                jwtToken.Header.Alg != SecurityAlgorithms.HmacSha256)
            {
                throw new SecurityTokenException(
                    "Unsupported JWT signing algorithm.");
            }
        }
        catch (Exception exception)
            when (exception is SecurityTokenException
                or ArgumentException)
        {
            throw new InvalidOperationException(
                "MCP access token validation failed.",
                exception);
        }

        var userId = Principal.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new InvalidOperationException(
                "The validated JWT does not contain a user identity.");
        }

        UserId = userId;

        Roles = Principal
            .FindAll(ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();
    }
}
