using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Tests.Domain;

public sealed class InvoiceValidationReportTests
{
    [Fact]
    public void Valid_ShouldCreateReportWithoutIssues()
    {
        var report = InvoiceValidationReport.Valid();

        Assert.Empty(report.Issues);
        Assert.False(report.HasIssues);
        Assert.False(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void FromIssues_ShouldCreateReportWithIssues()
    {
        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.Single(report.Issues);
        Assert.True(report.HasIssues);
    }

    [Fact]
    public void HasErrors_ShouldReturnTrue_WhenReportContainsError()
    {
        var issue = InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.True(report.HasErrors);
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void HasWarnings_ShouldReturnTrue_WhenReportContainsWarning()
    {
        var issue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE_FIELD",
            "InvoiceNumber",
            "Invoice number was extracted with low confidence.");

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.True(report.HasWarnings);
        Assert.False(report.HasErrors);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void FromIssues_ShouldThrow_WhenIssuesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            InvoiceValidationReport.FromIssues(null!));
    }

    [Fact]
    public void FromIssues_ShouldThrow_WhenIssuesContainsNull()
    {
        InvoiceValidationIssue? nullIssue = null;

        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationReport.FromIssues([nullIssue!]));
    }

    [Fact]
    public void Issues_ShouldNotBeAffected_WhenOriginalCollectionChanges()
    {
        var issue = InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE_FIELD",
            "InvoiceNumber",
            "Invoice number was extracted with low confidence.");

        var issues = new List<InvoiceValidationIssue> { issue };

        var report = InvoiceValidationReport.FromIssues(issues);

        issues.Clear();

        Assert.Single(report.Issues);
    }
}