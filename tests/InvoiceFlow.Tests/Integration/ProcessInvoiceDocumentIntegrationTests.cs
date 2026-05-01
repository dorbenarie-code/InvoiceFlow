using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;

namespace InvoiceFlow.Tests.Integration;

public sealed class ProcessInvoiceDocumentIntegrationTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public async Task ProcessAsync_ShouldRunFullPipelineAndSaveVerifiedInvoice()
    {
        var documentStorage = new InMemoryDocumentStorage();
        var documentExtractor = new FakeDocumentExtractor();
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.Verified, result.Status);
        Assert.False(result.ValidationReport.HasIssues);

        Assert.Single(documentStorage.Documents);
        Assert.Single(invoiceRepository.Invoices);

        var storedDocument = documentStorage.Documents.Single();
        var savedInvoice = invoiceRepository.Invoices.Single();

        Assert.Equal(storedDocument.Id, result.DocumentId);
        Assert.Equal(storedDocument.Id, savedInvoice.SourceDocumentId);
        Assert.Equal(savedInvoice.Id, result.InvoiceId);
        Assert.Equal(savedInvoice, result.Invoice);

        Assert.Equal("Cohen Office Supplies Ltd", savedInvoice.Vendor?.Name);
        Assert.Equal("516789123", savedInvoice.Vendor?.TaxId);
        Assert.Equal("INV-1001", savedInvoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), savedInvoice.IssueDate);
        Assert.Equal(1000, savedInvoice.SubtotalAmount?.Amount);
        Assert.Equal(180, savedInvoice.VatAmount?.Amount);
        Assert.Equal(1180, savedInvoice.TotalAmount?.Amount);
        Assert.Equal("ILS", savedInvoice.TotalAmount?.Currency);

        Assert.Equal("Cohen Office Supplies Ltd", savedInvoice.Metadata["VendorName"]);
        Assert.Equal("INV-1001", savedInvoice.Metadata["InvoiceNumber"]);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSaveInvoiceAsRequiresHumanReview_WhenTotalDoesNotMatch()
    {
        var extractedDocument = new ExtractedDocument(
            "invoice with invalid total",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["VendorTaxId"] = "516789123",
                ["InvoiceNumber"] = "INV-1001",
                ["IssueDate"] = "2026-04-30",
                ["SubtotalAmount"] = "1000",
                ["VatAmount"] = "170",
                ["TotalAmount"] = "1180",
                ["Currency"] = "ILS"
            });

        var documentStorage = new InMemoryDocumentStorage();
        var documentExtractor = new FakeDocumentExtractor(extractedDocument);
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.True(result.ValidationReport.RequiresHumanReview);

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "TOTAL_MISMATCH"
            && issue.FieldName == "TotalAmount");

        Assert.Single(documentStorage.Documents);
        Assert.Single(invoiceRepository.Invoices);

        var savedInvoice = invoiceRepository.Invoices.Single();

        Assert.Equal(InvoiceStatus.RequiresHumanReview, savedInvoice.Status);
        Assert.True(savedInvoice.ValidationReport.RequiresHumanReview);
    }

    [Fact]
    public async Task ProcessAsync_ShouldSaveInvoiceAsRequiresHumanReview_WhenRequiredFieldsAreMissing()
    {
        var extractedDocument = new ExtractedDocument(
            "empty invoice",
            new Dictionary<string, string>());

        var documentStorage = new InMemoryDocumentStorage();
        var documentExtractor = new FakeDocumentExtractor(extractedDocument);
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.True(result.ValidationReport.RequiresHumanReview);

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_VENDOR");

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_INVOICE_NUMBER");

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_ISSUE_DATE");

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_SUBTOTAL_AMOUNT");

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_VAT_AMOUNT");

        Assert.Contains(result.ValidationReport.Issues, issue =>
            issue.Code == "MISSING_TOTAL_AMOUNT");

        Assert.Single(documentStorage.Documents);
        Assert.Single(invoiceRepository.Invoices);
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }
}