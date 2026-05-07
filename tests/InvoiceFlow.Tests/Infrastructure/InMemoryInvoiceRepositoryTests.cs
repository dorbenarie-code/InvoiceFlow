using InvoiceFlow.Infrastructure.Invoices;
using InvoiceFlow.Tests.Domain;

namespace InvoiceFlow.Tests.Infrastructure;

public sealed class InMemoryInvoiceRepositoryTests
{
    private const int ConcurrentSaveCount = 100;

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

        var invoices = Enumerable
            .Range(1, ConcurrentSaveCount)
            .Select(index =>
                TestInvoiceFactory.CreateValidInvoice(
                    invoiceNumber: $"INV-{index}"))
            .ToArray();

        var tasks = invoices
            .Select(invoice =>
                Task.Run(() =>
                    repository.SaveAsync(invoice)))
            .ToArray();

        await Task.WhenAll(tasks);

        var savedInvoices = repository.Invoices;

        Assert.Equal(ConcurrentSaveCount, savedInvoices.Count);
        Assert.Equal(
            ConcurrentSaveCount,
            savedInvoices.Select(invoice => invoice.Id).Distinct().Count());

        Assert.All(invoices, invoice =>
            Assert.Contains(invoice, savedInvoices));

        Assert.All(invoices, invoice =>
            Assert.Contains(savedInvoices, savedInvoice =>
                savedInvoice.InvoiceNumber == invoice.InvoiceNumber));
    }

    [Fact]
    public async Task Invoices_ShouldReturnSnapshot()
    {
        var repository = new InMemoryInvoiceRepository();

        var firstSnapshot = repository.Invoices;

        await repository.SaveAsync(TestInvoiceFactory.CreateValidInvoice());

        Assert.Empty(firstSnapshot);
        Assert.Single(repository.Invoices);
    }

    [Fact]
    public async Task SaveAsync_ShouldNotStoreInvoice_WhenCancellationRequested()
    {
        var repository = new InMemoryInvoiceRepository();
        var invoice = TestInvoiceFactory.CreateValidInvoice();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.SaveAsync(
                invoice,
                cancellationTokenSource.Token));

        Assert.Empty(repository.Invoices);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenInvoiceIsNull()
    {
        var repository = new InMemoryInvoiceRepository();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!));

        Assert.Empty(repository.Invoices);
    }
}