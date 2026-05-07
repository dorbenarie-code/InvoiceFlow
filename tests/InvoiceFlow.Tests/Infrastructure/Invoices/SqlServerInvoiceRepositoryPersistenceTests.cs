using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Invoices;

public sealed class SqlServerInvoiceRepositoryPersistenceTests
{
    private const string ConnectionStringEnvironmentVariable =
        "INVOICEFLOW_SQLSERVER_TEST_CONNECTION_STRING";

    [Fact]
    public async Task SaveAsync_ShouldPersistInvoiceToSqlServer()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        await EnsureInvoicesTableExistsAsync(connectionString);
        await ClearInvoicesTableAsync(connectionString);

        var repository = new SqlServerInvoiceRepository(
            Options.Create(
                new SqlServerInvoiceRepositoryOptions
                {
                    ConnectionString = connectionString
                }));

        var invoice = CreateVerifiedInvoice();

        await repository.SaveAsync(invoice);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            SELECT
                Id,
                SourceDocumentId,
                VendorName,
                VendorTaxId,
                InvoiceNumber,
                IssueDate,
                SubtotalAmount,
                SubtotalCurrency,
                VatAmount,
                VatCurrency,
                TotalAmount,
                TotalCurrency,
                Status,
                MetadataJson,
                ValidationReportJson
            FROM dbo.Invoices
            WHERE Id = @Id;
            """;

        command.Parameters.AddWithValue("@Id", invoice.Id);

        await using var reader = await command.ExecuteReaderAsync();

        Assert.True(reader.Read());

        Assert.Equal(invoice.Id, reader.GetGuid(reader.GetOrdinal("Id")));
        Assert.Equal(invoice.SourceDocumentId, reader.GetGuid(reader.GetOrdinal("SourceDocumentId")));
        Assert.Equal("SQL Vendor Ltd", reader.GetString(reader.GetOrdinal("VendorName")));
        Assert.Equal("123456789", reader.GetString(reader.GetOrdinal("VendorTaxId")));
        Assert.Equal("INV-SQL-1001", reader.GetString(reader.GetOrdinal("InvoiceNumber")));
        Assert.Equal(new DateTime(2026, 4, 30), reader.GetDateTime(reader.GetOrdinal("IssueDate")));

        Assert.Equal(1000m, reader.GetDecimal(reader.GetOrdinal("SubtotalAmount")));
        Assert.Equal("ILS", reader.GetString(reader.GetOrdinal("SubtotalCurrency")));

        Assert.Equal(180m, reader.GetDecimal(reader.GetOrdinal("VatAmount")));
        Assert.Equal("ILS", reader.GetString(reader.GetOrdinal("VatCurrency")));

        Assert.Equal(1180m, reader.GetDecimal(reader.GetOrdinal("TotalAmount")));
        Assert.Equal("ILS", reader.GetString(reader.GetOrdinal("TotalCurrency")));

        Assert.Equal("Verified", reader.GetString(reader.GetOrdinal("Status")));

        var metadataJson = reader.GetString(reader.GetOrdinal("MetadataJson"));
        var validationReportJson = reader.GetString(reader.GetOrdinal("ValidationReportJson"));

        Assert.Contains("VendorName", metadataJson);
        Assert.Contains("SQL Vendor Ltd", metadataJson);
        Assert.Contains("hasIssues", validationReportJson, StringComparison.OrdinalIgnoreCase);
    }

    private static Invoice CreateVerifiedInvoice()
    {
        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: new Vendor("SQL Vendor Ltd", "123456789"),
            invoiceNumber: "INV-SQL-1001",
            issueDate: new DateOnly(2026, 4, 30),
            subtotalAmount: new CurrencyAmount(1000m, "ILS"),
            vatAmount: new CurrencyAmount(180m, "ILS"),
            totalAmount: new CurrencyAmount(1180m, "ILS"),
            metadata: new Dictionary<string, string>
            {
                ["VendorName"] = "SQL Vendor Ltd",
                ["InvoiceNumber"] = "INV-SQL-1001",
                ["TotalAmount"] = "1180",
                ["Currency"] = "ILS"
            });

        invoice.ApplyValidationReport(InvoiceValidationReport.Valid());

        return invoice;
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
                    Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    SourceDocumentId UNIQUEIDENTIFIER NOT NULL,
                    VendorName NVARCHAR(250) NULL,
                    VendorTaxId NVARCHAR(100) NULL,
                    InvoiceNumber NVARCHAR(100) NULL,
                    IssueDate DATE NULL,
                    SubtotalAmount DECIMAL(18, 2) NULL,
                    SubtotalCurrency NVARCHAR(10) NULL,
                    VatAmount DECIMAL(18, 2) NULL,
                    VatCurrency NVARCHAR(10) NULL,
                    TotalAmount DECIMAL(18, 2) NULL,
                    TotalCurrency NVARCHAR(10) NULL,
                    Status NVARCHAR(50) NOT NULL,
                    MetadataJson NVARCHAR(MAX) NOT NULL,
                    ValidationReportJson NVARCHAR(MAX) NOT NULL,
                    CreatedAtUtc DATETIME2 NOT NULL
                );
            END
            """;

        await command.ExecuteNonQueryAsync();
    }

    private static async Task ClearInvoicesTableAsync(
        string connectionString)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        command.CommandText = """
            DELETE FROM dbo.Invoices;
            """;

        await command.ExecuteNonQueryAsync();
    }
}
