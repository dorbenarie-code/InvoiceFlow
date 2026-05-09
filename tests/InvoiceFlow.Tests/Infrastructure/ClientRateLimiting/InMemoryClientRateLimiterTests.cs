using InvoiceFlow.Infrastructure.ClientRateLimiting;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.ClientRateLimiting;

public sealed class InMemoryClientRateLimiterTests
{
    private const string InvoiceProcessResource = "/api/invoices/process";
    private const string OtherResource = "/api/invoices/other";

    private static readonly Guid ClientA =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static readonly Guid ClientB =
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task AcquireAsync_ShouldAllowRequestsWithinPermitLimit()
    {
        var limiter = CreateLimiter(
            permitLimit: 2);

        var firstResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        var secondResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        Assert.True(firstResult.IsAllowed);
        Assert.False(firstResult.IsExceeded);

        Assert.True(secondResult.IsAllowed);
        Assert.False(secondResult.IsExceeded);
    }

    [Fact]
    public async Task AcquireAsync_ShouldReturnExceeded_WhenPermitLimitIsExceeded()
    {
        var limiter = CreateLimiter(
            permitLimit: 1);

        var firstResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        var secondResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        Assert.True(firstResult.IsAllowed);

        Assert.False(secondResult.IsAllowed);
        Assert.True(secondResult.IsExceeded);
    }

    [Fact]
    public async Task AcquireAsync_ShouldTrackEachClientSeparately()
    {
        var limiter = CreateLimiter(
            permitLimit: 1);

        var firstClientResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        var secondClientResult = await limiter.AcquireAsync(
            ClientB,
            InvoiceProcessResource);

        Assert.True(firstClientResult.IsAllowed);
        Assert.True(secondClientResult.IsAllowed);
    }

    [Fact]
    public async Task AcquireAsync_ShouldTrackEachResourceSeparately()
    {
        var limiter = CreateLimiter(
            permitLimit: 1);

        var firstResourceResult = await limiter.AcquireAsync(
            ClientA,
            InvoiceProcessResource);

        var secondResourceResult = await limiter.AcquireAsync(
            ClientA,
            OtherResource);

        Assert.True(firstResourceResult.IsAllowed);
        Assert.True(secondResourceResult.IsAllowed);
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrow_WhenClientIdIsEmpty()
    {
        var limiter = CreateLimiter();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            limiter.AcquireAsync(
                Guid.Empty,
                InvoiceProcessResource));

        Assert.Equal("clientId", exception.ParamName);
        Assert.Contains(
            "Client id is required.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AcquireAsync_ShouldThrow_WhenResourceIsMissing(
        string resource)
    {
        var limiter = CreateLimiter();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            limiter.AcquireAsync(
                ClientA,
                resource));

        Assert.Equal("resource", exception.ParamName);
        Assert.Contains(
            "Rate limit resource is required.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenOptionsAreNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new InMemoryClientRateLimiter(null!));

        Assert.Equal("options", exception.ParamName);
    }

    private static InMemoryClientRateLimiter CreateLimiter(
        int permitLimit = 5,
        TimeSpan? window = null)
    {
        return new InMemoryClientRateLimiter(
            Options.Create(
                new ClientRateLimitOptions
                {
                    PermitLimit = permitLimit,
                    Window = window ?? TimeSpan.FromMinutes(1)
                }));
    }
}
