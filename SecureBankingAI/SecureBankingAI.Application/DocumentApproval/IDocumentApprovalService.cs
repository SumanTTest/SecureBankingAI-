
namespace SecureBankingAI.Application.DocumentApproval;

public interface IDocumentApprovalService
{
    Task<bool> SetTrustStatusAsync(
        string documentId,
        bool isTrusted,
        CancellationToken cancellationToken = default);
}
