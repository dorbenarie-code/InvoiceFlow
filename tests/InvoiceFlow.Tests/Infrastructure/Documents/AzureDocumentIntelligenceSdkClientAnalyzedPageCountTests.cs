using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceSdkClientAnalyzedPageCountTests
{
    [Fact]
    public async Task AnalyzeAsync_ShouldMapAnalyzedPageCount_FromAnalyzeResultPages()
    {
        var analyzeResult = DocumentIntelligenceModelFactory.AnalyzeResult(
            modelId: AzureDocumentIntelligenceOptions.DefaultModelId,
            content: "azure invoice raw text",
            pages:
            [
                DocumentIntelligenceModelFactory.DocumentPage(pageNumber: 1),
                DocumentIntelligenceModelFactory.DocumentPage(pageNumber: 2),
                DocumentIntelligenceModelFactory.DocumentPage(pageNumber: 3)
            ],
            documents: []);

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);

        var result = await client.AnalyzeAsync(CreateRequest());

        Assert.Equal(3, result.AnalyzedPageCount);
        Assert.Equal("azure invoice raw text", result.RawText);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnNullAnalyzedPageCount_WhenAnalyzeResultHasNoPages()
    {
        var analyzeResult = DocumentIntelligenceModelFactory.AnalyzeResult(
            modelId: AzureDocumentIntelligenceOptions.DefaultModelId,
            content: "azure invoice raw text",
            pages: [],
            documents: []);

        var sdkClient = new FixedAnalyzeResultDocumentIntelligenceClient(
            analyzeResult);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);

        var result = await client.AnalyzeAsync(CreateRequest());

        Assert.Null(result.AnalyzedPageCount);
        Assert.Equal("azure invoice raw text", result.RawText);
    }

    private static AzureDocumentIntelligenceAnalyzeRequest CreateRequest()
    {
        return new AzureDocumentIntelligenceAnalyzeRequest(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            CreateDocument(),
            AzureDocumentIntelligenceOptions.DefaultMinimumConfidenceThreshold);
    }

    private static DocumentInput CreateDocument()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D
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
