using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Domain;

internal static class TestInvoiceFactory
{
    private static readonly DateOnly DefaultIssueDate = new(2026, 4, 30);

    public static Invoice CreateValidInvoice(
        DateOnly? issueDate = null,
        string? invoiceNumber = "INV-1001",
        CurrencyAmount? subtotalAmount = null,
        CurrencyAmount? vatAmount = null,
        CurrencyAmount? totalAmount = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: invoiceNumber,
            issueDate: issueDate ?? DefaultIssueDate,
            subtotalAmount: subtotalAmount ?? new CurrencyAmount(1000, "ILS"),
            vatAmount: vatAmount ?? new CurrencyAmount(180, "ILS"),
            totalAmount: totalAmount ?? new CurrencyAmount(1180, "ILS"),
            metadata: metadata);
    }

    public static Invoice CreateExtractedInvoice(
        Vendor? vendor = null,
        string? invoiceNumber = null,
        DateOnly? issueDate = null,
        CurrencyAmount? subtotalAmount = null,
        CurrencyAmount? vatAmount = null,
        CurrencyAmount? totalAmount = null,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: vendor,
            invoiceNumber: invoiceNumber,
            issueDate: issueDate,
            subtotalAmount: subtotalAmount,
            vatAmount: vatAmount,
            totalAmount: totalAmount,
            metadata: metadata);
    }
}
