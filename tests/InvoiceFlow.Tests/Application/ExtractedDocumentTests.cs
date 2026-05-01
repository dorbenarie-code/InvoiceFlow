using System.Collections.ObjectModel;
using InvoiceFlow.Application.Documents;

namespace InvoiceFlow.Tests.Application;

public sealed class ExtractedDocumentTests
{
    [Fact]
    public void Constructor_ShouldCreateExtractedDocument()
    {
        var fields = new Dictionary<string, string>
        {
            ["VendorName"] = "Cohen Office Supplies Ltd"
        };

        var document = new ExtractedDocument(
            " raw text ",
            fields);

        Assert.Equal("raw text", document.RawText);
        Assert.Equal("Cohen Office Supplies Ltd", document.Fields["VendorName"]);
    }

    [Fact]
    public void Constructor_ShouldAllowMissingFields()
    {
        var document = new ExtractedDocument("raw text");

        Assert.Empty(document.Fields);
    }

    [Fact]
    public void Constructor_ShouldCopyFields()
    {
        var fields = new Dictionary<string, string>
        {
            ["VendorName"] = "Cohen Office Supplies Ltd"
        };

        var document = new ExtractedDocument("raw text", fields);

        fields["VendorName"] = "Changed";

        Assert.Equal("Cohen Office Supplies Ltd", document.Fields["VendorName"]);
    }
    [Fact]
public void Constructor_ShouldSupportCaseInsensitiveFieldLookup()
{
    var fields = new Dictionary<string, string>
    {
        ["VendorName"] = "Cohen Office Supplies Ltd"
    };

    var document = new ExtractedDocument("raw text", fields);

    Assert.True(document.Fields.TryGetValue("vendorname", out var vendorName));
    Assert.Equal("Cohen Office Supplies Ltd", vendorName);
}

    [Fact]
    public void Constructor_ShouldExposeFieldsAsReadOnlyDictionary()
    {
        var fields = new Dictionary<string, string>
        {
            ["VendorName"] = "Cohen Office Supplies Ltd"
        };

        var document = new ExtractedDocument("raw text", fields);

        Assert.IsType<ReadOnlyDictionary<string, string>>(document.Fields);
    }
}