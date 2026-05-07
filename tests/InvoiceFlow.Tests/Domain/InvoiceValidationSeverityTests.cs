using InvoiceFlow.Domain.Invoices;
using Xunit;

namespace InvoiceFlow.Tests.Domain;

public sealed class InvoiceValidationSeverityTests
{
    [Fact]
    public void InvoiceValidationSeverity_ShouldKeepExpectedNumericValues()
    {
        Assert.Equal(1, (int)InvoiceValidationSeverity.Warning);
        Assert.Equal(2, (int)InvoiceValidationSeverity.Error);
    }
}