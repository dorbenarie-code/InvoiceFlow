using System.Globalization;
using System.Net;
using System.Text.Json;
using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientRateLimiting;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceApiRateLimitingTests
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ValidApiKey = "if_dev_valid-secret-key";
    private const string InvalidApiKey = "if_dev_invalid-secret-key";
    private const string KeyPrefix = "if_dev_";
    private const string InvoiceProcessResource = "/api/invoices/process";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ProcessInvoice_ShouldCallRateLimiterAndProcessor_WhenApiKeyIsValidAndRequestIsAllowed()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            limiter,
            processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Equal(1, limiter.CallCount);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(InvoiceProcessResource, limiter.ReceivedResource);

        Assert.Equal(1, processor.CallCount);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnTooManyRequestsAndNotCallProcessor_WhenApiKeyIsValidAndRateLimitIsExceeded()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Exceeded());

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            limiter,
            processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "RATE_LIMIT_EXCEEDED");

        Assert.Equal(1, limiter.CallCount);
        Assert.Equal(ClientId, limiter.ReceivedClientId);
        Assert.Equal(InvoiceProcessResource, limiter.ReceivedResource);

        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnUnauthorizedAndNotCallRateLimiterOrProcessor_WhenApiKeyHeaderIsMissing()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            limiter,
            processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_API_KEY");

        Assert.Equal(0, limiter.CallCount);
        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnUnauthorizedAndNotCallRateLimiterOrProcessor_WhenApiKeyIsInvalid()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Allowed());

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            limiter,
            processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            InvalidApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_API_KEY");

        Assert.Equal(0, limiter.CallCount);
        Assert.Equal(0, processor.CallCount);
    }

    [Fact]
    public async Task Health_ShouldNotUseRateLimiter_WhenRateLimiterIsRegistered()
    {
        var limiter = new CapturingClientRateLimiter(
            ClientRateLimitResult.Exceeded());

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            limiter,
            processor);

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(0, limiter.CallCount);
        Assert.Equal(0, processor.CallCount);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IClientRateLimiter limiter,
        IInvoiceDocumentProcessor processor)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["InvoiceFlow:Upload:MaxFileSizeInBytes"] =
                                InvoiceDocumentUploadOptions
                                    .DefaultMaxFileSizeInBytes
                                    .ToString(CultureInfo.InvariantCulture)
                        });
                });

                builder.ConfigureServices(services =>
                {
                    services
                        .AddInvoiceFlow()
                        .UseApiKeyClientIdentity(options =>
                        {
                            options.AddClient(
                                clientId: ClientId,
                                keyHash: ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                                keyPrefix: KeyPrefix);
                        });

                    services.RemoveAll<IClientRateLimiter>();
                    services.AddSingleton(limiter);

                    services.RemoveAll<IInvoiceDocumentProcessor>();
                    services.AddSingleton(processor);
                });
            });
    }

    private static MultipartFormDataContent CreateMultipartFileContent()
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(CreatePdfBytes());
        fileContent.Headers.ContentType =
            new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        content.Add(
            fileContent,
            "file",
            "invoice.pdf");

        return content;
    }

    private static async Task<HttpResponseMessage> PostProcessInvoiceAsync(
        HttpClient client,
        string? apiKey = null)
    {
        using var content = CreateMultipartFileContent();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            InvoiceProcessResource)
        {
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add(
                ApiKeyHeaderName,
                apiKey);
        }

        return await client.SendAsync(request);
    }

    private static byte[] CreatePdfBytes()
    {
        return
        [
            0x25, 0x50, 0x44, 0x46, 0x2D,
            0x31, 0x2E, 0x37,
            0x0A
        ];
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

    private sealed class SuccessfulSpyInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        public int CallCount { get; private set; }

        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            CallCount++;

            var sourceDocumentId = Guid.NewGuid();

            var invoice = Invoice.CreateExtracted(
                sourceDocumentId: sourceDocumentId,
                vendor: new Vendor("Rate Limit Test Vendor Ltd", "123456789"),
                invoiceNumber: "INV-RATE-LIMIT-1001",
                issueDate: DateOnly.FromDateTime(DateTime.UtcNow),
                subtotalAmount: new CurrencyAmount(100m, "ILS"),
                vatAmount: new CurrencyAmount(18m, "ILS"),
                totalAmount: new CurrencyAmount(118m, "ILS"),
                metadata: new Dictionary<string, string>
                {
                    ["Source"] = "RateLimitIntegrationTest"
                });

            invoice.ApplyValidationReport(
                InvoiceValidationReport.Valid());

            var result = new ProcessInvoiceDocumentResult(
                sourceDocumentId,
                invoice);

            return Task.FromResult(result);
        }
    }
}
