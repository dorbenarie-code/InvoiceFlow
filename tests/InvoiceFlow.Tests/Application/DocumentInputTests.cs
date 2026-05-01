using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Tests.Application;

public sealed class DocumentInputTests
{
    [Fact]
    public void Constructor_ShouldCreateDocumentInput()
    {
        byte[] content = [1, 2, 3];

        var document = new DocumentInput(
            " invoice.pdf ",
            " application/pdf ",
            content);

        Assert.Equal("invoice.pdf", document.FileName);
        Assert.Equal("application/pdf", document.ContentType);
        Assert.Equal(3, document.Content.Length);
        Assert.Equal(1, document.Content.Span[0]);
        Assert.Equal(2, document.Content.Span[1]);
        Assert.Equal(3, document.Content.Span[2]);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenFileNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new DocumentInput("", "application/pdf", new byte[] { 1 }));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentTypeIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new DocumentInput("invoice.pdf", "", new byte[] { 1 }));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenContentIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new DocumentInput("invoice.pdf", "application/pdf", ReadOnlyMemory<byte>.Empty));
    }
}