using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceFlowApiAzureBlobStorageConfigurationTests
{
    [Fact]
    public void ApiStartup_ShouldUseInMemoryDocumentStorage_WhenAzureBlobStorageConfigurationIsMissing()
    {
        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var storage = scope.ServiceProvider
            .GetRequiredService<IDocumentStorage>();

        Assert.IsType<InMemoryDocumentStorage>(storage);
    }

    [Fact]
    public void ApiStartup_ShouldUseAzureBlobDocumentStorage_WhenAzureBlobStorageConfigurationIsConfigured()
    {
        const string connectionString = "UseDevelopmentStorage=true";
        const string containerName = "invoice-documents";

        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ConnectionString"] = connectionString,
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ContainerName"] = containerName
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var storage = scope.ServiceProvider
            .GetRequiredService<IDocumentStorage>();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
            .Value;

        Assert.IsType<AzureBlobDocumentStorage>(storage);
        Assert.Equal(connectionString, options.ConnectionString);
        Assert.Equal(containerName, options.ContainerName);
    }

    [Fact]
    public void ApiStartup_ShouldFail_WhenAzureBlobStorageSectionExistsButConnectionStringIsMissing()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ContainerName"] =
                    "invoice-documents"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage connection string is required.",
            exception.Message);
    }

    [Fact]
    public void ApiStartup_ShouldFail_WhenAzureBlobStorageSectionExistsButContainerNameIsMissing()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                [$"{AzureBlobDocumentStorageOptions.ConfigurationSectionName}:ConnectionString"] =
                    "UseDevelopmentStorage=true"
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage container name is required.",
            exception.Message);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? configurationValues = null)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                if (configurationValues is not null)
                {
                    builder.ConfigureAppConfiguration((_, configuration) =>
                    {
                        configuration.AddInMemoryCollection(configurationValues);
                    });
                }
            });
    }
}
