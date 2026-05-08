using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

namespace InvoiceFlow.Api.Invoices;

public static class InvoiceEndpoints
{
    public static IEndpointRouteBuilder MapInvoiceEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints
            .MapGroup("/api/invoices")
            .WithTags("Invoices");

        group.MapPost("/process", ProcessInvoiceAsync)
            .RequireClientApiKeyWhenConfigured()
            .WithName("ProcessInvoiceDocument")
            .WithSummary("Processes an invoice document.")
            .WithDescription(
                "Receives an invoice or receipt file using multipart/form-data. " +
                "Supported file types are PDF, JPG, and PNG. " +
                "A technically valid request returns 200 OK even when business validation finds invoice issues. " +
                "Clients must inspect the response status field. " +
                "Verified means the invoice passed validation. " +
                "RequiresHumanReview means the document was processed successfully, but business validation found issues that should be reviewed by a human.")
            .Produces<ProcessInvoiceResponse>(
                StatusCodes.Status200OK,
                "application/json")
            .Produces<ApiErrorResponse>(
                StatusCodes.Status400BadRequest,
                "application/json")
            .Produces<ApiErrorResponse>(
                StatusCodes.Status401Unauthorized,
                "application/json")
            .Produces<ApiErrorResponse>(
                StatusCodes.Status413PayloadTooLarge,
                "application/json")
            .Produces<ApiErrorResponse>(
                StatusCodes.Status503ServiceUnavailable,
                "application/json")
            .WithOpenApi(operation =>
            {
                operation.Security =
                [
                    new OpenApiSecurityRequirement
                    {
                        [
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "ApiKey"
                                }
                            }
                        ] = []
                    }
                ];

                operation.RequestBody = new OpenApiRequestBody
                {
                    Required = true,
                    Description = "Invoice or receipt document file. Supported formats: PDF, JPG, PNG.",
                    Content =
                    {
                        ["multipart/form-data"] = new OpenApiMediaType
                        {
                            Schema = new OpenApiSchema
                            {
                                Type = "object",
                                Required = new HashSet<string>
                                {
                                    "file"
                                },
                                Properties =
                                {
                                    ["file"] = new OpenApiSchema
                                    {
                                        Type = "string",
                                        Format = "binary",
                                        Description = "Invoice document file."
                                    }
                                }
                            }
                        }
                    }
                };

                return operation;
            });

        return endpoints;
    }

    private static async Task<IResult> ProcessInvoiceAsync(
        HttpRequest request,
        IInvoiceDocumentProcessor processor,
        IOptions<InvoiceDocumentUploadOptions> uploadOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(uploadOptions);

        var uploadResult = await InvoiceDocumentUploadReader.ReadAsync(
            request,
            uploadOptions.Value,
            cancellationToken);

        if (!uploadResult.Succeeded)
        {
            return Results.Json(
                uploadResult.Error,
                statusCode: uploadResult.StatusCode);
        }

        try
        {
            var result = await processor.ProcessAsync(
                uploadResult.Document!,
                cancellationToken);

            return Results.Ok(ProcessInvoiceResponse.FromResult(result));
        }
        catch (DocumentStorageFailedException)
        {
            return Results.Json(
                new ApiErrorResponse(
                    "DOCUMENT_STORAGE_FAILED",
                    "Document storage failed. Please try again later."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (DocumentExtractionFailedException)
        {
            return Results.Json(
                new ApiErrorResponse(
                    "DOCUMENT_EXTRACTION_FAILED",
                    "Document extraction failed. Please try again later."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (InvoicePersistenceFailedException)
        {
            return Results.Json(
                new ApiErrorResponse(
                    "INVOICE_PERSISTENCE_FAILED",
                    "Invoice persistence failed. Please try again later."),
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }
        catch (ArgumentException exception)
        {
            return Results.BadRequest(
                new ApiErrorResponse(
                    "INVALID_DOCUMENT",
                    exception.Message));
        }
    }
}
