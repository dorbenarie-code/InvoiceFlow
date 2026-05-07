using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.Invoices;

public sealed class SqlServerInvoiceRepository : IInvoiceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private readonly SqlServerInvoiceRepositoryOptions _options;

    public SqlServerInvoiceRepository(
        IOptions<SqlServerInvoiceRepositoryOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public async Task SaveAsync(
        Invoice invoice,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                "SQL Server invoice repository connection string is required.");
        }

        await using var connection = new SqlConnection(
            _options.ConnectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();

        command.CommandText = """
            INSERT INTO dbo.Invoices
            (
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
                ValidationReportJson,
                CreatedAtUtc
            )
            VALUES
            (
                @Id,
                @SourceDocumentId,
                @VendorName,
                @VendorTaxId,
                @InvoiceNumber,
                @IssueDate,
                @SubtotalAmount,
                @SubtotalCurrency,
                @VatAmount,
                @VatCurrency,
                @TotalAmount,
                @TotalCurrency,
                @Status,
                @MetadataJson,
                @ValidationReportJson,
                SYSUTCDATETIME()
            );
            """;

        AddGuidParameter(command, "@Id", invoice.Id);
        AddGuidParameter(command, "@SourceDocumentId", invoice.SourceDocumentId);

        AddStringParameter(command, "@VendorName", invoice.Vendor?.Name, 250);
        AddStringParameter(command, "@VendorTaxId", invoice.Vendor?.TaxId, 100);
        AddStringParameter(command, "@InvoiceNumber", invoice.InvoiceNumber, 100);

        AddDateParameter(command, "@IssueDate", invoice.IssueDate);

        AddDecimalParameter(command, "@SubtotalAmount", invoice.SubtotalAmount?.Amount);
        AddStringParameter(command, "@SubtotalCurrency", invoice.SubtotalAmount?.Currency, 10);

        AddDecimalParameter(command, "@VatAmount", invoice.VatAmount?.Amount);
        AddStringParameter(command, "@VatCurrency", invoice.VatAmount?.Currency, 10);

        AddDecimalParameter(command, "@TotalAmount", invoice.TotalAmount?.Amount);
        AddStringParameter(command, "@TotalCurrency", invoice.TotalAmount?.Currency, 10);

        AddStringParameter(command, "@Status", invoice.Status.ToString(), 50);

        AddJsonParameter(
            command,
            "@MetadataJson",
            JsonSerializer.Serialize(invoice.Metadata, JsonOptions));

        AddJsonParameter(
            command,
            "@ValidationReportJson",
            JsonSerializer.Serialize(invoice.ValidationReport, JsonOptions));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static void AddGuidParameter(
        SqlCommand command,
        string name,
        Guid value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.UniqueIdentifier)
            {
                Value = value
            });
    }

    private static void AddStringParameter(
        SqlCommand command,
        string name,
        string? value,
        int size)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.NVarChar, size)
            {
                Value = string.IsNullOrWhiteSpace(value)
                    ? DBNull.Value
                    : value
            });
    }

    private static void AddDateParameter(
        SqlCommand command,
        string name,
        DateOnly? value)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.Date)
            {
                Value = value is null
                    ? DBNull.Value
                    : value.Value.ToDateTime(TimeOnly.MinValue)
            });
    }

    private static void AddDecimalParameter(
        SqlCommand command,
        string name,
        decimal? value)
    {
        var parameter = new SqlParameter(name, SqlDbType.Decimal)
        {
            Precision = 18,
            Scale = 2,
            Value = value is null
                ? DBNull.Value
                : value.Value
        };

        command.Parameters.Add(parameter);
    }

    private static void AddJsonParameter(
        SqlCommand command,
        string name,
        string json)
    {
        command.Parameters.Add(
            new SqlParameter(name, SqlDbType.NVarChar, -1)
            {
                Value = json
            });
    }
}
