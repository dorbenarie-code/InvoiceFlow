using System.Security.Cryptography;
using System.Text;

namespace InvoiceFlow.Infrastructure.ClientIdentity;

public static class ClientApiKeyHash
{
    public static string ComputeSha256Hex(
        string apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException(
                "API key is required.",
                nameof(apiKey));
        }

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(apiKey));

        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
