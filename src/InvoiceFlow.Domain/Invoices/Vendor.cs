namespace InvoiceFlow.Domain.Invoices;

public sealed record Vendor
{
    private const int MaxTaxIdLength = 50;

    public string Name { get; }
    public string? TaxId { get; }

    public Vendor(string name, string? taxId = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Vendor name is required.", nameof(name));
        }

        Name = name.Trim();
        TaxId = NormalizeTaxId(taxId);
    }

    private static string? NormalizeTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
        {
            return null;
        }

        var cleaned = new string(
            taxId
                .Trim()
                .Where(character => !char.IsWhiteSpace(character) && character != '-')
                .ToArray())
            .ToUpperInvariant();

        if (cleaned.Length == 0)
        {
            return null;
        }

        if (cleaned.Length > MaxTaxIdLength)
        {
            throw new ArgumentException(
                $"Vendor tax id cannot be longer than {MaxTaxIdLength} characters.",
                nameof(taxId));
        }

        if (!cleaned.All(char.IsAsciiLetterOrDigit))
        {
            throw new ArgumentException(
                "Vendor tax id must contain ASCII letters or digits only.",
                nameof(taxId));
        }

        return cleaned;
    }
}