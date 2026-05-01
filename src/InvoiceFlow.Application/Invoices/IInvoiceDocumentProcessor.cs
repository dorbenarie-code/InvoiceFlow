using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Application.Invoices;

public interface IInvoiceDocumentProcessor
{
    Task<ProcessInvoiceDocumentResult> ProcessAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default);
}
