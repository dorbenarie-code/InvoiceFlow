using System.Globalization;
using System.Text.Json;
using InvoiceFlow.Api.Invoices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceApiKeyOpenApiTests
{
    [Fact]
    public async Task SwaggerJson_ShouldExposeApiKeySecurityScheme()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        var root = json.RootElement;

        Assert.True(
            root.TryGetProperty("components", out var components),
            "Swagger JSON should include components.");

        Assert.True(
            components.TryGetProperty("securitySchemes", out var securitySchemes),
            "Swagger JSON should include components.securitySchemes.");

        Assert.True(
            securitySchemes.TryGetProperty("ApiKey", out var apiKeyScheme),
            "Swagger JSON should include an ApiKey security scheme.");

        Assert.Equal(
            "apiKey",
            apiKeyScheme.GetProperty("type").GetString());

        Assert.Equal(
            "X-API-Key",
            apiKeyScheme.GetProperty("name").GetString());

        Assert.Equal(
            "header",
            apiKeyScheme.GetProperty("in").GetString());
    }

    [Fact]
    public async Task SwaggerJson_ShouldMarkProcessInvoiceEndpointAsRequiringApiKey()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        var root = json.RootElement;

        var postOperation = root
            .GetProperty("paths")
            .GetProperty("/api/invoices/process")
            .GetProperty("post");

        Assert.True(
            postOperation.TryGetProperty("security", out var securityRequirements),
            "POST /api/invoices/process should declare OpenAPI security requirements.");

        Assert.Contains(
            securityRequirements.EnumerateArray(),
            requirement =>
                requirement.TryGetProperty("ApiKey", out var apiKeyRequirement)
                && apiKeyRequirement.ValueKind == JsonValueKind.Array);
    }

    [Fact]
    public async Task SwaggerJson_ShouldDocumentUnauthorizedResponseForProcessInvoiceEndpoint()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        var root = json.RootElement;

        var postOperation = root
            .GetProperty("paths")
            .GetProperty("/api/invoices/process")
            .GetProperty("post");

        Assert.True(
            postOperation.TryGetProperty("responses", out var responses),
            "POST /api/invoices/process should declare OpenAPI responses.");

        Assert.True(
            responses.TryGetProperty("401", out var unauthorizedResponse),
            "POST /api/invoices/process should document 401 Unauthorized.");

        Assert.True(
            unauthorizedResponse.TryGetProperty("content", out var content),
            "401 Unauthorized should document a response body content type.");

        Assert.True(
            content.TryGetProperty("application/json", out _),
            "401 Unauthorized should document the ApiErrorResponse JSON contract.");
    }

    [Fact]
    public async Task SwaggerJson_ShouldNotMarkHealthEndpointAsRequiringApiKey()
    {
        await using var factory = CreateFactory();

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        var root = json.RootElement;

        var getOperation = root
            .GetProperty("paths")
            .GetProperty("/health")
            .GetProperty("get");

        Assert.False(
            getOperation.TryGetProperty("security", out _),
            "GET /health should remain public and must not declare OpenAPI security requirements.");
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["InvoiceFlow:Upload:MaxFileSizeInBytes"] =
                                InvoiceDocumentUploadOptions
                                    .DefaultMaxFileSizeInBytes
                                    .ToString(CultureInfo.InvariantCulture)
                        });
                });
            });
    }
}
