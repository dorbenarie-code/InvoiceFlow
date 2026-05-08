using InvoiceFlow.Api.ClientIdentity;
using Microsoft.AspNetCore.Http;

namespace InvoiceFlow.Tests.Api.ClientIdentity;

public sealed class HttpProcessingClientContextTests
{
    private const string ClientIdItemKey = "InvoiceFlow.ClientIdentity.ClientId";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void ClientId_ShouldReturnClientId_WhenHttpContextContainsClientId()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClientIdItemKey] = ClientId;

        var context = new HttpProcessingClientContext(
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        Assert.Equal(ClientId, context.ClientId);
    }

    [Fact]
    public void ClientId_ShouldThrow_WhenHttpContextIsMissing()
    {
        var context = new HttpProcessingClientContext(
            new HttpContextAccessor
            {
                HttpContext = null
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.ClientId);

        Assert.Contains(
            "HTTP context is required to resolve the processing client id.",
            exception.Message);
    }

    [Fact]
    public void ClientId_ShouldThrow_WhenClientIdWasNotResolved()
    {
        var context = new HttpProcessingClientContext(
            new HttpContextAccessor
            {
                HttpContext = new DefaultHttpContext()
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.ClientId);

        Assert.Contains(
            "Processing client id was not resolved for the current HTTP request.",
            exception.Message);
    }

    [Fact]
    public void ClientId_ShouldThrow_WhenResolvedClientIdIsEmpty()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClientIdItemKey] = Guid.Empty;

        var context = new HttpProcessingClientContext(
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.ClientId);

        Assert.Contains(
            "Processing client id was not resolved for the current HTTP request.",
            exception.Message);
    }

    [Fact]
    public void ClientId_ShouldThrow_WhenResolvedClientIdIsNotGuid()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Items[ClientIdItemKey] = "not-a-guid";

        var context = new HttpProcessingClientContext(
            new HttpContextAccessor
            {
                HttpContext = httpContext
            });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            context.ClientId);

        Assert.Contains(
            "Processing client id was not resolved for the current HTTP request.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenHttpContextAccessorIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new HttpProcessingClientContext(null!));

        Assert.Equal("httpContextAccessor", exception.ParamName);
    }
}
