using System.Threading.RateLimiting;
using InvoiceFlow.Application.ClientRateLimiting;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.ClientRateLimiting;

public sealed class InMemoryClientRateLimiter
    : IClientRateLimiter, IAsyncDisposable
{
    private readonly ClientRateLimitOptions _options;

    private readonly PartitionedRateLimiter<string> _limiter;

    public InMemoryClientRateLimiter(
        IOptions<ClientRateLimitOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value
            ?? throw new ArgumentException(
                "Client rate limit options are required.",
                nameof(options));

        if (_options.PermitLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.PermitLimit,
                "Client rate limit permit limit must be greater than zero.");
        }

        if (_options.Window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                _options.Window,
                "Client rate limit window must be greater than zero.");
        }

        _limiter = PartitionedRateLimiter.Create<string, string>(
            partitionKey =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = _options.PermitLimit,
                        Window = _options.Window,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    }));
    }

    public async Task<ClientRateLimitResult> AcquireAsync(
        Guid clientId,
        string resource,
        CancellationToken cancellationToken = default)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException(
                "Client id is required.",
                nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException(
                "Rate limit resource is required.",
                nameof(resource));
        }

        cancellationToken.ThrowIfCancellationRequested();

        var partitionKey = CreatePartitionKey(
            clientId,
            resource);

        using var lease = await _limiter.AcquireAsync(
            partitionKey,
            permitCount: 1,
            cancellationToken);

        return lease.IsAcquired
            ? ClientRateLimitResult.Allowed()
            : ClientRateLimitResult.Exceeded();
    }

    public async ValueTask DisposeAsync()
    {
        await _limiter.DisposeAsync();
    }

    private static string CreatePartitionKey(
        Guid clientId,
        string resource)
    {
        return $"{clientId:N}:{resource.Trim()}";
    }
}
