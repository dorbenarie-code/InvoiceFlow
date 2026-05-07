using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceSdkClientFieldMappingTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldMapSupportedAzureInvoiceFieldsToExtractedDocumentFields()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["VendorName"] = CreateStringField("Azure Vendor Ltd"),
                ["VendorTaxId"] = CreateStringField("123456789"),
                ["InvoiceId"] = CreateStringField("INV-AZ-1001"),
                ["InvoiceDate"] = CreateDateField(new DateOnly(2026, 4, 30)),
                ["SubTotal"] = CreateCurrencyField(1000, "ILS"),
                ["TotalTax"] = CreateCurrencyField(180, "ILS"),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS")
            });

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("azure invoice raw text", result.RawText);

        Assert.Equal("Azure Vendor Ltd", result.Fields["VendorName"]);
        Assert.Equal("123456789", result.Fields["VendorTaxId"]);
        Assert.Equal("INV-AZ-1001", result.Fields["InvoiceNumber"]);
        Assert.Equal("2026-04-30", result.Fields["IssueDate"]);
        Assert.Equal("1000", result.Fields["SubtotalAmount"]);
        Assert.Equal("180", result.Fields["VatAmount"]);
        Assert.Equal("1180", result.Fields["TotalAmount"]);
        Assert.Equal("ILS", result.Fields["Currency"]);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldIgnoreMissingAzureFields()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["VendorName"] = CreateStringField("Partial Azure Vendor Ltd"),
                ["InvoiceId"] = CreateStringField("INV-PARTIAL-1001")
            });

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("azure invoice raw text", result.RawText);

        Assert.Equal(2, result.Fields.Count);
        Assert.Equal("Partial Azure Vendor Ltd", result.Fields["VendorName"]);
        Assert.Equal("INV-PARTIAL-1001", result.Fields["InvoiceNumber"]);

        Assert.False(result.Fields.ContainsKey("VendorTaxId"));
        Assert.False(result.Fields.ContainsKey("IssueDate"));
        Assert.False(result.Fields.ContainsKey("SubtotalAmount"));
        Assert.False(result.Fields.ContainsKey("VatAmount"));
        Assert.False(result.Fields.ContainsKey("TotalAmount"));
        Assert.False(result.Fields.ContainsKey("Currency"));
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldIgnoreAzureFieldsBelowMinimumConfidenceThreshold()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["VendorName"] = CreateStringField("High Confidence Vendor Ltd", confidence: 0.95f),
                ["VendorTaxId"] = CreateStringField("123456789", confidence: 0.79f),
                ["InvoiceId"] = CreateStringField("INV-LOW-1001", confidence: 0.79f),
                ["InvoiceDate"] = CreateDateField(new DateOnly(2026, 4, 30), confidence: 0.79f),
                ["SubTotal"] = CreateCurrencyField(1000, "ILS", confidence: 0.79f),
                ["TotalTax"] = CreateCurrencyField(180, "ILS", confidence: 0.79f),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS", confidence: 0.79f)
            });

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest(minimumConfidenceThreshold: 0.8f);

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("azure invoice raw text", result.RawText);

        var field = Assert.Single(result.Fields);

        Assert.Equal("VendorName", field.Key);
        Assert.Equal("High Confidence Vendor Ltd", field.Value);

        Assert.False(result.Fields.ContainsKey("VendorTaxId"));
        Assert.False(result.Fields.ContainsKey("InvoiceNumber"));
        Assert.False(result.Fields.ContainsKey("IssueDate"));
        Assert.False(result.Fields.ContainsKey("SubtotalAmount"));
        Assert.False(result.Fields.ContainsKey("VatAmount"));
        Assert.False(result.Fields.ContainsKey("TotalAmount"));
        Assert.False(result.Fields.ContainsKey("Currency"));
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnRawTextAndEmptyFields_WhenDocumentHasNoFields()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>());

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("azure invoice raw text", result.RawText);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnRawTextAndEmptyFields_WhenAnalyzeResultHasNoDocuments()
    {
        var analyzeResult = CreateAnalyzeResultWithoutDocuments();

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("azure invoice raw text", result.RawText);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldUseInvoiceTotalCurrency_WhenMultipleCurrencyFieldsExist()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["SubTotal"] = CreateCurrencyField(1000, "USD"),
                ["TotalTax"] = CreateCurrencyField(180, "EUR"),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS")
            });

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("1000", result.Fields["SubtotalAmount"]);
        Assert.Equal("180", result.Fields["VatAmount"]);
        Assert.Equal("1180", result.Fields["TotalAmount"]);
        Assert.Equal("ILS", result.Fields["Currency"]);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldUseSubTotalCurrency_WhenInvoiceTotalCurrencyIsBelowMinimumConfidenceThreshold()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["SubTotal"] = CreateCurrencyField(1000, "USD", confidence: 0.95f),
                ["TotalTax"] = CreateCurrencyField(180, "EUR", confidence: 0.95f),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS", confidence: 0.79f)
            });

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest(minimumConfidenceThreshold: 0.8f);

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("1000", result.Fields["SubtotalAmount"]);
        Assert.Equal("180", result.Fields["VatAmount"]);
        Assert.False(result.Fields.ContainsKey("TotalAmount"));
        Assert.Equal("USD", result.Fields["Currency"]);
    }

    private static AnalyzeResult CreateAnalyzeResult(
        IReadOnlyDictionary<string, DocumentField> fields)
    {
        var fieldDictionary = DocumentIntelligenceModelFactory
            .DocumentFieldDictionary(fields);

        var document = DocumentIntelligenceModelFactory.AnalyzedDocument(
            "prebuilt:invoice",
            boundingRegions: null,
            spans: null,
            fields: fieldDictionary,
            confidence: 0.99f);

        return DocumentIntelligenceModelFactory.AnalyzeResult(
            modelId: AzureDocumentIntelligenceOptions.DefaultModelId,
            content: "azure invoice raw text",
            documents:
            [
                document
            ]);
    }

    private static AnalyzeResult CreateAnalyzeResultWithoutDocuments()
    {
        return DocumentIntelligenceModelFactory.AnalyzeResult(
            modelId: AzureDocumentIntelligenceOptions.DefaultModelId,
            content: "azure invoice raw text",
            documents: []);
    }

    private static DocumentField CreateStringField(
        string value,
        float confidence = 0.99f)
    {
        return DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.String,
            valueString: value,
            content: value,
            confidence: confidence);
    }

    private static DocumentField CreateDateField(
        DateOnly value,
        float confidence = 0.99f)
    {
        var date = new DateTimeOffset(
            value.Year,
            value.Month,
            value.Day,
            0,
            0,
            0,
            TimeSpan.Zero);

        return DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Date,
            valueDate: date,
            content: value.ToString("yyyy-MM-dd"),
            confidence: confidence);
    }

    private static DocumentField CreateCurrencyField(
        double amount,
        string currencyCode,
        float confidence = 0.99f)
    {
        var currency = DocumentIntelligenceModelFactory.CurrencyValue(
            amount: amount,
            currencyCode: currencyCode);

        return DocumentIntelligenceModelFactory.DocumentField(
            fieldType: DocumentFieldType.Currency,
            valueCurrency: currency,
            content: amount.ToString("0.##"),
            confidence: confidence);
    }

    private static AzureDocumentIntelligenceAnalyzeRequest CreateRequest(
        float minimumConfidenceThreshold = 0.8f)
    {
        return new AzureDocumentIntelligenceAnalyzeRequest(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            CreateDocument(),
            minimumConfidenceThreshold);
    }

    private static DocumentInput CreateDocument()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            CreatePdfBytes());
    }

    private static byte[] CreatePdfBytes()
    {
        return new byte[]
        {
            0x25, 0x50, 0x44, 0x46, 0x2D,
            0x31, 0x2E, 0x37,
            0x0A
        };
    }

    private sealed class FixedAnalyzeResultDocumentIntelligenceClient
        : DocumentIntelligenceClient
    {
        private readonly AnalyzeResult _result;

        public FixedAnalyzeResultDocumentIntelligenceClient(
            AnalyzeResult result)
        {
            _result = result;
        }

        public override Operation<AnalyzeResult> AnalyzeDocument(
            WaitUntil waitUntil,
            string modelId,
            BinaryData bytesSource,
            CancellationToken cancellationToken = default)
        {
            return new CompletedAnalyzeDocumentOperation(_result);
        }
    }

    private sealed class CompletedAnalyzeDocumentOperation
        : Operation<AnalyzeResult>
    {
        private readonly AnalyzeResult _value;

        public CompletedAnalyzeDocumentOperation(AnalyzeResult value)
        {
            _value = value;
        }

        public override string Id => "completed-test-operation";

        public override bool HasCompleted => true;

        public override bool HasValue => true;

        public override AnalyzeResult Value => _value;

        public override Response GetRawResponse()
        {
            return new FakeResponse();
        }

        public override Response UpdateStatus(
            CancellationToken cancellationToken = default)
        {
            return new FakeResponse();
        }

        public override ValueTask<Response> UpdateStatusAsync(
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<Response>(new FakeResponse());
        }
    }

    private sealed class FakeResponse : Response
    {
        public override int Status => 200;

        public override string ReasonPhrase => "OK";

        public override Stream? ContentStream
        {
            get => null;
            set
            {
            }
        }

        public override string ClientRequestId
        {
            get => string.Empty;
            set
            {
            }
        }

        public override void Dispose()
        {
        }

        protected override bool ContainsHeader(string name)
        {
            return false;
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            return Array.Empty<HttpHeader>();
        }

        protected override bool TryGetHeader(
            string name,
            out string value)
        {
            value = string.Empty;
            return false;
        }

        protected override bool TryGetHeaderValues(
            string name,
            out IEnumerable<string> values)
        {
            values = Array.Empty<string>();
            return false;
        }
    }
}
