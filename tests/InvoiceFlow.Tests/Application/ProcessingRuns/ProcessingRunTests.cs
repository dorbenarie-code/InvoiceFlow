using InvoiceFlow.Application.ProcessingRuns;

namespace InvoiceFlow.Tests.Application.ProcessingRuns;

public sealed class ProcessingRunTests
{
    private static readonly Guid RunId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly Guid ClientId =
        Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Guid DocumentId =
        Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static readonly Guid InvoiceId =
        Guid.Parse("44444444-4444-4444-4444-444444444444");

    private static readonly DateTime CreatedAtUtc =
        new(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Constructor_ShouldSetProperties_WhenValuesAreValid()
    {
        var processingRun = new ProcessingRun(
            id: RunId,
            clientId: ClientId,
            documentId: DocumentId,
            invoiceId: InvoiceId,
            status: " Verified ",
            analyzedPageCount: 3,
            durationMs: 9985,
            errorCode: " TOTAL_MISMATCH ",
            createdAtUtc: CreatedAtUtc);

        Assert.Equal(RunId, processingRun.Id);
        Assert.Equal(ClientId, processingRun.ClientId);
        Assert.Equal(DocumentId, processingRun.DocumentId);
        Assert.Equal(InvoiceId, processingRun.InvoiceId);
        Assert.Equal("Verified", processingRun.Status);
        Assert.Equal(3, processingRun.AnalyzedPageCount);
        Assert.Equal(9985, processingRun.DurationMs);
        Assert.Equal("TOTAL_MISMATCH", processingRun.ErrorCode);
        Assert.Equal(CreatedAtUtc, processingRun.CreatedAtUtc);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingDocumentAndInvoiceIds_WhenRunFailedBeforePersistence()
    {
        var processingRun = new ProcessingRun(
            id: RunId,
            clientId: ClientId,
            documentId: null,
            invoiceId: null,
            status: "Failed",
            analyzedPageCount: null,
            durationMs: 120,
            errorCode: "DOCUMENT_STORAGE_FAILED",
            createdAtUtc: CreatedAtUtc);

        Assert.Null(processingRun.DocumentId);
        Assert.Null(processingRun.InvoiceId);
        Assert.Null(processingRun.AnalyzedPageCount);
        Assert.Equal("Failed", processingRun.Status);
        Assert.Equal("DOCUMENT_STORAGE_FAILED", processingRun.ErrorCode);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingAnalyzedPageCount_WhenExtractionDidNotComplete()
    {
        var processingRun = new ProcessingRun(
            id: RunId,
            clientId: ClientId,
            documentId: DocumentId,
            invoiceId: null,
            status: "Failed",
            analyzedPageCount: null,
            durationMs: 2500,
            errorCode: "DOCUMENT_EXTRACTION_FAILED",
            createdAtUtc: CreatedAtUtc);

        Assert.Null(processingRun.AnalyzedPageCount);
        Assert.Equal("DOCUMENT_EXTRACTION_FAILED", processingRun.ErrorCode);
    }

    [Fact]
    public void Constructor_ShouldNormalizeBlankErrorCodeToNull()
    {
        var processingRun = new ProcessingRun(
            id: RunId,
            clientId: ClientId,
            documentId: DocumentId,
            invoiceId: InvoiceId,
            status: "Verified",
            analyzedPageCount: 1,
            durationMs: 1000,
            errorCode: "   ",
            createdAtUtc: CreatedAtUtc);

        Assert.Null(processingRun.ErrorCode);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProcessingRun(
                id: Guid.Empty,
                clientId: ClientId,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: "Verified",
                analyzedPageCount: 1,
                durationMs: 1000,
                errorCode: null,
                createdAtUtc: CreatedAtUtc));

        Assert.Contains("Processing run id is required.", exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenClientIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProcessingRun(
                id: RunId,
                clientId: Guid.Empty,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: "Verified",
                analyzedPageCount: 1,
                durationMs: 1000,
                errorCode: null,
                createdAtUtc: CreatedAtUtc));

        Assert.Contains("Processing run client id is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_ShouldThrow_WhenStatusIsMissing(string status)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            new ProcessingRun(
                id: RunId,
                clientId: ClientId,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: status,
                analyzedPageCount: 1,
                durationMs: 1000,
                errorCode: null,
                createdAtUtc: CreatedAtUtc));

        Assert.Contains("Processing run status is required.", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenAnalyzedPageCountIsNotPositive(
        int analyzedPageCount)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProcessingRun(
                id: RunId,
                clientId: ClientId,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: "Verified",
                analyzedPageCount: analyzedPageCount,
                durationMs: 1000,
                errorCode: null,
                createdAtUtc: CreatedAtUtc));

        Assert.Contains(
            "Processing run analyzed page count must be greater than zero when provided.",
            exception.Message);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-500)]
    public void Constructor_ShouldThrow_WhenDurationMsIsNegative(long durationMs)
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProcessingRun(
                id: RunId,
                clientId: ClientId,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: "Verified",
                analyzedPageCount: 1,
                durationMs: durationMs,
                errorCode: null,
                createdAtUtc: CreatedAtUtc));

        Assert.Contains(
            "Processing run duration cannot be negative.",
            exception.Message);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_ShouldThrow_WhenCreatedAtIsNotUtc(
        DateTimeKind dateTimeKind)
    {
        var createdAt = new DateTime(
            2026,
            5,
            7,
            10,
            0,
            0,
            dateTimeKind);

        var exception = Assert.Throws<ArgumentException>(() =>
            new ProcessingRun(
                id: RunId,
                clientId: ClientId,
                documentId: DocumentId,
                invoiceId: InvoiceId,
                status: "Verified",
                analyzedPageCount: 1,
                durationMs: 1000,
                errorCode: null,
                createdAtUtc: createdAt));

        Assert.Contains(
            "Processing run creation time must be UTC.",
            exception.Message);
    }
}
