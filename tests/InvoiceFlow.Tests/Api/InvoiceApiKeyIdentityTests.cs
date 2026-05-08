using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.ProcessingRuns;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceApiKeyIdentityTests
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private const string ValidApiKey = "if_dev_valid-secret-key";
    private const string InvalidApiKey = "if_dev_invalid-secret-key";
    private const string KeyPrefix = "if_dev_";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ProcessInvoice_ShouldReturnUnauthorizedAndNotCallProcessor_WhenApiKeyHeaderIsMissing()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(client);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_API_KEY");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnUnauthorizedAndNotCallProcessor_WhenApiKeyIsInvalid()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            InvalidApiKey);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_API_KEY");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnOk_WhenApiKeyIsValid()
    {
        await using var factory = CreateFactory(
            extractedDocument: CreateValidExtractedDocument());

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldSaveProcessingRunWithClientIdFromApiKey_WhenApiKeyIsValid()
    {
        await using var factory = CreateFactory(
            extractedDocument: CreateValidExtractedDocument());

        var client = factory.CreateClient();

        var response = await PostProcessInvoiceAsync(
            client,
            ValidApiKey);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var processingRunRepository = factory.Services
            .GetRequiredService<IProcessingRunRepository>();

        var inMemoryProcessingRunRepository =
            Assert.IsType<InMemoryProcessingRunRepository>(
                processingRunRepository);

        var processingRun = Assert.Single(
            inMemoryProcessingRunRepository.ProcessingRuns);

        Assert.Equal(ClientId, processingRun.ClientId);
    }

    [Fact]
    public async Task Health_ShouldNotRequireApiKey_WhenApiKeyIdentityIsEnabled()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ExtractedDocument? extractedDocument = null,
        IInvoiceDocumentProcessor? invoiceDocumentProcessor = null)
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
                                keyHash: ComputeSha256Hash(ValidApiKey),
                                keyPrefix: KeyPrefix);
                        });

                    if (extractedDocument is not null)
                    {
                        services.RemoveAll<IDocumentExtractor>();
                        services.AddSingleton<IDocumentExtractor>(
                            new FakeDocumentExtractor(extractedDocument));
                    }

                    if (invoiceDocumentProcessor is not null)
                    {
                        services.RemoveAll<IInvoiceDocumentProcessor>();
                        services.AddSingleton(invoiceDocumentProcessor);
                    }
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
            "/api/invoices/process")
        {
            Content = content
        };

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Add(ApiKeyHeaderName, apiKey);
        }

        return await client.SendAsync(request);
    }


    private static ExtractedDocument CreateValidExtractedDocument()
    {
        return new ExtractedDocument(
            "api key identity test extracted invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "API Key Test Vendor Ltd",
                ["VendorTaxId"] = "123456789",
                ["InvoiceNumber"] = "INV-API-KEY-1001",
                ["IssueDate"] = CreateCurrentIssueDate(),
                ["SubtotalAmount"] = "100",
                ["VatAmount"] = "18",
                ["TotalAmount"] = "118",
                ["Currency"] = "ILS"
            });
    }

    private static string CreateCurrentIssueDate()
    {
        return DateOnly
            .FromDateTime(DateTime.UtcNow)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
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

    private static string ComputeSha256Hash(
        string value)
    {
        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(value));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class SpyInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        public bool WasCalled { get; private set; }

        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException(
                "The invoice document processor should not be called when API key validation fails.");
        }
    }
}
