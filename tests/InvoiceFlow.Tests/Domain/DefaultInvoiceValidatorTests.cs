using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Tests.Domain;

public sealed class DefaultInvoiceValidatorTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void Validate_ShouldReturnValidReport_WhenInvoiceIsValid()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(issueDate: ValidationDate);
        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.False(report.HasIssues);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnMissingVendorIssue_WhenVendorIsMissing()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: null,
            invoiceNumber: "INV-1001",
            issueDate: ValidationDate,
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "MISSING_VENDOR"
            && issue.FieldName == "Vendor"
            && issue.Severity == InvoiceValidationSeverity.Error);

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnMissingInvoiceNumberIssue_WhenInvoiceNumberIsMissing()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: null,
            issueDate: ValidationDate,
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "MISSING_INVOICE_NUMBER"
            && issue.FieldName == "InvoiceNumber");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnFutureIssueDateIssue_WhenIssueDateIsInTheFuture()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            issueDate: ValidationDate.AddDays(1));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "FUTURE_ISSUE_DATE"
            && issue.FieldName == "IssueDate");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnMissingAmountIssues_WhenAmountsAreMissing()
    {
        var invoice = TestInvoiceFactory.CreateExtractedInvoice(
            vendor: new Vendor("Cohen Office Supplies Ltd", "516789123"),
            invoiceNumber: "INV-1001",
            issueDate: ValidationDate,
            subtotalAmount: null,
            vatAmount: null,
            totalAmount: null);

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue => issue.Code == "MISSING_SUBTOTAL_AMOUNT");
        Assert.Contains(report.Issues, issue => issue.Code == "MISSING_VAT_AMOUNT");
        Assert.Contains(report.Issues, issue => issue.Code == "MISSING_TOTAL_AMOUNT");
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldReturnCurrencyMismatchIssue_WhenAmountsUseDifferentCurrencies()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "USD"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "USD"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "CURRENCY_MISMATCH"
            && issue.FieldName == "Currency");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldNotReturnTotalMismatch_WhenCurrenciesAreDifferent()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "USD"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "USD"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == "TOTAL_MISMATCH");
    }

    [Fact]
    public void Validate_ShouldReturnTotalMismatchIssue_WhenSubtotalPlusVatDoesNotMatchTotal()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(170, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "TOTAL_MISMATCH"
            && issue.FieldName == "TotalAmount");

        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void Validate_ShouldAllowSmallRoundingDifference()
    {
        var invoice = TestInvoiceFactory.CreateValidInvoice(
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180.009m, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var validator = new DefaultInvoiceValidator(ValidationDate);

        var report = validator.Validate(invoice);

        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == "TOTAL_MISMATCH");
    }

    [Fact]
    public void Validate_ShouldThrow_WhenInvoiceIsNull()
    {
        var validator = new DefaultInvoiceValidator(ValidationDate);

        Assert.Throws<ArgumentNullException>(() =>
            validator.Validate(null!));
    }
}
