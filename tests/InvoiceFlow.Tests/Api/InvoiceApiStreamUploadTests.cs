using System.Net;
using System.Net.Http.Headers;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceApiStreamUploadTests
{
    [Fact]
    public async Task ProcessInvoice_ShouldPassStreamBasedDocumentInputToProcessor()
    {
        var processor = new CapturingInvoiceDocumentProcessor();

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IInvoiceDocumentProcessor>();
                    services.AddSingleton<IInvoiceDocumentProcessor>(processor);
                });
            });

        var client = factory.CreateClient();

        var fileBytes = CreatePdfBytes();

        using var content = new MultipartFormDataContent();

        using var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType =
            MediaTypeHeaderValue.Parse("application/pdf");

        content.Add(
            fileContent,
            "file",
            "invoice.pdf");

        var response = await client.PostAsync(
            "/api/invoices/process",
            content);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(processor.WasCalled);

        Assert.Equal("invoice.pdf", processor.FileName);
        Assert.Equal("application/pdf", processor.ContentType);
        Assert.Equal(fileBytes.Length, processor.ContentLength);

        Assert.Equal(fileBytes, processor.StreamBytes);
    }

    private static byte[] CreatePdfBytes()
    {
        return "%PDF-1.7 stream upload invoice content"u8.ToArray();
    }

    private sealed class CapturingInvoiceDocumentProcessor
        : IInvoiceDocumentProcessor
    {
        public bool WasCalled { get; private set; }

        public string? FileName { get; private set; }

        public string? ContentType { get; private set; }

        public long? ContentLength { get; private set; }

        public byte[]? StreamBytes { get; private set; }

        public async Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(document);

            WasCalled = true;
            FileName = document.FileName;
            ContentType = document.ContentType;
            ContentLength = document.ContentLength;
            await using var stream = await document.OpenReadStreamAsync(
                cancellationToken);

            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(
                memoryStream,
                cancellationToken);

            StreamBytes = memoryStream.ToArray();

            var documentId = Guid.NewGuid();

            var invoice = Invoice.CreateExtracted(
                sourceDocumentId: documentId,
                vendor: new Vendor("Stream Upload Vendor Ltd", "516789123"),
                invoiceNumber: "INV-UPLOAD-STREAM-1001",
                issueDate: new DateOnly(2026, 4, 30),
                subtotalAmount: new CurrencyAmount(1000m, "ILS"),
                vatAmount: new CurrencyAmount(180m, "ILS"),
                totalAmount: new CurrencyAmount(1180m, "ILS"));

            invoice.ApplyValidationReport(
                InvoiceValidationReport.Valid());

            return new ProcessInvoiceDocumentResult(
                documentId,
                invoice);
        }
    }
}
