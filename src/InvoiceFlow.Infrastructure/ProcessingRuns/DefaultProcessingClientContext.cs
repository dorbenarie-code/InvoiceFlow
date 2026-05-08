using InvoiceFlow.Application.ProcessingRuns;

namespace InvoiceFlow.Infrastructure.ProcessingRuns;

public sealed class DefaultProcessingClientContext : IProcessingClientContext
{
    private static readonly Guid DefaultClientId =
        Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid ClientId => DefaultClientId;
}
