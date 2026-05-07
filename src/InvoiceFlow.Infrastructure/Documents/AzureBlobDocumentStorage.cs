using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using InvoiceFlow.Application.Documents;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.Documents;

public sealed class AzureBlobDocumentStorage : IDocumentStorage
{
    private readonly AzureBlobDocumentStorageOptions _options;

    public AzureBlobDocumentStorage(
        IOptions<AzureBlobDocumentStorageOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public async Task<StoredDocument> SaveAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage connection string is required.");
        }

        if (string.IsNullOrWhiteSpace(_options.ContainerName))
        {
            throw new InvalidOperationException(
                "Azure Blob Storage container name is required.");
        }

        var storedDocument = new StoredDocument(
            Guid.NewGuid(),
            document.FileName);

        var containerClient = new BlobContainerClient(
            _options.ConnectionString,
            _options.ContainerName);

        await containerClient.CreateIfNotExistsAsync(
            cancellationToken: cancellationToken);

        var blobName = CreateBlobName(storedDocument);

        var blobClient = containerClient.GetBlobClient(blobName);

        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = document.ContentType
            }
        };

        await using var stream = await document.OpenReadStreamAsync(
            cancellationToken);

        await blobClient.UploadAsync(
            stream,
            uploadOptions,
            cancellationToken);

        return storedDocument;
    }

    private static string CreateBlobName(
        StoredDocument storedDocument)
    {
        var extension = Path.GetExtension(storedDocument.FileName);

        if (string.IsNullOrWhiteSpace(extension))
        {
            return storedDocument.Id.ToString();
        }

        return $"{storedDocument.Id}{extension}";
    }
}
