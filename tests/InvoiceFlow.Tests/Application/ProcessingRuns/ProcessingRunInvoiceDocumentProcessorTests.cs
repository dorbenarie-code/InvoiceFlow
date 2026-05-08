using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Application.ProcessingRuns;

public sealed class ProcessingRunInvoiceDocumentProcessorTests
{
    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid DocumentId =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly Guid InvoiceId =
        Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static readonly DateTimeOffset StartTime =
        new(2026, 5, 7, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessAsync_ShouldSaveProcessingRun_WhenInnerProcessorSucceeds()
    {
        var timeProvider = new ManualTimeProvider(StartTime);

        var innerResult = CreateSuccessfulResult(analyzedPageCount: 3);

        var innerProcessor = new SuccessfulInvoiceDocumentProcessor(
            innerResult,
            beforeReturn: () => timeProvider.AdvanceMilliseconds(1234));

        var repository = new CapturingProcessingRunRepository();

        var processor = new ProcessingRunInvoiceDocumentProcessor(
            innerProcessor,
            repository,
            new FixedProcessingClientContext(ClientId),
            timeProvider);

        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await processor.ProcessAsync(
            CreateDocumentInput(),
            cancellationTokenSource.Token);

        Assert.Same(innerResult, result);

        var processingRun = Assert.Single(repository.ProcessingRuns);

        Assert.NotEqual(Guid.Empty, processingRun.Id);
        Assert.Equal(ClientId, processingRun.ClientId);
        Assert.Equal(DocumentId, processingRun.DocumentId);
        Assert.Equal(innerResult.InvoiceId, processingRun.InvoiceId);
        Assert.Equal("Verified", processingRun.Status);
        Assert.Equal(3, processingRun.AnalyzedPageCount);
        Assert.Equal(1234, processingRun.DurationMs);
        Assert.Null(processingRun.ErrorCode);
        Assert.Equal(StartTime.UtcDateTime, processingRun.CreatedAtUtc);

        Assert.Equal(
            cancellationTokenSource.Token,
            repository.ReceivedCancellationToken);
    }

    [Theory]
    [InlineData("DOCUMENT_STORAGE_FAILED")]
    [InlineData("DOCUMENT_EXTRACTION_FAILED")]
    [InlineData("INVOICE_PERSISTENCE_FAILED")]
    public async Task ProcessAsync_ShouldSaveFailedProcessingRunAndRethrow_WhenInnerProcessorFails(
        string expectedErrorCode)
    {
        var timeProvider = new ManualTimeProvider(StartTime);

        var exception = CreateExceptionForErrorCode(expectedErrorCode);

        var innerProcessor = new ThrowingInvoiceDocumentProcessor(
            exception,
            beforeThrow: () => timeProvider.AdvanceMilliseconds(250));

        var repository = new CapturingProcessingRunRepository();

        var processor = new ProcessingRunInvoiceDocumentProcessor(
            innerProcessor,
            repository,
            new FixedProcessingClientContext(ClientId),
            timeProvider);

        var thrownException = await Record.ExceptionAsync(() =>
            processor.ProcessAsync(CreateDocumentInput()));

        Assert.Same(exception, thrownException);

        var processingRun = Assert.Single(repository.ProcessingRuns);

        Assert.NotEqual(Guid.Empty, processingRun.Id);
        Assert.Equal(ClientId, processingRun.ClientId);
        Assert.Null(processingRun.DocumentId);
        Assert.Null(processingRun.InvoiceId);
        Assert.Equal("Failed", processingRun.Status);
        Assert.Null(processingRun.AnalyzedPageCount);
        Assert.Equal(250, processingRun.DurationMs);
        Assert.Equal(expectedErrorCode, processingRun.ErrorCode);
        Assert.Equal(StartTime.UtcDateTime, processingRun.CreatedAtUtc);
    }

    [Fact]
    public async Task ProcessAsync_ShouldPassCancellationTokenToInnerProcessor()
    {
        var timeProvider = new ManualTimeProvider(StartTime);

        var innerProcessor = new SuccessfulInvoiceDocumentProcessor(
            CreateSuccessfulResult(analyzedPageCount: 1));

        var repository = new CapturingProcessingRunRepository();

        var processor = new ProcessingRunInvoiceDocumentProcessor(
            innerProcessor,
            repository,
            new FixedProcessingClientContext(ClientId),
            timeProvider);

        using var cancellationTokenSource = new CancellationTokenSource();

        await processor.ProcessAsync(
            CreateDocumentInput(),
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            innerProcessor.ReceivedCancellationToken);
    }

    private static ProcessInvoiceDocumentResult CreateSuccessfulResult(
        int? analyzedPageCount)
    {
        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: DocumentId,
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 5, 7),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        invoice.ApplyValidationReport(InvoiceValidationReport.Valid());

        return new ProcessInvoiceDocumentResult(
            DocumentId,
            invoice,
            analyzedPageCount);
    }

