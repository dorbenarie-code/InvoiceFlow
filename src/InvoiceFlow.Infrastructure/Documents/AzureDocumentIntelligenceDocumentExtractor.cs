using InvoiceFlow.Application.Documents;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceDocumentExtractor : IDocumentExtractor
{
    private readonly AzureDocumentIntelligenceOptions _options;
    private readonly IAzureDocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceDocumentExtractor(
        IOptions<AzureDocumentIntelligenceOptions> options)
        : this(
            options,
            new NotImplementedAzureDocumentIntelligenceClient())
    {
    }

    internal AzureDocumentIntelligenceDocumentExtractor(
        IOptions<AzureDocumentIntelligenceOptions> options,
        IAzureDocumentIntelligenceClient client)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
        _client = client
            ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        cancellationToken.ThrowIfCancellationRequested();

        var request = new AzureDocumentIntelligenceAnalyzeRequest(
            _options.ModelId,
            document,
            _options.MinimumConfidenceThreshold);

        var extractedDocument = await _client.AnalyzeAsync(
            request,
            cancellationToken);

        if (extractedDocument is null)
        {
            throw new InvalidOperationException(
                "Azure Document Intelligence client returned no extracted document.");
        }

        return extractedDocument;
    }
}
