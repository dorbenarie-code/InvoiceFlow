namespace Microsoft.Extensions.DependencyInjection;

public interface IInvoiceFlowBuilder
{
    IServiceCollection Services { get; }
}
