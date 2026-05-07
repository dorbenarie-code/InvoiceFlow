namespace InvoiceFlow.Application.Documents;

public sealed class DocumentStorageFailedException : Exception
{
    public DocumentStorageFailedException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
