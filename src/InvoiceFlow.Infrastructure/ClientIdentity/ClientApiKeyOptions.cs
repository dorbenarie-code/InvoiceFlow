namespace InvoiceFlow.Infrastructure.ClientIdentity;

public sealed class ClientApiKeyOptions
{
    public Guid ClientId { get; set; }

    public string KeyPrefix { get; set; } = string.Empty;

    public string KeyHash { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;
}
