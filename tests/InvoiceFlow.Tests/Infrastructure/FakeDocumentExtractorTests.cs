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
        Assert.Equal(8, result.Fields.Count);

        AssertField(result, "VendorName", "Cohen Office Supplies Ltd");
        AssertField(result, "VendorTaxId", "516789123");
        AssertField(result, "InvoiceNumber", "INV-1001");
        AssertField(result, "IssueDate", "2026-04-30");
        AssertField(result, "SubtotalAmount", "1000");
        AssertField(result, "VatAmount", "180");
        AssertField(result, "TotalAmount", "1180");
        AssertField(result, "Currency", "ILS");
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnConfiguredExtractedDocument()
    {
        var expected = new ExtractedDocument(
            "custom text",
            new Dictionary<string, string>
            {
                ["InvoiceNumber"] = "CUSTOM-1",
                ["Currency"] = "USD"
            });

        var extractor = new FakeDocumentExtractor(expected);

        var result = await extractor.ExtractAsync(CreateDocumentInput());

        Assert.Equal(expected.RawText, result.RawText);
        Assert.Equal(expected.Fields.Count, result.Fields.Count);
        AssertField(result, "InvoiceNumber", "CUSTOM-1");
        AssertField(result, "Currency", "USD");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenExtractedDocumentIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FakeDocumentExtractor(null!));
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var extractor = new FakeDocumentExtractor();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            extractor.ExtractAsync(null!));
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrow_WhenCancellationRequested()
    {
        var extractor = new FakeDocumentExtractor();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            extractor.ExtractAsync(
                CreateDocumentInput(),
                cancellationTokenSource.Token));
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }

    private static void AssertField(
        ExtractedDocument document,
        string fieldName,
        string expectedValue)
    {
        Assert.True(
            document.Fields.TryGetValue(fieldName, out var actualValue),
            $"Expected extracted document to contain field '{fieldName}'.");

        Assert.Equal(expectedValue, actualValue);
    }
}