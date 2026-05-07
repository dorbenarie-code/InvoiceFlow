using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class InMemoryDocumentStorageTests
{
    private const int ConcurrentSaveCount = 100;

    [Fact]
    public async Task SaveAsync_ShouldReturnStoredDocument()
    {
        var storage = new InMemoryDocumentStorage();
        var document = CreateDocumentInput();

        var storedDocument = await storage.SaveAsync(document);

        Assert.NotEqual(Guid.Empty, storedDocument.Id);
        Assert.Equal("invoice.pdf", storedDocument.FileName);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreReturnedDocument()
    {
        var storage = new InMemoryDocumentStorage();
        var document = CreateDocumentInput();

        var storedDocument = await storage.SaveAsync(document);

        var savedDocument = Assert.Single(storage.Documents);

        Assert.Equal(storedDocument, savedDocument);
    }

    [Fact]
    public async Task SaveAsync_ShouldPreserveDocumentFileName()
    {
        var storage = new InMemoryDocumentStorage();
        var document = CreateDocumentInput("custom-invoice.pdf");

        var storedDocument = await storage.SaveAsync(document);

        Assert.Equal("custom-invoice.pdf", storedDocument.FileName);
        Assert.Equal("custom-invoice.pdf", Assert.Single(storage.Documents).FileName);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreAllDocuments_WhenCalledConcurrently()
    {
        var storage = new InMemoryDocumentStorage();

        var expectedFileNames = Enumerable
            .Range(1, ConcurrentSaveCount)
            .Select(index => $"invoice-{index}.pdf")
            .ToArray();

        var tasks = expectedFileNames
            .Select(fileName =>
                Task.Run(() =>
                    storage.SaveAsync(CreateDocumentInput(fileName))))
            .ToArray();

        var storedDocuments = await Task.WhenAll(tasks);

        var savedDocuments = storage.Documents;

        Assert.Equal(ConcurrentSaveCount, storedDocuments.Length);
        Assert.Equal(ConcurrentSaveCount, savedDocuments.Count);
        Assert.Equal(
            ConcurrentSaveCount,
            storedDocuments.Select(document => document.Id).Distinct().Count());
        Assert.Equal(
            ConcurrentSaveCount,
            savedDocuments.Select(document => document.Id).Distinct().Count());

        Assert.All(storedDocuments, storedDocument =>
            Assert.Contains(storedDocument, savedDocuments));

        Assert.All(expectedFileNames, expectedFileName =>
            Assert.Contains(savedDocuments, document =>
                document.FileName == expectedFileName));
    }

    [Fact]
    public async Task Documents_ShouldReturnSnapshot()
    {
        var storage = new InMemoryDocumentStorage();

        var firstSnapshot = storage.Documents;

        await storage.SaveAsync(CreateDocumentInput());

        Assert.Empty(firstSnapshot);
        Assert.Single(storage.Documents);
    }

    [Fact]
    public async Task SaveAsync_ShouldNotStoreDocument_WhenCancellationRequested()
    {
        var storage = new InMemoryDocumentStorage();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            storage.SaveAsync(
                CreateDocumentInput(),
                cancellationTokenSource.Token));

        Assert.Empty(storage.Documents);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var storage = new InMemoryDocumentStorage();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            storage.SaveAsync(null!));

        Assert.Empty(storage.Documents);
    }

    private static DocumentInput CreateDocumentInput(
        string fileName = "invoice.pdf")
    {
        return new DocumentInput(
            fileName,
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }
}