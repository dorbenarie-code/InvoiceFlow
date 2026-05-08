using Microsoft.Extensions.Options;

namespace InvoiceFlow.Infrastructure.ClientIdentity;

public sealed class ClientApiKeyIdentityOptionsValidator
    : IValidateOptions<ClientApiKeyIdentityOptions>
{
    private const int Sha256HexLength = 64;

    public ValidateOptionsResult Validate(
        string? name,
        ClientApiKeyIdentityOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var failures = new List<string>();

        if (options.Clients.Count == 0)
        {
            failures.Add("At least one client API key must be configured.");
        }

        foreach (var client in options.Clients)
        {
            ValidateClient(client, failures);
        }

        if (failures.Count > 0)
        {
            return ValidateOptionsResult.Fail(failures);
        }

        return ValidateOptionsResult.Success;
    }

    private static void ValidateClient(
        ClientApiKeyOptions client,
        List<string> failures)
    {
        if (client.ClientId == Guid.Empty)
        {
            failures.Add("Client API key client id is required.");
        }

        if (string.IsNullOrWhiteSpace(client.KeyPrefix))
        {
            failures.Add("Client API key prefix is required.");
        }

        if (string.IsNullOrWhiteSpace(client.KeyHash))
        {
            failures.Add("Client API key hash is required.");
            return;
        }

        if (!IsSha256Hex(client.KeyHash))
        {
            failures.Add("Client API key hash must be a SHA-256 hex string.");
        }
    }

    private static bool IsSha256Hex(
        string value)
    {
        if (value.Length != Sha256HexLength)
        {
            return false;
        }

        return value.All(character =>
            character is >= '0' and <= '9'
            || character is >= 'a' and <= 'f'
            || character is >= 'A' and <= 'F');
    }
}
