using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileIsMissing()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();

        content.Add(
            new StringContent("metadata-only"),
            "description");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "FILE_REQUIRED");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenContentTypeIsNotMultipart()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = new StringContent("not a multipart request");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_CONTENT_TYPE");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenRequestIsFormUrlEncoded()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["file"] = "not-a-real-file"
            });

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_CONTENT_TYPE");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenUploadedFileUsesUnexpectedFieldName()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "application/pdf",
            fileName: "invoice.pdf",
            fieldName: "document");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "FILE_REQUIRED");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenMultipleFilesAreUploaded()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var secondFileContent = new ByteArrayContent(CreatePdfBytes());

        secondFileContent.Headers.ContentType =
            MediaTypeHeaderValue.Parse("application/pdf");

        content.Add(
            secondFileContent,
            "file",
            "second-invoice.pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "TOO_MANY_FILES");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileIsEmpty()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(Array.Empty<byte>());

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "FILE_REQUIRED");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileNameIsDotOnly()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "application/pdf",
            fileName: ".");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_FILE_NAME");

        Assert.False(processor.WasCalled);
    }
    [Fact]
public async Task ProcessInvoice_ShouldReturnServiceUnavailable_WhenDocumentExtractorFails()
{
    await using var factory = CreateFactory(
        configureServices: services =>
        {
            services.RemoveAll<IDocumentExtractor>();
            services.AddSingleton<IDocumentExtractor, ThrowingDocumentExtractor>();
        });

    var client = factory.CreateClient();

    using var content = CreateMultipartFileContent();

    var response = await client.PostAsync(
        "/api/invoices/process",
        content);

    Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

    await AssertApiErrorCodeAsync(
        response,
        "DOCUMENT_EXTRACTION_FAILED");

    var responseBody = await response.Content.ReadAsStringAsync();

    Assert.DoesNotContain(
        "RequiresHumanReview",
        responseBody,
        StringComparison.OrdinalIgnoreCase);
}

    [Fact]
    public async Task ProcessInvoice_ShouldReturnServiceUnavailable_WhenDocumentStorageFailsThroughRealPipeline()
    {
        var storage = new ThrowingDocumentStorage();

        await using var factory = CreateFactory(
            extractedDocument: CreateValidExtractedDocument(),
            configureServices: services =>
            {
                services.RemoveAll<IDocumentStorage>();
                services.AddSingleton<IDocumentStorage>(storage);
            });

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "DOCUMENT_STORAGE_FAILED");

        Assert.True(storage.WasCalled);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "RequiresHumanReview",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnServiceUnavailable_WhenInvoicePersistenceFails()
    {
        await using var factory = CreateFactory(
            invoiceDocumentProcessor: new ThrowingPersistenceInvoiceDocumentProcessor());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVOICE_PERSISTENCE_FAILED");

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "RequiresHumanReview",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileNameIsDoubleDot()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "application/pdf",
            fileName: "..");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_FILE_NAME");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileContentTypeIsUnsupported()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: CreatePdfBytes(),
            contentType: "text/plain");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "UNSUPPORTED_FILE_CONTENT_TYPE");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnBadRequestAndNotCallProcessor_WhenFileSignatureDoesNotMatchContentType()
    {
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: [1, 2, 3, 4],
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVALID_FILE_SIGNATURE");

        Assert.False(processor.WasCalled);
    }

    [Theory]
    [MemberData(nameof(SupportedFileCases))]
    public async Task ProcessInvoice_ShouldAcceptSupportedFileTypes_WhenSignatureMatchesContentType(
        byte[] fileBytes,
        string contentType,
        string fileName)
    {
        await using var factory = CreateFactory(CreateValidExtractedDocument());

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: fileBytes,
            contentType: contentType,
            fileName: fileName);

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
    public async Task ProcessInvoice_ShouldReturnPayloadTooLargeAndNotCallProcessor_WhenFileExceedsConfiguredLimit()
    {
        var pdfBytes = CreatePdfBytes();
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            configureServices: services =>
            {
                services.Configure<InvoiceDocumentUploadOptions>(options =>
                {
                    options.MaxFileSizeInBytes = pdfBytes.Length - 1;
                });
            },
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: pdfBytes,
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "FILE_TOO_LARGE");

        Assert.False(processor.WasCalled);
    }

    [Fact]
    public async Task ProcessInvoice_ShouldReturnPayloadTooLargeAndNotCallProcessor_WhenFileExceedsLimitFromConfiguration()
    {
        var pdfBytes = CreatePdfBytes();
        var processor = new SpyInvoiceDocumentProcessor();

        await using var factory = CreateFactory(
            configureConfiguration: configuration =>
            {
                configuration.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = (pdfBytes.Length - 1).ToString()
                    });
            },
            invoiceDocumentProcessor: processor);

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent(
            fileBytes: pdfBytes,
            contentType: "application/pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "FILE_TOO_LARGE");

        Assert.False(processor.WasCalled);
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

    [Fact]
    public async Task ProcessInvoice_ShouldReturnServiceUnavailable_WhenInvoiceRepositoryFailsThroughRealPipeline()
    {
        var repository = new ThrowingInvoiceRepository();

        await using var factory = CreateFactory(
            extractedDocument: CreateValidExtractedDocument(),
            configureServices: services =>
            {
                services.RemoveAll<IInvoiceRepository>();
                services.AddSingleton<IInvoiceRepository>(repository);
            });

        var client = factory.CreateClient();

        using var content = CreateMultipartFileContent();

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        await AssertApiErrorCodeAsync(
            response,
            "INVOICE_PERSISTENCE_FAILED");

        Assert.True(repository.WasCalled);
        Assert.NotNull(repository.ReceivedInvoice);
        Assert.Equal(InvoiceStatus.Verified, repository.ReceivedInvoice!.Status);

        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(
            "RequiresHumanReview",
            responseBody,
            StringComparison.OrdinalIgnoreCase);
    }

    public static IEnumerable<object[]> SupportedFileCases()
    {
        yield return
        [
            CreatePdfBytes(),
            "application/pdf",
            "invoice.pdf"
        ];

        yield return
        [
            CreateJpegBytes(),
            "image/jpeg",
            "invoice.jpg"
        ];

        yield return
        [
            CreatePngBytes(),
            "image/png",
            "invoice.png"
        ];
    }

    private static WebApplicationFactory<Program> CreateFactory(
    ExtractedDocument? extractedDocument = null,
    Action<IServiceCollection>? configureServices = null,
    Action<IConfigurationBuilder>? configureConfiguration = null,
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
                            InvoiceDocumentUploadOptions.DefaultMaxFileSizeInBytes
                                .ToString(CultureInfo.InvariantCulture)
                    });

                configureConfiguration?.Invoke(configuration);
            });

            builder.ConfigureServices(services =>
            {
                if (extractedDocument is not null)
                {
                    services.AddSingleton<IDocumentExtractor>(
                        new FakeDocumentExtractor(extractedDocument));
                }

                if (invoiceDocumentProcessor is not null)
                {
                    services.RemoveAll<IInvoiceDocumentProcessor>();
                    services.AddSingleton(invoiceDocumentProcessor);
                }

                configureServices?.Invoke(services);
            });
        });
}

    private static MultipartFormDataContent CreateMultipartFileContent(
        byte[]? fileBytes = null,
        string contentType = "application/pdf",
        string fileName = "invoice.pdf",
        string fieldName = "file")
    {
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(
            fileBytes ?? CreatePdfBytes());

        fileContent.Headers.ContentType =
            MediaTypeHeaderValue.Parse(contentType);

        content.Add(
            fileContent,
            fieldName,
            fileName);

        return content;
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

    private static byte[] CreateJpegBytes()
    {
        return
        [
            0xFF, 0xD8, 0xFF,
            0xE0,
            0x00,
            0x10
        ];
    }

    private static byte[] CreatePngBytes()
    {
        return
        [
            0x89, 0x50, 0x4E, 0x47,
            0x0D, 0x0A, 0x1A, 0x0A,
            0x00
        ];
    }
    private sealed class ThrowingDocumentStorage : IDocumentStorage
    {
        public bool WasCalled { get; private set; }

        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;

            throw new InvalidOperationException("Blob storage upload failed.");
        }
    }

    private sealed class ThrowingInvoiceRepository : IInvoiceRepository
    {
        public bool WasCalled { get; private set; }

        public Invoice? ReceivedInvoice { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            ReceivedInvoice = invoice;
            ReceivedCancellationToken = cancellationToken;

            throw new InvalidOperationException("SQL insert failed.");
        }
    }

    private sealed class ThrowingDocumentExtractor : IDocumentExtractor
{
    public Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Azure rate limit.");
    }
}

    private sealed class ThrowingPersistenceInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            throw new InvoicePersistenceFailedException(
                "Invoice persistence failed.",
                new InvalidOperationException("SQL insert failed."));
        }
    }

    private sealed class SpyInvoiceDocumentProcessor : IInvoiceDocumentProcessor
    {
        public bool WasCalled { get; private set; }

        public DocumentInput? LastDocument { get; private set; }

        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastDocument = document;

            throw new InvalidOperationException(
                "The invoice document processor should not be called for invalid upload requests.");
        }
    }
}