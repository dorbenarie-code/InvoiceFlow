using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceFlow.Tests.Api;

public sealed class SwaggerApiTests
{
    [Fact]
    public async Task SwaggerJson_ShouldExposeExpectedApiMetadata()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        var info = root.GetProperty("info");

        Assert.Equal("InvoiceFlow API", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.Contains(
            "developer-facing invoice processing API",
            info.GetProperty("description").GetString());
    }

    [Fact]
    public async Task SwaggerJson_ShouldExposeHealthEndpoint()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var paths = json.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/health", out var healthPath));
        Assert.True(healthPath.TryGetProperty("get", out var healthGet));

        Assert.Equal(
            "Returns API health status.",
            healthGet.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SwaggerJson_ShouldExposeInvoiceProcessEndpointAsMultipartUpload()
    {
        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
            });

        var client = factory.CreateClient();

        var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var paths = json.RootElement.GetProperty("paths");

        Assert.True(paths.TryGetProperty("/api/invoices/process", out var invoicePath));
        Assert.True(invoicePath.TryGetProperty("post", out var postOperation));

        Assert.Equal(
            "Processes an invoice document.",
            postOperation.GetProperty("summary").GetString());

        Assert.Contains(
            "RequiresHumanReview",
            postOperation.GetProperty("description").GetString());

        var requestBody = postOperation.GetProperty("requestBody");
        var content = requestBody.GetProperty("content");

        Assert.True(content.TryGetProperty("multipart/form-data", out var multipartContent));

        var schema = multipartContent.GetProperty("schema");
        var properties = schema.GetProperty("properties");

        Assert.True(properties.TryGetProperty("file", out var fileProperty));
        Assert.Equal("string", fileProperty.GetProperty("type").GetString());
        Assert.Equal("binary", fileProperty.GetProperty("format").GetString());

        var responses = postOperation.GetProperty("responses");

        Assert.True(responses.TryGetProperty("200", out _));
        Assert.True(responses.TryGetProperty("400", out _));
        Assert.True(responses.TryGetProperty("413", out _));
    }
}
