using System.Net;
using System.Text.Json;
using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceFlow.Tests.Api;

[Collection(InvoiceFlowApiConfigurationTestCollection.Name)]
public sealed class InvoiceApiConfiguredRateLimitingIntegrationTests
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ValidApiKey = "if_dev_valid-secret-key";
    private const string KeyPrefix = "if_dev_";
    private const string InvoiceProcessResource = "/api/invoices/process";

    private const string PermitLimitEnvironmentVariable =
        "InvoiceFlow__ClientRateLimiting__PermitLimit";

    private const string WindowEnvironmentVariable =
        "InvoiceFlow__ClientRateLimiting__Window";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ProcessInvoice_ShouldReturnTooManyRequestsAfterConfiguredInMemoryLimitIsExceeded()
    {
        using var environment = TemporaryEnvironmentVariables.Set(
            new Dictionary<string, string?>
            {
                [PermitLimitEnvironmentVariable] = "1",
                [WindowEnvironmentVariable] = "00:10:00"
            });

        var processor = new SuccessfulSpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(processor);

        var client = factory.CreateClient();

        var firstResponse = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        var secondResponse = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            secondResponse.StatusCode);

        await AssertApiErrorCodeAsync(
            secondResponse,
            "RATE_LIMIT_EXCEEDED");

        Assert.Equal(1, processor.CallCount);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IInvoiceDocumentProcessor processor)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

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
        string apiKey)
    {
        using var content = CreateMultipartFileContent();

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            InvoiceProcessResource)
        {
            Content = content
        };

        request.Headers.Add(
            ApiKeyHeaderName,
            apiKey);

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

        Assert.Equal(
            "Rate limit exceeded. Please try again later.",
            json.RootElement.GetProperty("message").GetString());
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
                vendor: new Vendor("Configured Rate Limit Test Vendor Ltd", "123456789"),
                invoiceNumber: "INV-CONFIGURED-RATE-LIMIT-1001",
                issueDate: new DateOnly(2024, 1, 1),
                subtotalAmount: new CurrencyAmount(100m, "ILS"),
                vatAmount: new CurrencyAmount(18m, "ILS"),
                totalAmount: new CurrencyAmount(118m, "ILS"),
                metadata: new Dictionary<string, string>
                {
                    ["Source"] = "ConfiguredRateLimitIntegrationTest"
                });

            invoice.ApplyValidationReport(
                InvoiceValidationReport.Valid());

            var result = new ProcessInvoiceDocumentResult(
                sourceDocumentId,
                invoice);

            return Task.FromResult(result);
        }
    }

    private sealed class TemporaryEnvironmentVariables : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;

        private TemporaryEnvironmentVariables(
            IEnumerable<string> variableNames)
        {
            _originalValues = variableNames.ToDictionary(
                variableName => variableName,
                Environment.GetEnvironmentVariable);
        }

        public static TemporaryEnvironmentVariables Set(
            IReadOnlyDictionary<string, string?> values)
        {
            var environment = new TemporaryEnvironmentVariables(values.Keys);

            foreach (var item in values)
            {
                Environment.SetEnvironmentVariable(
                    item.Key,
                    item.Value);
            }

            return environment;
        }

        public void Dispose()
        {
            foreach (var item in _originalValues)
            {
                Environment.SetEnvironmentVariable(
                    item.Key,
                    item.Value);
            }
        }
    }
}
