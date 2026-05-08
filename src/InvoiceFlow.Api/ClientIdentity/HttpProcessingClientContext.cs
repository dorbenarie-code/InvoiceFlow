using InvoiceFlow.Application.ProcessingRuns;

namespace InvoiceFlow.Api.ClientIdentity;

public sealed class HttpProcessingClientContext : IProcessingClientContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpProcessingClientContext(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor
            ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public Guid ClientId
    {
        get
        {
            var httpContext = _httpContextAccessor.HttpContext;

            if (httpContext is null)
            {
                throw new InvalidOperationException(
                    "HTTP context is required to resolve the processing client id.");
            }

            if (!httpContext.Items.TryGetValue(
                    ClientApiKeyHttpContextKeys.ClientId,
                    out var value)
                || value is not Guid clientId
                || clientId == Guid.Empty)
            {
                throw new InvalidOperationException(
                    "Processing client id was not resolved for the current HTTP request.");
            }

            return clientId;
        }
    }
}
