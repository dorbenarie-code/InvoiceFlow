using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Invoices;

namespace InvoiceFlow.Tests.Application;

public sealed class ProcessInvoiceDocumentServiceStreamTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public async Task ProcessAsync_ShouldAllowStorageAndExtractorToReadDocumentContentIndependently()
    {
        var documentBytes = "%PDF-1.7 stream-based invoice content"u8.ToArray();

        var document = new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            _ => ValueTask.FromResult<Stream>(
                new MemoryStream(
                    documentBytes,
                    writable: false)),
            contentLength: documentBytes.Length);

        var documentStorage = new ReadingDocumentStorage();
        var documentExtractor = new ReadingDocumentExtractor();
        var invoiceMapper = new FieldBasedInvoiceMapper();
        var invoiceValidator = new DefaultInvoiceValidator(ValidationDate);
        var invoiceRepository = new InMemoryInvoiceRepository();

        var service = new ProcessInvoiceDocumentService(
            documentStorage,
            documentExtractor,
            invoiceMapper,
            invoiceValidator,
            invoiceRepository);

        var result = await service.ProcessAsync(document);

        Assert.Equal(InvoiceStatus.Verified, result.Status);

        Assert.Equal(documentBytes, documentStorage.ReadBytes);
        Assert.Equal(documentBytes, documentExtractor.ReadBytes);

        Assert.NotSame(
            documentStorage.ReadBytes,
            documentExtractor.ReadBytes);

        Assert.Single(invoiceRepository.Invoices);
    }

    private sealed class ReadingDocumentStorage : IDocumentStorage
    {
        public byte[]? ReadBytes { get; private set; }

        public async Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            await using var stream = await document.OpenReadStreamAsync(
                cancellationToken);

            ReadBytes = await ReadAllBytesAsync(
                stream,
                cancellationToken);

            return new StoredDocument(
                Guid.NewGuid(),
                document.FileName);
        }
    }

    private sealed class ReadingDocumentExtractor : IDocumentExtractor
    {
        public byte[]? ReadBytes { get; private set; }

        public async Task<ExtractedDocument> ExtractAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            await using var stream = await document.OpenReadStreamAsync(
                cancellationToken);

            ReadBytes = await ReadAllBytesAsync(
                stream,
                cancellationToken);

            return new ExtractedDocument(
                "Stream-based extracted invoice text",
                new Dictionary<string, string>
                {
                    ["VendorName"] = "Stream Vendor Ltd",
                    ["VendorTaxId"] = "516789123",
                    ["InvoiceNumber"] = "INV-STREAM-1001",
                    ["IssueDate"] = "2026-04-30",
                    ["SubtotalAmount"] = "1000",
                    ["VatAmount"] = "180",
                    ["TotalAmount"] = "1180",
                    ["Currency"] = "ILS"
                });
        }
    }

    private static async Task<byte[]> ReadAllBytesAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        using var memoryStream = new MemoryStream();

        await stream.CopyToAsync(
            memoryStream,
            cancellationToken);

        return memoryStream.ToArray();
    }
}
