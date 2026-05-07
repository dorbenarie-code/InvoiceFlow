namespace InvoiceFlow.Infrastructure.Documents;

public sealed class AzureBlobDocumentStorageOptions
{
    public const string ConfigurationSectionName =
        "InvoiceFlow:AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;
}
