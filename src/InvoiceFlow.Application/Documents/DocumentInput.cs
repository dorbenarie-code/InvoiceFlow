namespace InvoiceFlow.Application.Documents;

public sealed record DocumentInput
{
    public string FileName { get; }
    public string ContentType { get; }
    public ReadOnlyMemory<byte> Content { get; }

    public DocumentInput(
        string fileName,
        string contentType,
        ReadOnlyMemory<byte> content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (content.IsEmpty)
        {
            throw new ArgumentException("Document content is required.", nameof(content));
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        Content = content;
    }
}