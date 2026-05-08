using System.Text.Json;
using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientIdentity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Api.ClientIdentity;

public sealed class ClientApiKeyEndpointFilterTests
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ValidApiKey = "if_dev_valid-secret-key";
    private const string InvalidApiKey = "if_dev_invalid-secret-key";
    private const string ClientIdItemKey = "InvoiceFlow.ClientIdentity.ClientId";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyHeaderIsMissing()
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Valid(ClientId));

        var filter = new ClientApiKeyEndpointFilter(validator);

        var httpContext = CreateHttpContext();
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
        Assert.Null(validator.ReceivedApiKey);
        Assert.False(httpContext.Items.ContainsKey(ClientIdItemKey));
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("INVALID_API_KEY", error.Code);
        Assert.Equal("A valid API key is required.", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyHeaderIsEmptyOrWhiteSpace(
        string apiKey)
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Valid(ClientId));

        var filter = new ClientApiKeyEndpointFilter(validator);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[ApiKeyHeaderName] = apiKey;

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
        Assert.Null(validator.ReceivedApiKey);
        Assert.False(httpContext.Items.ContainsKey(ClientIdItemKey));
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("INVALID_API_KEY", error.Code);
        Assert.Equal("A valid API key is required.", error.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnUnauthorized_WhenApiKeyIsInvalid()
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Invalid());

        var filter = new ClientApiKeyEndpointFilter(validator);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[ApiKeyHeaderName] = InvalidApiKey;

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
        Assert.Equal(InvalidApiKey, validator.ReceivedApiKey);
        Assert.False(httpContext.Items.ContainsKey(ClientIdItemKey));
        Assert.Equal(StatusCodes.Status401Unauthorized, httpContext.Response.StatusCode);
        Assert.Equal("INVALID_API_KEY", error.Code);
        Assert.Equal("A valid API key is required.", error.Message);
    }

    [Fact]
    public async Task InvokeAsync_ShouldStoreClientIdAndCallNext_WhenApiKeyIsValid()
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Valid(ClientId));

        var filter = new ClientApiKeyEndpointFilter(validator);

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[ApiKeyHeaderName] = ValidApiKey;

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
        Assert.Equal(ValidApiKey, validator.ReceivedApiKey);
        Assert.True(httpContext.Items.ContainsKey(ClientIdItemKey));
        Assert.Equal(ClientId, httpContext.Items[ClientIdItemKey]);
    }

    [Fact]
    public async Task InvokeAsync_ShouldPassRequestAbortedCancellationTokenToValidator()
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Valid(ClientId));

        var filter = new ClientApiKeyEndpointFilter(validator);

        using var cancellationTokenSource = new CancellationTokenSource();

        var httpContext = CreateHttpContext();
        httpContext.Request.Headers[ApiKeyHeaderName] = ValidApiKey;
        httpContext.RequestAborted = cancellationTokenSource.Token;

        var invocationContext = new TestEndpointFilterInvocationContext(httpContext);

        await filter.InvokeAsync(
            invocationContext,
            _ => ValueTask.FromResult<object?>("next-result"));

        Assert.Equal(ValidApiKey, validator.ReceivedApiKey);
        Assert.Equal(
            cancellationTokenSource.Token,
            validator.ReceivedCancellationToken);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenValidatorIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ClientApiKeyEndpointFilter(null!));

        Assert.Equal("validator", exception.ParamName);
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

    private sealed class CapturingClientApiKeyValidator
        : IClientApiKeyValidator
    {
        private readonly ClientApiKeyValidationResult _result;

        public string? ReceivedApiKey { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult result)
        {
            _result = result
                ?? throw new ArgumentNullException(nameof(result));
        }

        public Task<ClientApiKeyValidationResult> ValidateAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            ReceivedApiKey = apiKey;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }
}
