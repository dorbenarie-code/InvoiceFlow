namespace InvoiceFlow.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceOptions
{
    public const string ConfigurationSectionName =
        "InvoiceFlow:AzureDocumentIntelligence";

    public const string DefaultModelId = "prebuilt-invoice";

    public const float DefaultMinimumConfidenceThreshold = 0.8f;

    public string Endpoint { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string ModelId { get; set; } = DefaultModelId;

    public float MinimumConfidenceThreshold { get; set; } =
        DefaultMinimumConfidenceThreshold;
}
