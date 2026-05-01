using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

public sealed class InMemoryDocumentStorage : IDocumentStorage
{
    private readonly object _syncRoot = new();
    private readonly List<StoredDocument> _documents = [];

    public IReadOnlyCollection<StoredDocument> Documents
    {
        get
        {
            lock (_syncRoot)
            {
                return _documents.ToList().AsReadOnly();
            }
        }
    }

    public Task<StoredDocument> SaveAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var storedDocument = new StoredDocument(
            Guid.NewGuid(),
            document.FileName);

        lock (_syncRoot)
        {
            _documents.Add(storedDocument);
        }

        return Task.FromResult(storedDocument);
    }
}
