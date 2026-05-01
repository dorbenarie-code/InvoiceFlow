using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Infrastructure.Invoices;

public sealed class InMemoryInvoiceRepository : IInvoiceRepository
{
    private readonly object _syncRoot = new();
    private readonly List<Invoice> _invoices = [];

    public IReadOnlyCollection<Invoice> Invoices
    {
        get
        {
            lock (_syncRoot)
            {
                return _invoices.ToList().AsReadOnly();
            }
        }
    }

    public Task SaveAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        cancellationToken.ThrowIfCancellationRequested();

        lock (_syncRoot)
        {
            _invoices.Add(invoice);
        }

        return Task.CompletedTask;
    }
}
