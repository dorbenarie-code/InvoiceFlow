using InvoiceFlow.Application.ClientIdentity;
using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Api.ClientIdentity;

public static class InvoiceFlowApiKeyClientIdentityBuilderExtensions
{
    public static IInvoiceFlowBuilder UseApiKeyClientIdentity(
        this IInvoiceFlowBuilder builder,
        Action<ClientApiKeyIdentityOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services
            .AddOptions<ClientApiKeyIdentityOptions>()
            .Configure(configureOptions);

        builder.Services.RemoveAll<IValidateOptions<ClientApiKeyIdentityOptions>>();
        builder.Services.AddSingleton<
            IValidateOptions<ClientApiKeyIdentityOptions>,
            ClientApiKeyIdentityOptionsValidator>();

        builder.Services.RemoveAll<IClientApiKeyValidator>();
        builder.Services.AddSingleton<
            IClientApiKeyValidator,
            ConfiguredClientApiKeyValidator>();

        builder.Services.AddHttpContextAccessor();

        builder.Services.RemoveAll<IProcessingClientContext>();
        builder.Services.AddScoped<
            IProcessingClientContext,
            HttpProcessingClientContext>();

        return builder;
    }
}
