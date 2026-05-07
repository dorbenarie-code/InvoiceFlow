using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Invoices;

public sealed class SqlServerInvoiceRepositoryOptionsTests
{
    [Fact]
    public void ConfigurationSectionName_ShouldMatchExpectedSqlServerSection()
    {
        Assert.Equal(
            "InvoiceFlow:SqlServer",
            SqlServerInvoiceRepositoryOptions.ConfigurationSectionName);
    }

    [Fact]
    public void Options_ShouldDefaultConnectionStringToEmpty()
    {
        var options = new SqlServerInvoiceRepositoryOptions();

        Assert.Equal(string.Empty, options.ConnectionString);
    }

    [Fact]
    public void UseSqlServerInvoiceRepository_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services
            .AddInvoiceFlow()
            .UseInMemoryInfrastructure();

        var returnedBuilder = builder.UseSqlServerInvoiceRepository(options =>
        {
            options.ConnectionString = CreateConnectionString();
        });

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseSqlServerInvoiceRepository_ShouldRegisterConnectionStringOptions()
    {
        using var serviceProvider = CreateServiceProvider(
            CreateConnectionString());

        var options = serviceProvider
            .GetRequiredService<IOptions<SqlServerInvoiceRepositoryOptions>>()
            .Value;

        Assert.Equal(
            CreateConnectionString(),
            options.ConnectionString);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void UseSqlServerInvoiceRepository_ShouldFailValidation_WhenConnectionStringIsMissing(
        string connectionString)
    {
        using var serviceProvider = CreateServiceProvider(
            connectionString);

        var exception = Assert.Throws<OptionsValidationException>(() =>
            serviceProvider
                .GetRequiredService<IOptions<SqlServerInvoiceRepositoryOptions>>()
                .Value);

        Assert.Contains(
            "SQL Server invoice repository connection string is required.",
            exception.Message);
    }

    [Fact]
    public void UseSqlServerInvoiceRepository_ShouldReplaceInMemoryInvoiceRepositoryRegistration()
    {
        using var serviceProvider = CreateServiceProvider(
            CreateConnectionString());

        var repository = serviceProvider
            .GetRequiredService<IInvoiceRepository>();

        Assert.IsType<SqlServerInvoiceRepository>(repository);
    }

    private static ServiceProvider CreateServiceProvider(
        string connectionString)
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseInMemoryInfrastructure()
            .UseSqlServerInvoiceRepository(options =>
            {
                options.ConnectionString = connectionString;
            });

        return services.BuildServiceProvider();
    }

    private static string CreateConnectionString()
    {
        return
            "Server=localhost;" +
            "Database=InvoiceFlowTests;" +
            "User Id=sa;" +
            "Password=Your_password123;" +
            "TrustServerCertificate=True;";
    }
}
