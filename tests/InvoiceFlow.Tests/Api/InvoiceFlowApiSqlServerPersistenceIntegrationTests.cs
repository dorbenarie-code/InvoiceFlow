using System.Net;
using System.Net.Http.Headers;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceFlowApiSqlServerPersistenceIntegrationTests
{
    private const string SqlConnectionStringEnvironmentVariable =
        "INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING";

    [Fact]
    public async Task ProcessInvoiceEndpoint_ShouldPersistInvoiceToSqlServer_WhenSqlServerIsConfigured()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            SqlConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await EnsureInvoicesTableExistsAsync(connectionString);
        await ClearInvoicesTableAsync(connectionString);

        using var factory = CreateFactory(connectionString);
        using var client = factory.CreateClient();

        using var content = new MultipartFormDataContent();

        var fileBytes = "%PDF-1.7 fake invoice content"u8.ToArray();

        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");

        content.Add(
            fileContent,
            "file",
            "invoice.pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TOP (1)
                VendorName,
                VendorTaxId,
                InvoiceNumber,
                Status,
                MetadataJson,
                ValidationReportJson
            FROM dbo.Invoices
            ORDER BY CreatedAtUtc DESC;
            """;

        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(await reader.ReadAsync());

        Assert.Equal("Cohen Office Supplies Ltd", reader.GetString(0));
        Assert.Equal("516789123", reader.GetString(1));
        Assert.Equal("INV-1001", reader.GetString(2));
        Assert.Equal("Verified", reader.GetString(3));

        var metadataJson = reader.GetString(4);
        var validationReportJson = reader.GetString(5);

        Assert.Contains("VendorName", metadataJson);
        Assert.Contains("Cohen Office Supplies Ltd", metadataJson);
        Assert.Contains("InvoiceNumber", metadataJson);
        Assert.Contains("INV-1001", metadataJson);
        Assert.Contains("hasErrors", validationReportJson);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string connectionString)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = "10485760",
                            [$"{SqlServerInvoiceRepositoryOptions.ConfigurationSectionName}:ConnectionString"] =
                                connectionString
                        });
                });
            });
    }

    private static async Task EnsureInvoicesTableExistsAsync(
        string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            IF OBJECT_ID(N'dbo.Invoices', N'U') IS NULL
            BEGIN
                CREATE TABLE dbo.Invoices
                (
                    Id uniqueidentifier NOT NULL CONSTRAINT PK_Invoices PRIMARY KEY,
                    SourceDocumentId uniqueidentifier NOT NULL,
                    VendorName nvarchar(250) NULL,
                    VendorTaxId nvarchar(100) NULL,
                    InvoiceNumber nvarchar(100) NULL,
                    IssueDate date NULL,
                    SubtotalAmount decimal(18, 2) NULL,
                    SubtotalCurrency nvarchar(10) NULL,
                    VatAmount decimal(18, 2) NULL,
                    VatCurrency nvarchar(10) NULL,
                    TotalAmount decimal(18, 2) NULL,
                    TotalCurrency nvarchar(10) NULL,
                    Status nvarchar(50) NOT NULL,
                    MetadataJson nvarchar(max) NOT NULL,
                    ValidationReportJson nvarchar(max) NOT NULL,
                    CreatedAtUtc datetime2(7) NOT NULL
                        CONSTRAINT DF_Invoices_CreatedAtUtc DEFAULT SYSUTCDATETIME()
                );
            END;
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearInvoicesTableAsync(
        string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM dbo.Invoices;";

        await command.ExecuteNonQueryAsync();
    }
}
