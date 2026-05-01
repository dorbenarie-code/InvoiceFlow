namespace InvoiceFlow.Api.Health;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health", () =>
            Results.Ok(new HealthResponse("Healthy")))
            .WithTags("Health")
            .WithName("GetHealth")
            .WithSummary("Returns API health status.")
            .WithDescription("Returns a simple health status response for checking that the API is running.")
            .Produces<HealthResponse>(
                StatusCodes.Status200OK,
                "application/json")
            .WithOpenApi();

        return endpoints;
    }
}
