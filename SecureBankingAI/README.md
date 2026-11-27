SecureBankingAI
A secure banking knowledge assistant built with C#, .NET 10, ASP.NET Core, Retrieval-Augmented Generation (RAG), semantic search, and an AI-powered banking agent.

The project demonstrates how to combine an enterprise .NET backend with local or cloud-based LLMs, document retrieval, banking tools, authentication, data ownership controls, and persistent multi-turn conversations.
Project status: Phase 3 (persistent conversational agent and API integration testing) is complete. The latest full solution test run reported 107 passed, 0 failed, 0 skipped.

Overview
SecureBankingAI is a portfolio project that explores a grounded, security-conscious AI assistant for banking information. It can ingest banking knowledge documents, retrieve relevant content through vector similarity search, answer questions using retrieved context, and route supported banking requests to application tools.

The assistant is designed to avoid inventing banking policies or account information. Retrieved documents are treated as reference material rather than trusted instructions, and answers should be grounded in the information available to the application.
Key features
Retrieval-Augmented Generation (RAG)
- Document ingestion and text chunking.
- Embedding generation through Ollama.
- Vector storage in SQLite.
- Semantic search over indexed document chunks.
- Question answering using retrieved context.
- Source information returned with search results.
- Configurable RAG similarity threshold.
Banking agent and tools
- Agent service for interpreting banking requests.
- Banking tools for account status, recent transactions, and account-closure policy retrieval.
- Account ownership checks before accessing account-specific information.
- Agent response includes intent and approval metadata.
- Provider timeout and failure handling with fallback behaviour.
Persistent conversations
- Create conversations for the authenticated user.
- Store user and assistant messages in SQLite.
- Retrieve conversation history.
- Use stored history as context for subsequent agent requests.
- Enforce conversation ownership so one user cannot access another user's conversation.
- Limit the number of messages loaded for an API history request.
Security and access control
- JWT bearer authentication.
- Issuer, audience, signing-key, and token-lifetime validation.
- Role-based authorization for administrative document operations.
- User identity taken from authenticated claims for protected user operations.
- Document ownership and account ownership checks.
- Development-only token-generation endpoints.
Testing
- Unit tests for application and agent behaviour.
- Integration tests using an isolated SQLite database.
- HTTP-level tests for conversation creation, message submission, history retrieval, ownership isolation, and unauthenticated access.
Technology stack
Area	Technology
Language	C#
Framework	.NET 10, ASP.NET Core Minimal APIs
AI / LLM	Ollama; Amazon Bedrock integration
Embeddings	Ollama (nomic-embed-text used in local development)
Chat model	Configurable Ollama model; Bedrock model configured by application settings
RAG	Custom document ingestion, chunking, semantic retrieval, and grounded answering
Persistence	SQLite
ORM / migrations	Entity Framework Core
Authentication	JWT Bearer
Testing	xUnit, ASP.NET Core integration testing
Architecture	Application / Infrastructure / API separation
Tool integration	Banking application tools; MCP banking project


Solution structure
SecureBankingAI/
├── SecureBankingAI.slnx
├── SecureBankingAI.Api/
│   └── Program.cs
├── SecureBankingAI.Application/
│   ├── Agent/
│   ├── Conversations/
│   ├── Documents/
│   ├── Embeddings/
│   ├── Ingestion/
│   ├── Rag/
│   ├── Search/
│   └── ...
├── SecureBankingAI.Infrastructure/
│   ├── Accounts/
│   ├── Agent/
│   ├── Chat/
│   ├── Conversations/
│   ├── Embeddings/
│   ├── Ingestion/
│   └── VectorStore/
├── SecureBankingAI.IntegrationTests/
├── SecureBankingAI.UnitTests/
└── SecureBankingAI.Mcp.Banking/
- API – HTTP endpoints, authentication, authorization, and request validation.
- Application – use cases, contracts, agent abstractions, and application-level services.
- Infrastructure – Ollama and Bedrock integrations, SQLite persistence, vector storage, repositories, and tool implementations.
- UnitTests – focused tests for application and agent behaviour.
- IntegrationTests – persistence and HTTP pipeline tests.
- Mcp.Banking – banking MCP project.
Prerequisites
- .NET 10 SDK.
- Git.
- Ollama for the local development provider.
- The models configured by your application settings. The current local setup has used:
  - nomic-embed-text for embeddings.
  - qwen2.5:3b for chat.
Amazon Bedrock support is also present. It requires valid AWS credentials and access to the configured model in the AWS region used by the application. Local Ollama is the low-cost development path.
Getting started
1. Clone the repository
git clone https://github.com/SumanTTest/SecureBankingAI-.git
cd SecureBankingAI-/SecureBankingAI
2. Restore and build
dotnet restore .\SecureBankingAI.slnx
dotnet build .\SecureBankingAI.slnx
3. Install and start Ollama
Install Ollama from ollama.com, then pull the models used by the local setup:
ollama pull nomic-embed-text
ollama pull qwen2.5:3b
Ensure the Ollama service is running and that the configured base URL is reachable from the API.
4. Configure application settings
The API requires JWT configuration and the Infrastructure project requires a SQLite connection string. AI provider, Ollama, Bedrock, and RAG options are bound to configuration sections by the application.

