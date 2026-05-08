namespace InvoiceFlow.Application.ClientIdentity;

public sealed record ClientApiKeyValidationResult
{
    public bool IsValid { get; }

    public Guid? ClientId { get; }

    private ClientApiKeyValidationResult(
        bool isValid,
        Guid? clientId)
    {
        IsValid = isValid;
        ClientId = clientId;
    }

    public static ClientApiKeyValidationResult Valid(
        Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException(
                "Client id is required.",
                nameof(clientId));
        }

        return new ClientApiKeyValidationResult(
            true,
            clientId);
    }

    public static ClientApiKeyValidationResult Invalid()
    {
        return new ClientApiKeyValidationResult(
            false,
            null);
    }
}
