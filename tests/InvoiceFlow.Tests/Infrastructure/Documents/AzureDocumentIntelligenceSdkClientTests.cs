using Azure;
using Azure.AI.DocumentIntelligence;
using Azure.Core;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceSdkClientTests
{
    [Fact]
    public void Constructor_ShouldThrow_WhenClientIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureDocumentIntelligenceSdkClient(null!));

        Assert.Equal("client", exception.ParamName);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldThrow_WhenRequestIsNull()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient();
        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            client.AnalyzeAsync(null!));

        Assert.Equal("request", exception.ParamName);
        Assert.Equal(0, sdkClient.CallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldNotCallSdk_WhenCancellationTokenIsAlreadyCanceled()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient();
        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            client.AnalyzeAsync(
                request,
                cancellationTokenSource.Token));

        Assert.Equal(0, sdkClient.CallCount);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldCallSdkWithConfiguredModelId()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient();
        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest(modelId: "custom-model-id");

        await client.AnalyzeAsync(request);

        Assert.Equal(1, sdkClient.CallCount);
        Assert.Equal("custom-model-id", sdkClient.LastModelId);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldPassCancellationTokenToSdk()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient();
        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        using var cancellationTokenSource = new CancellationTokenSource();

        await client.AnalyzeAsync(
            request,
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            sdkClient.LastCancellationToken);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSendDocumentBytesToSdk()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient();
        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);

        var expectedBytes = CreatePdfBytes();
        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(
                    expectedBytes,
                    writable: false)),
            contentLength: expectedBytes.Length);

        var request = new AzureDocumentIntelligenceAnalyzeRequest(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            document);

        await client.AnalyzeAsync(request);

        Assert.NotNull(sdkClient.LastBytesSource);

        var sentBytes = sdkClient.LastBytesSource.ToArray();

        Assert.Equal(
            expectedBytes,
            sentBytes);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnExtractedDocumentWithRawText()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient(
            rawText: "raw text from azure sdk");

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal("raw text from azure sdk", result.RawText);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldReturnEmptyRawText_WhenAnalyzeResultContentIsNull()
    {
        var sdkClient = new RecordingDocumentIntelligenceClient(
            rawText: null);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var result = await client.AnalyzeAsync(request);

        Assert.Equal(string.Empty, result.RawText);
        Assert.Empty(result.Fields);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldPropagateSdkFailure()
    {
        var expectedException = new RequestFailedException(
            500,
            "Azure service failed.");

        var sdkClient = new RecordingDocumentIntelligenceClient(
            exceptionToThrow: expectedException);

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);
        var request = CreateRequest();

        var exception = await Assert.ThrowsAsync<RequestFailedException>(() =>
            client.AnalyzeAsync(request));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSendDocumentStreamBytesToAzureSdk()
    {
        var documentBytes = CreatePdfBytes();

        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(
                    documentBytes,
                    writable: false)),
            contentLength: documentBytes.Length);

        var request = new AzureDocumentIntelligenceAnalyzeRequest(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            document);

        var sdkClient = new RecordingDocumentIntelligenceClient();

        var client = new AzureDocumentIntelligenceSdkClient(sdkClient);

        await client.AnalyzeAsync(request);

        Assert.NotNull(sdkClient.LastBytesSource);

        var sentBytes = sdkClient.LastBytesSource.ToArray();

        Assert.Equal(
            documentBytes,
            sentBytes);
    }

    private static AzureDocumentIntelligenceAnalyzeRequest CreateRequest(
        string modelId = AzureDocumentIntelligenceOptions.DefaultModelId)
    {
        return new AzureDocumentIntelligenceAnalyzeRequest(
            modelId,
            CreateDocument());
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

    private sealed class RecordingDocumentIntelligenceClient
        : DocumentIntelligenceClient
    {
        private readonly string? _rawText;
        private readonly RequestFailedException? _exceptionToThrow;

        public int CallCount { get; private set; }

        public string? LastModelId { get; private set; }

        public BinaryData? LastBytesSource { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public RecordingDocumentIntelligenceClient(
            string? rawText = "default azure raw text",
            RequestFailedException? exceptionToThrow = null)
        {
            _rawText = rawText;
            _exceptionToThrow = exceptionToThrow;
        }

        public override Operation<AnalyzeResult> AnalyzeDocument(
            WaitUntil waitUntil,
            string modelId,
            BinaryData bytesSource,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastModelId = modelId;
            LastBytesSource = bytesSource;
            LastCancellationToken = cancellationToken;

            if (_exceptionToThrow is not null)
            {
                throw _exceptionToThrow;
            }

            var result = DocumentIntelligenceModelFactory.AnalyzeResult(
                content: _rawText);

            return new CompletedAnalyzeDocumentOperation(result);
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
