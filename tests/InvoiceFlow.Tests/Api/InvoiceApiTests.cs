using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceApiTests
{
    [Fact]
    public async Task ProcessInvoice_ShouldReturnVerifiedInvoice_WhenFileIsValid()
    {
        await using var factory = CreateFactory(CreateValidExtractedDocument());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("Verified", root.GetProperty("status").GetString());
        Assert.False(root.GetProperty("validationReport").GetProperty("hasIssues").GetBoolean());

        var invoice = root.GetProperty("invoice");

        Assert.Equal("INV-API-1001", invoice.GetProperty("invoiceNumber").GetString());
        Assert.Equal("API Test Vendor Ltd", invoice.GetProperty("vendorName").GetString());
        Assert.Equal("ILS", invoice.GetProperty("totalAmount").GetProperty("currency").GetString());
        Assert.Equal(118m, invoice.GetProperty("totalAmount").GetProperty("amount").GetDecimal());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnRequiresHumanReview_WhenValidationHasErrors()
    {
        await using var factory = CreateFactory(CreateTotalMismatchExtractedDocument());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("RequiresHumanReview", root.GetProperty("status").GetString());

        var validationReport = root.GetProperty("validationReport");

        Assert.True(validationReport.GetProperty("hasIssues").GetBoolean());
        Assert.True(validationReport.GetProperty("hasErrors").GetBoolean());
        Assert.True(validationReport.GetProperty("requiresHumanReview").GetBoolean());

        var issues = validationReport.GetProperty("issues").EnumerateArray();

        Assert.Contains(issues, issue =>
            issue.GetProperty("code").GetString() == "TOTAL_MISMATCH"
            && issue.GetProperty("fieldName").GetString() == "TotalAmount"
            && issue.GetProperty("severity").GetString() == "Error");
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequest_WhenFileIsMissing()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();

        content.Add(
            new StringContent("metadata-only"),
            "description");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("FILE_REQUIRED", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequest_WhenContentTypeIsNotMultipart()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        using var content = new StringContent("not a multipart request");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("INVALID_CONTENT_TYPE", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequest_WhenFileIsEmpty()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(Array.Empty<byte>());

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("FILE_REQUIRED", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequest_WhenFileContentTypeIsUnsupported()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "text/plain");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("UNSUPPORTED_FILE_CONTENT_TYPE", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequest_WhenFileSignatureDoesNotMatchContentType()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: [1, 2, 3, 4],
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("INVALID_FILE_SIGNATURE", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldAcceptFile_WhenContentTypeContainsParameters()
    {
        await using var factory = CreateFactory(CreateValidExtractedDocument());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "application/pdf; charset=utf-8");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnPayloadTooLarge_WhenFileExceedsConfiguredLimit()
    {
        var pdfBytes = CreatePdfBytes();

        await using var factory = CreateFactory(
            configureServices: services =>
            {
                services.Configure<InvoiceDocumentUploadOptions>(options =>
                {
                    options.MaxFileSizeInBytes = pdfBytes.Length - 1;
                });
            });

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: pdfBytes,
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("FILE_TOO_LARGE", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnPayloadTooLarge_WhenFileExceedsLimitFromConfiguration()
    {
        var pdfBytes = CreatePdfBytes();

        await using var factory = CreateFactory(
            configureConfiguration: configuration =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = (pdfBytes.Length - 1).ToString()
                    });
            });

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: pdfBytes,
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal("FILE_TOO_LARGE", root.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ProcessInvoice_ShouldStoreSanitizedFileName_WhenFileNameContainsPathSegments()
    {
        await using var factory = CreateFactory(CreateValidExtractedDocument());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "application/pdf",
            fileName: "..\\..\\invoice.pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var documentStorage = factory.Services.GetRequiredService<IDocumentStorage>();
        var inMemoryDocumentStorage = Assert.IsType<InMemoryDocumentStorage>(documentStorage);

        var storedDocument = Assert.Single(inMemoryDocumentStorage.Documents);

        Assert.Equal("invoice.pdf", storedDocument.FileName);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        ExtractedDocument? extractedDocument = null,
        Action<IServiceCollection>? configureServices = null,
        Action<IConfigurationBuilder>? configureConfiguration = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                if (configureConfiguration is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configureConfiguration(configuration);
                    });
                }

                builder.ConfigureServices(services =>
                {
                    if (extractedDocument is not null)
                    {
                        services.AddSingleton<IDocumentExtractor>(
                            new FakeDocumentExtractor(extractedDocument));
                    }

                    configureServices?.Invoke(services);
                });
            });
    }

    private static MultipartFormDataContent CreateMultipartFileContent(
        byte[]? fileBytes = null,
        string contentType = "application/pdf",
        string fileName = "invoice.pdf")
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(
            fileBytes ?? CreatePdfBytes());

        fileContent.Headers.ContentType =
            MediaTypeHeaderValue.Parse(contentType);

        content.Add(fileContent, "file", fileName);

        return content;
    }

    private static ExtractedDocument CreateValidExtractedDocument()
    {
        return new ExtractedDocument(
            "api test extracted invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "API Test Vendor Ltd",
                ["VendorTaxId"] = "123456789",
                ["InvoiceNumber"] = "INV-API-1001",
                ["IssueDate"] = CreateCurrentIssueDate(),
                ["SubtotalAmount"] = "100",
                ["VatAmount"] = "18",
                ["TotalAmount"] = "118",
                ["Currency"] = "ILS"
            });
    }

    private static ExtractedDocument CreateTotalMismatchExtractedDocument()
    {
        return new ExtractedDocument(
            "api test extracted invoice text with total mismatch",
            new Dictionary<string, string>
            {
                ["VendorName"] = "API Test Vendor Ltd",
                ["VendorTaxId"] = "123456789",
                ["InvoiceNumber"] = "INV-API-REVIEW-1001",
                ["IssueDate"] = CreateCurrentIssueDate(),
                ["SubtotalAmount"] = "100",
                ["VatAmount"] = "17",
                ["TotalAmount"] = "118",
                ["Currency"] = "ILS"
            });
    }

    private static string CreateCurrentIssueDate()
    {
        return DateOnly
            .FromDateTime(DateTime.UtcNow)
            .ToString("yyyy-MM-dd");
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
}
