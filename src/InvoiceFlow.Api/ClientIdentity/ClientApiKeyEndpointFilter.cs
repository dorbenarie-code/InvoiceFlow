using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientIdentity;

namespace InvoiceFlow.Api.ClientIdentity;

public sealed class ClientApiKeyEndpointFilter : IEndpointFilter
{
    private const string ApiKeyHeaderName = "X-API-Key";

    private readonly IClientApiKeyValidator _validator;

    public ClientApiKeyEndpointFilter(
        IClientApiKeyValidator validator)
    {
        _validator = validator
            ?? throw new ArgumentNullException(nameof(validator));
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;

        var apiKey = GetApiKey(httpContext);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Unauthorized();
        }

        var validationResult = await _validator.ValidateAsync(
            apiKey,
            httpContext.RequestAborted);

        if (!validationResult.IsValid
            || validationResult.ClientId is null
            || validationResult.ClientId.Value == Guid.Empty)
        {
            return Unauthorized();
        }

        httpContext.Items[ClientApiKeyHttpContextKeys.ClientId] =
            validationResult.ClientId.Value;

        return await next(context);
    }

    private static string? GetApiKey(
        HttpContext httpContext)
    {
        if (!httpContext.Request.Headers.TryGetValue(
                ApiKeyHeaderName,
                out var values))
        {
            return null;
        }

        return values.ToString();
    }

    private static IResult Unauthorized()
    {
        return Results.Json(
            new ApiErrorResponse(
                "INVALID_API_KEY",
                "A valid API key is required."),
            statusCode: StatusCodes.Status401Unauthorized);
    }
}
