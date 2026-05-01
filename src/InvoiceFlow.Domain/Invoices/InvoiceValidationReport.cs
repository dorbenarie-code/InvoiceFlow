namespace InvoiceFlow.Domain.Invoices;

public sealed record InvoiceValidationReport
{
    public IReadOnlyCollection<InvoiceValidationIssue> Issues { get; }

    public bool HasIssues { get; }

    public bool HasErrors { get; }

    public bool HasWarnings { get; }

    public bool RequiresHumanReview => HasErrors;

    private InvoiceValidationReport(
        IReadOnlyCollection<InvoiceValidationIssue> issues)
    {
        Issues = issues;
        HasIssues = issues.Count > 0;
        HasErrors = issues.Any(issue =>
            issue.Severity == InvoiceValidationSeverity.Error);
        HasWarnings = issues.Any(issue =>
            issue.Severity == InvoiceValidationSeverity.Warning);
    }

    public static InvoiceValidationReport Valid()
    {
        return new InvoiceValidationReport(
            Array.Empty<InvoiceValidationIssue>());
    }

    public static InvoiceValidationReport FromIssues(
        IEnumerable<InvoiceValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var issueList = issues.ToList();

        if (issueList.Any(issue => issue is null))
        {
            throw new ArgumentException(
                "Validation report cannot contain null issues.",
                nameof(issues));
        }

        return new InvoiceValidationReport(issueList.AsReadOnly());
    }
}