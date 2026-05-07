using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Integration;

public sealed class AzureDocumentIntelligencePipelineIntegrationTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public async Task ProcessAsync_ShouldProduceVerifiedInvoice_WhenAzureMappedFieldsAreValid()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["VendorName"] = CreateStringField("Azure Vendor Ltd"),
                ["VendorTaxId"] = CreateStringField("123456789"),
                ["InvoiceId"] = CreateStringField("INV-AZ-PIPELINE-1001"),
                ["InvoiceDate"] = CreateDateField(new DateOnly(2026, 4, 30)),
                ["SubTotal"] = CreateCurrencyField(1000, "ILS"),
                ["TotalTax"] = CreateCurrencyField(180, "ILS"),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS")
            });

        var documentStorage = new InMemoryDocumentStorage();
        var documentExtractor = CreateAzureDocumentExtractor(analyzeResult);
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.Verified, result.Status);
        Assert.False(result.ValidationReport.HasIssues);

        Assert.Single(documentStorage.Documents);
        Assert.Single(invoiceRepository.Invoices);

        var savedInvoice = invoiceRepository.Invoices.Single();

        Assert.Equal(result.InvoiceId, savedInvoice.Id);
        Assert.Equal(result.DocumentId, savedInvoice.SourceDocumentId);

        Assert.Equal("Azure Vendor Ltd", savedInvoice.Vendor?.Name);
        Assert.Equal("123456789", savedInvoice.Vendor?.TaxId);
        Assert.Equal("INV-AZ-PIPELINE-1001", savedInvoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), savedInvoice.IssueDate);

        Assert.Equal(1000, savedInvoice.SubtotalAmount?.Amount);
        Assert.Equal(180, savedInvoice.VatAmount?.Amount);
        Assert.Equal(1180, savedInvoice.TotalAmount?.Amount);

        Assert.Equal("ILS", savedInvoice.SubtotalAmount?.Currency);
        Assert.Equal("ILS", savedInvoice.VatAmount?.Currency);
        Assert.Equal("ILS", savedInvoice.TotalAmount?.Currency);

        Assert.Equal("Azure Vendor Ltd", savedInvoice.Metadata["VendorName"]);
        Assert.Equal("INV-AZ-PIPELINE-1001", savedInvoice.Metadata["InvoiceNumber"]);
        Assert.Equal("1180", savedInvoice.Metadata["TotalAmount"]);
        Assert.Equal("ILS", savedInvoice.Metadata["Currency"]);
    }

    [Fact]
    public async Task ProcessAsync_ShouldRequireHumanReview_WhenAzureRequiredFieldsAreMissingOrBelowConfidenceThreshold()
    {
        var analyzeResult = CreateAnalyzeResult(
            new Dictionary<string, DocumentField>
            {
                ["VendorName"] = CreateStringField("Partial Azure Vendor Ltd", confidence: 0.95f),
                ["InvoiceId"] = CreateStringField("INV-AZ-PARTIAL-1001", confidence: 0.95f),
                ["InvoiceDate"] = CreateDateField(new DateOnly(2026, 4, 30), confidence: 0.95f),

                ["SubTotal"] = CreateCurrencyField(1000, "ILS", confidence: 0.79f),
                ["TotalTax"] = CreateCurrencyField(180, "ILS", confidence: 0.79f),
                ["InvoiceTotal"] = CreateCurrencyField(1180, "ILS", confidence: 0.79f)
            });

        var documentStorage = new InMemoryDocumentStorage();
        var documentExtractor = CreateAzureDocumentExtractor(analyzeResult);
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.RequiresHumanReview, result.Status);
        Assert.True(result.ValidationReport.HasIssues);
        Assert.True(result.ValidationReport.HasErrors);
        Assert.True(result.ValidationReport.RequiresHumanReview);

        Assert.Single(documentStorage.Documents);
        Assert.Single(invoiceRepository.Invoices);

        var savedInvoice = invoiceRepository.Invoices.Single();

        Assert.Equal(result.InvoiceId, savedInvoice.Id);
        Assert.Equal(result.DocumentId, savedInvoice.SourceDocumentId);
        Assert.Equal(InvoiceStatus.RequiresHumanReview, savedInvoice.Status);

        Assert.Equal("Partial Azure Vendor Ltd", savedInvoice.Vendor?.Name);
        Assert.Equal("INV-AZ-PARTIAL-1001", savedInvoice.InvoiceNumber);
        Assert.Equal(new DateOnly(2026, 4, 30), savedInvoice.IssueDate);

        Assert.Null(savedInvoice.SubtotalAmount);
        Assert.Null(savedInvoice.VatAmount);
        Assert.Null(savedInvoice.TotalAmount);

        Assert.Equal("Partial Azure Vendor Ltd", savedInvoice.Metadata["VendorName"]);
        Assert.Equal("INV-AZ-PARTIAL-1001", savedInvoice.Metadata["InvoiceNumber"]);

        Assert.False(savedInvoice.Metadata.ContainsKey("SubtotalAmount"));
        Assert.False(savedInvoice.Metadata.ContainsKey("VatAmount"));
        Assert.False(savedInvoice.Metadata.ContainsKey("TotalAmount"));
        Assert.False(savedInvoice.Metadata.ContainsKey("Currency"));

        Assert.Contains(
            result.ValidationReport.Issues,
            issue => issue.Severity == InvoiceValidationSeverity.Error);
    }

    private static AzureDocumentIntelligenceDocumentExtractor CreateAzureDocumentExtractor(
        AnalyzeResult analyzeResult)
    {
        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var azureClient = new AzureDocumentIntelligenceSdkClient(
            sdkClient);

        var options = Options.Create(
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/",
                ApiKey = "test-api-key",
                ModelId = AzureDocumentIntelligenceOptions.DefaultModelId,
                MinimumConfidenceThreshold =
                    AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold
            });

        return new AzureDocumentIntelligenceDocumentExtractor(
            options,
            azureClient);
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

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D,
                0x31, 0x2E, 0x37,
                0x0A
            });
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
