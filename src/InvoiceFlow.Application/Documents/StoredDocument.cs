namespace InvoiceFlow.Application.Documents;

public sealed record StoredDocument
{
    public Guid Id { get; }
    public string FileName { get; }

    public StoredDocument(Guid id, string fileName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Stored document id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("Stored document file name is required.", nameof(fileName));
        }

        Id = id;
        FileName = fileName.Trim();
    }
}