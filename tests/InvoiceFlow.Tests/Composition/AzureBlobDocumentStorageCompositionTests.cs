using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Composition;

public sealed class AzureBlobDocumentStorageCompositionTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void AzureBlobDocumentStorageOptions_ShouldExposeExpectedConfigurationSectionName()
    {
        Assert.Equal(
            "InvoiceFlow:AzureBlobStorage",
            AzureBlobDocumentStorageOptions.ConfigurationSectionName);
    }

    [Fact]
    public void AzureBlobDocumentStorageOptions_ShouldDefaultValuesToEmpty()
    {
        var options = new AzureBlobDocumentStorageOptions();

        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(string.Empty, options.ContainerName);
    }

    [Fact]
    public void UseAzureBlobDocumentStorage_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure();

        var returnedBuilder = builder.UseAzureBlobDocumentStorage(options =>
        {
            options.ConnectionString = CreateConnectionString();
            options.ContainerName = "invoice-documents";
        });

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseAzureBlobDocumentStorage_ShouldRegisterAzureBlobDocumentStorageAsDocumentStorage()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorage(options =>
            {
                options.ConnectionString = CreateConnectionString();
                options.ContainerName = "invoice-documents";
            });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var documentStorage = provider.GetRequiredService<IDocumentStorage>();

        Assert.IsType<AzureBlobDocumentStorage>(documentStorage);
    }

    [Fact]
    public void UseAzureBlobDocumentStorage_ShouldConfigureOptions()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure()
            .UseAzureBlobDocumentStorage(options =>
            {
                options.ConnectionString = CreateConnectionString();
                options.ContainerName = "invoice-documents";
            });

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
            .Value;

        Assert.Equal(CreateConnectionString(), options.ConnectionString);
        Assert.Equal("invoice-documents", options.ContainerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UseAzureBlobDocumentStorage_ShouldFailOptionsValidation_WhenConnectionStringIsMissing(
        string connectionString)
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseAzureBlobDocumentStorage(options =>
            {
                options.ConnectionString = connectionString;
                options.ContainerName = "invoice-documents";
            });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage connection string is required.",
            exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UseAzureBlobDocumentStorage_ShouldFailOptionsValidation_WhenContainerNameIsMissing(
        string containerName)
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseAzureBlobDocumentStorage(options =>
            {
                options.ConnectionString = CreateConnectionString();
                options.ContainerName = containerName;
            });

        using var provider = services.BuildServiceProvider();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            provider
                .GetRequiredService<IOptions<AzureBlobDocumentStorageOptions>>()
                .Value);

        Assert.Contains(
            "Azure Blob Storage container name is required.",
            exception.Message);
    }

    [Fact]
    public void UseAzureBlobDocumentStorage_ShouldThrow_WhenBuilderIsNull()
    {
        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureBlobDocumentStorage(options =>
            {
                options.ConnectionString = CreateConnectionString();
                options.ContainerName = "invoice-documents";
            }));
    }

    [Fact]
    public void UseAzureBlobDocumentStorage_ShouldThrow_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow(ValidationDate);

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseAzureBlobDocumentStorage(null!));
    }

    private static string CreateConnectionString()
    {
        return "UseDevelopmentStorage=true";
    }
}
