namespace InvoiceFlow.Application.ClientIdentity;

public interface IClientApiKeyValidator
{
    Task<ClientApiKeyValidationResult> ValidateAsync(
        string? apiKey,
        CancellationToken cancellationToken = default);
}
