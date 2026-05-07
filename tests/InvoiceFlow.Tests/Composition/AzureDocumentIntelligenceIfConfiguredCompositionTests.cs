using System.Globalization;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Composition;

public sealed class AzureDocumentIntelligenceIfConfiguredCompositionTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        var builder = services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure();

        var returnedBuilder = builder.UseAzureDocumentIntelligenceIfConfigured();

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldThrow_WhenBuilderIsNull()
    {
        var configuration = CreateConfiguration();

        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureDocumentIntelligenceIfConfigured());
    }


    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldKeepFakeDocumentExtractor_WhenAzureConfigurationIsMissing()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligenceIfConfigured();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var extractor = provider.GetRequiredService<IDocumentExtractor>();

        Assert.IsType<FakeDocumentExtractor>(extractor);
    }

    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldUseAzureDocumentIntelligenceDocumentExtractor_WhenAzureConfigurationExists()
    {
        const string endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/";
        const string apiKey = "test-api-key";

        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:Endpoint"] = endpoint,
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:ApiKey"] = apiKey
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligenceIfConfigured();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var extractor = provider.GetRequiredService<IDocumentExtractor>();

        var options = provider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        Assert.IsType<AzureDocumentIntelligenceDocumentExtractor>(extractor);
        Assert.Equal(endpoint, options.Endpoint);
        Assert.Equal(apiKey, options.ApiKey);
        Assert.Equal(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            options.ModelId);
        Assert.Equal(
            AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold,
            options.MinimumConfidenceThreshold);
    }

    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldBindOptionalAzureConfiguration_WhenConfigured()
    {
        const string endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/";
        const string apiKey = "test-api-key";
        const string modelId = "custom-invoice-model";
        const float minimumConfidenceThreshold = 0.65f;

        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:Endpoint"] = endpoint,
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:ApiKey"] = apiKey,
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:ModelId"] = modelId,
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:MinimumConfidenceThreshold"] =
                    minimumConfidenceThreshold.ToString(CultureInfo.InvariantCulture)
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligenceIfConfigured();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var options = provider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        Assert.Equal(endpoint, options.Endpoint);
        Assert.Equal(apiKey, options.ApiKey);
        Assert.Equal(modelId, options.ModelId);
        Assert.Equal(
            minimumConfidenceThreshold,
            options.MinimumConfidenceThreshold);
    }

    [Fact]
    public void UseAzureDocumentIntelligenceIfConfigured_ShouldFailOptionsValidation_WhenAzureSectionExistsButApiKeyIsMissing()
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureDocumentIntelligenceOptions.ConfigurationSectionName}:Endpoint"] =
                    "https://invoiceflow-test.cognitiveservices.azure.com/"
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligenceIfConfigured();

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence API key is required.",
            exception.Message);
    }

    private static IConfiguration CreateConfiguration(
        IReadOnlyDictionary<string, string?>? values = null)
    {
        var builder = new ConfigurationBuilder();

        if (values is not null)
        {
            builder.AddInMemoryCollection(values);
        }

        return builder.Build();
    }
}
