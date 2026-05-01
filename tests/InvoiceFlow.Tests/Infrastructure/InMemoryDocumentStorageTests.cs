using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class InMemoryDocumentStorageTests
{
    [Fact]
    public async Task SaveAsync_ShouldReturnStoredDocument()
    {
        var storage = new InMemoryDocumentStorage();
        var document = CreateDocumentInput();

        var storedDocument = await storage.SaveAsync(document);

        Assert.NotEqual(Guid.Empty, storedDocument.Id);
        Assert.Equal("invoice.pdf", storedDocument.FileName);
        Assert.Single(storage.Documents);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreAllDocuments_WhenCalledConcurrently()
    {
        var storage = new InMemoryDocumentStorage();

        var tasks = Enumerable
            .Range(1, 100)
            .Select(index =>
                Task.Run(() =>
                    storage.SaveAsync(CreateDocumentInput($"invoice-{index}.pdf"))));

        var storedDocuments = await Task.WhenAll(tasks);

        Assert.Equal(100, storedDocuments.Length);
        Assert.Equal(100, storage.Documents.Count);
        Assert.Equal(100, storedDocuments.Select(document => document.Id).Distinct().Count());
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenDocumentIsNull()
    {
        var storage = new InMemoryDocumentStorage();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            storage.SaveAsync(null!));
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
