namespace Microsoft.Extensions.DependencyInjection;

internal sealed class InvoiceFlowBuilder : IInvoiceFlowBuilder
{
    public IServiceCollection Services { get; }

    public InvoiceFlowBuilder(IServiceCollection services)
    {
        Services = services
            ?? throw new ArgumentNullException(nameof(services));
    }
}
