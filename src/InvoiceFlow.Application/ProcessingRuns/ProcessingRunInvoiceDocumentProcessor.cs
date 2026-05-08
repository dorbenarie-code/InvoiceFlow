using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;

namespace InvoiceFlow.Application.ProcessingRuns;

public sealed class ProcessingRunInvoiceDocumentProcessor
    : IInvoiceDocumentProcessor
{
    private readonly IInvoiceDocumentProcessor _innerProcessor;
    private readonly IProcessingRunRepository _processingRunRepository;
    private readonly IProcessingClientContext _clientContext;
    private readonly TimeProvider _timeProvider;

    public ProcessingRunInvoiceDocumentProcessor(
        IInvoiceDocumentProcessor innerProcessor,
        IProcessingRunRepository processingRunRepository,
        IProcessingClientContext clientContext,
        TimeProvider timeProvider)
    {
        _innerProcessor = innerProcessor
            ?? throw new ArgumentNullException(nameof(innerProcessor));

        _processingRunRepository = processingRunRepository
            ?? throw new ArgumentNullException(nameof(processingRunRepository));

        _clientContext = clientContext
            ?? throw new ArgumentNullException(nameof(clientContext));

        _timeProvider = timeProvider
            ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ProcessInvoiceDocumentResult> ProcessAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        var startedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var startedTimestamp = _timeProvider.GetTimestamp();

        try
        {
            var result = await _innerProcessor.ProcessAsync(
                document,
                cancellationToken);

            await SaveSuccessfulProcessingRunAsync(
                result,
                startedAtUtc,
                startedTimestamp,
                cancellationToken);

            return result;
        }
        catch (DocumentStorageFailedException)
        {
            await SaveFailedProcessingRunAsync(
                "DOCUMENT_STORAGE_FAILED",
                startedAtUtc,
                startedTimestamp,
                cancellationToken);

            throw;
        }
        catch (DocumentExtractionFailedException)
        {
            await SaveFailedProcessingRunAsync(
                "DOCUMENT_EXTRACTION_FAILED",
                startedAtUtc,
                startedTimestamp,
                cancellationToken);

            throw;
        }
        catch (InvoicePersistenceFailedException)
        {
            await SaveFailedProcessingRunAsync(
                "INVOICE_PERSISTENCE_FAILED",
                startedAtUtc,
                startedTimestamp,
                cancellationToken);

            throw;
        }
    }

    private async Task SaveSuccessfulProcessingRunAsync(
        ProcessInvoiceDocumentResult result,
        DateTime startedAtUtc,
        long startedTimestamp,
        CancellationToken cancellationToken)
    {
        var processingRun = new ProcessingRun(
            id: Guid.NewGuid(),
            clientId: _clientContext.ClientId,
            documentId: result.DocumentId,
            invoiceId: result.InvoiceId,
            status: result.Status.ToString(),
            analyzedPageCount: result.AnalyzedPageCount,
            durationMs: GetDurationMs(startedTimestamp),
            errorCode: null,
            createdAtUtc: startedAtUtc);

        await _processingRunRepository.SaveAsync(
            processingRun,
            cancellationToken);
    }

    private async Task SaveFailedProcessingRunAsync(
        string errorCode,
        DateTime startedAtUtc,
        long startedTimestamp,
        CancellationToken cancellationToken)
    {
        var processingRun = new ProcessingRun(
            id: Guid.NewGuid(),
            clientId: _clientContext.ClientId,
            documentId: null,
            invoiceId: null,
            status: "Failed",
            analyzedPageCount: null,
            durationMs: GetDurationMs(startedTimestamp),
            errorCode: errorCode,
            createdAtUtc: startedAtUtc);

        await _processingRunRepository.SaveAsync(
            processingRun,
            cancellationToken);
    }

    private long GetDurationMs(
        long startedTimestamp)
    {
        var elapsed = _timeProvider.GetElapsedTime(
            startedTimestamp,
            _timeProvider.GetTimestamp());

        return (long)elapsed.TotalMilliseconds;
    }
}
