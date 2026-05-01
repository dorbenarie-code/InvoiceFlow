using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Api.Invoices;

public sealed record ProcessInvoiceResponse(
    Guid DocumentId,
    Guid InvoiceId,
    string Status,
    InvoiceResponse Invoice,
    ValidationReportResponse ValidationReport)
{
    public static ProcessInvoiceResponse FromResult(
        ProcessInvoiceDocumentResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ProcessInvoiceResponse(
            result.DocumentId,
            result.InvoiceId,
            result.Status.ToString(),
            InvoiceResponse.FromInvoice(result.Invoice),
            ValidationReportResponse.FromReport(result.ValidationReport));
    }
}

public sealed record InvoiceResponse(
    Guid Id,
    Guid SourceDocumentId,
    string? VendorName,
    string? VendorTaxId,
    string? InvoiceNumber,
    DateOnly? IssueDate,
    AmountResponse? SubtotalAmount,
    AmountResponse? VatAmount,
    AmountResponse? TotalAmount,
    string Status,
    IReadOnlyDictionary<string, string> Metadata)
{
    public static InvoiceResponse FromInvoice(Invoice invoice)
    {
        ArgumentNullException.ThrowIfNull(invoice);

        return new InvoiceResponse(
            invoice.Id,
            invoice.SourceDocumentId,
            invoice.Vendor?.Name,
            invoice.Vendor?.TaxId,
            invoice.InvoiceNumber,
            invoice.IssueDate,
            AmountResponse.FromAmount(invoice.SubtotalAmount),
            AmountResponse.FromAmount(invoice.VatAmount),
            AmountResponse.FromAmount(invoice.TotalAmount),
            invoice.Status.ToString(),
            invoice.Metadata);
    }
}

public sealed record AmountResponse(
    decimal Amount,
    string Currency)
{
    public static AmountResponse? FromAmount(CurrencyAmount? amount)
    {
        return amount is null
            ? null
            : new AmountResponse(amount.Amount, amount.Currency);
    }
}

public sealed record ValidationReportResponse(
    bool HasIssues,
    bool HasErrors,
    bool HasWarnings,
    bool RequiresHumanReview,
    IReadOnlyCollection<ValidationIssueResponse> Issues)
{
    public static ValidationReportResponse FromReport(
        InvoiceValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new ValidationReportResponse(
            report.HasIssues,
            report.HasErrors,
            report.HasWarnings,
            report.RequiresHumanReview,
            report.Issues
                .Select(ValidationIssueResponse.FromIssue)
                .ToArray());
    }
}

public sealed record ValidationIssueResponse(
    string Code,
    string? FieldName,
    string Message,
    string Severity)
{
    public static ValidationIssueResponse FromIssue(
        InvoiceValidationIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);

        return new ValidationIssueResponse(
            issue.Code,
            issue.FieldName,
            issue.Message,
            issue.Severity.ToString());
    }
}
