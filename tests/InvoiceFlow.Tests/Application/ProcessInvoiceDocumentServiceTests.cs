using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Application;

public sealed class ProcessInvoiceDocumentServiceTests
{
    [Fact]
    public async Task ProcessAsync_ShouldProcessDocumentAndSaveVerifiedInvoice()
    {
        var calls = new List<string>();
        var documentId = Guid.NewGuid();

        var documentStorage = new FakeDocumentStorage(documentId, calls);
        var documentExtractor = new FakeDocumentExtractor(calls);
        var invoiceMapper = new FakeInvoiceMapper(calls);
        var invoiceValidator = new FakeInvoiceValidator(
            InvoiceValidationReport.Valid(),
            calls);
        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var document = CreateDocumentInput();

        var result = await service.ProcessAsync(document);

        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(documentId, result.Invoice.SourceDocumentId);
        Assert.Equal(InvoiceStatus.Verified, result.Status);
        Assert.False(result.ValidationReport.HasIssues);

        Assert.Same(document, documentStorage.ReceivedDocument);
        Assert.Same(document, documentExtractor.ReceivedDocument);
        Assert.Same(documentExtractor.ExtractedDocument, invoiceMapper.ReceivedDocument);
        Assert.Equal(documentId, invoiceMapper.ReceivedSourceDocumentId);

        Assert.Same(invoiceMapper.MappedInvoice, invoiceValidator.ReceivedInvoice);
        Assert.Same(invoiceMapper.MappedInvoice, invoiceRepository.SavedInvoice);
        Assert.Equal(InvoiceStatus.Verified, invoiceRepository.SavedInvoice?.Status);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper",
            "Validator",
            "Repository"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnRequiresHumanReview_WhenValidationHasError()
    {
        var calls = new List<string>();
        var documentId = Guid.NewGuid();

        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal amount plus VAT amount must match total amount.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(documentId, calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(report, calls),
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.True(result.ValidationReport.RequiresHumanReview);
        Assert.Contains(result.ValidationReport.Issues, validationIssue =>
            validationIssue.Code == "TOTAL_MISMATCH");

        Assert.Same(result.Invoice, invoiceRepository.SavedInvoice);
        Assert.Equal(InvoiceStatus.RequiresHumanReview, invoiceRepository.SavedInvoice?.Status);
    }

    [Fact]
    public async Task ProcessAsync_ShouldReturnVerified_WhenValidationHasWarningsOnly()
    {
        var calls = new List<string>();
        var documentId = Guid.NewGuid();

        var issue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE",
            "VendorName",
            "Vendor name was extracted with low confidence.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(documentId, calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(report, calls),
            new FakeInvoiceRepository(calls));

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.Verified, result.Status);
        Assert.True(result.ValidationReport.HasWarnings);
        Assert.False(result.ValidationReport.HasErrors);
        Assert.False(result.ValidationReport.RequiresHumanReview);
    }

    [Fact]
    public async Task ProcessAsync_ShouldForwardCancellationTokenToAsyncDependencies()
    {
        var calls = new List<string>();
        var documentId = Guid.NewGuid();

        var documentStorage = new FakeDocumentStorage(documentId, calls);
        var documentExtractor = new FakeDocumentExtractor(calls);
        var invoiceMapper = new FakeInvoiceMapper(calls);
        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            new FakeInvoiceValidator(InvoiceValidationReport.Valid(), calls),
            invoiceRepository);

        using var cancellationTokenSource = new CancellationTokenSource();

        await service.ProcessAsync(
            CreateDocumentInput(),
            cancellationTokenSource.Token);

