using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceSmokeTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [AzureSmokeFact]
    [Trait("Category", "Smoke")]
    [Trait("Provider", "AzureDocumentIntelligence")]
    public async Task ExtractAsync_ShouldReturnExtractedDocument_WhenAzureSmokeTestConfigurationIsProvided()
    {
        var endpoint = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.EndpointVariable);

        var apiKey = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.ApiKeyVariable);

        var documentPath = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.DocumentPathVariable);

        if (!File.Exists(documentPath))
        {
            throw new InvalidOperationException(
                $"Azure smoke test document was not found. Path: {documentPath}");
        }

        if (!string.Equals(
                Path.GetExtension(documentPath),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Azure smoke test document must be a PDF file.");
        }

        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = endpoint;
                options.ApiKey = apiKey;
            });

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var extractor = provider.GetRequiredService<IDocumentExtractor>();

        var document = await CreateDocumentInputAsync(documentPath);

        var extractedDocument = await extractor.ExtractAsync(document);

        Assert.NotNull(extractedDocument);

        var hasRawText = !string.IsNullOrWhiteSpace(
            extractedDocument.RawText);

        var hasFields = extractedDocument.Fields.Count > 0;

        Assert.True(
            hasRawText || hasFields,
            "Azure smoke test expected extracted raw text or at least one extracted field.");
    }

    [AzureSmokeFact]
    [Trait("Category", "Smoke")]
    [Trait("Provider", "AzureDocumentIntelligence")]
    [Trait("Scope", "FullPipeline")]
    public async Task ProcessAsync_ShouldRunFullPipelineWithAzureDocumentIntelligence_WhenAzureSmokeTestConfigurationIsProvided()
    {
        var endpoint = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.EndpointVariable);

        var apiKey = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.ApiKeyVariable);

        var documentPath = GetRequiredEnvironmentVariable(
            AzureSmokeTestConfiguration.DocumentPathVariable);

        if (!File.Exists(documentPath))
        {
            throw new InvalidOperationException(
                $"Azure smoke test document was not found. Path: {documentPath}");
        }

        if (!string.Equals(
                Path.GetExtension(documentPath),
                ".pdf",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Azure smoke test document must be a PDF file.");
        }

        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = endpoint;
                options.ApiKey = apiKey;
            });

        await using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        await using var scope = provider.CreateAsyncScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IInvoiceDocumentProcessor>();

        var document = await CreateDocumentInputAsync(documentPath);

        var result = await processor.ProcessAsync(document);

        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.NotEqual(Guid.Empty, result.InvoiceId);
        Assert.NotNull(result.Invoice);
        Assert.NotNull(result.ValidationReport);

        Assert.Equal(
            result.DocumentId,
            result.Invoice.SourceDocumentId);

        Assert.Contains(
            result.Status,
            new[]
            {
                InvoiceStatus.Verified,
                InvoiceStatus.RequiresHumanReview
            });

        if (result.ValidationReport.RequiresHumanReview)
        {
            Assert.Equal(
                InvoiceStatus.RequiresHumanReview,
                result.Status);
        }
        else
        {
            Assert.Equal(
                InvoiceStatus.Verified,
                result.Status);
        }

        var invoiceRepository = scope.ServiceProvider
            .GetRequiredService<IInvoiceRepository>();

        var inMemoryInvoiceRepository =
            Assert.IsType<InMemoryInvoiceRepository>(invoiceRepository);

        var savedInvoice = Assert.Single(
            inMemoryInvoiceRepository.Invoices);

        Assert.Equal(result.InvoiceId, savedInvoice.Id);
        Assert.Equal(result.DocumentId, savedInvoice.SourceDocumentId);
        Assert.Equal(result.Status, savedInvoice.Status);
    }

    private static string GetRequiredEnvironmentVariable(
        string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"Azure smoke test is enabled, but required environment variable '{variableName}' is missing.");
        }

        return value.Trim();
    }

    private static async Task<DocumentInput> CreateDocumentInputAsync(
        string documentPath)
    {
        var content = await File.ReadAllBytesAsync(documentPath);

        return new DocumentInput(
            fileName: Path.GetFileName(documentPath),
            contentType: "application/pdf",
            content: content);
    }
}

internal sealed class AzureSmokeFactAttribute : FactAttribute
{
    public AzureSmokeFactAttribute()
    {
        if (!AzureSmokeTestConfiguration.IsEnabled())
        {
            Skip =
                $"Azure smoke test skipped. Set {AzureSmokeTestConfiguration.SmokeTestsEnabledVariable}=true to run it.";
        }
    }
}

internal static class AzureSmokeTestConfiguration
{
    public const string SmokeTestsEnabledVariable =
        "INVOICEFLOW_AZURE_SMOKE_TESTS";

    public const string EndpointVariable =
        "INVOICEFLOW_AZURE_TEST_ENDPOINT";

    public const string ApiKeyVariable =
        "INVOICEFLOW_AZURE_TEST_API_KEY";

    public const string DocumentPathVariable =
        "INVOICEFLOW_AZURE_TEST_DOCUMENT_PATH";

    public static bool IsEnabled()
    {
        var value = Environment.GetEnvironmentVariable(
            SmokeTestsEnabledVariable);

        return string.Equals(
            value,
            "true",
            StringComparison.OrdinalIgnoreCase);
    }
}
