using InvoiceFlow.Api.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceDocumentUploadOptionsTests
{
    [Fact]
    public void DefaultMaxFileSizeInBytes_ShouldBeTenMegabytes()
    {
        Assert.Equal(
            10 * 1024 * 1024,
            InvoiceDocumentUploadOptions.DefaultMaxFileSizeInBytes);
    }

    [Fact]
    public void ConfigurationSectionName_ShouldMatchExpectedUploadSection()
    {
        Assert.Equal(
            "InvoiceFlow:Upload",
            InvoiceDocumentUploadOptions.ConfigurationSectionName);
    }

    [Fact]
    public void Options_ShouldUseDefaultMaxFileSize_WhenConfigurationSectionIsMissing()
    {
        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<IOptions<InvoiceDocumentUploadOptions>>()
            .Value;

        Assert.Equal(
            InvoiceDocumentUploadOptions.DefaultMaxFileSizeInBytes,
            options.MaxFileSizeInBytes);
    }

    [Fact]
    public void Options_ShouldBindMaxFileSizeFromConfiguration()
    {
        const long configuredMaxFileSize = 5 * 1024 * 1024;

        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["InvoiceFlow:Upload:MaxFileSizeInBytes"] =
                    configuredMaxFileSize.ToString()
            });

        using var client = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<IOptions<InvoiceDocumentUploadOptions>>()
            .Value;

        Assert.Equal(
            configuredMaxFileSize,
            options.MaxFileSizeInBytes);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void ApiStartup_ShouldFail_WhenUploadMaxFileSizeIsNotGreaterThanZero(
        string invalidMaxFileSize)
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = invalidMaxFileSize
            });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            factory.CreateClient());

        Assert.Contains(
            "Maximum invoice document file size must be greater than zero.",
            exception.Message);
    }

    [Fact]
    public void ApiStartup_ShouldSucceed_WhenUploadMaxFileSizeIsOneByte()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = "1"
            });

        using var client = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<IOptions<InvoiceDocumentUploadOptions>>()
            .Value;

        Assert.Equal(
            1,
            options.MaxFileSizeInBytes);
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