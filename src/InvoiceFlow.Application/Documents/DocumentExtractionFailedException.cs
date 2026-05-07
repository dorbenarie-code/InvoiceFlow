namespace InvoiceFlow.Application.Documents;

public sealed class DocumentExtractionFailedException : Exception
{
    public DocumentExtractionFailedException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
