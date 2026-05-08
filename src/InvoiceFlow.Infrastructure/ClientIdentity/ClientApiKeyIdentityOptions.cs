namespace InvoiceFlow.Infrastructure.ClientIdentity;

public sealed class ClientApiKeyIdentityOptions
{
    public IList<ClientApiKeyOptions> Clients { get; } =
        new List<ClientApiKeyOptions>();

    public ClientApiKeyIdentityOptions AddClient(
        Guid clientId,
        string keyHash,
        string keyPrefix,
        bool isActive = true)
    {
        Clients.Add(
            new ClientApiKeyOptions
            {
                ClientId = clientId,
                KeyHash = keyHash,
                KeyPrefix = keyPrefix,
                IsActive = isActive
            });

        return this;
    }
}
