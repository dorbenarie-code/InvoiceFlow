using InvoiceFlow.Application.ProcessingRuns;

namespace InvoiceFlow.Tests.Application.ProcessingRuns;

public sealed class IProcessingRunRepositoryContractTests
{
    [Fact]
    public async Task SaveAsync_ShouldAcceptProcessingRunAndCancellationToken()
    {
        var repository = new CapturingProcessingRunRepository();

        var processingRun = new ProcessingRun(
            id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            clientId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            documentId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            invoiceId: Guid.Parse("44444444-4444-4444-4444-444444444444"),
            status: "Verified",
            analyzedPageCount: 1,
            durationMs: 9985,
            errorCode: null,
            createdAtUtc: new DateTime(2026, 5, 7, 10, 0, 0, DateTimeKind.Utc));

        using var cancellationTokenSource = new CancellationTokenSource();

        await repository.SaveAsync(
            processingRun,
            cancellationTokenSource.Token);

        Assert.Same(processingRun, repository.SavedProcessingRun);
        Assert.Equal(
            cancellationTokenSource.Token,
            repository.ReceivedCancellationToken);
    }

    private sealed class CapturingProcessingRunRepository
        : IProcessingRunRepository
    {
        public ProcessingRun? SavedProcessingRun { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public Task SaveAsync(
            ProcessingRun processingRun,
            CancellationToken cancellationToken = default)
        {
            SavedProcessingRun = processingRun;
            ReceivedCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }
}
