using System.Net.Http.Headers;
using InvoiceFlow.Application.Documents;
using Microsoft.AspNetCore.Http.Features;

namespace InvoiceFlow.Api.Invoices;

internal static class InvoiceDocumentUploadReader
{
    private const string DefaultContentType = "application/octet-stream";
    private const int MaxSignatureLength = 8;

    private static readonly HashSet<string> SupportedFileContentTypes = new(
        StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    private static readonly byte[] PdfSignature =
    [
        0x25, 0x50, 0x44, 0x46, 0x2D
    ];

    private static readonly byte[] JpegSignature =
    [
        0xFF, 0xD8, 0xFF
    ];

    private static readonly byte[] PngSignature =
    [
        0x89, 0x50, 0x4E, 0x47,
        0x0D, 0x0A, 0x1A, 0x0A
    ];

    public static async Task<InvoiceDocumentUploadReadResult> ReadAsync(
        HttpRequest request,
        InvoiceDocumentUploadOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(options);

        if (!request.HasFormContentType)
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "INVALID_CONTENT_TYPE",
                "Request must use multipart/form-data.");
        }

        IFormCollection form;

        try
        {
            form = await request.ReadFormAsync(cancellationToken);
        }
        catch (InvalidDataException)
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "INVALID_FORM_DATA",
                "Request form data is invalid.");
        }

        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();

        if (file is null || file.Length == 0)
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "FILE_REQUIRED",
                "Invoice document file is required.");
        }

        if (file.Length > options.MaxFileSizeInBytes)
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "FILE_TOO_LARGE",
                $"Invoice document file cannot be larger than {FormatFileSize(options.MaxFileSizeInBytes)}.",
                StatusCodes.Status413PayloadTooLarge);
        }

        var fileName = NormalizeFileName(file.FileName);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "INVALID_FILE_NAME",
                "Invoice document file name is invalid.");
        }

        var contentType = NormalizeContentType(file.ContentType);

        if (!SupportedFileContentTypes.Contains(contentType))
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "UNSUPPORTED_FILE_CONTENT_TYPE",
                "Invoice document file must be a PDF, JPG, or PNG.");
        }

        byte[] content;

        await using (var fileStream = file.OpenReadStream())
        {
            var signatureBuffer = new byte[MaxSignatureLength];

            var signatureBytesRead = await ReadSignatureBytesAsync(
                fileStream,
                signatureBuffer,
                cancellationToken);

            if (!HasExpectedFileSignature(
                    signatureBuffer,
                    signatureBytesRead,
                    contentType))
            {
                return InvoiceDocumentUploadReadResult.Failure(
                    "INVALID_FILE_SIGNATURE",
                    "Invoice document file content does not match its declared file type.");
            }

            using var memoryStream = new MemoryStream();

            memoryStream.Write(
                signatureBuffer,
                0,
                signatureBytesRead);

            await fileStream.CopyToAsync(
                memoryStream,
                cancellationToken);

            content = memoryStream.ToArray();
        }

        try
        {
            var document = new DocumentInput(
                fileName,
                contentType,
                content);

            return InvoiceDocumentUploadReadResult.Success(document);
        }
        catch (ArgumentException exception)
        {
            return InvoiceDocumentUploadReadResult.Failure(
                "INVALID_DOCUMENT",
                exception.Message);
        }
    }

    private static async Task<int> ReadSignatureBytesAsync(
        Stream stream,
        byte[] buffer,
        CancellationToken cancellationToken)
    {
        var totalBytesRead = 0;

        while (totalBytesRead < buffer.Length)
        {
            var bytesRead = await stream.ReadAsync(
                buffer.AsMemory(
                    totalBytesRead,
                    buffer.Length - totalBytesRead),
                cancellationToken);

            if (bytesRead == 0)
            {
                break;
            }

            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }

    private static string NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return DefaultContentType;
        }

        if (!MediaTypeHeaderValue.TryParse(contentType, out var mediaTypeHeader)
            || string.IsNullOrWhiteSpace(mediaTypeHeader.MediaType))
        {
            return contentType.Trim();
        }

        return mediaTypeHeader.MediaType.Trim();
    }

    private static string? NormalizeFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return null;
        }

        var normalizedSeparators = fileName
            .Trim()
            .Replace('\\', '/');

        return Path.GetFileName(normalizedSeparators);
    }

    private static bool HasExpectedFileSignature(
        byte[] content,
        int contentLength,
        string contentType)
    {
        return contentType.ToLowerInvariant() switch
        {
            "application/pdf" => StartsWith(content, contentLength, PdfSignature),
            "image/jpeg" => StartsWith(content, contentLength, JpegSignature),
            "image/png" => StartsWith(content, contentLength, PngSignature),
            _ => false
        };
    }

    private static bool StartsWith(
        byte[] content,
        int contentLength,
        byte[] signature)
    {
        if (contentLength < signature.Length)
        {
            return false;
        }

        for (var index = 0; index < signature.Length; index++)
        {
            if (content[index] != signature[index])
            {
                return false;
            }
        }

        return true;
    }

    private static string FormatFileSize(long sizeInBytes)
    {
        const long oneKilobyte = 1024;
        const long oneMegabyte = oneKilobyte * 1024;

        if (sizeInBytes % oneMegabyte == 0)
        {
            return $"{sizeInBytes / oneMegabyte} MB";
        }

        if (sizeInBytes % oneKilobyte == 0)
        {
            return $"{sizeInBytes / oneKilobyte} KB";
        }

        return $"{sizeInBytes} bytes";
    }
}
