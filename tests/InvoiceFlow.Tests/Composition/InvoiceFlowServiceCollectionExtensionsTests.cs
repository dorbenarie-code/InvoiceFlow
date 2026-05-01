using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.DependencyInjection;

namespace InvoiceFlow.Tests.Composition;

public sealed class InvoiceFlowServiceCollectionExtensionsTests
{
    private static readonly DateOnly ValidationDate = new(2026, 4, 30);

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
    public void UseInMemoryInfrastructure_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow(ValidationDate);

        var returnedBuilder = builder.UseInMemoryInfrastructure();

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
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

    private static DocumentInput CreateDocumentInput()
    {
        return new DocumentInput(
            "invoice.pdf",
            "application/pdf",
            new byte[] { 1, 2, 3 });
    }
}
