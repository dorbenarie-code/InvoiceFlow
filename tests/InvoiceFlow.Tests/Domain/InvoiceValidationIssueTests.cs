using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Tests.Domain;

public sealed class InvoiceValidationIssueTests
{
    [Fact]
    public void Error_ShouldNormalizeCodeFieldNameAndMessage()
    {
        var issue = InvoiceValidationIssue.Error(
            " total_mismatch ",
            " TotalAmount ",
            " Subtotal + VAT does not match total. ");

        Assert.Equal("TOTAL_MISMATCH", issue.Code);
        Assert.Equal("TotalAmount", issue.FieldName);
        Assert.Equal("Subtotal + VAT does not match total.", issue.Message);
        Assert.Equal(InvoiceValidationSeverity.Error, issue.Severity);
    }

    [Fact]
    public void Warning_ShouldNormalizeCodeFieldNameAndMessage()
    {
        var issue = InvoiceValidationIssue.Warning(
            " low_confidence_field ",
            " InvoiceNumber ",
            " Invoice number was extracted with low confidence. ");

        Assert.Equal("LOW_CONFIDENCE_FIELD", issue.Code);
        Assert.Equal("InvoiceNumber", issue.FieldName);
        Assert.Equal("Invoice number was extracted with low confidence.", issue.Message);
        Assert.Equal(InvoiceValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Error_ShouldAllowMissingFieldName()
    {
        var issue = InvoiceValidationIssue.Error(
            "DUPLICATE_INVOICE",
            null,
            "Invoice already exists for this vendor.");

        Assert.Null(issue.FieldName);
    }

    [Fact]
    public void Error_ShouldThrow_WhenCodeIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "",
                "TotalAmount",
                "Invalid total amount."));
    }

    [Fact]
    public void Error_ShouldThrow_WhenCodeContainsUnsupportedCharacters()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL-MISMATCH",
                "TotalAmount",
                "Invalid total amount."));
    }

    [Fact]
    public void Error_ShouldThrow_WhenMessageIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                "TotalAmount",
                ""));
    }

    [Fact]
    public void Warning_ShouldCreateWarningIssue()
    {
        var issue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE_FIELD",
            "InvoiceNumber",
            "Invoice number was extracted with low confidence.");

        Assert.Equal("LOW_CONFIDENCE_FIELD", issue.Code);
        Assert.Equal("InvoiceNumber", issue.FieldName);
        Assert.Equal(InvoiceValidationSeverity.Warning, issue.Severity);
    }

    [Fact]
    public void Error_ShouldCreateErrorIssue()
    {
        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        Assert.Equal("TOTAL_MISMATCH", issue.Code);
        Assert.Equal("TotalAmount", issue.FieldName);
        Assert.Equal(InvoiceValidationSeverity.Error, issue.Severity);
    }
}