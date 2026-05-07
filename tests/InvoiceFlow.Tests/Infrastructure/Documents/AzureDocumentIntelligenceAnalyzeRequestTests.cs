using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceAnalyzeRequestTests
{
    [Fact]
    public void Constructor_ShouldSetModelIdDocumentAndMinimumConfidenceThreshold()
    {
        var document = CreateDocumentInput();

        var request = new AzureDocumentIntelligenceAnalyzeRequest(
            " prebuilt-invoice ",
            document,
            0.75f);

        Assert.Equal("prebuilt-invoice", request.ModelId);
        Assert.Same(document, request.Document);
        Assert.Equal(0.75f, request.MinimumConfidenceThreshold);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenModelIdIsMissing()
    {
        var document = CreateDocumentInput();

        var exception = Assert.Throws<ArgumentException>(() =>
            new AzureDocumentIntelligenceAnalyzeRequest(
                " ",
                document,
                0.8f));

        Assert.Contains(
            "Azure Document Intelligence model id is required.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDocumentIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AzureDocumentIntelligenceAnalyzeRequest(
                "prebuilt-invoice",
                null!,
                0.8f));
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(-1f)]
    public void Constructor_ShouldThrow_WhenMinimumConfidenceThresholdIsBelowZero(
        float threshold)
    {
        var document = CreateDocumentInput();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AzureDocumentIntelligenceAnalyzeRequest(
                "prebuilt-invoice",
                document,
                threshold));

        Assert.Contains(
            "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.",
            exception.Message);
    }

    [Theory]
    [InlineData(1.01f)]
    [InlineData(2f)]
    public void Constructor_ShouldThrow_WhenMinimumConfidenceThresholdIsGreaterThanOne(
        float threshold)
    {
        var document = CreateDocumentInput();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new AzureDocumentIntelligenceAnalyzeRequest(
                "prebuilt-invoice",
                document,
                threshold));

        Assert.Contains(
            "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.",
            exception.Message);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(0.8f)]
    [InlineData(1f)]
    public void Constructor_ShouldAcceptMinimumConfidenceThreshold_WhenValueIsBetweenZeroAndOne(
        float threshold)
    {
        var document = CreateDocumentInput();

        var request = new AzureDocumentIntelligenceAnalyzeRequest(
            "prebuilt-invoice",
            document,
            threshold);

        Assert.Equal(threshold, request.MinimumConfidenceThreshold);
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D
            });
    }
}
