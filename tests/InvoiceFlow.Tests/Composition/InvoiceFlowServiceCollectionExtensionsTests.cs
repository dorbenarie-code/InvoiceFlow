using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Domain.ValueObjects;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Composition;

public sealed class InvoiceFlowServiceCollectionExtensionsTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

    [Fact]
    public void AddInvoiceFlow_ShouldReturnBuilderWithSameServiceCollection()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow();

        Assert.Same(services, builder.Services);
    }

    [Fact]
    public void UseInMemoryInfrastructure_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow(ValidationDate);

        var returnedBuilder = builder.UseInMemoryInfrastructure();

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }
    [Fact]
public void UseDocumentExtractor_ShouldReturnSameBuilder_ForMethodChaining()
{
    var services = new ServiceCollection();

    var builder = services.AddInvoiceFlow(ValidationDate);

    var returnedBuilder = builder.UseDocumentExtractor<CustomDocumentExtractor>();

    Assert.Same(builder, returnedBuilder);
    Assert.Same(services, returnedBuilder.Services);
}

    [Fact]
    public void AddInvoiceFlowCore_ShouldRegisterCoreServicesAsScoped()
    {
        var services = new ServiceCollection();

        services.AddInvoiceFlowCore(ValidationDate);

        AssertSingleRegistration<ProcessInvoiceDocumentService, ProcessInvoiceDocumentService>(
            services,
            ServiceLifetime.Scoped);

        AssertSingleFactoryRegistration<IInvoiceDocumentProcessor>(
            services,
            ServiceLifetime.Scoped);

        AssertSingleFactoryRegistration<IInvoiceValidator>(
            services,
            ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInvoiceFlowInMemory_ShouldRegisterInfrastructureServicesAsSingletons()
    {
        var services = new ServiceCollection();

        services.AddInvoiceFlowInMemory();

        AssertSingleRegistration<IDocumentStorage, InMemoryDocumentStorage>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IDocumentExtractor, FakeDocumentExtractor>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IInvoiceMapper, FieldBasedInvoiceMapper>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IInvoiceRepository, InMemoryInvoiceRepository>(
            services,
            ServiceLifetime.Singleton);
    }
    [Fact]
public void UseDocumentExtractor_ShouldRegisterCustomDocumentExtractorAsSingleton()
{
    var services = new ServiceCollection();

    services
        .AddInvoiceFlow(ValidationDate)
        .UseDocumentExtractor<CustomDocumentExtractor>();

    AssertSingleRegistration<IDocumentExtractor, CustomDocumentExtractor>(
        services,
        ServiceLifetime.Singleton);
}

    [Fact]
    public void AddInvoiceFlowCore_ShouldNotOverrideExistingCoreServices()
    {
        var services = new ServiceCollection();

        services.AddScoped<IInvoiceDocumentProcessor, CustomInvoiceDocumentProcessor>();
        services.AddScoped<IInvoiceValidator, CustomInvoiceValidator>();

        services.AddInvoiceFlowCore(ValidationDate);

        AssertSingleRegistration<IInvoiceDocumentProcessor, CustomInvoiceDocumentProcessor>(
            services,
            ServiceLifetime.Scoped);

        AssertSingleRegistration<IInvoiceValidator, CustomInvoiceValidator>(
            services,
            ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddInvoiceFlowInMemory_ShouldNotOverrideExistingInfrastructureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IDocumentStorage, CustomDocumentStorage>();
        services.AddSingleton<IDocumentExtractor, CustomDocumentExtractor>();
        services.AddSingleton<IInvoiceMapper, CustomInvoiceMapper>();
        services.AddSingleton<IInvoiceRepository, CustomInvoiceRepository>();

        services.AddInvoiceFlowInMemory();

        AssertSingleRegistration<IDocumentStorage, CustomDocumentStorage>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IDocumentExtractor, CustomDocumentExtractor>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IInvoiceMapper, CustomInvoiceMapper>(
            services,
            ServiceLifetime.Singleton);

        AssertSingleRegistration<IInvoiceRepository, CustomInvoiceRepository>(
            services,
            ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddInvoiceFlow_ShouldConfigureValidatorWithProvidedValidationDate()
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

        var validator = scope.ServiceProvider.GetRequiredService<IInvoiceValidator>();

        var invoice = Invoice.CreateExtracted(
            sourceDocumentId: Guid.NewGuid(),
            vendor: new Vendor("Cohen Office Supplies Ltd"),
            invoiceNumber: "INV-1001",
            issueDate: new DateOnly(2026, 5, 1),
            subtotalAmount: new CurrencyAmount(1000, "ILS"),
            vatAmount: new CurrencyAmount(180, "ILS"),
            totalAmount: new CurrencyAmount(1180, "ILS"));

        var report = validator.Validate(invoice);

        Assert.Contains(report.Issues, issue =>
            issue.Code == "FUTURE_ISSUE_DATE");
    }

    [Fact]
    public async Task AddInvoiceFlow_WithInMemoryInfrastructure_ShouldResolveAndProcessInvoice()
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
        Assert.False(result.ValidationReport.HasIssues);

        var repository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();
        var inMemoryRepository = Assert.IsType<InMemoryInvoiceRepository>(repository);

        Assert.Single(inMemoryRepository.Invoices);
        Assert.Equal(result.InvoiceId, inMemoryRepository.Invoices.Single().Id);
    }

    [Fact]
    public async Task UseInMemoryInfrastructure_ShouldNotOverrideAlreadyRegisteredDocumentExtractor()
    {
        var extractedDocument = new ExtractedDocument(
            "custom extracted invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Custom Vendor Ltd",
                ["VendorTaxId"] = "123456789",
                ["InvoiceNumber"] = "CUSTOM-1",
                ["IssueDate"] = "2026-04-30",
                ["SubtotalAmount"] = "100",
                ["VatAmount"] = "18",
                ["TotalAmount"] = "118",
                ["Currency"] = "ILS"
            });

        var services = new ServiceCollection();

        services.AddSingleton<IDocumentExtractor>(
            new FakeDocumentExtractor(extractedDocument));

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
        Assert.Equal("CUSTOM-1", result.Invoice.InvoiceNumber);
        Assert.Equal("Custom Vendor Ltd", result.Invoice.Vendor?.Name);
    }
    [Fact]
public async Task UseDocumentExtractor_ShouldOverrideInMemoryDocumentExtractor_WhenRegisteredAfterInMemoryInfrastructure()
{
    var services = new ServiceCollection();

    services
        .AddInvoiceFlow(ValidationDate)
        .UseInMemoryInfrastructure()
        .UseDocumentExtractor<CustomDocumentExtractor>();

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
    Assert.Equal("CUSTOM-FLUENT-1", result.Invoice.InvoiceNumber);
    Assert.Equal("Custom Fluent Vendor Ltd", result.Invoice.Vendor?.Name);
}

    [Fact]
    public void AddInvoiceFlow_ShouldThrow_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddInvoiceFlow());
    }
    [Fact]
