using InvoiceFlow.Application.ClientRateLimiting;

namespace InvoiceFlow.Tests.Application.ClientRateLimiting;

public sealed class ClientRateLimitResultTests
{
    [Fact]
    public void Allowed_ShouldReturnAllowedResult()
    {
        var result = ClientRateLimitResult.Allowed();

        Assert.True(result.IsAllowed);
        Assert.False(result.IsExceeded);
    }

    [Fact]
    public void Exceeded_ShouldReturnExceededResult()
    {
        var result = ClientRateLimitResult.Exceeded();

        Assert.False(result.IsAllowed);
        Assert.True(result.IsExceeded);
    }
}
