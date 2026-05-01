namespace InvoiceFlow.Domain.Invoices;

public interface IInvoiceValidator
{
    InvoiceValidationReport Validate(Invoice invoice);
}