using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.ProcessingRuns;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Composition;

public sealed class ProcessingRunInvoiceDocumentProcessorCompositionTests
{
    private static readonly DateOnly ValidationDate = new(2026, 5, 7);

    [Fact]
    public void AddInvoiceFlow_WithInMemoryInfrastructure_ShouldResolveInvoiceDocumentProcessorAsProcessingRunDecorator()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IInvoiceDocumentProcessor>();

        Assert.IsType<ProcessingRunInvoiceDocumentProcessor>(processor);
    }

    [Fact]
    public async Task AddInvoiceFlow_WithInMemoryInfrastructure_ShouldProcessInvoiceAndSaveProcessingRun()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow(ValidationDate)
            .UseInMemoryInfrastructure();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        using var scope = provider.CreateScope();

        var processor = scope.ServiceProvider
            .GetRequiredService<IInvoiceDocumentProcessor>();

        var result = await processor.ProcessAsync(CreateDocumentInput());

        Assert.Equal(InvoiceStatus.Verified, result.Status);

        var processingRunRepository = scope.ServiceProvider
            .GetRequiredService<IProcessingRunRepository>();

        var inMemoryProcessingRunRepository =
            Assert.IsType<InMemoryProcessingRunRepository>(processingRunRepository);

        var processingRun = Assert.Single(
            inMemoryProcessingRunRepository.ProcessingRuns);

        Assert.NotEqual(Guid.Empty, processingRun.Id);
        Assert.NotEqual(Guid.Empty, processingRun.ClientId);
        Assert.Equal(result.DocumentId, processingRun.DocumentId);
        Assert.Equal(result.InvoiceId, processingRun.InvoiceId);
        Assert.Equal("Verified", processingRun.Status);
        Assert.Equal(result.AnalyzedPageCount, processingRun.AnalyzedPageCount);
        Assert.Null(processingRun.ErrorCode);
        Assert.True(processingRun.DurationMs >= 0);
        Assert.Equal(DateTimeKind.Utc, processingRun.CreatedAtUtc.Kind);
    }

    [Fact]
    public void AddInvoiceFlowInMemory_ShouldRegisterDefaultProcessingClientContextAsSingleton()
    {
        var services = new ServiceCollection();

        services.AddInvoiceFlowInMemory();

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IProcessingClientContext)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(DefaultProcessingClientContext),
            descriptor.ImplementationType);
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[]
            {
                0x25, 0x50, 0x44, 0x46, 0x2D
            });
    }
}
