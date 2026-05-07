using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Composition;

public sealed class AzureBlobDocumentStorageIfConfiguredCompositionTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        var builder = services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure();

        var returnedBuilder = builder.UseAzureBlobDocumentStorageIfConfigured(
            configuration);

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldThrow_WhenBuilderIsNull()
    {
        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureBlobDocumentStorageIfConfigured());
    }

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldKeepInMemoryDocumentStorage_WhenBlobConfigurationIsMissing()
    {
        var configuration = CreateConfiguration();
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorageIfConfigured();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var storage = provider.GetRequiredService<IDocumentStorage>();

        Assert.IsType<InMemoryDocumentStorage>(storage);
    }

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldUseAzureBlobDocumentStorage_WhenBlobConfigurationExists()
    {
        const string connectionString = "UseDevelopmentStorage=true";
        const string containerName = "invoice-documents";

        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ConnectionString"] = connectionString,
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ContainerName"] = containerName
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorageIfConfigured();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var storage = provider.GetRequiredService<IDocumentStorage>();

        var options = provider
            .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
            .Value;

        Assert.IsType<AzureBlobDocumentStorage>(storage);
        Assert.Equal(connectionString, options.ConnectionString);
        Assert.Equal(containerName, options.ContainerName);
    }

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldFailOptionsValidation_WhenBlobSectionExistsButConnectionStringIsMissing()
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ContainerName"] = "invoice-documents"
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorageIfConfigured();

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage connection string is required.",
            exception.Message);
    }

    [Fact]
    public void UseAzureBlobDocumentStorageIfConfigured_ShouldFailOptionsValidation_WhenBlobSectionExistsButContainerNameIsMissing()
    {
        var configuration = CreateConfiguration(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ConnectionString"] =
                    "UseDevelopmentStorage=true"
            });

        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(configuration);

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorageIfConfigured();

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage container name is required.",
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
