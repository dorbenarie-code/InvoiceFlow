using System.Collections.Generic;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using Xunit;

namespace InvoiceFlow.Tests.Domain;

public sealed class InvoiceTests
{
    [Fact]
    public void CreateExtracted_ShouldCreateInvoiceWithExtractedStatus()
    {
        var sourceDocumentId = Guid.NewGuid();
        var vendor = new Vendor("Cohen Office Supplies Ltd", "516789123");
        var subtotal = new CurrencyAmount(1000, "ILS");
        var vat = new CurrencyAmount(180, "ILS");
        var total = new CurrencyAmount(1180, "ILS");

        var invoice = Invoice.CreateExtracted(
            sourceDocumentId,
            vendor,
            "INV-1001",
            new DateOnly(2026, 4, 30),
            subtotal,
            vat,
            total);

        Assert.NotEqual(Guid.Empty, invoice.Id);
        Assert.Equal(sourceDocumentId, invoice.SourceDocumentId);
        Assert.Equal(vendor, invoice.Vendor);
        Assert.Equal("INV-1001", invoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), invoice.IssueDate);
        Assert.Equal(subtotal, invoice.SubtotalAmount);
        Assert.Equal(vat, invoice.VatAmount);
        Assert.Equal(total, invoice.TotalAmount);
        Assert.Empty(invoice.Metadata);
        Assert.Equal(InvoiceStatus.Extracted, invoice.Status);
        Assert.False(invoice.ValidationReport.HasIssues);
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenSourceDocumentIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            Invoice.CreateExtracted(
                sourceDocumentId: Guid.Empty,
                vendor: null,
                invoiceNumber: "INV-1001",
                issueDate: null,
                subtotalAmount: null,
                vatAmount: null,
                totalAmount: null));
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMissingExtractedFields()
    {
        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: null,
            invoiceNumber: null,
            issueDate: null,
            subtotalAmount: null,
            vatAmount: null,
            totalAmount: null);

        Assert.Null(invoice.Vendor);
        Assert.Null(invoice.InvoiceNumber);
        Assert.Null(invoice.IssueDate);
        Assert.Null(invoice.SubtotalAmount);
        Assert.Null(invoice.VatAmount);
        Assert.Null(invoice.TotalAmount);
        Assert.Empty(invoice.Metadata);
        Assert.Equal(InvoiceStatus.Extracted, invoice.Status);
    }

    [Fact]
    public void CreateExtracted_ShouldNormalizeInvoiceNumber()
    {
        var invoice = CreateExtractedInvoice(
            invoiceNumber: "  INV-1001  ");

        Assert.Equal("INV-1001", invoice.InvoiceNumber);
    }

    [Fact]
    public void CreateExtracted_ShouldNormalizeBlankInvoiceNumberToNull()
    {
        var invoice = CreateExtractedInvoice(
            invoiceNumber: "   ");

        Assert.Null(invoice.InvoiceNumber);
    }

    [Fact]
    public void CreateExtracted_ShouldAllowInvoiceNumberAtMaximumLength()
    {
        var invoiceNumber = new string('A', 100);

        var invoice = CreateExtractedInvoice(
            invoiceNumber: invoiceNumber);

        Assert.Equal(invoiceNumber, invoice.InvoiceNumber);
    }

    [Fact]
    public void CreateExtracted_ShouldAllowInvoiceNumberAtMaximumLengthAfterTrim()
    {
        var invoiceNumber = new string('A', 100);

        var invoice = CreateExtractedInvoice(
            invoiceNumber: $" {invoiceNumber} ");

        Assert.Equal(invoiceNumber, invoice.InvoiceNumber);
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenInvoiceNumberIsTooLong()
    {
        var invoiceNumber = new string('A', 101);

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(invoiceNumber: invoiceNumber));
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenTrimmedInvoiceNumberIsTooLong()
    {
        var invoiceNumber = new string('A', 101);

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(invoiceNumber: $" {invoiceNumber} "));
    }

    [Fact]
    public void ApplyValidationReport_ShouldMarkInvoiceAsVerified_WhenReportHasNoErrors()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice();
        var report = InvoiceValidationReport.Valid();

        invoice.ApplyValidationReport(report);

        Assert.Equal(InvoiceStatus.Verified, invoice.Status);
        Assert.Equal(report, invoice.ValidationReport);
    }

    [Fact]
    public void ApplyValidationReport_ShouldMarkInvoiceAsVerified_WhenReportHasWarningsOnly()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice();

        var issue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE_FIELD",
            "InvoiceNumber",
            "Invoice number was extracted with low confidence.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        invoice.ApplyValidationReport(report);

        Assert.Equal(InvoiceStatus.Verified, invoice.Status);
        Assert.Equal(report, invoice.ValidationReport);
    }

    [Fact]
    public void ApplyValidationReport_ShouldMarkInvoiceAsRequiresHumanReview_WhenReportHasError()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice();

        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        invoice.ApplyValidationReport(report);

        Assert.Equal(InvoiceStatus.RequiresHumanReview, invoice.Status);
        Assert.Equal(report, invoice.ValidationReport);
    }

    [Fact]
    public void ApplyValidationReport_ShouldThrow_WhenReportIsNull()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice();

        Assert.Throws<ArgumentNullException>(() =>
            invoice.ApplyValidationReport(null!));
    }

    [Fact]
    public void ApplyValidationReport_ShouldNotChangeExtractedInvoiceData()
    {
        var sourceDocumentId = Guid.NewGuid();
        var vendor = new Vendor("Cohen Office Supplies Ltd", "516789123");
        var issueDate = new DateOnly(2026, 4, 30);
        var subtotal = new CurrencyAmount(1000, "ILS");
        var vat = new CurrencyAmount(180, "ILS");
        var total = new CurrencyAmount(1180, "ILS");

        var invoice = Invoice.CreateExtracted(
            sourceDocumentId,
            vendor,
            "INV-1001",
            issueDate,
            subtotal,
            vat,
            total,
            new Dictionary<string, string>
            {
                ["ProjectCode"] = "P-450"
            });

        var originalInvoiceId = invoice.Id;

        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        invoice.ApplyValidationReport(report);

        Assert.Equal(originalInvoiceId, invoice.Id);
        Assert.Equal(sourceDocumentId, invoice.SourceDocumentId);
        Assert.Equal(vendor, invoice.Vendor);
        Assert.Equal("INV-1001", invoice.InvoiceNumber);
        Assert.Equal(issueDate, invoice.IssueDate);
        Assert.Equal(subtotal, invoice.SubtotalAmount);
        Assert.Equal(vat, invoice.VatAmount);
        Assert.Equal(total, invoice.TotalAmount);
        Assert.Equal("P-450", invoice.Metadata["ProjectCode"]);
        Assert.Equal(InvoiceStatus.RequiresHumanReview, invoice.Status);
        Assert.Equal(report, invoice.ValidationReport);
    }

    [Fact]
    public void CreateExtracted_ShouldCreateEmptyMetadata_WhenMetadataIsNull()
    {
        var invoice = CreateExtractedInvoice(
            metadata: null);

        Assert.Empty(invoice.Metadata);
    }

    [Fact]
    public void CreateExtracted_ShouldCreateEmptyMetadata_WhenMetadataIsEmpty()
    {
        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>());

        Assert.Empty(invoice.Metadata);
    }

    [Fact]
    public void CreateExtracted_ShouldStoreMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["ProjectCode"] = "P-450",
            ["DocumentLanguage"] = "he"
        };

        var invoice = CreateExtractedInvoice(
            metadata: metadata);

        Assert.Equal("P-450", invoice.Metadata["ProjectCode"]);
        Assert.Equal("he", invoice.Metadata["DocumentLanguage"]);
    }

    [Fact]
    public void CreateExtracted_ShouldNormalizeMetadataKeysAndValues()
    {
        var metadata = new Dictionary<string, string>
        {
            [" ProjectCode "] = " P-450 "
        };

        var invoice = CreateExtractedInvoice(
            metadata: metadata);

        Assert.True(invoice.Metadata.ContainsKey("ProjectCode"));
        Assert.Equal("P-450", invoice.Metadata["ProjectCode"]);
    }

    [Fact]
    public void CreateExtracted_ShouldNotBeAffected_WhenOriginalMetadataChanges()
    {
        var metadata = new Dictionary<string, string>
        {
            ["ProjectCode"] = "P-450"
        };

        var invoice = CreateExtractedInvoice(
            metadata: metadata);

        metadata["ProjectCode"] = "Changed";
        metadata["DocumentLanguage"] = "he";

        Assert.Single(invoice.Metadata);
        Assert.Equal("P-450", invoice.Metadata["ProjectCode"]);
        Assert.DoesNotContain("DocumentLanguage", invoice.Metadata.Keys);
    }

    [Fact]
    public void Metadata_ShouldNotAllowMutationThroughDictionaryInterface()
    {
        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>
            {
                ["ProjectCode"] = "P-450"
            });

        var metadata = Assert.IsAssignableFrom<IDictionary<string, string>>(
            invoice.Metadata);

        Assert.True(metadata.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            metadata.Add("DocumentLanguage", "he"));

        Assert.Throws<NotSupportedException>(() =>
        {
            metadata["ProjectCode"] = "Changed";
        });

        Assert.Equal("P-450", invoice.Metadata["ProjectCode"]);
        Assert.DoesNotContain("DocumentLanguage", invoice.Metadata.Keys);
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenMetadataKeyIsEmpty()
    {
        var metadata = new Dictionary<string, string>
        {
            [" "] = "P-450"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenMetadataValueIsNull()
    {
        var metadata = new Dictionary<string, string>
        {
            ["ProjectCode"] = null!
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMetadataKeyAtMaximumLength()
    {
        var key = new string('A', 100);

        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>
            {
                [key] = "P-450"
            });

        Assert.Equal("P-450", invoice.Metadata[key]);
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMetadataKeyAtMaximumLengthAfterTrim()
    {
        var key = new string('A', 100);

        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>
            {
                [$" {key} "] = "P-450"
            });

        Assert.Equal("P-450", invoice.Metadata[key]);
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenMetadataKeyIsTooLong()
    {
        var key = new string('A', 101);

        var metadata = new Dictionary<string, string>
        {
            [key] = "P-450"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenTrimmedMetadataKeyIsTooLong()
    {
        var key = new string('A', 101);

        var metadata = new Dictionary<string, string>
        {
            [$" {key} "] = "P-450"
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMetadataValueAtMaximumLength()
    {
        var value = new string('A', 500);

        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>
            {
                ["Description"] = value
            });

        Assert.Equal(value, invoice.Metadata["Description"]);
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMetadataValueAtMaximumLengthAfterTrim()
    {
        var value = new string('A', 500);

        var invoice = CreateExtractedInvoice(
            metadata: new Dictionary<string, string>
            {
                ["Description"] = $" {value} "
            });

        Assert.Equal(value, invoice.Metadata["Description"]);
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenMetadataValueIsTooLong()
    {
        var value = new string('A', 501);

        var metadata = new Dictionary<string, string>
        {
            ["Description"] = value
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldThrow_WhenTrimmedMetadataValueIsTooLong()
    {
        var value = new string('A', 501);

        var metadata = new Dictionary<string, string>
        {
            ["Description"] = $" {value} "
        };

        Assert.Throws<ArgumentException>(() =>
            CreateExtractedInvoice(metadata: metadata));
    }

    [Fact]
    public void CreateExtracted_ShouldAllowMixedCurrenciesUntilValidationStage()
    {
        var subtotal = new CurrencyAmount(100, "USD");
        var vat = new CurrencyAmount(17, "ILS");
        var total = new CurrencyAmount(117, "USD");

        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: null,
            invoiceNumber: "INV-1001",
            issueDate: null,
            subtotalAmount: subtotal,
            vatAmount: vat,
            totalAmount: total);

        Assert.Equal(subtotal, invoice.SubtotalAmount);
        Assert.Equal(vat, invoice.VatAmount);
        Assert.Equal(total, invoice.TotalAmount);
        Assert.Equal(InvoiceStatus.Extracted, invoice.Status);
    }

    private static Invoice CreateExtractedInvoice(
        string? invoiceNumber = "INV-1001",
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: null,
            invoiceNumber: invoiceNumber,
            issueDate: null,
            subtotalAmount: null,
            vatAmount: null,
            totalAmount: null,
            metadata: metadata);
    }
}