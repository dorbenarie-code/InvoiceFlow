using InvoiceFlow.Domain.Invoices;

namespace InvoiceFlow.Tests.Domain;

public sealed class InvoiceStatusTests
{
    [Fact]
    public void InvoiceStatus_ShouldKeepExpectedNumericValues()
    {
        Assert.Equal(1, (int)InvoiceStatus.Extracted);
        Assert.Equal(2, (int)InvoiceStatus.RequiresHumanReview);
        Assert.Equal(3, (int)InvoiceStatus.Verified);
        Assert.Equal(4, (int)InvoiceStatus.Rejected);
        Assert.Equal(5, (int)InvoiceStatus.Exported);
    }
}