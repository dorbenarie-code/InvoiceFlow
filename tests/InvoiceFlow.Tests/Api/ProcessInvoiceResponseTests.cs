using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Api;

public sealed class ProcessInvoiceResponseTests
{
    [Fact]
    public void FromResult_ShouldMapVerifiedInvoiceResult()
    {
        var documentId = Guid.NewGuid();

        var invoice = CreateInvoice(
            documentId,
            metadata: new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["InvoiceNumber"] = "INV-1001"
            });

        invoice.ApplyValidationReport(InvoiceValidationReport.Valid());

        var result = new ProcessInvoiceDocumentResult(
            documentId,
            invoice);

        var response = ProcessInvoiceResponse.FromResult(result);

        Assert.Equal(documentId, response.DocumentId);
        Assert.Equal(invoice.Id, response.InvoiceId);
        Assert.Equal("Verified", response.Status);

        Assert.Equal(invoice.Id, response.Invoice.Id);
        Assert.Equal(documentId, response.Invoice.SourceDocumentId);
        Assert.Equal("Cohen Office Supplies Ltd", response.Invoice.VendorName);
        Assert.Equal("516789123", response.Invoice.VendorTaxId);
        Assert.Equal("INV-1001", response.Invoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), response.Invoice.IssueDate);
        Assert.Equal("Verified", response.Invoice.Status);

        Assert.Equal(1000m, response.Invoice.SubtotalAmount?.Amount);
        Assert.Equal("ILS", response.Invoice.SubtotalAmount?.Currency);

        Assert.Equal(180m, response.Invoice.VatAmount?.Amount);
        Assert.Equal("ILS", response.Invoice.VatAmount?.Currency);

        Assert.Equal(1180m, response.Invoice.TotalAmount?.Amount);
        Assert.Equal("ILS", response.Invoice.TotalAmount?.Currency);

        Assert.Equal("Cohen Office Supplies Ltd", response.Invoice.Metadata["VendorName"]);
        Assert.Equal("INV-1001", response.Invoice.Metadata["InvoiceNumber"]);

        Assert.False(response.ValidationReport.HasIssues);
        Assert.False(response.ValidationReport.HasErrors);
        Assert.False(response.ValidationReport.HasWarnings);
        Assert.False(response.ValidationReport.RequiresHumanReview);
        Assert.Empty(response.ValidationReport.Issues);
    }

    [Fact]
    public void FromResult_ShouldMapRequiresHumanReviewResult_WhenValidationHasErrors()
    {
        var documentId = Guid.NewGuid();
        var invoice = CreateInvoice(documentId);

        var validationReport = InvoiceValidationReport.FromIssues(
        [
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                "TotalAmount",
                "Subtotal amount plus VAT amount must match total amount.")
        ]);

        invoice.ApplyValidationReport(validationReport);

        var result = new ProcessInvoiceDocumentResult(
            documentId,
            invoice);

        var response = ProcessInvoiceResponse.FromResult(result);

        Assert.Equal(documentId, response.DocumentId);
        Assert.Equal(invoice.Id, response.InvoiceId);
        Assert.Equal("RequiresHumanReview", response.Status);
        Assert.Equal("RequiresHumanReview", response.Invoice.Status);

        Assert.True(response.ValidationReport.HasIssues);
        Assert.True(response.ValidationReport.HasErrors);
        Assert.False(response.ValidationReport.HasWarnings);
        Assert.True(response.ValidationReport.RequiresHumanReview);

        var issue = Assert.Single(response.ValidationReport.Issues);

        Assert.Equal("TOTAL_MISMATCH", issue.Code);
        Assert.Equal("TotalAmount", issue.FieldName);
        Assert.Equal(
            "Subtotal amount plus VAT amount must match total amount.",
            issue.Message);
        Assert.Equal("Error", issue.Severity);
    }

    [Fact]
    public void FromInvoice_ShouldMapMissingOptionalInvoiceFieldsAsNull()
    {
        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: null,
            invoiceNumber: null,
            issueDate: null,
            subtotalAmount: null,
            vatAmount: null,
            totalAmount: null);

        var response = InvoiceResponse.FromInvoice(invoice);

        Assert.Equal(invoice.Id, response.Id);
        Assert.Equal(invoice.SourceDocumentId, response.SourceDocumentId);
        Assert.Null(response.VendorName);
        Assert.Null(response.VendorTaxId);
        Assert.Null(response.InvoiceNumber);
        Assert.Null(response.IssueDate);
        Assert.Null(response.SubtotalAmount);
        Assert.Null(response.VatAmount);
        Assert.Null(response.TotalAmount);
        Assert.Equal("Extracted", response.Status);
        Assert.Empty(response.Metadata);
    }

    [Fact]
    public void FromInvoice_ShouldMapMetadata()
    {
        var invoice = CreateInvoice(
            Guid.NewGuid(),
            metadata: new Dictionary<string, string>
            {
                ["Source"] = "OCR",
                ["Confidence"] = "High"
            });

        var response = InvoiceResponse.FromInvoice(invoice);

        Assert.Equal(2, response.Metadata.Count);
        Assert.Equal("OCR", response.Metadata["Source"]);
        Assert.Equal("High", response.Metadata["Confidence"]);
    }

    [Fact]
    public void FromReport_ShouldMapWarningOnlyReportWithoutHumanReview()
    {
        var report = InvoiceValidationReport.FromIssues(
        [
            InvoiceValidationIssue.Warning(
                "LOW_CONFIDENCE",
                "VendorName",
                "Extracted vendor name confidence is low.")
        ]);

        var response = ValidationReportResponse.FromReport(report);

        Assert.True(response.HasIssues);
        Assert.False(response.HasErrors);
        Assert.True(response.HasWarnings);
        Assert.False(response.RequiresHumanReview);

        var issue = Assert.Single(response.Issues);

        Assert.Equal("LOW_CONFIDENCE", issue.Code);
        Assert.Equal("VendorName", issue.FieldName);
        Assert.Equal(
            "Extracted vendor name confidence is low.",
            issue.Message);
        Assert.Equal("Warning", issue.Severity);
    }

    [Fact]
    public void FromReport_ShouldPreserveValidationIssueOrder()
    {
        var firstIssue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE",
            "VendorName",
            "Extracted vendor name confidence is low.");

        var secondIssue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal amount plus VAT amount must match total amount.");

        var report = InvoiceValidationReport.FromIssues(
        [
            firstIssue,
            secondIssue
        ]);

        var response = ValidationReportResponse.FromReport(report);

        var issues = response.Issues.ToArray();

        Assert.Equal(2, issues.Length);
        Assert.Equal("LOW_CONFIDENCE", issues[0].Code);
        Assert.Equal("Warning", issues[0].Severity);
        Assert.Equal("TOTAL_MISMATCH", issues[1].Code);
        Assert.Equal("Error", issues[1].Severity);
    }

    [Fact]
    public void FromIssue_ShouldMapNullFieldName()
    {
        var issue = InvoiceValidationIssue.Warning(
            "GENERAL_WARNING",
            fieldName: null,
            "General validation warning.");

        var response = ValidationIssueResponse.FromIssue(issue);

        Assert.Equal("GENERAL_WARNING", response.Code);
        Assert.Null(response.FieldName);
        Assert.Equal("General validation warning.", response.Message);
        Assert.Equal("Warning", response.Severity);
    }

    [Fact]
    public void FromAmount_ShouldMapCurrencyAmount()
    {
        var amount = new CurrencyAmount(250.75m, "ils");

        var response = AmountResponse.FromAmount(amount);

        Assert.NotNull(response);
        Assert.Equal(250.75m, response.Amount);
        Assert.Equal("ILS", response.Currency);
    }

    [Fact]
    public void FromAmount_ShouldReturnNull_WhenAmountIsNull()
    {
        var response = AmountResponse.FromAmount(null);

        Assert.Null(response);
    }

    [Fact]
    public void MappingMethods_ShouldThrow_WhenInputIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ProcessInvoiceResponse.FromResult(null!));

        Assert.Throws<ArgumentNullException>(() =>
            InvoiceResponse.FromInvoice(null!));

        Assert.Throws<ArgumentNullException>(() =>
            ValidationReportResponse.FromReport(null!));

        Assert.Throws<ArgumentNullException>(() =>
            ValidationIssueResponse.FromIssue(null!));
    }

    private static Invoice CreateInvoice(
        Guid sourceDocumentId,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: sourceDocumentId,
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 4, 30),
            subtotalAmount: new CurrencyAmount(1000m, "ILS"),
            vatAmount: new CurrencyAmount(180m, "ILS"),
            totalAmount: new CurrencyAmount(1180m, "ILS"),
            metadata: metadata);
    }
}