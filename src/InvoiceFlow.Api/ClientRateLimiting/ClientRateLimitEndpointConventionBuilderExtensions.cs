using InvoiceFlow.Application.ClientIdentity;
using InvoiceFlow.Application.ClientRateLimiting;

namespace InvoiceFlow.Api.ClientRateLimiting;

public static class ClientRateLimitEndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequireClientRateLimitWhenConfigured(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var limiter = context.HttpContext.RequestServices
                .GetService<IClientRateLimiter>();

            var apiKeyValidator = context.HttpContext.RequestServices
                .GetService<IClientApiKeyValidator>();

            if (limiter is null || apiKeyValidator is null)
            {
                return await next(context);
            }

            var filter = new ClientRateLimitEndpointFilter(limiter);

            return await filter.InvokeAsync(
                context,
                next);
        });

        return builder;
    }
}