public void UseDocumentExtractor_ShouldThrow_WhenBuilderIsNull()
{
    IInvoiceFlowBuilder builder = null!;

    Assert.Throws<ArgumentNullException>(() =>
        builder.UseDocumentExtractor<CustomDocumentExtractor>());
}

    [Fact]
    public void UseInMemoryInfrastructure_ShouldThrow_WhenBuilderIsNull()
    {
        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseInMemoryInfrastructure());
    }

    [Fact]
    public void AddInvoiceFlowInMemory_ShouldThrow_WhenServicesIsNull()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddInvoiceFlowInMemory());
    }

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }

    private static void AssertSingleRegistration<TService, TImplementation>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(TService)));

        Assert.Equal(expectedLifetime, descriptor.Lifetime);
        Assert.Equal(typeof(TImplementation), descriptor.ImplementationType);
    }

    private static void AssertSingleFactoryRegistration<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(TService)));

        Assert.Equal(expectedLifetime, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationFactory);
    }

    private sealed class CustomDocumentStorage : IDocumentStorage
    {
        public Task<StoredDocument> SaveAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CustomDocumentExtractor : IDocumentExtractor
{
    public Task<ExtractedDocument> ExtractAsync(
        DocumentInput document,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);
        cancellationToken.ThrowIfCancellationRequested();

        var extractedDocument = new ExtractedDocument(
            "custom fluent extracted invoice text",
            new Dictionary<string, string>
            {
                ["VendorName"] = "Custom Fluent Vendor Ltd",
                ["VendorTaxId"] = "123456789",
                ["InvoiceNumber"] = "CUSTOM-FLUENT-1",
                ["IssueDate"] = "2026-04-30",
                ["SubtotalAmount"] = "100",
                ["VatAmount"] = "18",
                ["TotalAmount"] = "118",
                ["Currency"] = "ILS"
            });

        return Task.FromResult(extractedDocument);
    }
}

    private sealed class CustomInvoiceMapper : IInvoiceMapper
    {
        public Task<Invoice> MapAsync(
            ExtractedDocument document,
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CustomInvoiceRepository : IInvoiceRepository
    {
        public Task SaveAsync(
            Invoice invoice,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CustomInvoiceDocumentProcessor : IInvoiceDocumentProcessor
    {
        public Task<ProcessInvoiceDocumentResult> ProcessAsync(
            DocumentInput document,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class CustomInvoiceValidator : IInvoiceValidator
    {
        public InvoiceValidationReport Validate(Invoice invoice)
        {
            return InvoiceValidationReport.Valid();
        }
    }
}