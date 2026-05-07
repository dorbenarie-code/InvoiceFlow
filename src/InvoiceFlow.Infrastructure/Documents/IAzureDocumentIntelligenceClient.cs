using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

internal interface IAzureDocumentIntelligenceClient
{
    Task<ExtractedDocument> AnalyzeAsync(
        AzureDocumentIntelligenceAnalyzeRequest request,
        CancellationToken cancellationToken = default);
}
