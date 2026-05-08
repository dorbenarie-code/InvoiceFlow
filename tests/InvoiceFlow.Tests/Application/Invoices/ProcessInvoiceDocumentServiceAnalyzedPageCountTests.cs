using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;

namespace InvoiceFlow.Tests.Application.Invoices;

public sealed class ProcessInvoiceDocumentServiceAnalyzedPageCountTests
{
    private static readonly DateOnly ValidationDate = new(2026, 5, 7);

    [Fact]
    public async Task ProcessAsync_ShouldReturnAnalyzedPageCount_FromExtractedDocument()
    {
        var extractedDocument = new ExtractedDocument(
            "invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["VendorTaxId"] = "516789123",
                ["InvoiceNumber"] = "INV-1001",
                ["IssueDate"] = "2026-05-07",
                ["SubtotalAmount"] = "1000",
                ["VatAmount"] = "180",
                ["TotalAmount"] = "1180",
                ["Currency"] = "ILS"
            },
            analyzedPageCount: 7);

        var service = new ProcessInvoiceDocumentService(
            documentStorage: new InMemoryDocumentStorage(),
            documentExtractor: new FakeDocumentExtractor(extractedDocument),
            invoiceMapper: new FieldBasedInvoiceMapper(),
            invoiceValidator: new DefaultInvoiceValidator(ValidationDate),
            invoiceRepository: new InMemoryInvoiceRepository());

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(7, result.AnalyzedPageCount);
        Assert.Equal(InvoiceStatus.Verified, result.Status);
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D
            });
    }
}
