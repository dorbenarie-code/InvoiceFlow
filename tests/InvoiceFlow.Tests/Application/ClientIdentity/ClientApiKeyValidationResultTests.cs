using InvoiceFlow.Application.ClientIdentity;

namespace InvoiceFlow.Tests.Application.ClientIdentity;

public sealed class ClientApiKeyValidationResultTests
{
    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Valid_ShouldReturnValidResult()
    {
        var result = ClientApiKeyValidationResult.Valid(ClientId);

        Assert.True(result.IsValid);
        Assert.Equal(ClientId, result.ClientId);
    }

    [Fact]
    public void Valid_ShouldThrow_WhenClientIdIsEmpty()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ClientApiKeyValidationResult.Valid(Guid.Empty));

        Assert.Equal("clientId", exception.ParamName);
        Assert.Contains("Client id is required.", exception.Message);
    }

    [Fact]
    public void Invalid_ShouldReturnInvalidResult()
    {
        var result = ClientApiKeyValidationResult.Invalid();

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }
}
