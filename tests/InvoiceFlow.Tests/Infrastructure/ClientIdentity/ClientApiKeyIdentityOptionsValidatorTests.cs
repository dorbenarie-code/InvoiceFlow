using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.ClientIdentity;

public sealed class ClientApiKeyIdentityOptionsValidatorTests
{
    private const string ValidKeyPrefix = "if_dev_";

    private const string ValidKeyHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public void Validate_ShouldSucceed_WhenAtLeastOneValidClientIsConfigured()
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = ValidKeyPrefix,
                KeyHash = ValidKeyHash,
                IsActive = true
            });

        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_ShouldFail_WhenNoClientsAreConfigured()
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions();

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            "At least one client API key must be configured.",
            result.FailureMessage);
    }

    [Fact]
    public void Validate_ShouldFail_WhenClientIdIsEmpty()
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions(
            new ClientApiKeyOptions
            {
                ClientId = Guid.Empty,
                KeyPrefix = ValidKeyPrefix,
                KeyHash = ValidKeyHash,
                IsActive = true
            });

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            "Client API key client id is required.",
            result.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldFail_WhenKeyPrefixIsMissing(
        string keyPrefix)
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = keyPrefix,
                KeyHash = ValidKeyHash,
                IsActive = true
            });

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            "Client API key prefix is required.",
            result.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldFail_WhenKeyHashIsMissing(
        string keyHash)
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = ValidKeyPrefix,
                KeyHash = keyHash,
                IsActive = true
            });

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            "Client API key hash is required.",
            result.FailureMessage);
    }

    [Theory]
    [InlineData("not-a-hash")]
    [InlineData("abc")]
    [InlineData("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdeg")]
    public void Validate_ShouldFail_WhenKeyHashIsNotSha256Hex(
        string keyHash)
    {
        var validator = new ClientApiKeyIdentityOptionsValidator();

        var options = CreateOptions(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = ValidKeyPrefix,
                KeyHash = keyHash,
                IsActive = true
            });

        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(
            "Client API key hash must be a SHA-256 hex string.",
            result.FailureMessage);
    }

    private static ClientApiKeyIdentityOptions CreateOptions(
        params ClientApiKeyOptions[] clients)
    {
        var options = new ClientApiKeyIdentityOptions();

        foreach (var client in clients)
        {
            options.Clients.Add(client);
        }

        return options;
    }
}
