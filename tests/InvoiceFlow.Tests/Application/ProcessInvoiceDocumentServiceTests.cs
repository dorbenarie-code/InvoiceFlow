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
        Assert.NotNull(invoiceRepository.SavedInvoice);
        Assert.Equal(InvoiceStatus.Verified, invoiceRepository.SavedInvoice.Status);

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

        var service = new ProcessInvoiceDocumentService(
            new FakeDocumentStorage(documentId, calls),
            new FakeDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(report, calls),
            new FakeInvoiceRepository(calls));

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.True(result.ValidationReport.RequiresHumanReview);
        Assert.Contains(result.ValidationReport.Issues, validationIssue =>
            validationIssue.Code == "TOTAL_MISMATCH");
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

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }

    private sealed class FakeDocumentStorage : IDocumentStorage
    {
        private readonly Guid _documentId;
        private readonly List<string> _calls;

        public FakeDocumentStorage(Guid documentId, List<string> calls)
        {
            _documentId = documentId;
            _calls = calls;
        }

        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Storage");

            return Task.FromResult(
                new StoredDocument(_documentId, document.FileName));
        }
    }

    private sealed class FakeDocumentExtractor : IDocumentExtractor
    {
        private readonly List<string> _calls;

        public FakeDocumentExtractor(List<string> calls)
        {
            _calls = calls;
        }

        public Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Extractor");

            var extractedDocument = new ExtractedDocument(
                "raw invoice text",
                new Dictionary<string, string>
                {
                    ["VendorName"] = "Cohen Office Supplies Ltd"
                });

            return Task.FromResult(extractedDocument);
        }
    }

    private sealed class FakeInvoiceMapper : IInvoiceMapper
    {
        private readonly List<string> _calls;

        public FakeInvoiceMapper(List<string> calls)
        {
            _calls = calls;
        }

        public Task<Invoice> MapAsync(
            ExtractedDocument document,
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Mapper");

            var invoice = Invoice.CreateExtracted(
                sourceDocumentId: sourceDocumentId,
                vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
                invoiceNumber: "INV-1001",
                issueDate: new DateOnly(2026, 4, 30),
                subtotalAmount: new CurrencyAmount(1000, "ILS"),
                vatAmount: new CurrencyAmount(180, "ILS"),
                totalAmount: new CurrencyAmount(1180, "ILS"));

            return Task.FromResult(invoice);
        }
    }

    private sealed class FakeInvoiceValidator : IInvoiceValidator
    {
        private readonly InvoiceValidationReport _report;
        private readonly List<string> _calls;

        public FakeInvoiceValidator(
            InvoiceValidationReport report,
            List<string> calls)
        {
            _report = report;
            _calls = calls;
        }

        public InvoiceValidationReport Validate(Invoice invoice)
        {
            _calls.Add("Validator");

            return _report;
        }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly List<string> _calls;

        public Invoice? SavedInvoice { get; private set; }

        public FakeInvoiceRepository(List<string> calls)
        {
            _calls = calls;
        }

        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Repository");
            SavedInvoice = invoice;

            return Task.CompletedTask;
        }
    }
}