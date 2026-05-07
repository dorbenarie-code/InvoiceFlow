using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceDocumentExtractorTests
{
    private const string NotImplementedMessage =
        "Azure Document Intelligence extraction is not implemented yet.";

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureDocumentIntelligenceDocumentExtractor(null!));

        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var extractor = CreateExtractor();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            extractor.ExtractAsync(null!));

        Assert.Equal("document", exception.ParamName);
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenIsAlreadyCanceled()
    {
        var extractor = CreateExtractor();
        var document = CreateDocument();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            extractor.ExtractAsync(
                document,
                cancellationTokenSource.Token));
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrowClearNotSupportedException_WhenExtractionIsNotImplementedYet()
    {
        var extractor = CreateExtractor();
        var document = CreateDocument();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            extractor.ExtractAsync(document));

        Assert.Equal(NotImplementedMessage, exception.Message);
    }

    private static AzureDocumentIntelligenceDocumentExtractor CreateExtractor()
    {
        var options = Options.Create(
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = "https://example.cognitiveservices.azure.com/",
                ApiKey = "test-api-key"
            });

        return new AzureDocumentIntelligenceDocumentExtractor(options);
    }

    private static DocumentInput CreateDocument()
{
    return new DocumentInput(
        "invoice.pdf",
        "application/pdf",
        CreatePdfBytes());
}

private static byte[] CreatePdfBytes()
{
    return new byte[]
    {
        0x25, 0x50, 0x44, 0x46, 0x2D,
        0x31, 0x2E, 0x37,
        0x0A
    };
}
}
