using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class FakeDocumentExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ShouldReturnDefaultExtractedDocument()
    {
        var extractor = new FakeDocumentExtractor();

        var result = await extractor.ExtractAsync(CreateDocumentInput());

        Assert.Equal("Fake extracted invoice text", result.RawText);
        Assert.Equal("Cohen Office Supplies Ltd", result.Fields["VendorName"]);
        Assert.Equal("INV-1001", result.Fields["InvoiceNumber"]);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnConfiguredExtractedDocument()
    {
        var expected = new ExtractedDocument(
            "custom text",
            new Dictionary<string, string>
            {
                ["InvoiceNumber"] = "CUSTOM-1"
            });

        var extractor = new FakeDocumentExtractor(expected);

        var result = await extractor.ExtractAsync(CreateDocumentInput());

        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var extractor = new FakeDocumentExtractor();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            extractor.ExtractAsync(null!));
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }
}