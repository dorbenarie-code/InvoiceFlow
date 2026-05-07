using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Application.Invoices;

public sealed class ProcessInvoiceDocumentServiceDocumentStorageFailureTests
{
    [Fact]
    public async Task ProcessAsync_ShouldWrapDocumentStorageFailure_WhenDocumentStorageFails()
    {
        var calls = new List<string>();

        var expectedException = new InvalidOperationException(
            "Blob storage upload failed.");

        var service = new ProcessInvoiceDocumentService(
            new ThrowingDocumentStorage(calls, expectedException),
            new SpyDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(calls),
            new FakeInvoiceRepository(calls));

        var exception = await Assert.ThrowsAsync<DocumentStorageFailedException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Equal("Document storage failed.", exception.Message);
        Assert.Same(expectedException, exception.InnerException);

        Assert.Equal(
        [
            "Storage"
        ], calls);
    }

    [Fact]
    public async Task ProcessAsync_ShouldNotWrapCancellation_WhenDocumentStorageIsCanceled()
    {
        var calls = new List<string>();

        var expectedException = new OperationCanceledException(
            "Blob storage upload was canceled.");

        var service = new ProcessInvoiceDocumentService(
            new ThrowingDocumentStorage(calls, expectedException),
            new SpyDocumentExtractor(calls),
            new FakeInvoiceMapper(calls),
            new FakeInvoiceValidator(calls),
            new FakeInvoiceRepository(calls));

        var exception = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ProcessAsync(CreateDocumentInput()));

        Assert.Same(expectedException, exception);

        Assert.Equal(
        [
            "Storage"
        ], calls);
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

    private static Invoice CreateInvoice(Guid sourceDocumentId)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: sourceDocumentId,
            vendor: new Vendor("Storage Test Vendor Ltd", "123456789"),
            invoiceNumber: "INV-STORAGE-1001",
            issueDate: new DateOnly(2026, 4, 30),
            subtotalAmount: new CurrencyAmount(100m, "ILS"),
            vatAmount: new CurrencyAmount(18m, "ILS"),
            totalAmount: new CurrencyAmount(118m, "ILS"));
    }

    private sealed class ThrowingDocumentStorage : IDocumentStorage
    {
        private readonly List<string> _calls;
        private readonly Exception _exceptionToThrow;

        public ThrowingDocumentStorage(
            List<string> calls,
            Exception exceptionToThrow)
        {
            _calls = calls;
            _exceptionToThrow = exceptionToThrow;
        }

        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Storage");

            throw _exceptionToThrow;
        }
    }

    private sealed class SpyDocumentExtractor : IDocumentExtractor
    {
        private readonly List<string> _calls;

        public SpyDocumentExtractor(List<string> calls)
        {
            _calls = calls;
        }

        public Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Extractor");

            return Task.FromResult(
                new ExtractedDocument(
                    "storage failure test extracted text",
                    new Dictionary<string, string>()));
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

            return Task.FromResult(CreateInvoice(sourceDocumentId));
        }
    }

    private sealed class FakeInvoiceValidator : IInvoiceValidator
    {
        private readonly List<string> _calls;

        public FakeInvoiceValidator(List<string> calls)
        {
            _calls = calls;
        }

        public InvoiceValidationReport Validate(Invoice invoice)
        {
            _calls.Add("Validator");

            return InvoiceValidationReport.Valid();
        }
    }

    private sealed class FakeInvoiceRepository : IInvoiceRepository
    {
        private readonly List<string> _calls;

        public FakeInvoiceRepository(List<string> calls)
        {
            _calls = calls;
        }

        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            _calls.Add("Repository");

            return Task.CompletedTask;
        }
    }
}
