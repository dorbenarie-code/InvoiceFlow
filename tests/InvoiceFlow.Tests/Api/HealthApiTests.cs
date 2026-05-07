using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InvoiceFlow.Tests.Api;

public sealed class HealthApiTests
{
    [Fact]
    public async Task Health_ShouldReturnOk_WhenRunningInTesting()
    {
        await using var factory = CreateFactory("Testing");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Health_ShouldReturnJsonContentType()
    {
        await using var factory = CreateFactory("Testing");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            "application/json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Health_ShouldReturnStableHealthyPayload()
    {
        await using var factory = CreateFactory("Testing");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);
        var root = json.RootElement;

        Assert.Equal(JsonValueKind.Object, root.ValueKind);

        var properties = root
            .EnumerateObject()
            .ToArray();

        var property = Assert.Single(properties);

        Assert.Equal("status", property.Name);
        Assert.Equal("Healthy", property.Value.GetString());
    }

    [Fact]
    public async Task Health_ShouldBeAvailable_WhenRunningInDevelopment()
    {
        await using var factory = CreateFactory("Development");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertHealthyStatusAsync(response);
    }

    [Fact]
    public async Task Health_ShouldBeAvailable_WhenRunningInTesting()
    {
        await using var factory = CreateFactory("Testing");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertHealthyStatusAsync(response);
    }

    [Fact]
    public async Task Health_ShouldBeAvailable_WhenRunningInProductionOverHttps()
    {
        await using var factory = CreateFactory("Production");

        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await AssertHealthyStatusAsync(response);
    }

    [Fact]
    public async Task Health_ShouldReturnNotFound_WhenRouteIsUnknown()
    {
        await using var factory = CreateFactory("Testing");

        var client = factory.CreateClient();

        var response = await client.GetAsync("/health/details");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(
        string environmentName)
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environmentName);
            });
    }

    private static async Task AssertHealthyStatusAsync(
        HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(responseBody);

        Assert.Equal(
            "Healthy",
            json.RootElement.GetProperty("status").GetString());
    }
}