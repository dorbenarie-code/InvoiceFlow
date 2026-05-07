using System.Collections.ObjectModel;
using InvoiceFlow.Domain.ValueObjects;

namespace InvoiceFlow.Domain.Invoices;

public sealed class Invoice
{
    private const int MaxInvoiceNumberLength = 100;
    private const int MaxMetadataKeyLength = 100;
    private const int MaxMetadataValueLength = 500;

    public Guid Id { get; }
    public Vendor? Vendor { get; }
    public string? InvoiceNumber { get; }
    public DateOnly? IssueDate { get; }
    public CurrencyAmount? SubtotalAmount { get; }
    public CurrencyAmount? VatAmount { get; }
    public CurrencyAmount? TotalAmount { get; }
    public Guid SourceDocumentId { get; }
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public InvoiceStatus Status { get; private set; }
    public InvoiceValidationReport ValidationReport { get; private set; }

    private Invoice(
        Guid id,
        Guid sourceDocumentId,
        Vendor? vendor,
        string? invoiceNumber,
        DateOnly? issueDate,
        CurrencyAmount? subtotalAmount,
        CurrencyAmount? vatAmount,
        CurrencyAmount? totalAmount,
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Invoice id is required.", nameof(id));
        }

        if (sourceDocumentId == Guid.Empty)
        {
            throw new ArgumentException("Source document id is required.", nameof(sourceDocumentId));
        }

        Id = id;
        SourceDocumentId = sourceDocumentId;
        Vendor = vendor;
        InvoiceNumber = NormalizeInvoiceNumber(invoiceNumber);
        IssueDate = issueDate;
        SubtotalAmount = subtotalAmount;
        VatAmount = vatAmount;
        TotalAmount = totalAmount;
        Metadata = NormalizeMetadata(metadata);
        Status = InvoiceStatus.Extracted;
        ValidationReport = InvoiceValidationReport.Valid();
    }

    public static Invoice CreateExtracted(
        Guid sourceDocumentId,
        Vendor? vendor,
        string? invoiceNumber,
        DateOnly? issueDate,
        CurrencyAmount? subtotalAmount,
        CurrencyAmount? vatAmount,
        CurrencyAmount? totalAmount,
        IReadOnlyDictionary<string, string>? metadata = null)
    {
        return new Invoice(
            Guid.NewGuid(),
            sourceDocumentId,
            vendor,
            invoiceNumber,
            issueDate,
            subtotalAmount,
            vatAmount,
            totalAmount,
            metadata);
    }

    public void ApplyValidationReport(InvoiceValidationReport validationReport)
    {
        ArgumentNullException.ThrowIfNull(validationReport);

        ValidationReport = validationReport;
        Status = validationReport.RequiresHumanReview
            ? InvoiceStatus.RequiresHumanReview
            : InvoiceStatus.Verified;
    }

    private static string? NormalizeInvoiceNumber(string? invoiceNumber)
    {
        if (string.IsNullOrWhiteSpace(invoiceNumber))
        {
            return null;
        }

        var normalizedInvoiceNumber = invoiceNumber.Trim();

        if (normalizedInvoiceNumber.Length > MaxInvoiceNumberLength)
        {
            throw new ArgumentException(
                $"Invoice number cannot be longer than {MaxInvoiceNumberLength} characters.",
                nameof(invoiceNumber));
        }

        return normalizedInvoiceNumber;
    }

    private static IReadOnlyDictionary<string, string> NormalizeMetadata(
        IReadOnlyDictionary<string, string>? metadata)
    {
        if (metadata is null || metadata.Count == 0)
        {
            return new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>());
        }

        var normalizedMetadata = new Dictionary<string, string>();

        foreach (var item in metadata)
        {
            if (string.IsNullOrWhiteSpace(item.Key))
            {
                throw new ArgumentException("Metadata key is required.", nameof(metadata));
            }

            if (item.Value is null)
            {
                throw new ArgumentException("Metadata value cannot be null.", nameof(metadata));
            }

            var key = item.Key.Trim();
            var value = item.Value.Trim();

            if (key.Length > MaxMetadataKeyLength)
            {
                throw new ArgumentException(
                    $"Metadata key cannot be longer than {MaxMetadataKeyLength} characters.",
                    nameof(metadata));
            }

            if (value.Length > MaxMetadataValueLength)
            {
                throw new ArgumentException(
                    $"Metadata value cannot be longer than {MaxMetadataValueLength} characters.",
                    nameof(metadata));
            }

            normalizedMetadata[key] = value;
        }

        return new ReadOnlyDictionary<string, string>(normalizedMetadata);
    }
}