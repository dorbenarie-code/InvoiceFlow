using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Application.Invoices;

public sealed class ProcessInvoiceDocumentResultAnalyzedPageCountTests
{
    private static readonly Guid DocumentId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Constructor_ShouldSetAnalyzedPageCount_WhenProvided()
    {
        var invoice = CreateVerifiedInvoice();

        var result = new ProcessInvoiceDocumentResult(
            DocumentId,
            invoice,
            analyzedPageCount: 3);

        Assert.Equal(3, result.AnalyzedPageCount);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingAnalyzedPageCount()
    {
        var invoice = CreateVerifiedInvoice();

        var result = new ProcessInvoiceDocumentResult(
            DocumentId,
            invoice);

        Assert.Null(result.AnalyzedPageCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenAnalyzedPageCountIsNotPositive(
        int analyzedPageCount)
    {
        var invoice = CreateVerifiedInvoice();

        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ProcessInvoiceDocumentResult(
                DocumentId,
                invoice,
                analyzedPageCount: analyzedPageCount));

        Assert.Contains(
            "Analyzed page count must be greater than zero when provided.",
            exception.Message);
    }

    private static Invoice CreateVerifiedInvoice()
    {
        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: DocumentId,
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 5, 7),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        invoice.ApplyValidationReport(InvoiceValidationReport.Valid());

        return invoice;
    }
}
