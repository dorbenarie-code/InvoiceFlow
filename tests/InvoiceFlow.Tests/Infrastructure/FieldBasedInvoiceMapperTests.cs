using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Invoices;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class FieldBasedInvoiceMapperTests
{
    [Fact]
    public async Task MapAsync_ShouldMapExtractedFieldsToInvoice()
    {
        var sourceDocumentId = Guid.NewGuid();

        var document = new ExtractedDocument(
            "raw text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["VendorTaxId"] = "516789123",
                ["InvoiceNumber"] = "INV-1001",
                ["IssueDate"] = "2026-04-30",
                ["SubtotalAmount"] = "1000",
                ["VatAmount"] = "180",
                ["TotalAmount"] = "1180",
                ["Currency"] = "ILS"
            });

        var mapper = new FieldBasedInvoiceMapper();

        var invoice = await mapper.MapAsync(document, sourceDocumentId);

        Assert.Equal(sourceDocumentId, invoice.SourceDocumentId);
        Assert.Equal("Cohen Office Supplies Ltd", invoice.Vendor?.Name);
        Assert.Equal("516789123", invoice.Vendor?.TaxId);
        Assert.Equal("INV-1001", invoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), invoice.IssueDate);
        Assert.Equal(1000, invoice.SubtotalAmount?.Amount);
        Assert.Equal(180, invoice.VatAmount?.Amount);
        Assert.Equal(1180, invoice.TotalAmount?.Amount);
        Assert.Equal("ILS", invoice.TotalAmount?.Currency);
    }

    [Fact]
    public async Task MapAsync_ShouldMapCurrencySymbolToCurrencyCode()
    {
        var document = new ExtractedDocument(
            "raw text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["InvoiceNumber"] = "INV-1001",
                ["IssueDate"] = "30/04/2026",
                ["SubtotalAmount"] = "1000",
                ["VatAmount"] = "180",
                ["TotalAmount"] = "1180",
                ["Currency"] = "₪"
            });

        var mapper = new FieldBasedInvoiceMapper();

        var invoice = await mapper.MapAsync(document, Guid.NewGuid());

        Assert.Equal("ILS", invoice.TotalAmount?.Currency);
        Assert.Equal(new DateOnly(2026, 4, 30), invoice.IssueDate);
    }
    [Fact]
public async Task MapAsync_ShouldMapIssueDate_WhenValueHasLeadingAndTrailingWhitespace()
{
    var document = new ExtractedDocument(
        "raw text",
        new Dictionary<string, string>
        {
            ["IssueDate"] = " 2026-04-30 "
        });

    var mapper = new FieldBasedInvoiceMapper();

    var invoice = await mapper.MapAsync(document, Guid.NewGuid());

    Assert.Equal(new DateOnly(2026, 4, 30), invoice.IssueDate);
}

    [Fact]
    public async Task MapAsync_ShouldAllowMissingFields()
    {
        var document = new ExtractedDocument("raw text");
        var mapper = new FieldBasedInvoiceMapper();

        var invoice = await mapper.MapAsync(document, Guid.NewGuid());

        Assert.Null(invoice.Vendor);
        Assert.Null(invoice.InvoiceNumber);
        Assert.Null(invoice.IssueDate);
        Assert.Null(invoice.SubtotalAmount);
        Assert.Null(invoice.VatAmount);
        Assert.Null(invoice.TotalAmount);
    }

    [Fact]
    public async Task MapAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var mapper = new FieldBasedInvoiceMapper();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            mapper.MapAsync(null!, Guid.NewGuid()));
    }
}