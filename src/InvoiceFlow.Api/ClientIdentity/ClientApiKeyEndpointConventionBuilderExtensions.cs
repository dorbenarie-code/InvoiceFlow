using InvoiceFlow.Application.ClientIdentity;

namespace InvoiceFlow.Api.ClientIdentity;

public static class ClientApiKeyEndpointConventionBuilderExtensions
{
    public static RouteHandlerBuilder RequireClientApiKeyWhenConfigured(
        this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddEndpointFilter(async (context, next) =>
        {
            var validator = context.HttpContext.RequestServices
                .GetService<IClientApiKeyValidator>();

            if (validator is null)
            {
                return await next(context);
            }

            var filter = new ClientApiKeyEndpointFilter(validator);

            return await filter.InvokeAsync(
                context,
                next);
        });

        return builder;
    }
}
