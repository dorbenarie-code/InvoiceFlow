using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.ClientRateLimiting;

namespace InvoiceFlow.Api.ClientRateLimiting;

public sealed class ClientRateLimitEndpointFilter : IEndpointFilter
{
    private readonly IClientRateLimiter _limiter;

    public ClientRateLimitEndpointFilter(
        IClientRateLimiter limiter)
    {
        _limiter = limiter
            ?? throw new ArgumentNullException(nameof(limiter));
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var httpContext = context.HttpContext;

        var clientId = GetResolvedClientId(httpContext);
        var resource = GetResource(httpContext);

        var result = await _limiter.AcquireAsync(
            clientId,
            resource,
            httpContext.RequestAborted);

        if (result.IsExceeded)
        {
            return TooManyRequests();
        }

        return await next(context);
    }

    private static Guid GetResolvedClientId(
        HttpContext httpContext)
    {
        if (!httpContext.Items.TryGetValue(
                ClientApiKeyHttpContextKeys.ClientId,
                out var value)
            || value is not Guid clientId
            || clientId == Guid.Empty)
        {
            throw new InvalidOperationException(
                "Client id was not resolved for rate limiting.");
        }

        return clientId;
    }

    private static string GetResource(
        HttpContext httpContext)
    {
        var path = httpContext.Request.Path.Value;

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException(
                "HTTP request path is required for rate limiting.");
        }

        return path;
    }

    private static IResult TooManyRequests()
    {
        return Results.Json(
            new ApiErrorResponse(
                "RATE_LIMIT_EXCEEDED",
                "Rate limit exceeded. Please try again later."),
            statusCode: StatusCodes.Status429TooManyRequests);
    }
}
