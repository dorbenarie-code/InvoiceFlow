using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Infrastructure.ProcessingRuns;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Composition;

public sealed class ProcessingRunCompositionTests
{
    [Fact]
    public void AddInvoiceFlowInMemory_ShouldRegisterProcessingRunRepositoryAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddInvoiceFlowInMemory();

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IProcessingRunRepository)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(InMemoryProcessingRunRepository),
            descriptor.ImplementationType);
    }

    [Fact]
    public void AddInvoiceFlowInMemory_ShouldNotOverrideExistingProcessingRunRepository()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IProcessingRunRepository, CustomProcessingRunRepository>();

        services.AddInvoiceFlowInMemory();

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IProcessingRunRepository)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(CustomProcessingRunRepository),
            descriptor.ImplementationType);
    }

    private sealed class CustomProcessingRunRepository
        : IProcessingRunRepository
    {
        public Task SaveAsync(
            ProcessingRun processingRun,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
