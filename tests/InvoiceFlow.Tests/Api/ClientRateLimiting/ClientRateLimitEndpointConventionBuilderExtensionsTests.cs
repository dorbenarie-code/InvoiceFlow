using System.Net;
using System.Text.Json;
using InvoiceFlow.Api.ClientRateLimiting;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientIdentity;
using InvoiceFlow.Application.ClientRateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Api.ClientRateLimiting;

public sealed class ClientRateLimitEndpointConventionBuilderExtensionsTests
{
    private const string ClientIdItemKey = "InvoiceFlow.ClientIdentity.ClientId";
    private const string LimitedResource = "/limited";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task RequireClientRateLimitWhenConfigured_ShouldContinue_WhenLimiterIsNotRegistered()
    {
        await using var app = await CreateAppAsync(
            limiter: null,
            resolveClientId: false);

        var client = app.GetTestClient();

        var response = await client.GetAsync(LimitedResource);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capture = app.Services.GetRequiredService<RequestCapture>();

        Assert.True(capture.HandlerWasCalled);
    }

    [Fact]
    public async Task RequireClientRateLimitWhenConfigured_ShouldContinue_WhenApiKeyIdentityIsNotRegistered()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Exceeded());

        await using var app = await CreateAppAsync(
            limiter,
            resolveClientId: false,
            registerApiKeyIdentity: false);

        var client = app.GetTestClient();

        var response = await client.GetAsync(LimitedResource);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capture = app.Services.GetRequiredService<RequestCapture>();

        Assert.True(capture.HandlerWasCalled);
        Assert.Equal(0, limiter.CallCount);
    }

    [Fact]
    public async Task RequireClientRateLimitWhenConfigured_ShouldInvokeLimiterAndContinue_WhenRequestIsAllowed()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        await using var app = await CreateAppAsync(
            limiter,
            resolveClientId: true,
            registerApiKeyIdentity: true);

        var client = app.GetTestClient();

        var response = await client.GetAsync(LimitedResource);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var capture = app.Services.GetRequiredService<RequestCapture>();

        Assert.True(capture.HandlerWasCalled);
        Assert.Equal(1, limiter.CallCount);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(LimitedResource, limiter.ReceivedResource);
    }

    [Fact]
    public async Task RequireClientRateLimitWhenConfigured_ShouldReturnTooManyRequestsAndNotCallHandler_WhenRequestIsBlocked()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Exceeded());

        await using var app = await CreateAppAsync(
            limiter,
            resolveClientId: true,
            registerApiKeyIdentity: true);

        var client = app.GetTestClient();

        var response = await client.GetAsync(LimitedResource);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        var capture = app.Services.GetRequiredService<RequestCapture>();

        Assert.False(capture.HandlerWasCalled);
        Assert.Equal(1, limiter.CallCount);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(LimitedResource, limiter.ReceivedResource);

        await AssertApiErrorCodeAsync(
            response,
            "RATE_LIMIT_EXCEEDED");
    }

    private static async Task<WebApplication> CreateAppAsync(
        IClientRateLimiter? limiter,
        bool resolveClientId,
        bool registerApiKeyIdentity = false)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        builder.Services.AddSingleton<RequestCapture>();

        if (registerApiKeyIdentity)
        {
            builder.Services.AddSingleton<IClientApiKeyValidator>(
                new StubClientApiKeyValidator());
        }

        if (limiter is not null)
        {
            builder.Services.AddSingleton(limiter);
        }

        var app = builder.Build();

        var route = app.MapGet(LimitedResource, (RequestCapture capture) =>
        {
            capture.HandlerWasCalled = true;

            return Results.Ok(new
            {
                Status = "OK"
            });
        });

        if (resolveClientId)
        {
            route.AddEndpointFilter((context, next) =>
            {
                context.HttpContext.Items[ClientIdItemKey] = ClientId;

                return next(context);
            });
        }

        route.RequireClientRateLimitWhenConfigured();

        await app.StartAsync();

        return app;
    }

    private static async Task AssertApiErrorCodeAsync(
        HttpResponseMessage response,
        string expectedCode)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        Assert.Equal(
            expectedCode,
            json.RootElement.GetProperty("code").GetString());

        Assert.Equal(
            "Rate limit exceeded. Please try again later.",
            json.RootElement.GetProperty("message").GetString());
    }

    private sealed class StubClientApiKeyValidator : IClientApiKeyValidator
    {
        public Task<ClientApiKeyValidationResult> ValidateAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                ClientApiKeyValidationResult.Invalid());
        }
    }

    private sealed class RequestCapture
    {
        public bool HandlerWasCalled { get; set; }
    }

    private sealed class CapturingClientRateLimiter
        : IClientRateLimiter
    {
        private readonly ClientRateLimitResult _result;

        public int CallCount { get; private set; }

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
            CallCount++;
            ReceivedClientId = clientId;
            ReceivedResource = resource;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }
}