Use User Secrets for local development rather than committing signing keys or credentials:
dotnet user-secrets init --project .\SecureBankingAI.Api
dotnet user-secrets set "Jwt:Issuer" "SecureBankingAI" --project .\SecureBankingAI.Api
dotnet user-secrets set "Jwt:Audience" "SecureBankingAI" --project .\SecureBankingAI.Api
dotnet user-secrets set "Jwt:SigningKey" "<use-a-long-random-secret>" --project .\SecureBankingAI.Api
dotnet user-secrets set "ConnectionStrings:SecureBankingAi" "Data Source=securebankingai.db" --project .\SecureBankingAI.Api
Configure the AI provider and its Ollama / Bedrock options using the option section names and property names defined in the Infrastructure project. For local development, select Ollama as the chat provider and configure the Ollama base URL, chat model, embedding model, and timeouts.

Do not commit User Secrets, AWS credentials, JWT signing keys, production tokens, or databases containing real or personal banking data.
5. Run the API
dotnet run --project .\SecureBankingAI.Api
During Development, the application applies EF Core migrations and seeds demo banking data. OpenAPI is also mapped in the Development environment.
API endpoints
The following endpoints are defined in the API project.
Method	Endpoint	Purpose	Access
POST	/api/embeddings/test	Generate an embedding for supplied text	No authorization requirement is currently applied in the endpoint
POST	/api/search	Search indexed document chunks	Authenticated
POST	/api/documents/ingest	Ingest a document	Admin role
DELETE	/api/documents/{id}	Delete an indexed document	Admin role
PUT	/api/documents/{id}/trust	Set a document's trust status	Admin role
POST	/api/ask	Ask a question using RAG	Authenticated
POST	/api/agent	Submit a request to the banking agent	Authenticated
POST	/api/conversations	Create a conversation	Authenticated
GET	/api/conversations/{conversationId}	Retrieve conversation history (maxMessages defaults to 20; range 1–100)	Authenticated, owner only
POST	/api/conversations/{conversationId}/messages	Send a message and receive an agent response	Authenticated, owner only


Development-only token endpoints:
- POST /api/auth/dev-admin-token
- POST /api/auth/dev-user-token
These endpoints are mapped only when the application runs in the Development environment. They are for local testing, not production authentication.
Conversation API examples
The examples below assume you have obtained a valid bearer token from your configured authentication flow.
Create a conversation
POST /api/conversations
Authorization: Bearer <token>
Content-Type: application/json

{
  "title": "Account enquiries"
}
The response includes the newly created conversation, including its ID.
Send a message
POST /api/conversations/{conversationId}/messages
Authorization: Bearer <token>
Content-Type: application/json

{
  "message": "What is the account closure process?"
}
The response contains the agent response. The user message and assistant response are persisted in the conversation history.
Retrieve conversation history
GET /api/conversations/{conversationId}?maxMessages=20
Authorization: Bearer <token>
Only the authenticated owner can retrieve or add messages to that conversation. A conversation that does not exist or does not belong to the caller is returned as not found.
Database and migrations
The application uses Entity Framework Core with SQLite. Database migrations are applied automatically at startup outside the Testing environment. Demo banking data is seeded in Development.

To inspect migrations, run from the solution directory:
dotnet ef migrations list --project .\SecureBankingAI.Infrastructure --startup-project .\SecureBankingAI.Api --context SecureBankingAiDbContext
The database is for local/demo use. Do not use real customer data in this portfolio project.
Run tests
Run the complete solution test suite:
dotnet test .\SecureBankingAI.slnx
Latest test result
The latest reported full-solution run:
Result	Count
Passed	107
Failed	0
Skipped	0


Build succeeded. The conversation API integration tests cover authenticated conversation creation, sending a message and receiving an agent response, persistence and retrieval of user and assistant messages, access isolation between users, and rejection of unauthenticated requests.

The conversation API tests use a test authentication handler and a fake agent service. They validate the HTTP pipeline, authorization behaviour, and persistence; they do not exercise a live LLM provider or validate JWT signatures end-to-end.
Design principles demonstrated
- Separation of concerns across API, Application, and Infrastructure.
- Dependency injection and interface-driven application services.
- Retrieval-grounded responses rather than unsupported banking claims.
- Treating retrieved document content as untrusted input.
- Authentication, role authorization, and ownership enforcement.
- Persistent conversation history for multi-turn interactions.
- Automated unit and integration testing.
Current scope and limitations
This is an actively developed portfolio/demo project, not a production banking system. It does not connect to a real bank or process real financial transactions.
- Banking records and policies are demonstration data.
- The local Ollama provider is suitable for development and experimentation.
- Bedrock availability depends on AWS account access, model enablement, and quotas.
- The current embedding test endpoint does not have an authorization requirement in its route definition; review and restrict it before deploying beyond a trusted development environment.
- The development token endpoints must remain unavailable outside Development.
- Production deployment would require additional security, operational, privacy, audit, monitoring, and resilience work.
Roadmap
- Strengthen agent intent-to-tool enforcement and tool output DTO boundaries.
- Improve token-aware conversation context management.
- Add more endpoint-level tests for RAG, agent, and administrative document operations.
- Add agent evaluation datasets and repeatable quality/safety tests.
- Add observability for retrieval, tool execution, model latency, and failures.
- Expand MCP tool integration and document the supported MCP transport/configuration.
- Add a frontend for interactive conversation workflows.