using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public interface IInvoiceRepository
{
    Task SaveAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default);
}