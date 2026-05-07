using System;
using System.Collections.Generic;
using InvoiceFlow.Domain.Invoices;
using Xunit;

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
    public void FromIssues_ShouldCreateReportWithoutIssues_WhenIssuesCollectionIsEmpty()
    {
        var report = InvoiceValidationReport.FromIssues([]);

        Assert.Empty(report.Issues);
        Assert.False(report.HasIssues);
        Assert.False(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void FromIssues_ShouldCreateReportWithIssues()
    {
        var issue = CreateErrorIssue();

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.Single(report.Issues);
        Assert.Contains(issue, report.Issues);
        Assert.True(report.HasIssues);
    }

    [Fact]
    public void FromIssues_ShouldSetHasErrorsAndRequiresHumanReview_WhenReportContainsError()
    {
        var issue = CreateErrorIssue();

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.True(report.HasIssues);
        Assert.True(report.HasErrors);
        Assert.False(report.HasWarnings);
        Assert.True(report.RequiresHumanReview);
    }

    [Fact]
    public void FromIssues_ShouldSetHasWarningsWithoutHumanReview_WhenReportContainsWarningOnly()
    {
        var issue = CreateWarningIssue();

        var report = InvoiceValidationReport.FromIssues([issue]);

        Assert.True(report.HasIssues);
        Assert.False(report.HasErrors);
        Assert.True(report.HasWarnings);
        Assert.False(report.RequiresHumanReview);
    }

    [Fact]
    public void FromIssues_ShouldSetErrorsAndWarnings_WhenReportContainsBoth()
    {
        var warning = CreateWarningIssue();
        var error = CreateErrorIssue();

        var report = InvoiceValidationReport.FromIssues([warning, error]);

        Assert.Equal(2, report.Issues.Count);
        Assert.True(report.HasIssues);
        Assert.True(report.HasErrors);
        Assert.True(report.HasWarnings);
        Assert.True(report.RequiresHumanReview);
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
        var issues = new List<InvoiceValidationIssue>
        {
            null!
        };

        Assert.Throws<ArgumentException>(() =>
            InvoiceValidationReport.FromIssues(issues));
    }

    [Fact]
    public void Issues_ShouldNotBeAffected_WhenOriginalCollectionChanges()
    {
        var issue = CreateWarningIssue();

        var issues = new List<InvoiceValidationIssue>
        {
            issue
        };

        var report = InvoiceValidationReport.FromIssues(issues);

        issues.Clear();

        Assert.Single(report.Issues);
        Assert.Contains(issue, report.Issues);
    }

    [Fact]
    public void Issues_ShouldNotAllowMutationThroughCollectionInterface()
    {
        var report = InvoiceValidationReport.FromIssues(
        [
            CreateWarningIssue()
        ]);

        var issues = Assert.IsAssignableFrom<ICollection<InvoiceValidationIssue>>(
            report.Issues);

        Assert.True(issues.IsReadOnly);

        Assert.Throws<NotSupportedException>(() =>
            issues.Add(CreateErrorIssue()));

        Assert.Single(report.Issues);
        Assert.DoesNotContain(report.Issues, issue =>
            issue.Code == "TOTAL_MISMATCH");
    }

    private static InvoiceValidationIssue CreateErrorIssue()
    {
        return InvoiceValidationIssue.Error(
            "TOTAL_MISMATCH",
            "TotalAmount",
            "Subtotal + VAT does not match total amount.");
    }

    private static InvoiceValidationIssue CreateWarningIssue()
    {
        return InvoiceValidationIssue.Warning(
            "LOW_CONFIDENCE_FIELD",
            "InvoiceNumber",
            "Invoice number was extracted with low confidence.");
    }
}