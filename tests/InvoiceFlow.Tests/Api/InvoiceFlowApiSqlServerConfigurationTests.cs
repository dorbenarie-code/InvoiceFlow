using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceFlowApiSqlServerConfigurationTests
{
    [Fact]
    public void ApiStartup_ShouldUseInMemoryInvoiceRepository_WhenSqlServerConfigurationIsMissing()
    {
        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<IInvoiceRepository>();

        Assert.IsNotType<SqlServerInvoiceRepository>(repository);
    }

    [Fact]
    public void ApiStartup_ShouldUseSqlServerInvoiceRepository_WhenSqlServerConnectionStringIsConfigured()
    {
        const string connectionString =
            "Server=localhost;Database=InvoiceFlowTests;User Id=invoiceflow_test;Password=test;TrustServerCertificate=True;";

        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["InvoiceFlow:SqlServer:ConnectionString"] = connectionString
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var repository = scope.ServiceProvider
            .GetRequiredService<IInvoiceRepository>();

        var options = scope.ServiceProvider
            .GetRequiredService<IOptions<SqlServerInvoiceRepositoryOptions>>()
            .Value;

        Assert.IsType<SqlServerInvoiceRepository>(repository);
        Assert.Equal(connectionString, options.ConnectionString);
    }

    [Fact]
    public void ApiStartup_ShouldFail_WhenSqlServerSectionExistsButConnectionStringIsMissing()
    {
        using var factory = CreateFactory(
            new Dictionary<string, string?>
            {
                ["InvoiceFlow:SqlServer:ConnectionString"] = " "
            });

        using var client = factory.CreateClient();

        using var scope = factory.Services.CreateScope();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            scope.ServiceProvider
                .GetRequiredService<IOptions<SqlServerInvoiceRepositoryOptions>>()
                .Value);

        Assert.Contains(
            "SQL Server invoice repository connection string is required.",
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
