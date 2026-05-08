using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Infrastructure.ProcessingRuns;

namespace InvoiceFlow.Tests.Infrastructure.ProcessingRuns;

public sealed class InMemoryProcessingRunRepositoryTests
{
    [Fact]
    public async Task SaveAsync_ShouldStoreProcessingRun()
    {
        var repository = new InMemoryProcessingRunRepository();

        var processingRun = CreateProcessingRun();

        await repository.SaveAsync(processingRun);

        var savedProcessingRun = Assert.Single(repository.ProcessingRuns);

        Assert.Equal(processingRun, savedProcessingRun);
    }

    [Fact]
    public async Task SaveAsync_ShouldStoreMultipleProcessingRuns()
    {
        var repository = new InMemoryProcessingRunRepository();

        var firstRun = CreateProcessingRun(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var secondRun = CreateProcessingRun(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"));

        await repository.SaveAsync(firstRun);
        await repository.SaveAsync(secondRun);

        Assert.Equal(2, repository.ProcessingRuns.Count);
        Assert.Contains(firstRun, repository.ProcessingRuns);
        Assert.Contains(secondRun, repository.ProcessingRuns);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrow_WhenProcessingRunIsNull()
    {
        var repository = new InMemoryProcessingRunRepository();

        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!));

        Assert.Equal("processingRun", exception.ParamName);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenIsAlreadyCanceled()
    {
        var repository = new InMemoryProcessingRunRepository();

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.SaveAsync(
                CreateProcessingRun(),
                cancellationTokenSource.Token));

        Assert.Empty(repository.ProcessingRuns);
    }

    [Fact]
    public async Task ProcessingRuns_ShouldReturnSnapshot()
    {
        var repository = new InMemoryProcessingRunRepository();

        var firstRun = CreateProcessingRun(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"));

        var secondRun = CreateProcessingRun(
            id: Guid.Parse("22222222-2222-2222-2222-222222222222"));

        await repository.SaveAsync(firstRun);

        var snapshot = repository.ProcessingRuns;

        await repository.SaveAsync(secondRun);

        Assert.Single(snapshot);
        Assert.Equal(2, repository.ProcessingRuns.Count);
    }

    private static ProcessingRun CreateProcessingRun(
        Guid? id = null)
    {
        return new ProcessingRun(
            id: id ?? Guid.Parse("11111111-1111-1111-1111-111111111111"),
            clientId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            documentId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            invoiceId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            status: "Verified",
            analyzedPageCount: 1,
            durationMs: 9985,
            errorCode: null,
            createdAtUtc: new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc));
    }
}
