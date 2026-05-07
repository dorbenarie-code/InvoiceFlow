using System;
using InvoiceFlow.Domain.Invoices;
using Xunit;

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
    public void Error_ShouldCreateErrorIssue()
    {
        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        Assert.Equal("TOTAL_MISMATCH", issue.Code);
        Assert.Equal("TotalAmount", issue.FieldName);
        Assert.Equal("Subtotal + VAT does not match total amount.", issue.Message);
        Assert.Equal(InvoiceValidationSeverity.Error, issue.Severity);
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
    public void Error_ShouldTreatWhitespaceFieldNameAsMissing()
    {
        var issue = InvoiceValidationIssue.Error(
            "DUPLICATE_INVOICE",
            "   ",
            "Invoice already exists for this vendor.");

        Assert.Null(issue.FieldName);
    }

    [Fact]
    public void Error_ShouldAllowCodeWithLettersDigitsAndUnderscores()
    {
        var issue = InvoiceValidationIssue.Error(
            "RULE_123",
            "TotalAmount",
            "Invalid total amount.");

        Assert.Equal("RULE_123", issue.Code);
    }

    [Fact]
    public void Error_ShouldAllowCodeAtMaximumLength()
    {
        var code = new string('A', 100);

        var issue = InvoiceValidationIssue.Error(
            code,
            "TotalAmount",
            "Invalid total amount.");

        Assert.Equal(code, issue.Code);
    }

    [Fact]
    public void Error_ShouldThrow_WhenCodeIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                null!,
                "TotalAmount",
                "Invalid total amount."));
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
    public void Error_ShouldThrow_WhenCodeIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "   ",
                "TotalAmount",
                "Invalid total amount."));
    }

    [Fact]
    public void Error_ShouldThrow_WhenCodeIsTooLong()
    {
        var code = new string('A', 101);

        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                code,
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
    public void Error_ShouldThrow_WhenCodeContainsNonAsciiLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "חשבונית",
                "TotalAmount",
                "Invalid total amount."));
    }

    [Fact]
    public void Error_ShouldAllowFieldNameAtMaximumLength()
    {
        var fieldName = new string('A', 100);

        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            fieldName,
            "Invalid total amount.");

        Assert.Equal(fieldName, issue.FieldName);
    }

    [Fact]
    public void Error_ShouldThrow_WhenFieldNameIsTooLong()
    {
        var fieldName = new string('A', 101);

        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                fieldName,
                "Invalid total amount."));
    }

    [Fact]
    public void Error_ShouldAllowMessageAtMaximumLength()
    {
        var message = new string('A', 500);

        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            message);

        Assert.Equal(message, issue.Message);
    }

    [Fact]
    public void Error_ShouldThrow_WhenMessageIsNull()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                "TotalAmount",
                null!));
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
    public void Error_ShouldThrow_WhenMessageIsWhitespace()
    {
        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                "TotalAmount",
                "   "));
    }

    [Fact]
    public void Error_ShouldThrow_WhenMessageIsTooLong()
    {
        var message = new string('A', 501);

        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationIssue.Error(
                "TOTAL_MISMATCH",
                "TotalAmount",
                message));
    }
}