using System.Collections.ObjectModel;

namespace InvoiceFlow.Application.Documents;

public sealed record ExtractedDocument
{
    public string RawText { get; }
    public IReadOnlyDictionary<string, string> Fields { get; }

    public ExtractedDocument(
        string rawText,
        IReadOnlyDictionary<string, string>? fields = null)
    {
        RawText = rawText?.Trim() ?? string.Empty;

        var copiedFields = fields is null
            ? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(
                fields,
                StringComparer.OrdinalIgnoreCase);

        Fields = new ReadOnlyDictionary<string, string>(copiedFields);
    }
}