using InvoiceFlow.Application.ProcessingRuns;

namespace InvoiceFlow.Infrastructure.ProcessingRuns;

public sealed class InMemoryProcessingRunRepository : IProcessingRunRepository
{
    private readonly object _syncRoot = new();
    private readonly List<ProcessingRun> _processingRuns = [];

    public IReadOnlyCollection<ProcessingRun> ProcessingRuns
    {
        get
        {
            lock (_syncRoot)
            {
                return _processingRuns.ToList().AsReadOnly();
            }
        }
    }

    public Task SaveAsync(
        ProcessingRun processingRun,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processingRun);

        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _processingRuns.Add(processingRun);
        }

        return Task.CompletedTask;
    }
}
