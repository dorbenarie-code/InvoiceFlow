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

        var storedDocument = await _documentStorage.SaveAsync(
            document,
            cancellationToken);

        var extractedDocument = await _documentExtractor.ExtractAsync(
            document,
            cancellationToken);

        var invoice = await _invoiceMapper.MapAsync(
            extractedDocument,
            storedDocument.Id,
            cancellationToken);

        var validationReport = _invoiceValidator.Validate(invoice);

        invoice.ApplyValidationReport(validationReport);

        await _invoiceRepository.SaveAsync(
            invoice,
            cancellationToken);

        return new ProcessInvoiceDocumentResult(
            storedDocument.Id,
            invoice);
    }
}
