using Azure.Storage.Blobs;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.Documents;

public sealed class AzureBlobDocumentStorageIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable =
        "INVOICEFLOW_AZURITE_BLOB_CONNECTION_STRING";

    private const string ContainerName =
        "invoiceflow-test-documents";

    [Fact]
    public async Task SaveAsync_ShouldUploadDocumentToBlobStorage_WhenAzuriteConnectionStringIsProvided()
    {
        var connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        var containerClient = new BlobContainerClient(
            connectionString,
            ContainerName);

        await containerClient.CreateIfNotExistsAsync();

        await DeleteExistingBlobsAsync(containerClient);

        var storage = new AzureBlobDocumentStorage(
            Options.Create(
                new AzureBlobDocumentStorageOptions
                {
                    ConnectionString = connectionString,
                    ContainerName = ContainerName
                }));

        var document = CreateDocumentInput();

        var storedDocument = await storage.SaveAsync(document);

        Assert.NotEqual(Guid.Empty, storedDocument.Id);
        Assert.Equal(document.FileName, storedDocument.FileName);

        var blobs = new List<string>();

        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            blobs.Add(blob.Name);
        }

        var blobName = Assert.Single(blobs);

        Assert.Contains(
            storedDocument.Id.ToString(),
            blobName,
            StringComparison.OrdinalIgnoreCase);

        Assert.EndsWith(
            ".pdf",
            blobName,
            StringComparison.OrdinalIgnoreCase);

        var blobClient = containerClient.GetBlobClient(blobName);

        Assert.True(await blobClient.ExistsAsync());

        var properties = await blobClient.GetPropertiesAsync();

        Assert.Equal(
            "application/pdf",
            properties.Value.ContentType);

        var downloadResult = await blobClient.DownloadContentAsync();

        var uploadedBytes = downloadResult.Value.Content.ToArray();

        Assert.Equal(
            CreateDocumentBytes(),
            uploadedBytes);
    }

    private static async Task DeleteExistingBlobsAsync(
        BlobContainerClient containerClient)
    {
        await foreach (var blob in containerClient.GetBlobsAsync())
        {
            await containerClient.DeleteBlobIfExistsAsync(blob.Name);
        }
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            CreateDocumentBytes());
    }

    private static byte[] CreateDocumentBytes()
    {
        return
        [
            0x25, 0x50, 0x44, 0x46, 0x2D,
            0x31, 0x2E, 0x37,
            0x0A
        ];
    }
}
