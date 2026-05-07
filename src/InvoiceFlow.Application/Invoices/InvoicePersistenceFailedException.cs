namespace InvoiceFlow.Application.Invoices;

public sealed class InvoicePersistenceFailedException : Exception
{
    public InvoicePersistenceFailedException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
