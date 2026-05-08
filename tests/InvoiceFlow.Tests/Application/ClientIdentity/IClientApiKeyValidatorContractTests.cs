using InvoiceFlow.Application.ClientIdentity;

namespace InvoiceFlow.Tests.Application.ClientIdentity;

public sealed class IClientApiKeyValidatorContractTests
{
    private const string ApiKey = "if_dev_test-api-key";

    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task ValidateAsync_ShouldAcceptApiKeyAndCancellationToken()
    {
        var validator = new CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult.Valid(ClientId));

        using var cancellationTokenSource = new CancellationTokenSource();

        var result = await validator.ValidateAsync(
            ApiKey,
            cancellationTokenSource.Token);

        Assert.True(result.IsValid);
        Assert.Equal(ClientId, result.ClientId);
        Assert.Equal(ApiKey, validator.ReceivedApiKey);
        Assert.Equal(
            cancellationTokenSource.Token,
            validator.ReceivedCancellationToken);
    }

    private sealed class CapturingClientApiKeyValidator
        : IClientApiKeyValidator
    {
        private readonly ClientApiKeyValidationResult _result;

        public string? ReceivedApiKey { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public CapturingClientApiKeyValidator(
            ClientApiKeyValidationResult result)
        {
            _result = result
                ?? throw new ArgumentNullException(nameof(result));
        }

        public Task<ClientApiKeyValidationResult> ValidateAsync(
            string? apiKey,
            CancellationToken cancellationToken = default)
        {
            ReceivedApiKey = apiKey;
            ReceivedCancellationToken = cancellationToken;

            return Task.FromResult(_result);
        }
    }
}
