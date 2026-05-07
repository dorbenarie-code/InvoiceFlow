using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Tests.Application;

public sealed class DocumentInputTests
{
    [Fact]
    public void Constructor_ShouldCreateDocumentInput()
    {
        var document = new DocumentInput(
            " invoice.pdf ",
            " application/pdf ",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(new byte[] { 1, 2, 3 })),
            contentLength: 3);

        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(3, document.ContentLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenFileNameIsMissing(string? fileName)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentInput(
                fileName!,
                "application/pdf",
                _ => ValueTask.FromResult<Stream>(
                    new MemoryStream(new byte[] { 1 }))));

        Assert.Equal("fileName", exception.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenContentTypeIsMissing(string? contentType)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new DocumentInput(
                "invoice.pdf",
                contentType!,
                _ => ValueTask.FromResult<Stream>(
                    new MemoryStream(new byte[] { 1 }))));

        Assert.Equal("contentType", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOpenReadStreamIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DocumentInput(
                "invoice.pdf",
                "application/pdf",
                openReadStream: null!));

        Assert.Equal("openReadStream", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentLengthIsNegative()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new DocumentInput(
                "invoice.pdf",
                "application/pdf",
                _ => ValueTask.FromResult<Stream>(
                    new MemoryStream(new byte[] { 1 })),
                contentLength: -1));

        Assert.Equal("contentLength", exception.ParamName);
    }

    [Fact]
    public async Task OpenReadStreamAsync_ShouldReturnReadableStream()
    {
        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(new byte[] { 1, 2, 3 })),
            contentLength: 3);

        await using var stream = await document.OpenReadStreamAsync();

        Assert.True(stream.CanRead);

        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        Assert.Equal(
            new byte[] { 1, 2, 3 },
            memoryStream.ToArray());
    }

    [Fact]
    public async Task OpenReadStreamAsync_ShouldReturnIndependentStreams_WhenOpenedMoreThanOnce()
    {
        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(new byte[] { 1, 2, 3 })),
            contentLength: 3);

        await using var firstStream = await document.OpenReadStreamAsync();
        await using var secondStream = await document.OpenReadStreamAsync();

        Assert.NotSame(firstStream, secondStream);

        Assert.Equal(
            new byte[] { 1, 2, 3 },
            await ReadAllBytesAsync(firstStream));

        Assert.Equal(
            new byte[] { 1, 2, 3 },
            await ReadAllBytesAsync(secondStream));
    }

    [Fact]
    public async Task OpenReadStreamAsync_ShouldThrow_WhenFactoryReturnsNull()
    {
        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(null!));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await document.OpenReadStreamAsync());

        Assert.Equal(
            "Document input stream factory returned no stream.",
            exception.Message);
    }

    [Fact]
    public async Task OpenReadStreamAsync_ShouldPassCancellationTokenToFactory()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var expectedToken = cancellationTokenSource.Token;

        CancellationToken receivedToken = default;

        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            cancellationToken =>
            {
                receivedToken = cancellationToken;

                return ValueTask.FromResult<Stream>(
                    new MemoryStream(new byte[] { 1 }));
            });

        await using var stream = await document.OpenReadStreamAsync(expectedToken);

        Assert.Equal(expectedToken, receivedToken);
    }

    private static async Task<byte[]> ReadAllBytesAsync(Stream stream)
    {
        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}
