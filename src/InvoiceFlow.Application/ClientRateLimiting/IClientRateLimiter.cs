namespace InvoiceFlow.Application.ClientRateLimiting;

public interface IClientRateLimiter
{
    Task<ClientRateLimitResult> AcquireAsync(
        Guid clientId,
        string resource,
        CancellationToken cancellationToken = default);
}
