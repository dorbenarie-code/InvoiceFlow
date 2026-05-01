using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Tests.Application;

public sealed class StoredDocumentTests
{
    [Fact]
    public void Constructor_ShouldCreateStoredDocument()
    {
        var id = Guid.NewGuid();

        var document = new StoredDocument(id, " invoice.pdf ");

        Assert.Equal(id, document.Id);
        Assert.Equal("invoice.pdf", document.FileName);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIdIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new StoredDocument(Guid.Empty, "invoice.pdf"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenFileNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new StoredDocument(Guid.NewGuid(), ""));
    }
}
