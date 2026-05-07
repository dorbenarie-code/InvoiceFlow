using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

internal sealed record AzureDocumentIntelligenceAnalyzeRequest
{
    public string ModelId { get; }

    public DocumentInput Document { get; }

    public float MinimumConfidenceThreshold { get; }

    public AzureDocumentIntelligenceAnalyzeRequest(
        string modelId,
        DocumentInput document)
        : this(
            modelId,
            document,
            AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold)
    {
    }

    public AzureDocumentIntelligenceAnalyzeRequest(
        string modelId,
        DocumentInput document,
        float minimumConfidenceThreshold)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException(
                "Azure Document Intelligence model id is required.",
                nameof(modelId));
        }

        ArgumentNullException.ThrowIfNull(document);

        if (minimumConfidenceThreshold is < 0f or > 1f)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumConfidenceThreshold),
                minimumConfidenceThreshold,
                "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.");
        }

        ModelId = modelId.Trim();
        Document = document;
        MinimumConfidenceThreshold = minimumConfidenceThreshold;
    }
}
