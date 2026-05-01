using InvoiceFlow.Infrastructure.Invoices;
using InvoiceFlow.Tests.Domain;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class InMemoryInvoiceRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldStoreInvoice()
    {
        var repository = new InMemoryInvoiceRepository();
        var invoice = TestInvoiceFactory.CreateValidInvoice();

        await repository.SaveAsync(invoice);

        Assert.Single(repository.Invoices);
        Assert.Contains(invoice, repository.Invoices);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreAllInvoices_WhenCalledConcurrently()
    {
        var repository = new InMemoryInvoiceRepository();

        var tasks = Enumerable
            .Range(1, 100)
            .Select(index =>
                Task.Run(() =>
                    repository.SaveAsync(
                        TestInvoiceFactory.CreateValidInvoice(
                            invoiceNumber: $"INV-{index}"))));

        await Task.WhenAll(tasks);

        Assert.Equal(100, repository.Invoices.Count);
        Assert.Equal(100, repository.Invoices.Select(invoice => invoice.Id).Distinct().Count());
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenInvoiceIsNull()
    {
        var repository = new InMemoryInvoiceRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!));
    }
}
