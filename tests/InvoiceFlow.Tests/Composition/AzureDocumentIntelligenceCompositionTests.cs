using Azure.AI.DocumentIntelligence;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
namespace InvoiceFlow.Tests.Composition;

public sealed class AzureDocumentIntelligenceCompositionTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow(ValidationDate);

        var returnedBuilder = builder.UseAzureDocumentIntelligence(options =>
        {
            options.Endpoint = "https://example.cognitiveservices.azure.com/";
            options.ApiKey = "test-api-key";
        });

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldThrow_WhenBuilderIsNull()
    {
        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
                options.ApiKey = "test-api-key";
            }));
    }

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldThrow_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow(ValidationDate);

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureDocumentIntelligence(null!));
    }

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldConfigureAzureDocumentIntelligenceOptions()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
                options.ApiKey = "test-api-key";
            });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var options = provider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        Assert.Equal(
            "https://example.cognitiveservices.azure.com/",
            options.Endpoint);

        Assert.Equal(
            "test-api-key",
            options.ApiKey);
    }

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldRegisterAzureDocumentIntelligenceDocumentExtractor()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
                options.ApiKey = "test-api-key";
            });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var extractor = provider.GetRequiredService<IDocumentExtractor>();

        Assert.IsType<AzureDocumentIntelligenceDocumentExtractor>(extractor);
    }
    [Fact]
public void UseAzureDocumentIntelligence_ShouldRegisterDocumentIntelligenceClient()
{
    var services = new ServiceCollection();

    services
        .AddInvoiceFlow(ValidationDate)
        .UseInMemoryInfrastructure()
        .UseAzureDocumentIntelligence(options =>
        {
            options.Endpoint = "https://example.cognitiveservices.azure.com/";
            options.ApiKey = "test-api-key";
        });

    using var provider = services.BuildServiceProvider(
        new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });

    var client = provider.GetRequiredService<DocumentIntelligenceClient>();

    Assert.NotNull(client);
}

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldOverrideInMemoryDocumentExtractor_WhenRegisteredAfterInMemoryInfrastructure()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
                options.ApiKey = "test-api-key";
            });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var extractor = provider.GetRequiredService<IDocumentExtractor>();

        Assert.IsType<AzureDocumentIntelligenceDocumentExtractor>(extractor);
    }

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldFailOptionsValidation_WhenEndpointIsMissing()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseAzureDocumentIntelligence(options =>
            {
                options.ApiKey = "test-api-key";
            });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence endpoint is required.",
            exception.Message);
    }
[Fact]
public void UseAzureDocumentIntelligence_ShouldFailOptionsValidation_WhenEndpointIsNotAbsoluteUri()
{
    var services = new ServiceCollection();

    services
        .AddInvoiceFlow(ValidationDate)
        .UseAzureDocumentIntelligence(options =>
        {
            options.Endpoint = "not-a-valid-endpoint";
            options.ApiKey = "test-api-key";
        });

    using var provider = services.BuildServiceProvider();

    var exception = Assert.Throws<OptionsValidationException>(() =>
        provider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value);

    Assert.Contains(
        "Azure Document Intelligence endpoint must be an absolute URI.",
        exception.Message);
}

    [Fact]
public void UseAzureDocumentIntelligence_ShouldFailOptionsValidation_WhenModelIdIsMissing()
{
    var services = new ServiceCollection();

    services
        .AddInvoiceFlow(ValidationDate)
        .UseAzureDocumentIntelligence(options =>
        {
            options.Endpoint = "https://example.cognitiveservices.azure.com/";
            options.ApiKey = "test-api-key";
            options.ModelId = " ";
        });

    using var provider = services.BuildServiceProvider();

    var exception = Assert.Throws<OptionsValidationException>(() =>
        provider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value);

    Assert.Contains(
        "Azure Document Intelligence model id is required.",
        exception.Message);
}

    [Fact]
    public void UseAzureDocumentIntelligence_ShouldFailOptionsValidation_WhenApiKeyIsMissing()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
            });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence API key is required.",
            exception.Message);
    }
}
