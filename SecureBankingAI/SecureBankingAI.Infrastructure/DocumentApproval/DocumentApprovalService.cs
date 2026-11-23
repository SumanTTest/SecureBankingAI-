
using SecureBankingAI.Application.DocumentApproval;
using SecureBankingAI.Application.VectorStore;

namespace SecureBankingAI.Infrastructure.DocumentApproval;

public sealed class DocumentApprovalService : IDocumentApprovalService
{
    private readonly IDocumentVectorStore _vectorStore;

    public DocumentApprovalService(IDocumentVectorStore vectorStore)
    {
        _vectorStore = vectorStore;
    }

    public Task<bool> SetTrustStatusAsync(
        string documentId,
        bool isTrusted,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException(
                "Document ID cannot be empty.",
                nameof(documentId));
        }

        return _vectorStore.SetDocumentTrustAsync(
            documentId,
            isTrusted,
            cancellationToken);
    }
}
