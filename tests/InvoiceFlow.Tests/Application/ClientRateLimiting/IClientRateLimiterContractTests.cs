using InvoiceFlow.Application.ClientRateLimiting;

namespace InvoiceFlow.Tests.Application.ClientRateLimiting;

public sealed class IClientRateLimiterContractTests
{
    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private const string Resource = "/api/invoices/process";

    [Fact]
    public async Task AcquireAsync_ShouldAcceptClientIdResourceAndCancellationToken()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await limiter.AcquireAsync(
            ClientId,
            Resource,
            cancellationTokenSource.Token);

        Assert.True(result.IsAllowed);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(Resource, limiter.ReceivedResource);
        Assert.Equal(
            cancellationTokenSource.Token,
            limiter.ReceivedCancellationToken);
    }

    private sealed class CapturingClientRateLimiter
        : IClientRateLimiter
    {
        private readonly ClientRateLimitResult _result;

        public Guid ReceivedClientId { get; private set; }

        public string? ReceivedResource { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public CapturingClientRateLimiter(
            ClientRateLimitResult result)
        {
            _result = result
                ?? throw new ArgumentNullException(nameof(result));
        }

        public Task<ClientRateLimitResult> AcquireAsync(
            Guid clientId,
            string resource,
            CancellationToken cancellationToken = default)
        {
            ReceivedClientId = clientId;
            ReceivedResource = resource;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }
}
