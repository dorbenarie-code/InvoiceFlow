using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class InvoiceFlowServiceCollectionExtensions
{
    public static IInvoiceFlowBuilder AddInvoiceFlow(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInvoiceFlowCore();

        return new InvoiceFlowBuilder(services);
    }

    public static IInvoiceFlowBuilder AddInvoiceFlow(
        this IServiceCollection services,
        DateOnly validationDate)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInvoiceFlowCore(validationDate);

        return new InvoiceFlowBuilder(services);
    }

    public static IInvoiceFlowBuilder UseInMemoryInfrastructure(
        this IInvoiceFlowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddInvoiceFlowInMemory();

        return builder;
    }

    public static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddInvoiceFlowCore(
            () => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services,
        DateOnly validationDate)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddInvoiceFlowCore(() => validationDate);
    }

    public static IServiceCollection AddInvoiceFlowInMemory(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDocumentStorage, InMemoryDocumentStorage>();
        services.TryAddSingleton<IDocumentExtractor, FakeDocumentExtractor>();
        services.TryAddSingleton<IInvoiceMapper, FieldBasedInvoiceMapper>();
        services.TryAddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();

        return services;
    }

    private static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services,
        Func<DateOnly> validationDateProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(validationDateProvider);

        services.TryAddScoped<IInvoiceDocumentProcessor, ProcessInvoiceDocumentService>();

        services.TryAddScoped<IInvoiceValidator>(_ =>
            new DefaultInvoiceValidator(validationDateProvider()));

        return services;
    }
}
