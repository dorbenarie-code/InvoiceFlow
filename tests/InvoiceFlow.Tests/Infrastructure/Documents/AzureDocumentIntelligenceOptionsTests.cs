using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceOptionsTests
{
    [Fact]
    public void DefaultModelId_ShouldBePrebuiltInvoice()
    {
        Assert.Equal(
            "prebuilt-invoice",
            AzureDocumentIntelligenceOptions.DefaultModelId);
    }

    [Fact]
    public void DefaultMinimumConfidenceThreshold_ShouldBeEightyPercent()
    {
        Assert.Equal(
            0.8f,
            AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold);
    }

    [Fact]
    public void Options_ShouldUseDefaultMinimumConfidenceThreshold()
    {
        var options = new AzureDocumentIntelligenceOptions();

        Assert.Equal(
            AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold,
            options.MinimumConfidenceThreshold);
    }

    [Theory]
    [InlineData(0f)]
    [InlineData(0.5f)]
    [InlineData(0.8f)]
    [InlineData(1f)]
    public void UseAzureDocumentIntelligence_ShouldAcceptMinimumConfidenceThreshold_WhenValueIsBetweenZeroAndOne(
        float threshold)
    {
        using var serviceProvider = CreateServiceProvider(options =>
        {
            options.MinimumConfidenceThreshold = threshold;
        });

        var options = serviceProvider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        Assert.Equal(
            threshold,
            options.MinimumConfidenceThreshold);
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(-1f)]
    public void UseAzureDocumentIntelligence_ShouldFailValidation_WhenMinimumConfidenceThresholdIsBelowZero(
        float threshold)
    {
        using var serviceProvider = CreateServiceProvider(options =>
        {
            options.MinimumConfidenceThreshold = threshold;
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.",
            exception.Message);
    }

    [Theory]
    [InlineData(1.01f)]
    [InlineData(2f)]
    public void UseAzureDocumentIntelligence_ShouldFailValidation_WhenMinimumConfidenceThresholdIsGreaterThanOne(
        float threshold)
    {
        using var serviceProvider = CreateServiceProvider(options =>
        {
            options.MinimumConfidenceThreshold = threshold;
        });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value);

        Assert.Contains(
            "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.",
            exception.Message);
    }

    private static ServiceProvider CreateServiceProvider(
        Action<AzureDocumentIntelligenceOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseInMemoryInfrastructure()
            .UseAzureDocumentIntelligence(options =>
            {
                options.Endpoint = "https://example.cognitiveservices.azure.com/";
                options.ApiKey = "test-api-key";

                configureOptions?.Invoke(options);
            });

        return services.BuildServiceProvider();
    }
}
