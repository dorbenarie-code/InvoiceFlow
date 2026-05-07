using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Application;

public sealed class ProcessInvoiceDocumentResultTests
{
    [Fact]
    public void Constructor_ShouldExposeInvoiceDerivedValues()
    {
        var documentId = Guid.NewGuid();
        var invoice = CreateInvoice(documentId);
        var report = InvoiceValidationReport.Valid();
        invoice.ApplyValidationReport(report);

        var result = new ProcessInvoiceDocumentResult(
            documentId,
            invoice);

        Assert.Equal(documentId, result.DocumentId);
        Assert.Equal(invoice.Id, result.InvoiceId);
        Assert.Equal(InvoiceStatus.Verified, result.Status);
        Assert.Equal(report, result.ValidationReport);
        Assert.Equal(invoice, result.Invoice);
    }

    [Fact]
    public void Constructor_ShouldExposeHumanReviewValues_WhenInvoiceRequiresHumanReview()
    {
        var documentId = Guid.NewGuid();
        var invoice = CreateInvoice(documentId);
        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            nameof(invoice.TotalAmount),
            "Subtotal amount plus VAT amount must match total amount.");
        var report = InvoiceValidationReport.FromIssues(new[] { issue });
        invoice.ApplyValidationReport(report);

        var result = new ProcessInvoiceDocumentResult(
            documentId,
            invoice);

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.Equal(report, result.ValidationReport);
        Assert.True(result.ValidationReport.RequiresHumanReview);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDocumentIdIsEmpty()
    {
        var invoice = CreateInvoice(Guid.NewGuid());

        var exception = Assert.Throws<ArgumentException>(() =>
            new ProcessInvoiceDocumentResult(
                Guid.Empty,
                invoice));

        Assert.Equal("documentId", exception.ParamName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenInvoiceIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ProcessInvoiceDocumentResult(
                Guid.NewGuid(),
                null!));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenDocumentIdDoesNotMatchInvoiceSourceDocumentId()
    {
        var invoice = CreateInvoice(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() =>
            new ProcessInvoiceDocumentResult(
                Guid.NewGuid(),
                invoice));
    }

    private static Invoice CreateInvoice(Guid sourceDocumentId)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: sourceDocumentId,
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 4, 30),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));
    }
}
