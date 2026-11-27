using SecureBankingAI.Infrastructure;
using Microsoft.EntityFrameworkCore;
using SecureBankingAI.Infrastructure.VectorStore;
using SecureBankingAI.Application.Search;
using SecureBankingAI.Application.Ingestion;
using SecureBankingAI.Application.Documents;
using SecureBankingAI.Application.Rag;
using SecureBankingAI.Application.VectorStore;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SecureBankingAI.Application.DocumentApproval;
using SecureBankingAI.Application.Agent;
using SecureBankingAI.Infrastructure.Accounts;
using SecureBankingAI.Application.Conversations;

var builder = WebApplication.CreateBuilder(args);
var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is not configured.");

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            RoleClaimType = ClaimTypes.Role
        };
    });
    builder.Services
    .AddOptions<RagOptions>()
    .Bind(builder.Configuration.GetSection(RagOptions.SectionName))
    .Validate(
        options => options.MinimumSimilarity is >= 0.0 and <= 1.0,
        "RAG MinimumSimilarity must be between 0.0 and 1.0.")
    .ValidateOnStart();

builder.Services.AddAuthorization();

var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapPut("/api/documents/{id}/trust", async (
    string id,
    SetDocumentTrustRequest request,
    IDocumentApprovalService approvalService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new
        {
            message = "Document ID is required."
        });
    }

    var updated = await approvalService.SetTrustStatusAsync(
        id,
        request.IsTrusted,
        cancellationToken);

    if (!updated)
    {
        return Results.NotFound(new
        {
            message = "Document was not found."
        });
    }

    return Results.Ok(new
    {
        documentId = id,
        isTrusted = request.IsTrusted
    });
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));
if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/auth/dev-admin-token", () =>
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "dev-admin"),
            new Claim(ClaimTypes.Name, "Development Admin"),
            new Claim(ClaimTypes.Role, "Admin")
        };

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtSigningKey));

        var credentials = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256);

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer: jwtIssuer,
            audience: jwtAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(
                builder.Configuration.GetValue<int>("Jwt:ExpiryMinutes", 30)),
            signingCredentials: credentials);

        var tokenString =
            new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
                .WriteToken(token);

        return Results.Ok(new
        {
            access_token = tokenString,
            token_type = "Bearer",
            expires_in = builder.Configuration.GetValue<int>(
                "Jwt:ExpiryMinutes", 30)
        });
    })
    .AllowAnonymous();
    app.MapPost("/api/auth/dev-user-token", () =>
{
    var claims = new[]
    {
        new Claim(ClaimTypes.NameIdentifier, "dev-user"),
        new Claim(ClaimTypes.Name, "Development User")
    };

    var key = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtSigningKey));

    var credentials = new SigningCredentials(
        key,
        SecurityAlgorithms.HmacSha256);

    var expiryMinutes = builder.Configuration.GetValue<int>(
        "Jwt:ExpiryMinutes", 30);

    var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
        issuer: jwtIssuer,
        audience: jwtAudience,
        claims: claims,
        expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
        signingCredentials: credentials);

    var tokenString =
        new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler()
            .WriteToken(token);

    return Results.Ok(new
    {
        access_token = tokenString,
        token_type = "Bearer",
        expires_in = expiryMinutes * 60
    });
})
.AllowAnonymous();
}



if (!app.Environment.IsEnvironment("Testing"))
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider
            .GetRequiredService<SecureBankingAiDbContext>();

        await dbContext.Database.MigrateAsync();
        if (app.Environment.IsDevelopment())
        {
            await DemoBankingDataSeeder.SeedAsync(dbContext);
        }
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");


app.MapPost("/api/embeddings/test", async (
    EmbeddingTestRequest request,
    SecureBankingAI.Application.Embeddings.IEmbeddingGenerator embeddingGenerator,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Text))
    {
        return Results.BadRequest(new
        {
            error = "Text cannot be empty."
        });
    }

    var embedding = await embeddingGenerator.GenerateEmbeddingAsync(
        request.Text,
        cancellationToken);

    return Results.Ok(new
    {
        model = "nomic-embed-text",
        dimensions = embedding.Length,
        embedding
    });
});

app.MapPost("/api/search", async (
    SearchRequest request,
    HttpContext httpContext,
    ISemanticSearchService searchService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new
        {
            error = "Query cannot be empty."
        });
    }

    if (request.TopK is < 1 or > 20)
    {
        return Results.BadRequest(new
        {
            error = "TopK must be between 1 and 20."
        });
    }
    var ownerId = httpContext.User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(ownerId))
    {
        return Results.Forbid();
    }

    var results = await searchService.SearchAsync(
        request.Query,
        ownerId,
        request.TopK,
        cancellationToken
        );        

    return Results.Ok(new
    {
        query = request.Query,
        count = results.Count,
        results = results.Select(result => new
        {
            result.Chunk.Id,
            result.Chunk.DocumentId,
            result.Chunk.Source,
            result.Chunk.ChunkIndex,
            result.Chunk.Content,
            result.Similarity
        })
    });
})
.RequireAuthorization();

