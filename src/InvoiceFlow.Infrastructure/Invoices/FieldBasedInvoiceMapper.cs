using System.Globalization;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Infrastructure.Invoices;

public sealed class FieldBasedInvoiceMapper : IInvoiceMapper
{
    public Task<Invoice> MapAsync(
        ExtractedDocument document,
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var fields = document.Fields;

        var vendor = MapVendor(fields);
        var invoiceNumber = GetField(fields, "InvoiceNumber");
        var issueDate = MapIssueDate(fields);
        var currency = NormalizeCurrency(GetField(fields, "Currency"));

        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: sourceDocumentId,
            vendor: vendor,
            invoiceNumber: invoiceNumber,
            issueDate: issueDate,
            subtotalAmount: MapAmount(fields, "SubtotalAmount", currency),
            vatAmount: MapAmount(fields, "VatAmount", currency),
            totalAmount: MapAmount(fields, "TotalAmount", currency),
            metadata: fields);

        return Task.FromResult(invoice);
    }

    private static Vendor? MapVendor(IReadOnlyDictionary<string, string> fields)
    {
        var vendorName = GetField(fields, "VendorName");

        if (string.IsNullOrWhiteSpace(vendorName))
        {
            return null;
        }

        var vendorTaxId = GetField(fields, "VendorTaxId");

        try
        {
            return new Vendor(vendorName, vendorTaxId);
        }
        catch (ArgumentException)
        {
            return new Vendor(vendorName);
        }
    }

    private static DateOnly? MapIssueDate(IReadOnlyDictionary<string, string> fields)
    {
        var value = GetField(fields, "IssueDate")?.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string[] formats =
        [
            "yyyy-MM-dd",
            "dd/MM/yyyy",
            "d/M/yyyy"
        ];

        if (DateOnly.TryParseExact(
                value,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            return date;
        }

        return null;
    }

    private static CurrencyAmount? MapAmount(
        IReadOnlyDictionary<string, string> fields,
        string fieldName,
        string? currency)
    {
        var value = GetField(fields, fieldName);

        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(currency))
        {
            return null;
        }

        if (!decimal.TryParse(
                value,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var amount))
        {
            return null;
        }

        try
        {
            return new CurrencyAmount(amount, currency);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string? NormalizeCurrency(string? currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            return null;
        }

        return currency.Trim().ToUpperInvariant() switch
        {
            "₪" => "ILS",
            "NIS" => "ILS",
            "ILS" => "ILS",
            "$" => "USD",
            "USD" => "USD",
            "€" => "EUR",
            "EUR" => "EUR",
            var value => value
        };
    }

    private static string? GetField(
        IReadOnlyDictionary<string, string> fields,
        string key)
    {
        return fields.TryGetValue(key, out var value)
            ? value
            : null;
    }
}