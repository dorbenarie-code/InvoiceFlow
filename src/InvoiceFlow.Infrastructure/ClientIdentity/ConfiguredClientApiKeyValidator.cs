using InvoiceFlow.Application.ClientIdentity;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.ClientIdentity;

public sealed class ConfiguredClientApiKeyValidator : IClientApiKeyValidator
{
    private readonly ClientApiKeyIdentityOptions _options;

    public ConfiguredClientApiKeyValidator(
        IOptions<ClientApiKeyIdentityOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value
            ?? throw new ArgumentException(
                "Client API key identity options are required.",
                nameof(options));
    }

    public Task<ClientApiKeyValidationResult> ValidateAsync(
        string? apiKey,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return Task.FromResult(ClientApiKeyValidationResult.Invalid());
        }

        foreach (var client in _options.Clients)
        {
            if (!client.IsActive)
            {
                continue;
            }

            if (client.ClientId == Guid.Empty)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(client.KeyPrefix)
                || string.IsNullOrWhiteSpace(client.KeyHash))
            {
                continue;
            }

            if (!apiKey.StartsWith(
                    client.KeyPrefix,
                    StringComparison.Ordinal))
            {
                continue;
            }

            var apiKeyHash = ClientApiKeyHash.ComputeSha256Hex(apiKey);

            if (!string.Equals(
                    apiKeyHash,
                    client.KeyHash,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return Task.FromResult(
                ClientApiKeyValidationResult.Valid(client.ClientId));
        }

        return Task.FromResult(ClientApiKeyValidationResult.Invalid());
    }}
