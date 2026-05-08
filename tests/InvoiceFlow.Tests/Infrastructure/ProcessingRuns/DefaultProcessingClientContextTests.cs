using InvoiceFlow.Infrastructure.ProcessingRuns;

namespace InvoiceFlow.Tests.Infrastructure.ProcessingRuns;

public sealed class DefaultProcessingClientContextTests
{
    [Fact]
    public void ClientId_ShouldReturnStableNonEmptyDefaultClientId()
    {
        var context = new DefaultProcessingClientContext();

        Assert.NotEqual(Guid.Empty, context.ClientId);
        Assert.Equal(context.ClientId, context.ClientId);
    }
}
