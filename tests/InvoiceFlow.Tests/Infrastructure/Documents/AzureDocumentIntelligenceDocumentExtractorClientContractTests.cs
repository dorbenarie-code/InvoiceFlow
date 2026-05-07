using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureDocumentIntelligenceDocumentExtractorClientContractTests
{
    private const string NotImplementedMessage =
        "Azure Document Intelligence extraction is not implemented yet.";

    [Fact]
    public void Constructor_ShouldThrow_WhenClientIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AzureDocumentIntelligenceDocumentExtractor(
                CreateOptions(),
                client: null!));

        Assert.Equal("client", exception.ParamName);
    }

    [Fact]
    public async Task ExtractAsync_ShouldNotCallClient_WhenCancellationTokenIsAlreadyCanceled()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();
        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            extractor.ExtractAsync(
                document,
                cancellationTokenSource.Token));

        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task ExtractAsync_ShouldUseConfiguredModelId_WhenCallingClient()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();

        var options = Options.Create(
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/",
                ApiKey = "test-api-key",
                ModelId = "custom-invoice-model"
            });

        var extractor = new AzureDocumentIntelligenceDocumentExtractor(
            options,
            client);

        var document = CreateDocument();

        await extractor.ExtractAsync(document);

        Assert.NotNull(client.LastRequest);
        Assert.Equal("custom-invoice-model", client.LastRequest.ModelId);
    }

    [Fact]
    public async Task ExtractAsync_ShouldUseDefaultModelId_WhenModelIdIsNotConfigured()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();
        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        await extractor.ExtractAsync(document);

        Assert.NotNull(client.LastRequest);
        Assert.Equal(
            AzureDocumentIntelligenceOptions.DefaultModelId,
            client.LastRequest.ModelId);
    }

    [Fact]
    public async Task ExtractAsync_ShouldPassConfiguredMinimumConfidenceThresholdToClient()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();

        var options = Options.Create(
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/",
                ApiKey = "test-api-key",
                MinimumConfidenceThreshold = 0.65f
            });

        var extractor = new AzureDocumentIntelligenceDocumentExtractor(
            options,
            client);

        var document = CreateDocument();

        await extractor.ExtractAsync(document);

        Assert.NotNull(client.LastRequest);
        Assert.Equal(
            0.65f,
            client.LastRequest.MinimumConfidenceThreshold);
    }

    [Fact]
    public async Task ExtractAsync_ShouldPassDocumentToClient()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();
        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        await extractor.ExtractAsync(document);

        Assert.NotNull(client.LastRequest);
        Assert.Same(document, client.LastRequest.Document);
    }

    [Fact]
    public async Task ExtractAsync_ShouldPassCancellationTokenToClient()
    {
        var client = new RecordingAzureDocumentIntelligenceClient();
        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        using var cancellationTokenSource = new CancellationTokenSource();

        await extractor.ExtractAsync(
            document,
            cancellationTokenSource.Token);

        Assert.Equal(
            cancellationTokenSource.Token,
            client.LastCancellationToken);
    }

    [Fact]
    public async Task ExtractAsync_ShouldReturnExtractedDocument_FromClient()
    {
        var expectedDocument = new ExtractedDocument(
            "azure raw invoice text",
            new Dictionary<string, string>
            {
                ["InvoiceNumber"] = "INV-AZ-1001"
            });

        var client = new RecordingAzureDocumentIntelligenceClient
        {
            Result = expectedDocument
        };

        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        var result = await extractor.ExtractAsync(document);

        Assert.Same(expectedDocument, result);
        Assert.Equal("azure raw invoice text", result.RawText);
        Assert.Equal("INV-AZ-1001", result.Fields["InvoiceNumber"]);
    }

    [Fact]
    public async Task ExtractAsync_ShouldThrowInvalidOperationException_WhenClientReturnsNull()
    {
        var client = new RecordingAzureDocumentIntelligenceClient
        {
            Result = null
        };

        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            extractor.ExtractAsync(document));

        Assert.Equal(
            "Azure Document Intelligence client returned no extracted document.",
            exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_ShouldPropagateClientFailure_WhenClientThrows()
    {
        var expectedException = new InvalidOperationException(
            "Azure client failed.");

        var client = new RecordingAzureDocumentIntelligenceClient
        {
            ExceptionToThrow = expectedException
        };

        var extractor = CreateExtractor(client);
        var document = CreateDocument();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            extractor.ExtractAsync(document));

        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task ExtractAsync_ShouldStillThrowClearNotSupportedException_WhenUsingDefaultSkeletonClient()
    {
        var extractor = new AzureDocumentIntelligenceDocumentExtractor(
            CreateOptions());

        var document = CreateDocument();

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() =>
            extractor.ExtractAsync(document));

        Assert.Equal(NotImplementedMessage, exception.Message);
    }

    private static AzureDocumentIntelligenceDocumentExtractor CreateExtractor(
        IAzureDocumentIntelligenceClient? client = null)
    {
        return new AzureDocumentIntelligenceDocumentExtractor(
            CreateOptions(),
            client ?? new RecordingAzureDocumentIntelligenceClient());
    }

    private static IOptions<AzureDocumentIntelligenceOptions> CreateOptions()
    {
        return Options.Create(
            new AzureDocumentIntelligenceOptions
            {
                Endpoint = "https://invoiceflow-test.cognitiveservices.azure.com/",
                ApiKey = "test-api-key"
            });
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

    private sealed class RecordingAzureDocumentIntelligenceClient
        : IAzureDocumentIntelligenceClient
    {
        public int CallCount { get; private set; }

        public AzureDocumentIntelligenceAnalyzeRequest? LastRequest { get; private set; }

        public CancellationToken LastCancellationToken { get; private set; }

        public ExtractedDocument? Result { get; init; } =
            new ExtractedDocument("default azure client test result");

        public Exception? ExceptionToThrow { get; init; }

        public Task<ExtractedDocument> AnalyzeAsync(
            AzureDocumentIntelligenceAnalyzeRequest request,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastRequest = request;
            LastCancellationToken = cancellationToken;

            if (ExceptionToThrow is not null)
            {
                return Task.FromException<ExtractedDocument>(
                    ExceptionToThrow);
            }

            return Task.FromResult(Result!);
        }
    }
}