    private static Exception CreateExceptionForErrorCode(
        string errorCode)
    {
        return errorCode switch
        {
            "DOCUMENT_STORAGE_FAILED" => new DocumentStorageFailedException(
                "Document storage failed.",
                new InvalidOperationException("Blob storage failed.")),

            "DOCUMENT_EXTRACTION_FAILED" => new DocumentExtractionFailedException(
                "Document extraction failed.",
                new InvalidOperationException("Azure rate limit.")),

            "INVOICE_PERSISTENCE_FAILED" => new InvoicePersistenceFailedException(
                "Invoice persistence failed.",
                new InvalidOperationException("SQL insert failed.")),

            _ => throw new ArgumentOutOfRangeException(nameof(errorCode))
        };
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

    private sealed class FixedProcessingClientContext
        : IProcessingClientContext
    {
        public Guid ClientId { get; }

        public FixedProcessingClientContext(Guid clientId)
        {
            ClientId = clientId;
        }
    }

    private sealed class SuccessfulInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        private readonly ProcessInvoiceDocumentResult _result;
        private readonly Action? _beforeReturn;

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public SuccessfulInvoiceDocumentProcessor(
            ProcessInvoiceDocumentResult result,
            Action? beforeReturn = null)
        {
            _result = result;
            _beforeReturn = beforeReturn;
        }

        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            ReceivedCancellationToken = cancellationToken;

            _beforeReturn?.Invoke();

            return Task.FromResult(_result);
        }
    }

    private sealed class ThrowingInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        private readonly Exception _exception;
        private readonly Action? _beforeThrow;

        public ThrowingInvoiceDocumentProcessor(
            Exception exception,
            Action? beforeThrow = null)
        {
            _exception = exception;
            _beforeThrow = beforeThrow;
        }

        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _beforeThrow?.Invoke();

            throw _exception;
        }
    }

    private sealed class CapturingProcessingRunRepository
        : IProcessingRunRepository
    {
        private readonly List<ProcessingRun> _processingRuns = [];

        public IReadOnlyCollection<ProcessingRun> ProcessingRuns =>
            _processingRuns.AsReadOnly();

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task SaveAsync(
            ProcessingRun processingRun,
            CancellationToken cancellationToken = default)
        {
            _processingRuns.Add(processingRun);
            ReceivedCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;
        private long _timestamp;

        public ManualTimeProvider(DateTimeOffset utcNow)
        {
            _utcNow = utcNow;
        }

        public override long TimestampFrequency => 1000;

        public override DateTimeOffset GetUtcNow()
        {
            return _utcNow;
        }

        public override long GetTimestamp()
        {
            return _timestamp;
        }

        public void AdvanceMilliseconds(long milliseconds)
        {
            _timestamp += milliseconds;
            _utcNow = _utcNow.AddMilliseconds(milliseconds);
        }
    }
}
