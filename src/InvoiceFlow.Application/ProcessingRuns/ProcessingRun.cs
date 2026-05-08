namespace InvoiceFlow.Application.ProcessingRuns;

public sealed record ProcessingRun
{
    public Guid Id { get; }
    public Guid ClientId { get; }
    public Guid? DocumentId { get; }
    public Guid? InvoiceId { get; }
    public string Status { get; }
    public int? AnalyzedPageCount { get; }
    public long DurationMs { get; }
    public string? ErrorCode { get; }
    public DateTime CreatedAtUtc { get; }

    public ProcessingRun(
        Guid id,
        Guid clientId,
        Guid? documentId,
        Guid? invoiceId,
        string status,
        int? analyzedPageCount,
        long durationMs,
        string? errorCode,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Processing run id is required.",
                nameof(id));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException(
                "Processing run client id is required.",
                nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(status))
        {
            throw new ArgumentException(
                "Processing run status is required.",
                nameof(status));
        }

        if (analyzedPageCount.HasValue && analyzedPageCount.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(analyzedPageCount),
                analyzedPageCount,
                "Processing run analyzed page count must be greater than zero when provided.");
        }

        if (durationMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(durationMs),
                durationMs,
                "Processing run duration cannot be negative.");
        }

        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Processing run creation time must be UTC.",
                nameof(createdAtUtc));
        }

        Id = id;
        ClientId = clientId;
        DocumentId = documentId;
        InvoiceId = invoiceId;
        Status = status.Trim();
        AnalyzedPageCount = analyzedPageCount;
        DurationMs = durationMs;
        ErrorCode = NormalizeOptionalText(errorCode);
        CreatedAtUtc = createdAtUtc;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim();
    }
}
