using InvoiceFlow.Application.Documents;
using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public interface IInvoiceMapper
{
    Task<Invoice> MapAsync(
        ExtractedDocument document,
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default);
}