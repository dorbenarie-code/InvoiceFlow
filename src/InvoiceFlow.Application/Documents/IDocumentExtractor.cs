namespace InvoiceFlow.Application.Documents;

public interface IDocumentExtractor
{
    Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default);
}