        Assert.Equal(cancellationTokenSource.Token, documentStorage.ReceivedCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, documentExtractor.ReceivedCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, invoiceMapper.ReceivedCancellationToken);
        Assert.Equal(cancellationTokenSource.Token, invoiceRepository.ReceivedCancellationToken);
    }

    [Fact]
    public async Task ProcessAsync_ShouldNotValidateOrSave_WhenMappedInvoiceSourceDocumentIdDoesNotMatchStoredDocumentId()
    {
        var calls = new List<string>();
        var storedDocumentId = Guid.NewGuid();
        var wrongSourceDocumentId = Guid.NewGuid();

        var wrongInvoice = CreateInvoice(wrongSourceDocumentId);

        var invoiceValidator = new FakeInvoiceValidator(
            InvoiceValidationReport.Valid(),
            calls);

        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(storedDocumentId, calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls, wrongInvoice),
            invoiceValidator,
            invoiceRepository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Null(invoiceValidator.ReceivedInvoice);
        Assert.Null(invoiceRepository.SavedInvoice);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldStopPipeline_WhenDocumentStorageFails()
    {
        var calls = new List<string>();
        var expectedException = new InvalidOperationException("Storage failed.");

        var documentExtractor = new FakeDocumentExtractor(calls);
        var invoiceMapper = new FakeInvoiceMapper(calls);
        var invoiceValidator = new FakeInvoiceValidator(
            InvoiceValidationReport.Valid(),
            calls);
        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls, expectedException),
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<DocumentStorageFailedException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Equal(
            "Document storage failed.",
            exception.Message);

        Assert.Same(expectedException, exception.InnerException);
        Assert.Null(documentExtractor.ReceivedDocument);
        Assert.Null(invoiceMapper.ReceivedDocument);
        Assert.Null(invoiceValidator.ReceivedInvoice);
        Assert.Null(invoiceRepository.SavedInvoice);

        Assert.Equal(["Storage"], calls);
    }

    [Fact]

public async Task ProcessAsync_ShouldStopPipeline_WhenDocumentExtractorFails()
{
    var calls = new List<string>();
    var documentId = Guid.NewGuid();

    var expectedException = new InvalidOperationException(
        "Extraction failed.");

    var documentStorage = new FakeDocumentStorage(documentId, calls);
    var documentExtractor = new FakeDocumentExtractor(
        calls,
        exceptionToThrow: expectedException);

    var invoiceMapper = new FakeInvoiceMapper(calls);

    var invoiceValidator = new FakeInvoiceValidator(
        InvoiceValidationReport.Valid(),
        calls);

    var invoiceRepository = new FakeInvoiceRepository(calls);

    var service = new ProcessInvoiceDocumentService(
        documentStorage,
        documentExtractor,
        invoiceMapper,
        invoiceValidator,
        invoiceRepository);

    var exception = await Assert.ThrowsAsync<DocumentExtractionFailedException>(() =>
        service.ProcessAsync(CreateDocumentInput()));

    Assert.Equal("Document extraction failed.", exception.Message);
    Assert.Same(expectedException, exception.InnerException);

    Assert.Same(documentStorage.ReceivedDocument, documentExtractor.ReceivedDocument);
    Assert.Null(invoiceMapper.ReceivedDocument);
    Assert.Null(invoiceValidator.ReceivedInvoice);
    Assert.Null(invoiceRepository.SavedInvoice);

    Assert.Equal(
    [
        "Storage",
        "Extractor"
    ], calls);
}
    [Fact]
    public async Task ProcessAsync_ShouldStopPipeline_WhenInvoiceMapperFails()
    {
        var calls = new List<string>();
        var expectedException = new InvalidOperationException("Mapping failed.");

        var invoiceValidator = new FakeInvoiceValidator(
            InvoiceValidationReport.Valid(),
            calls);
        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls, exceptionToThrow: expectedException),
            invoiceValidator,
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Same(expectedException, exception);
        Assert.Null(invoiceValidator.ReceivedInvoice);
        Assert.Null(invoiceRepository.SavedInvoice);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldStopPipeline_WhenInvoiceValidatorFails()
    {
        var calls = new List<string>();
        var expectedException = new InvalidOperationException("Validation failed.");

        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(
                InvoiceValidationReport.Valid(),
                calls,
                expectedException),
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Same(expectedException, exception);
        Assert.Null(invoiceRepository.SavedInvoice);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper",
            "Validator"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldWrapRepositoryFailure_WhenInvoiceRepositoryFails()
    {
        var calls = new List<string>();
        var expectedException = new InvalidOperationException(
            "Persistence failed.");

        var invoiceRepository = new FakeInvoiceRepository(
            calls,
            exceptionToThrow: expectedException);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(InvoiceValidationReport.Valid(), calls),
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<InvoicePersistenceFailedException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Equal("Invoice persistence failed.", exception.Message);
        Assert.Same(expectedException, exception.InnerException);
        Assert.NotNull(invoiceRepository.SavedInvoice);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper",
            "Validator",
            "Repository"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldNotWrapCancellation_WhenInvoiceRepositoryIsCanceled()
    {
        var calls = new List<string>();
        var expectedException = new OperationCanceledException(
            "Persistence was canceled.");

        var invoiceRepository = new FakeInvoiceRepository(
            calls,
            exceptionToThrow: expectedException);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(InvoiceValidationReport.Valid(), calls),
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Same(expectedException, exception);

        Assert.Equal(
        [
            "Storage",
            "Extractor",
            "Mapper",
            "Validator",
            "Repository"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), []),
            new FakeDocumentExtractor([]),
            new FakeInvoiceMapper([]),
            new FakeInvoiceValidator(InvoiceValidationReport.Valid(), []),
            new FakeInvoiceRepository([]));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ProcessAsync(null!));
    }
    [Fact]