app.MapPost("/api/documents/ingest", async (
    IngestDocumentRequest request,
    IDocumentIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Id) ||
    string.IsNullOrWhiteSpace(request.Source) ||
    string.IsNullOrWhiteSpace(request.OwnerId))
{
    return Results.BadRequest(new
    {
        error = "Document ID, source and owner ID are required."
    });
}

    if (string.IsNullOrWhiteSpace(request.Content))
    {
        return Results.BadRequest(new
        {
            error = "Document content cannot be empty."
        });
    }

    var document = new DocumentRecord
    {
        Id = request.Id,
        OwnerId = request.OwnerId,
        Source = request.Source,
        Content = request.Content
    };

    var chunkCount = await ingestionService.IngestAsync(
        document,
        cancellationToken);

    return Results.Ok(new
    {
        documentId = request.Id,
        source = request.Source,
        chunksIngested = chunkCount
    });
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapDelete("/api/documents/{id}", async (
    string id,
    IDocumentVectorStore vectorStore,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new
        {
            error = "Document ID is required."
        });
    }

    var deleted = await vectorStore.DeleteDocumentAsync(
        id,
        cancellationToken);

    if (!deleted)
    {
        return Results.NotFound(new
        {
            error = $"Document '{id}' was not found."
        });
    }

    return Results.NoContent();
})
.WithName("DeleteDocument")
.RequireAuthorization(policy => policy.RequireRole("Admin"));

app.MapPost("/api/ask", async (
    AskRequest request,
    HttpContext httpContext,
    IRagService ragService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new
        {
            error = "Question is required."
        });
    }

    if (request.TopK is < 1 or > 20)
    {
        return Results.BadRequest(new
        {
            error = "TopK must be between 1 and 20."
        });
    }

    var ownerId = httpContext.User.FindFirstValue(
    ClaimTypes.NameIdentifier);

if (string.IsNullOrWhiteSpace(ownerId))
{
    return Results.Forbid();
}

var result = await ragService.AskAsync(
    request.Question,
    ownerId,
    request.TopK,
    cancellationToken);

    return Results.Ok(result);
})
.WithName("AskBankingQuestion")
.RequireAuthorization();

app.MapPost("/api/conversations", async (
    CreateConversationRequest request,
    HttpContext httpContext,
    IConversationRepository conversationRepository,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Forbid();
    }

    if (request.Title?.Length > 200)
    {
        return Results.BadRequest(new
        {
            error = "Conversation title cannot exceed 200 characters."
        });
    }

    var conversation = await conversationRepository.CreateAsync(
        userId,
        request.Title,
        cancellationToken);

    return Results.Created(
        $"/api/conversations/{conversation.Id}",
        conversation);
})
.WithName("CreateConversation")
.RequireAuthorization();

app.MapGet("/api/conversations/{conversationId:guid}", async (
    Guid conversationId,
    HttpContext httpContext,
    IConversationRepository conversationRepository,
    int? maxMessages,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Forbid();
    }

    var messageLimit = maxMessages ?? 20;

    if (messageLimit is < 1 or > 100)
    {
        return Results.BadRequest(new
        {
            error = "maxMessages must be between 1 and 100."
        });
    }

    var history = await conversationRepository.GetHistoryAsync(
        userId,
        conversationId,
        messageLimit,
        cancellationToken);

    if (history is null)
    {
        return Results.NotFound();
    }

    return Results.Ok(history);
})
.WithName("GetConversationHistory")
.RequireAuthorization();

app.MapPost("/api/conversations/{conversationId:guid}/messages", async (
    Guid conversationId,
    AgentRequest request,
    HttpContext httpContext,
    IConversationRepository conversationRepository,
    IAgentService agentService,
    CancellationToken cancellationToken) =>
{
    var userId = httpContext.User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Forbid();
    }

    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new
        {
            error = "Message cannot be empty."
        });
    }

    if (request.Message.Length > 4000)
    {
        return Results.BadRequest(new
        {
            error = "Message cannot exceed 4000 characters."
        });
    }

    // Load previous messages belonging to this user.
    var history = await conversationRepository.GetHistoryAsync(
        userId,
        conversationId,
        20,
        cancellationToken);

    if (history is null)
    {
        return Results.NotFound();
    }

    // Persist the current user message.
    var userMessageSaved =
        await conversationRepository.AddMessageAsync(
            userId,
            conversationId,
            "user",
            request.Message,
            cancellationToken);

    if (!userMessageSaved)
    {
        return Results.NotFound();
    }

    // Build context using trusted database history.
    var agentRequest = new AgentRequest(
        request.Message,
        history.Messages
            .Select(message => new AgentConversationMessage(
                message.Role,
                message.Content))
            .ToList());

    // Process through the existing banking agent.
    var response = await agentService.ProcessAsync(
        agentRequest,
        userId,
        cancellationToken);

    // Persist the assistant response.
    var assistantMessageSaved =
        await conversationRepository.AddMessageAsync(
            userId,
            conversationId,
            "assistant",
            response.Message,
            cancellationToken);

    if (!assistantMessageSaved)
    {
        return Results.NotFound();
    }

    return Results.Ok(new
    {
        conversationId,
        response
    });
})
.WithName("SendConversationMessage")
.RequireAuthorization();

app.MapPost("/api/agent", async (
    AgentRequest request,
    HttpContext httpContext,
    IAgentService agentService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Message))
    {
        return Results.BadRequest(new
        {
            error = "Message cannot be empty."
        });
    }

    var userId = httpContext.User.FindFirstValue(
        ClaimTypes.NameIdentifier);

    if (string.IsNullOrWhiteSpace(userId))
    {
        return Results.Forbid();
    }

    var response = await agentService.ProcessAsync(
        request,
        userId,
        cancellationToken);

    return Results.Ok(response);
})
.WithName("ProcessBankingAgentRequest")
.RequireAuthorization();

app.Run();
public sealed record IngestDocumentRequest(
    string Id,
    string OwnerId,
    string Source,
    string Content);
public sealed record SearchRequest(
    string Query,
    int TopK = 5);

public sealed record AskRequest(string Question, int TopK = 5);
public sealed record EmbeddingTestRequest(string Text);

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
public partial class Program { }
public sealed record SetDocumentTrustRequest(bool IsTrusted);
public sealed record CreateConversationRequest(string? Title);
