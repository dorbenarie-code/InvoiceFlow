using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Api.Invoices;

internal sealed record InvoiceDocumentUploadReadResult
{
    public DocumentInput? Document { get; }
    public ApiErrorResponse? Error { get; }
    public int StatusCode { get; }

    public bool Succeeded => Document is not null;

    private InvoiceDocumentUploadReadResult(
        DocumentInput? document,
        ApiErrorResponse? error,
        int statusCode)
    {
        Document = document;
        Error = error;
        StatusCode = statusCode;
    }

    public static InvoiceDocumentUploadReadResult Success(
        DocumentInput document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new InvoiceDocumentUploadReadResult(
            document,
            null,
            StatusCodes.Status200OK);
    }

    public static InvoiceDocumentUploadReadResult Failure(
        string code,
        string message,
        int statusCode = StatusCodes.Status400BadRequest)
    {
        return new InvoiceDocumentUploadReadResult(
            null,
            new ApiErrorResponse(code, message),
            statusCode);
    }
}
