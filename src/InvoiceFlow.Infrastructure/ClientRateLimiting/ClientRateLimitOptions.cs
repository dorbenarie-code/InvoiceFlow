namespace InvoiceFlow.Infrastructure.ClientRateLimiting;

public sealed class ClientRateLimitOptions
{
    public const string ConfigurationSectionName =
        "InvoiceFlow:ClientRateLimiting";

    public int PermitLimit { get; set; } = 5;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