public async Task ProcessAsync_ShouldThrowInvalidOperationException_WhenInvoiceMapperReturnsNull()
{
    var calls = new List<string>();
    var invoiceRepository = new FakeInvoiceRepository(calls);

    var service = new ProcessInvoiceDocumentService(
        new FakeDocumentStorage(Guid.NewGuid(), calls),
        new FakeDocumentExtractor(calls),
        new NullInvoiceMapper(calls),
        new FakeInvoiceValidator(InvoiceValidationReport.Valid(), calls),
        invoiceRepository);

    var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
        service.ProcessAsync(CreateDocumentInput()));

    Assert.Equal(
        "Invoice mapper returned no invoice.",
        exception.Message);

    Assert.Null(invoiceRepository.SavedInvoice);

    Assert.Equal(
    [
        "Storage",
        "Extractor",
        "Mapper"
    ], calls);
}

    [Fact]
    public async Task ProcessAsync_ShouldThrowInvalidOperationException_WhenDocumentExtractorReturnsNull()
    {
        var calls = new List<string>();
        var invoiceRepository = new FakeInvoiceRepository(calls);

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(Guid.NewGuid(), calls),
            new NullDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(InvoiceValidationReport.Valid(), calls),
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Equal(
            "Document extractor returned no extracted document.",
            exception.Message);

        Assert.Null(invoiceRepository.SavedInvoice);

        Assert.Equal(
        [
            "Storage",
            "Extractor"
        ], calls);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDocumentStorageIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentService(
                null!,
                new FakeDocumentExtractor([]),
                new FakeInvoiceMapper([]),
                new FakeInvoiceValidator(InvoiceValidationReport.Valid(), []),
                new FakeInvoiceRepository([])));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDocumentExtractorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentService(
                new FakeDocumentStorage(Guid.NewGuid(), []),
                null!,
                new FakeInvoiceMapper([]),
                new FakeInvoiceValidator(InvoiceValidationReport.Valid(), []),
                new FakeInvoiceRepository([])));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenInvoiceMapperIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentService(
                new FakeDocumentStorage(Guid.NewGuid(), []),
                new FakeDocumentExtractor([]),
                null!,
                new FakeInvoiceValidator(InvoiceValidationReport.Valid(), []),
                new FakeInvoiceRepository([])));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenInvoiceValidatorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentService(
                new FakeDocumentStorage(Guid.NewGuid(), []),
                new FakeDocumentExtractor([]),
                new FakeInvoiceMapper([]),
                null!,
                new FakeInvoiceRepository([])));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenInvoiceRepositoryIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentService(
                new FakeDocumentStorage(Guid.NewGuid(), []),
                new FakeDocumentExtractor([]),
                new FakeInvoiceMapper([]),
                new FakeInvoiceValidator(InvoiceValidationReport.Valid(), []),
                null!));
    }

    [Fact]
    public async Task ProcessAsync_ShouldThrowDocumentStorageFailedException_WhenDocumentStorageFails()
    {
        var documentStorage = new ThrowingDocumentStorage();
        var documentExtractor = new SpyDocumentExtractor();
        var invoiceMapper = new SpyInvoiceMapper();
        var invoiceValidator = new SpyInvoiceValidator();
        var invoiceRepository = new SpyInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var exception = await Assert.ThrowsAsync<DocumentStorageFailedException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Equal("Document storage failed.", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);

        Assert.False(documentExtractor.WasCalled);
        Assert.False(invoiceMapper.WasCalled);
        Assert.False(invoiceValidator.WasCalled);
        Assert.False(invoiceRepository.WasCalled);
    }

    private sealed class ThrowingDocumentStorage : IDocumentStorage
    {
        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Blob upload failed.");
        }
    }


    private sealed class SpyDocumentExtractor : IDocumentExtractor
    {
        public bool WasCalled { get; private set; }

        public Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "Document extractor should not be called when document storage fails.");
        }
    }

    private sealed class SpyInvoiceMapper : IInvoiceMapper
    {
        public bool WasCalled { get; private set; }

        public Task<Invoice> MapAsync(
            ExtractedDocument document,
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "Invoice mapper should not be called when document storage fails.");
        }
    }

    private sealed class SpyInvoiceValidator : IInvoiceValidator
    {
        public bool WasCalled { get; private set; }

        public InvoiceValidationReport Validate(Invoice invoice)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "Invoice validator should not be called when document storage fails.");
        }
    }

    private sealed class SpyInvoiceRepository : IInvoiceRepository
    {
        public bool WasCalled { get; private set; }

        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "Invoice repository should not be called when document storage fails.");
        }
    }


    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }

    private static Invoice CreateInvoice(Guid sourceDocumentId)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: sourceDocumentId,
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 4, 30),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        private readonly Guid _documentId;
        private readonly List<string> _calls;
        private readonly Exception? _exceptionToThrow;

        public DocumentInput? ReceivedDocument { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public FakeDocumentStorage(
            Guid documentId,
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            _documentId = documentId;
            _calls = calls;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Storage");
            ReceivedDocument = document;
            ReceivedCancellationToken = cancellationToken;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            return Task.FromResult(
                new StoredDocument(_documentId, document.FileName));
        }
    }

    private sealed class FakeDocumentExtractor : IDocumentExtractor
    {
        private readonly List<string> _calls;
        private readonly Exception? _exceptionToThrow;

        public DocumentInput? ReceivedDocument { get; private set; }
        public ExtractedDocument ExtractedDocument { get; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public FakeDocumentExtractor(
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            _calls = calls;
            _exceptionToThrow = exceptionToThrow;

            ExtractedDocument = new ExtractedDocument(
                "raw invoice text",
                new Dictionary<string, string>
                {
                    ["VendorName"] = "Cohen Office Supplies Ltd"
                });
        }

        public Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Extractor");
            ReceivedDocument = document;
            ReceivedCancellationToken = cancellationToken;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            cancellationToken.ThrowIfCancellationRequested();

            return Task.FromResult(ExtractedDocument);
        }
    }

    private sealed class NullDocumentExtractor : IDocumentExtractor
    {
        private readonly List<string> _calls;

        public NullDocumentExtractor(List<string> calls)
        {
            _calls = calls;
        }

        public Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Extractor");

            return Task.FromResult<ExtractedDocument>(null!);
        }
    }

    private sealed class FakeInvoiceMapper : IInvoiceMapper
    {
        private readonly List<string> _calls;
        private readonly Invoice? _invoiceToReturn;
        private readonly Exception? _exceptionToThrow;

        public ExtractedDocument? ReceivedDocument { get; private set; }
        public Guid ReceivedSourceDocumentId { get; private set; }
        public Invoice? MappedInvoice { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public FakeInvoiceMapper(
            List<string> calls,
            Invoice? invoiceToReturn = null,
            Exception? exceptionToThrow = null)
        {
            _calls = calls;
            _invoiceToReturn = invoiceToReturn;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<Invoice> MapAsync(
            ExtractedDocument document,
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Mapper");
            ReceivedDocument = document;
            ReceivedSourceDocumentId = sourceDocumentId;
            ReceivedCancellationToken = cancellationToken;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            MappedInvoice = _invoiceToReturn ?? CreateInvoice(sourceDocumentId);

            return Task.FromResult(MappedInvoice);
        }
    }
    private sealed class NullInvoiceMapper : IInvoiceMapper
{
    private readonly List<string> _calls;

    public NullInvoiceMapper(List<string> calls)
    {
        _calls = calls;
    }

    public Task<Invoice> MapAsync(
        ExtractedDocument document,
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default)
    {
        _calls.Add("Mapper");

        return Task.FromResult<Invoice>(null!);
    }
}

    private sealed class FakeInvoiceValidator : IInvoiceValidator
    {
        private readonly InvoiceValidationReport _report;
        private readonly List<string> _calls;
        private readonly Exception? _exceptionToThrow;

        public Invoice? ReceivedInvoice { get; private set; }

        public FakeInvoiceValidator(
            InvoiceValidationReport report,
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            _report = report;
            _calls = calls;
            _exceptionToThrow = exceptionToThrow;
        }

        public InvoiceValidationReport Validate(Invoice invoice)
        {
            _calls.Add("Validator");
            ReceivedInvoice = invoice;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            return _report;
        }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly List<string> _calls;
        private readonly Exception? _exceptionToThrow;

        public Invoice? SavedInvoice { get; private set; }
        public CancellationToken ReceivedCancellationToken { get; private set; }

        public FakeInvoiceRepository(
            List<string> calls,
            Exception? exceptionToThrow = null)
        {
            _calls = calls;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Repository");
            SavedInvoice = invoice;
            ReceivedCancellationToken = cancellationToken;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            return Task.CompletedTask;
        }
    }
}