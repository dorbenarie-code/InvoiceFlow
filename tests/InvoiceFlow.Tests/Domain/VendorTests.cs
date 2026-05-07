using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Tests.Domain;

public sealed class VendorTests
{
    [Fact]
    public void Constructor_ShouldTrimVendorName()
    {
        var vendor = new Vendor("  Cohen Office Supplies Ltd  ", "516789123");

        Assert.Equal("Cohen Office Supplies Ltd", vendor.Name);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenVendorNameIsEmpty()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vendor("", "516789123"));
    }

    [Fact]
    public void Constructor_ShouldAllowMissingTaxId()
    {
        var vendor = new Vendor("Cohen Office Supplies Ltd");

        Assert.Null(vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldTreatWhitespaceTaxIdAsMissing()
    {
        var vendor = new Vendor("Cohen Office Supplies Ltd", "   ");

        Assert.Null(vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldTreatTaxIdWithOnlyIgnoredCharactersAsMissing()
    {
        var vendor = new Vendor("Cohen Office Supplies Ltd", " - - ");

        Assert.Null(vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldNormalizeTaxId_WhenTaxIdContainsSpacesAndDashes()
    {
        var vendor = new Vendor("Cohen Office Supplies Ltd", "516-789 123");

        Assert.Equal("516789123", vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldNormalizeTaxIdToUppercase()
    {
        var vendor = new Vendor("Google Ireland", "ie-6388047v");

        Assert.Equal("IE6388047V", vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldAllowTaxIdWithLettersAndDigits()
    {
        var vendor = new Vendor("International Vendor", "GB123456789");

        Assert.Equal("GB123456789", vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldAllowTaxIdThatIsNotExactlyNineDigits()
    {
        var vendor = new Vendor("Short Tax Vendor", "12345678");

        Assert.Equal("12345678", vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldAllowTaxIdAtMaximumLength()
    {
        var taxId = new string('1', 50);

        var vendor = new Vendor("Cohen Office Supplies Ltd", taxId);

        Assert.Equal(taxId, vendor.TaxId);
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTaxIdContainsUnsupportedCharacters()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vendor("Cohen Office Supplies Ltd", "51678@123"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTaxIdContainsNonAsciiLetters()
    {
        Assert.Throws<ArgumentException>(() =>
            new Vendor("Cohen Office Supplies Ltd", "51678א123"));
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenTaxIdIsTooLong()
    {
        var longTaxId = new string('1', 51);

        Assert.Throws<ArgumentException>(() =>
            new Vendor("Cohen Office Supplies Ltd", longTaxId));
    }
}