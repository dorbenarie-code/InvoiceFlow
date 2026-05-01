using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Application.Invoices;

public sealed record ProcessInvoiceDocumentResult
{
    public Guid DocumentId => Invoice.SourceDocumentId;

    public Guid InvoiceId => Invoice.Id;

    public InvoiceStatus Status => Invoice.Status;

    public InvoiceValidationReport ValidationReport => Invoice.ValidationReport;

    public Invoice Invoice { get; }

    public ProcessInvoiceDocumentResult(
        Guid documentId,
        Invoice invoice)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Document id is required.", nameof(documentId));
        }

        ArgumentNullException.ThrowIfNull(invoice);

        if (invoice.SourceDocumentId != documentId)
        {
            throw new InvalidOperationException(
                "Invoice source document id must match the stored document id.");
        }

        Invoice = invoice;
    }
}