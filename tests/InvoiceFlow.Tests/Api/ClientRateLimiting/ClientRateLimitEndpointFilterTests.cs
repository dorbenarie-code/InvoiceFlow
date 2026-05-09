using System.Text.Json;
using InvoiceFlow.Api.ClientRateLimiting;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientRateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Api.ClientRateLimiting;

public sealed class ClientRateLimitEndpointFilterTests
{
    private const string ClientIdItemKey = "InvoiceFlow.ClientIdentity.ClientId";
    private const string InvoiceProcessResource = "/api/invoices/process";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task InvokeAsync_ShouldCallNext_WhenRequestIsAllowed()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var filter = new ClientRateLimitEndpointFilter(limiter);

        var httpContext = CreateHttpContext();
        httpContext.Items[ClientIdItemKey] = ClientId;
        httpContext.Request.Path = InvoiceProcessResource;

        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        var expectedResult = new object();
        var nextWasCalled = false;

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>(expectedResult);
            });

        Assert.True(nextWasCalled);
        Assert.Same(expectedResult, result);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(InvoiceProcessResource, limiter.ReceivedResource);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnTooManyRequestsAndNotCallNext_WhenRateLimitIsExceeded()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Exceeded());

        var filter = new ClientRateLimitEndpointFilter(limiter);

        var httpContext = CreateHttpContext();
        httpContext.Items[ClientIdItemKey] = ClientId;
        httpContext.Request.Path = InvoiceProcessResource;

        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        var nextWasCalled = false;

        var result = await filter.InvokeAsync(
            invocationContext,
            _ =>
            {
                nextWasCalled = true;
                return ValueTask.FromResult<object?>("next-result");
            });

        var error = await ExecuteErrorResultAsync(result, httpContext);

        Assert.False(nextWasCalled);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(InvoiceProcessResource, limiter.ReceivedResource);
        Assert.Equal(StatusCodes.Status429TooManyRequests, httpContext.Response.StatusCode);
        Assert.Equal("RATE_LIMIT_EXCEEDED", error.Code);
        Assert.Equal("Rate limit exceeded. Please try again later.", error.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassRequestAbortedCancellationTokenToLimiter()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var filter = new ClientRateLimitEndpointFilter(limiter);

        using var cancellationTokenSource = new CancellationTokenSource();

        var httpContext = CreateHttpContext();
        httpContext.Items[ClientIdItemKey] = ClientId;
        httpContext.Request.Path = InvoiceProcessResource;
        httpContext.RequestAborted = cancellationTokenSource.Token;

        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>("next-result"));

        Assert.Equal(
            cancellationTokenSource.Token,
            limiter.ReceivedCancellationToken);
    }

    [Fact]
    public async Task InvokeAsync_ShouldThrow_WhenClientIdWasNotResolved()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var filter = new ClientRateLimitEndpointFilter(limiter);

        var httpContext = CreateHttpContext();
        httpContext.Request.Path = InvoiceProcessResource;

        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        var nextWasCalled = false;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(
                invocationContext,
                _ =>
                {
                    nextWasCalled = true;
                    return ValueTask.FromResult<object?>("next-result");
                }));

        Assert.False(nextWasCalled);
        Assert.Equal(Guid.Empty, limiter.ReceivedClientId);
        Assert.Contains(
            "Client id was not resolved for rate limiting.",
            exception.Message);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLimiterIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ClientRateLimitEndpointFilter(null!));

        Assert.Equal("limiter", exception.ParamName);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.Configure<JsonOptions>(_ => { });

        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response =
            {
                Body = new MemoryStream()
            }
        };
    }

    private static async Task<ApiErrorResponse> ExecuteErrorResultAsync(
        object? result,
        HttpContext httpContext)
    {
        var httpResult = Assert.IsAssignableFrom<IResult>(result);

        await httpResult.ExecuteAsync(httpContext);

        httpContext.Response.Body.Position = 0;

        var error = await JsonSerializer.DeserializeAsync<ApiErrorResponse>(
            httpContext.Response.Body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        return Assert.IsType<ApiErrorResponse>(error);
    }

    private sealed class TestEndpointFilterInvocationContext
        : EndpointFilterInvocationContext
    {
        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; }

        public TestEndpointFilterInvocationContext(
            HttpContext httpContext,
            params object?[] arguments)
        {
            HttpContext = httpContext
                ?? throw new ArgumentNullException(nameof(httpContext));

            Arguments = arguments.ToList();
        }

        public override T GetArgument<T>(
            int index)
        {
            return (T)Arguments[index]!;
        }
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
