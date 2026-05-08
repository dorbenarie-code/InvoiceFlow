namespace InvoiceFlow.Application.ProcessingRuns;

public interface IProcessingRunRepository
{
    Task SaveAsync(
        ProcessingRun processingRun,
        CancellationToken cancellationToken = default);
}
