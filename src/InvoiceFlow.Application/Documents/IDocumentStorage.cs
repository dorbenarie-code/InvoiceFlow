namespace InvoiceFlow.Application.Documents;

public interface IDocumentStorage
{
    Task<StoredDocument> SaveAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default);
}