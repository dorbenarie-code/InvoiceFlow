using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Infrastructure.ClientIdentity;

public sealed class ConfiguredClientApiKeyValidatorTests
{
    private const string ValidApiKey = "if_dev_valid-secret-key";
    private const string UnknownApiKey = "if_dev_unknown-secret-key";
    private const string KeyWithWrongPrefix = "wrong_valid-secret-key";
    private const string KeyPrefix = "if_dev_";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ValidateAsync_ShouldReturnClientId_WhenApiKeyMatchesConfiguredHash()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = true
            });

        var result = await validator.ValidateAsync(ValidApiKey);

        Assert.True(result.IsValid);
        Assert.Equal(ClientId, result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenApiKeyIsNull()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = true
            });

        var result = await validator.ValidateAsync(null);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenApiKeyIsEmptyOrWhiteSpace(
        string apiKey)
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = true
            });

        var result = await validator.ValidateAsync(apiKey);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenApiKeyIsUnknown()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = true
            });

        var result = await validator.ValidateAsync(UnknownApiKey);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenClientIsInactive()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = false
            });

        var result = await validator.ValidateAsync(ValidApiKey);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenPrefixDoesNotMatch()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(KeyWithWrongPrefix),
                IsActive = true
            });

        var result = await validator.ValidateAsync(KeyWithWrongPrefix);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnInvalid_WhenNoClientsAreConfigured()
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(ValidApiKey);

        Assert.False(result.IsValid);
        Assert.Null(result.ClientId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowOperationCanceledException_WhenCancellationTokenIsAlreadyCanceled()
    {
        var validator = CreateValidator(
            new ClientApiKeyOptions
            {
                ClientId = ClientId,
                KeyPrefix = KeyPrefix,
                KeyHash = ClientApiKeyHash.ComputeSha256Hex(ValidApiKey),
                IsActive = true
            });

        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            validator.ValidateAsync(
                ValidApiKey,
                cancellationTokenSource.Token));
    }

    private static ConfiguredClientApiKeyValidator CreateValidator(
        params ClientApiKeyOptions[] clients)
    {
        var options = new ClientApiKeyIdentityOptions();

        foreach (var client in clients)
        {
            options.Clients.Add(client);
        }

        return new ConfiguredClientApiKeyValidator(
            Options.Create(options));
    }}
