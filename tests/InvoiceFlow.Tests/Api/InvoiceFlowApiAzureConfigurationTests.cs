using System.Globalization;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceFlowApiAzureConfigurationTests
{
    private const string AzureConfigurationSectionName =
        AzureDocumentIntelligenceOptions.ConfigurationSectionName;

    [Fact]
    public void ApiStartup_ShouldUseInMemoryDocumentExtractor_WhenAzureConfigurationIsMissing()
    {
        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var extractor = scope.ServiceProvider
            .GetRequiredService<IDocumentExtractor>();

        Assert.IsType<FakeDocumentExtractor>(extractor);
        Assert.IsNotType<AzureDocumentIntelligenceDocumentExtractor>(extractor);
    }

    [Fact]
    public void ApiStartup_ShouldUseAzureDocumentIntelligenceDocumentExtractor_WhenAzureConfigurationIsProvided()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureConfigurationSectionName}:Endpoint"] =
                    "https://example.cognitiveservices.azure.com/",
                [$"{AzureConfigurationSectionName}:ApiKey"] =
                    "test-api-key"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var extractor = scope.ServiceProvider
            .GetRequiredService<IDocumentExtractor>();

        Assert.IsType<AzureDocumentIntelligenceDocumentExtractor>(extractor);
    }

    [Fact]
    public void ApiStartup_ShouldConfigureAzureDocumentIntelligenceOptions_FromConfiguration()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureConfigurationSectionName}:Endpoint"] =
                    "https://example.cognitiveservices.azure.com/",
                [$"{AzureConfigurationSectionName}:ApiKey"] =
                    "test-api-key",
                [$"{AzureConfigurationSectionName}:ModelId"] =
                    "custom-invoice-model",
                [$"{AzureConfigurationSectionName}:MinimumConfidenceThreshold"] =
                    "0.65"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        Assert.Equal(
            "https://example.cognitiveservices.azure.com/",
            options.Endpoint);

        Assert.Equal(
            "test-api-key",
            options.ApiKey);

        Assert.Equal(
            "custom-invoice-model",
            options.ModelId);

        Assert.Equal(
            0.65f,
            options.MinimumConfidenceThreshold);
    }

    [Fact]
    public void ApiStartup_ShouldFailOptionsValidation_WhenAzureConfigurationExistsButEndpointIsMissing()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureConfigurationSectionName}:ApiKey"] =
                    "test-api-key"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence endpoint is required.",
            exception.Message);
    }

    [Fact]
    public void ApiStartup_ShouldFailOptionsValidation_WhenAzureConfigurationExistsButApiKeyIsMissing()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureConfigurationSectionName}:Endpoint"] =
                    "https://example.cognitiveservices.azure.com/"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence API key is required.",
            exception.Message);
    }

    [Fact]
    public void ApiStartup_ShouldFailOptionsValidation_WhenMinimumConfidenceThresholdIsInvalid()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureConfigurationSectionName}:Endpoint"] =
                    "https://example.cognitiveservices.azure.com/",
                [$"{AzureConfigurationSectionName}:ApiKey"] =
                    "test-api-key",
                [$"{AzureConfigurationSectionName}:MinimumConfidenceThreshold"] =
                    "not-a-number"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.",
            exception.Message);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var values = new Dictionary<string, string?>
                    {
                        ["InvoiceFlow:Upload:MaxFileSizeInBytes"] =
                            InvoiceDocumentUploadOptions
                                .DefaultMaxFileSizeInBytes
                                .ToString(CultureInfo.InvariantCulture)
                    };

                    if (configurationValues is not null)
                    {
                        foreach (var configurationValue in configurationValues)
                        {
                            values[configurationValue.Key] =
                                configurationValue.Value;
                        }
                    }

                    configuration.AddInMemoryCollection(values);
                });
            });
    }
}
