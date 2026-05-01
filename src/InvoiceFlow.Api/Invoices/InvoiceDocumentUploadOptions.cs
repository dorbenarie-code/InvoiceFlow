namespace InvoiceFlow.Api.Invoices;

public sealed class InvoiceDocumentUploadOptions
{
    public const string ConfigurationSectionName = "InvoiceFlow:Upload";
    public const long DefaultMaxFileSizeInBytes = 10 * 1024 * 1024;

    public long MaxFileSizeInBytes { get; set; } = DefaultMaxFileSizeInBytes;
}
