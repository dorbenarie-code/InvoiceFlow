namespace InvoiceFlow.Application.ClientRateLimiting;

public sealed record ClientRateLimitResult
{
    public bool IsAllowed { get; }

    public bool IsExceeded => !IsAllowed;

    private ClientRateLimitResult(
        bool isAllowed)
    {
        IsAllowed = isAllowed;
    }

    public static ClientRateLimitResult Allowed()
    {
        return new ClientRateLimitResult(
            true);
    }

    public static ClientRateLimitResult Exceeded()
    {
        return new ClientRateLimitResult(
            false);
    }
}
