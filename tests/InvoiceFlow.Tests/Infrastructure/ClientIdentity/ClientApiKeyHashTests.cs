using InvoiceFlow.Infrastructure.ClientIdentity;

namespace InvoiceFlow.Tests.Infrastructure.ClientIdentity;

public sealed class ClientApiKeyHashTests
{
    [Fact]
    public void ComputeSha256Hex_ShouldReturnStableSha256HexHash()
    {
        var hash = ClientApiKeyHash.ComputeSha256Hex(
            "if_dev_valid-secret-key");

        Assert.Equal(
            "cefca465b319c54338083122d2b38e990736aa56e5d8ec5f70e1ede0aafe6867",
            hash);
    }

    [Fact]
    public void ComputeSha256Hex_ShouldReturnLowercaseHex()
    {
        var hash = ClientApiKeyHash.ComputeSha256Hex(
            "if_dev_valid-secret-key");

        Assert.Equal(hash.ToLowerInvariant(), hash);
    }

    [Fact]
    public void ComputeSha256Hex_ShouldThrow_WhenApiKeyIsNull()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ClientApiKeyHash.ComputeSha256Hex(null!));

        Assert.Equal("apiKey", exception.ParamName);
        Assert.Contains("API key is required.", exception.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ComputeSha256Hex_ShouldThrow_WhenApiKeyIsEmptyOrWhiteSpace(
        string apiKey)
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            ClientApiKeyHash.ComputeSha256Hex(apiKey));

        Assert.Equal("apiKey", exception.ParamName);
        Assert.Contains("API key is required.", exception.Message);
    }
}
