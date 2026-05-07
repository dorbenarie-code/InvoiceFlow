using InvoiceFlow.Application.Documents;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed class ProcessInvoiceDocumentService : IInvoiceDocumentProcessor
{
    private readonly IDocumentStorage _documentStorage;
    private readonly IDocumentExtractor _documentExtractor;
    private readonly IInvoiceMapper _invoiceMapper;
    private readonly IInvoiceValidator _invoiceValidator;
    private readonly IInvoiceRepository _invoiceRepository;

    public ProcessInvoiceDocumentService(
        IDocumentStorage documentStorage,
        IDocumentExtractor documentExtractor,
        IInvoiceMapper invoiceMapper,
        IInvoiceValidator invoiceValidator,
        IInvoiceRepository invoiceRepository)
    {
        _documentStorage = documentStorage
            ?? throw new ArgumentNullException(nameof(documentStorage));

        _documentExtractor = documentExtractor
            ?? throw new ArgumentNullException(nameof(documentExtractor));

        _invoiceMapper = invoiceMapper
            ?? throw new ArgumentNullException(nameof(invoiceMapper));

        _invoiceValidator = invoiceValidator
            ?? throw new ArgumentNullException(nameof(invoiceValidator));

        _invoiceRepository = invoiceRepository
            ?? throw new ArgumentNullException(nameof(invoiceRepository));
    }

    public async Task<ProcessInvoiceDocumentResult> ProcessAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        StoredDocument storedDocument;

        try
        {
            storedDocument = await _documentStorage.SaveAsync(
                document,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentStorageFailedException(
                "Document storage failed.",
                exception);
        }

        ExtractedDocument extractedDocument;

        try
        {
            extractedDocument = await _documentExtractor.ExtractAsync(
                document,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DocumentExtractionFailedException(
                "Document extraction failed.",
                exception);
        }

        if (extractedDocument is null)
        {
            throw new InvalidOperationException(
                "Document extractor returned no extracted document.");
        }

        var invoice = await _invoiceMapper.MapAsync(
            extractedDocument,
            storedDocument.Id,
            cancellationToken);

        if (invoice is null)
        {
            throw new InvalidOperationException(
                "Invoice mapper returned no invoice.");
        }

        if (invoice.SourceDocumentId != storedDocument.Id)
        {
            throw new InvalidOperationException(
                "Mapped invoice source document id must match the stored document id.");
        }

        var validationReport = _invoiceValidator.Validate(invoice);

        invoice.ApplyValidationReport(validationReport);

        try
        {
            await _invoiceRepository.SaveAsync(
                invoice,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvoicePersistenceFailedException(
                "Invoice persistence failed.",
                exception);
        }

        return new ProcessInvoiceDocumentResult(
            storedDocument.Id,
            invoice);
    }
}
