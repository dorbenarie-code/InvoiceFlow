using System.Globalization;
using Azure;
using Azure.AI.DocumentIntelligence;
using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Infrastructure.Documents;

internal sealed class AzureDocumentIntelligenceSdkClient
    : IAzureDocumentIntelligenceClient
{
    private readonly DocumentIntelligenceClient _client;

    public AzureDocumentIntelligenceSdkClient(
        DocumentIntelligenceClient client)
    {
        _client = client
            ?? throw new ArgumentNullException(nameof(client));
    }

    public async Task<ExtractedDocument> AnalyzeAsync(
        AzureDocumentIntelligenceAnalyzeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        cancellationToken.ThrowIfCancellationRequested();

        await using var documentStream = await request.Document.OpenReadStreamAsync(
            cancellationToken);

        var documentContent = await BinaryData.FromStreamAsync(
            documentStream,
            cancellationToken);

        var operation = _client.AnalyzeDocument(
            WaitUntil.Completed,
            request.ModelId,
            documentContent,
            cancellationToken);

        var analyzeResult = operation.Value;
        var rawText = analyzeResult.Content ?? string.Empty;

        var fields = MapInvoiceFields(
            analyzeResult,
            request.MinimumConfidenceThreshold);

        return new ExtractedDocument(
            rawText,
            fields);
    }

    private static IReadOnlyDictionary<string, string> MapInvoiceFields(
        AnalyzeResult analyzeResult,
        float minimumConfidenceThreshold)
    {
        var mappedFields = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        var document = analyzeResult.Documents.FirstOrDefault();

        if (document is null)
        {
            return mappedFields;
        }

        var azureFields = document.Fields;

        AddStringField(
            mappedFields,
            azureFields,
            azureFieldName: "VendorName",
            targetFieldName: "VendorName",
            minimumConfidenceThreshold);

        AddStringField(
            mappedFields,
            azureFields,
            azureFieldName: "VendorTaxId",
            targetFieldName: "VendorTaxId",
            minimumConfidenceThreshold);

        AddStringField(
            mappedFields,
            azureFields,
            azureFieldName: "InvoiceId",
            targetFieldName: "InvoiceNumber",
            minimumConfidenceThreshold);

        AddStringField(
            mappedFields,
            azureFields,
            azureFieldName: "InvoiceNumber",
            targetFieldName: "InvoiceNumber",
            minimumConfidenceThreshold);

        AddDateField(
            mappedFields,
            azureFields,
            azureFieldName: "InvoiceDate",
            targetFieldName: "IssueDate",
            minimumConfidenceThreshold);

        AddCurrencyAmountField(
            mappedFields,
            azureFields,
            azureFieldName: "SubTotal",
            targetFieldName: "SubtotalAmount",
            minimumConfidenceThreshold);

        AddCurrencyAmountField(
            mappedFields,
            azureFields,
            azureFieldName: "TotalTax",
            targetFieldName: "VatAmount",
            minimumConfidenceThreshold);

        AddCurrencyAmountField(
            mappedFields,
            azureFields,
            azureFieldName: "InvoiceTotal",
            targetFieldName: "TotalAmount",
            minimumConfidenceThreshold);

        var currency = ExtractCurrency(
            azureFields,
            minimumConfidenceThreshold);

        if (!string.IsNullOrWhiteSpace(currency))
        {
            mappedFields["Currency"] = currency;
        }

        return mappedFields;
    }

    private static void AddStringField(
        IDictionary<string, string> mappedFields,
        IReadOnlyDictionary<string, DocumentField> azureFields,
        string azureFieldName,
        string targetFieldName,
        float minimumConfidenceThreshold)
    {
        if (!TryGetFieldAboveThreshold(
                azureFields,
                azureFieldName,
                minimumConfidenceThreshold,
                out var field))
        {
            return;
        }

        var value = field.ValueString;

        if (string.IsNullOrWhiteSpace(value))
        {
            value = field.Content;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        mappedFields[targetFieldName] = value.Trim();
    }

    private static void AddDateField(
        IDictionary<string, string> mappedFields,
        IReadOnlyDictionary<string, DocumentField> azureFields,
        string azureFieldName,
        string targetFieldName,
        float minimumConfidenceThreshold)
    {
        if (!TryGetFieldAboveThreshold(
                azureFields,
                azureFieldName,
                minimumConfidenceThreshold,
                out var field))
        {
            return;
        }

        if (field.FieldType == DocumentFieldType.Date
            && field.ValueDate.HasValue)
        {
            mappedFields[targetFieldName] = field.ValueDate.Value.ToString(
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture);

            return;
        }

        if (!string.IsNullOrWhiteSpace(field.Content))
        {
            mappedFields[targetFieldName] = field.Content.Trim();
        }
    }

    private static void AddCurrencyAmountField(
        IDictionary<string, string> mappedFields,
        IReadOnlyDictionary<string, DocumentField> azureFields,
        string azureFieldName,
        string targetFieldName,
        float minimumConfidenceThreshold)
    {
        if (!TryGetCurrencyFieldAboveThreshold(
                azureFields,
                azureFieldName,
                minimumConfidenceThreshold,
                out var currencyValue))
        {
            return;
        }

        mappedFields[targetFieldName] = FormatAmount(currencyValue.Amount);
    }

    private static string? ExtractCurrency(
        IReadOnlyDictionary<string, DocumentField> azureFields,
        float minimumConfidenceThreshold)
    {
        string[] currencyFieldPriority =
        [
            "InvoiceTotal",
            "SubTotal",
            "TotalTax"
        ];

        foreach (var fieldName in currencyFieldPriority)
        {
            if (!TryGetCurrencyFieldAboveThreshold(
                    azureFields,
                    fieldName,
                    minimumConfidenceThreshold,
                    out var currencyValue))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(currencyValue.CurrencyCode))
            {
                return currencyValue.CurrencyCode.Trim();
            }

            if (!string.IsNullOrWhiteSpace(currencyValue.CurrencySymbol))
            {
                return currencyValue.CurrencySymbol.Trim();
            }
        }

        return null;
    }

    private static bool TryGetCurrencyFieldAboveThreshold(
        IReadOnlyDictionary<string, DocumentField> azureFields,
        string fieldName,
        float minimumConfidenceThreshold,
        out CurrencyValue currencyValue)
    {
        currencyValue = null!;

        if (!TryGetFieldAboveThreshold(
                azureFields,
                fieldName,
                minimumConfidenceThreshold,
                out var field))
        {
            return false;
        }

        if (field.FieldType != DocumentFieldType.Currency)
        {
            return false;
        }

        if (field.ValueCurrency is null)
        {
            return false;
        }

        currencyValue = field.ValueCurrency;

        return true;
    }

    private static bool TryGetFieldAboveThreshold(
        IReadOnlyDictionary<string, DocumentField> azureFields,
        string fieldName,
        float minimumConfidenceThreshold,
        out DocumentField field)
    {
        field = null!;

        if (!azureFields.TryGetValue(fieldName, out var matchedField))
        {
            return false;
        }

        if (matchedField.Confidence < minimumConfidenceThreshold)
        {
            return false;
        }

        field = matchedField;

        return true;
    }

    private static string FormatAmount(double amount)
    {
        return amount.ToString(
            "0.################",
            CultureInfo.InvariantCulture);
    }
}
