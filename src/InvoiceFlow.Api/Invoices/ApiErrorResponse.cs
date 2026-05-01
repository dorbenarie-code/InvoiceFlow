namespace InvoiceFlow.Api.Invoices;

public sealed record ApiErrorResponse(
    string Code,
    string Message);
