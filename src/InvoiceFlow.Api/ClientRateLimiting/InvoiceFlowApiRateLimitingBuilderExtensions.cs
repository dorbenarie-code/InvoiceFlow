using InvoiceFlow.Application.ClientRateLimiting;
using InvoiceFlow.Infrastructure.ClientRateLimiting;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InvoiceFlow.Api.ClientRateLimiting;

public static class InvoiceFlowApiRateLimitingBuilderExtensions
{
    public static IInvoiceFlowBuilder UseClientRateLimiting(
        this IInvoiceFlowBuilder builder,
        Action<ClientRateLimitOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services
            .AddOptions<ClientRateLimitOptions>()
            .Configure(configureOptions)
            .Validate(
                options => options.PermitLimit > 0,
                "Client rate limit permit limit must be greater than zero.")
            .Validate(
                options => options.Window > TimeSpan.Zero,
                "Client rate limit window must be greater than zero.")
            .ValidateOnStart();

        builder.Services.RemoveAll<IClientRateLimiter>();
        builder.Services.AddSingleton<
            IClientRateLimiter,
            InMemoryClientRateLimiter>();

        return builder;
    }
}
