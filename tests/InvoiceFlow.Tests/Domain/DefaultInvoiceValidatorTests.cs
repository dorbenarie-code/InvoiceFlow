using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Domain;

public sealed class DefaultInvoiceValidatorTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void Validate_ShouldReturnValidReport_WhenInvoiceIsValid()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            issueDate: ValidationDate);

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        Assert.Empty(report.Issues);
        Assert.False(report.HasIssues);
        Assert.False(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldThrow_WhenInvoiceIsNull()
    {
        var validator = CreateValidator();

        Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(null!));
    }

    [Fact]
    public void Validate_ShouldReturnAllMissingRequiredFieldIssues_WhenAllRequiredFieldsAreMissing()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice();
        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        Assert.Equal(6, report.Issues.Count);

        AssertContainsErrorIssue(
            report,
            "MISSING_VENDOR",
            "Vendor",
            "Vendor is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_INVOICE_NUMBER",
            "InvoiceNumber",
            "Invoice number is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_ISSUE_DATE",
            "IssueDate",
            "Issue date is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_SUBTOTAL_AMOUNT",
            "SubtotalAmount",
            "Subtotal amount is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_VAT_AMOUNT",
            "VatAmount",
            "VAT amount is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_TOTAL_AMOUNT",
            "TotalAmount",
            "Total amount is required.");

        Assert.True(report.HasIssues);
        Assert.True(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnFutureIssueDateIssue_WhenIssueDateIsAfterValidationDate()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            issueDate: ValidationDate.AddDays(1));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "FUTURE_ISSUE_DATE",
            "IssueDate",
            "Issue date cannot be in the future.");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldAllowIssueDateEqualToValidationDate()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            issueDate: ValidationDate);

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertDoesNotContainIssue(report, "FUTURE_ISSUE_DATE");
        Assert.False(report.HasIssues);
    }

    [Fact]
    public void Validate_ShouldAllowIssueDateBeforeValidationDate()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            issueDate: ValidationDate.AddDays(-1));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertDoesNotContainIssue(report, "FUTURE_ISSUE_DATE");
        Assert.False(report.HasIssues);
    }

    [Fact]
    public void Validate_ShouldReturnCurrencyMismatchIssue_WhenAmountsUseDifferentCurrencies()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "USD"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "USD"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "CURRENCY_MISMATCH",
            "Currency",
            "Invoice amounts must use the same currency.");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnCurrencyMismatch_WhenOnlyAvailableAmountsUseDifferentCurrencies()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: ValidationDate,
            subtotalAmount: null,
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "USD"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "MISSING_SUBTOTAL_AMOUNT",
            "SubtotalAmount",
            "Subtotal amount is required.");

        AssertContainsErrorIssue(
            report,
            "CURRENCY_MISMATCH",
            "Currency",
            "Invoice amounts must use the same currency.");

        AssertDoesNotContainIssue(report, "TOTAL_MISMATCH");
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldNotReturnCurrencyMismatch_WhenOnlyOneAmountExists()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: ValidationDate,
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: null,
            totalAmount: null);

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertDoesNotContainIssue(report, "CURRENCY_MISMATCH");
    }

    [Fact]
    public void Validate_ShouldNotReturnTotalMismatch_WhenCurrenciesAreDifferent()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "USD"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "USD"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "CURRENCY_MISMATCH",
            "Currency",
            "Invoice amounts must use the same currency.");

        AssertDoesNotContainIssue(report, "TOTAL_MISMATCH");
    }

    [Fact]
    public void Validate_ShouldReturnTotalMismatchIssue_WhenSubtotalPlusVatDoesNotMatchTotal()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(170, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal amount plus VAT amount must match total amount.");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldAllowSmallRoundingDifference()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180.009m, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertDoesNotContainIssue(report, "TOTAL_MISMATCH");
        Assert.False(report.HasIssues);
    }

    [Fact]
    public void Validate_ShouldAllowTotalDifferenceEqualToTolerance()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180.01m, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertDoesNotContainIssue(report, "TOTAL_MISMATCH");
        Assert.False(report.HasIssues);
    }

    [Fact]
    public void Validate_ShouldReturnTotalMismatch_WhenTotalDifferenceIsJustAboveTolerance()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180.011m, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal amount plus VAT amount must match total amount.");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldNotReturnTotalMismatch_WhenAnyAmountIsMissing()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: ValidationDate,
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: null);

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "MISSING_TOTAL_AMOUNT",
            "TotalAmount",
            "Total amount is required.");

        AssertDoesNotContainIssue(report, "TOTAL_MISMATCH");
    }

    [Fact]
    public void Validate_ShouldReturnMultipleIssues_WhenInvoiceHasMultipleBusinessProblems()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: null,
            invoiceNumber: null,
            issueDate: ValidationDate.AddDays(1),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(100, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        AssertContainsErrorIssue(
            report,
            "MISSING_VENDOR",
            "Vendor",
            "Vendor is required.");

        AssertContainsErrorIssue(
            report,
            "MISSING_INVOICE_NUMBER",
            "InvoiceNumber",
            "Invoice number is required.");

        AssertContainsErrorIssue(
            report,
            "FUTURE_ISSUE_DATE",
            "IssueDate",
            "Issue date cannot be in the future.");

        AssertContainsErrorIssue(
            report,
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal amount plus VAT amount must match total amount.");

        Assert.True(report.HasIssues);
        Assert.True(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldNotApplyValidationReportToInvoice()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(100, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = CreateValidator();

        var report = validator.Validate(invoice);

        Assert.True(report.HasIssues);
        Assert.Equal(InvoiceStatus.Extracted, invoice.Status);
        Assert.False(invoice.ValidationReport.HasIssues);
    }

    private static DefaultInvoiceValidator CreateValidator()
    {
        return new DefaultInvoiceValidator(ValidationDate);
    }

    private static void AssertContainsErrorIssue(
        InvoiceValidationReport report,
        string code,
        string fieldName,
        string message)
    {
        Assert.Contains(report.Issues, issue =>
            issue.Code == code
            && issue.FieldName == fieldName
            && issue.Message == message
            && issue.Severity == InvoiceValidationSeverity.Error);
    }

    private static void AssertDoesNotContainIssue(
        InvoiceValidationReport report,
        string code)
    {
        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == code);
    }
}