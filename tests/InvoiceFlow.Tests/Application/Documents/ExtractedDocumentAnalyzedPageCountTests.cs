using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Tests.Application.Documents;

public sealed class ExtractedDocumentAnalyzedPageCountTests
{
    [Fact]
    public void Constructor_ShouldSetAnalyzedPageCount_WhenProvided()
    {
        var extractedDocument = new ExtractedDocument(
            rawText: " extracted invoice text ",
            fields: new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd"
            },
            analyzedPageCount: 3);

        Assert.Equal(3, extractedDocument.AnalyzedPageCount);
        Assert.Equal("extracted invoice text", extractedDocument.RawText);
        Assert.Equal("Cohen Office Supplies Ltd", extractedDocument.Fields["VendorName"]);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingAnalyzedPageCount()
    {
        var extractedDocument = new ExtractedDocument(
            rawText: "extracted invoice text",
            fields: new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd"
            });

        Assert.Null(extractedDocument.AnalyzedPageCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenAnalyzedPageCountIsNotPositive(
        int analyzedPageCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ExtractedDocument(
                rawText: "extracted invoice text",
                fields: null,
                analyzedPageCount: analyzedPageCount));

        Assert.Contains(
            "Analyzed page count must be greater than zero when provided.",
            exception.Message);
    }
}
