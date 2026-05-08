using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

public sealed class FakeDocumentExtractor : IDocumentExtractor
{
    private readonly ExtractedDocument _extractedDocument;

    public FakeDocumentExtractor()
        : this(CreateDefaultExtractedDocument())
    {
    }

    public FakeDocumentExtractor(ExtractedDocument extractedDocument)
    {
        _extractedDocument = extractedDocument
            ?? throw new ArgumentNullException(nameof(extractedDocument));
    }

    public Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(_extractedDocument);
    }

    private static ExtractedDocument CreateDefaultExtractedDocument()
    {
        return new ExtractedDocument(
            "Fake extracted invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Cohen Office Supplies Ltd",
                ["VendorTaxId"] = "516789123",
                ["InvoiceNumber"] = "INV-1001",
                ["IssueDate"] = "2026-04-30",
                ["SubtotalAmount"] = "1000",
                ["VatAmount"] = "180",
                ["TotalAmount"] = "1180",
                ["Currency"] = "ILS"
            },
            analyzedPageCount: 1);
    }
}