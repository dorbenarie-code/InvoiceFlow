namespace InvoiceFlow.Application.Documents;

public sealed record DocumentInput
{
    private readonly Func<CancellationToken, ValueTask<Stream>> _openReadStream;

    public string FileName { get; }

    public string ContentType { get; }

    public long? ContentLength { get; }

    public DocumentInput(
        string fileName,
        string contentType,
        ReadOnlyMemory<byte> content)
        : this(
            fileName,
            contentType,
            CreateMemoryStreamFactory(content),
            content.Length)
    {
    }

    public DocumentInput(
        string fileName,
        string contentType,
        Func<CancellationToken, ValueTask<Stream>> openReadStream,
        long? contentLength = null)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        ArgumentNullException.ThrowIfNull(openReadStream);

        if (contentLength < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(contentLength),
                contentLength,
                "Document content length cannot be negative.");
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        ContentLength = contentLength;
        _openReadStream = openReadStream;
    }

    public async ValueTask<Stream> OpenReadStreamAsync(
        CancellationToken cancellationToken = default)
    {
        var stream = await _openReadStream(cancellationToken);

        if (stream is null)
        {
            throw new InvalidOperationException(
                "Document input stream factory returned no stream.");
        }

        if (!stream.CanRead)
        {
            await stream.DisposeAsync();

            throw new InvalidOperationException(
                "Document input stream must be readable.");
        }

        return stream;
    }

    private static Func<CancellationToken, ValueTask<Stream>> CreateMemoryStreamFactory(
        ReadOnlyMemory<byte> content)
    {
        if (content.IsEmpty)
        {
            throw new ArgumentException(
                "Document content is required.",
                nameof(content));
        }

        var copiedContent = content.ToArray();

        return _ => ValueTask.FromResult<Stream>(
            new MemoryStream(
                copiedContent,
                writable: false));
    }
}
