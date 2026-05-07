namespace InvoiceFlow.Infrastructure.Invoices;

public sealed class SqlServerInvoiceRepositoryOptions
{
    public const string ConfigurationSectionName = "InvoiceFlow:SqlServer";

    public string ConnectionString { get; set; } = string.Empty;
}
