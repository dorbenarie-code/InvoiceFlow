using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

internal sealed class NotImplementedAzureDocumentIntelligenceClient
    : IAzureDocumentIntelligenceClient
{
    private const string NotImplementedMessage =
        "Azure Document Intelligence extraction is not implemented yet.";

    public Task<ExtractedDocument> AnalyzeAsync(
        AzureDocumentIntelligenceAnalyzeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        throw new NotSupportedException(NotImplementedMessage);
    }
}
