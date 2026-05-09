using InvoiceFlow.Application.ClientRateLimiting;
using InvoiceFlow.Infrastructure.ClientRateLimiting;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

[Collection(InvoiceFlowApiConfigurationTestCollection.Name)]
public sealed class InvoiceApiRateLimitingConfigurationTests
{
    private const string PermitLimitEnvironmentVariable =
        "InvoiceFlow__ClientRateLimiting__PermitLimit";

    private const string WindowEnvironmentVariable =
        "InvoiceFlow__ClientRateLimiting__Window";

    [Fact]
    public void ApiStartup_ShouldNotRegisterClientRateLimiter_WhenClientRateLimitingConfigurationIsMissing()
    {
        using var environment = TemporaryEnvironmentVariables.Clear(
            PermitLimitEnvironmentVariable,
            WindowEnvironmentVariable);

        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        var limiter = factory.Services.GetService<IClientRateLimiter>();

        Assert.Null(limiter);
    }

    [Fact]
    public void ApiStartup_ShouldRegisterInMemoryClientRateLimiter_WhenClientRateLimitingConfigurationIsConfigured()
    {
        using var environment = TemporaryEnvironmentVariables.Set(
            new Dictionary<string, string?>
            {
                [PermitLimitEnvironmentVariable] = "5",
                [WindowEnvironmentVariable] = "00:01:00"
            });

        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        var limiter = factory.Services.GetRequiredService<IClientRateLimiter>();

        Assert.IsType<InMemoryClientRateLimiter>(limiter);
    }

    [Fact]
    public void ApiStartup_ShouldBindClientRateLimitOptionsFromConfiguration()
    {
        using var environment = TemporaryEnvironmentVariables.Set(
            new Dictionary<string, string?>
            {
                [PermitLimitEnvironmentVariable] = "7",
                [WindowEnvironmentVariable] = "00:00:30"
            });

        using var factory = CreateFactory();

        using var client = factory.CreateClient();

        var options = factory.Services
            .GetRequiredService<IOptions<ClientRateLimitOptions>>()
            .Value;

        Assert.Equal(7, options.PermitLimit);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Window);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public void ApiStartup_ShouldFail_WhenClientRateLimitPermitLimitIsNotGreaterThanZero(
        string invalidPermitLimit)
    {
        using var environment = TemporaryEnvironmentVariables.Set(
            new Dictionary<string, string?>
            {
                [PermitLimitEnvironmentVariable] = invalidPermitLimit,
                [WindowEnvironmentVariable] = "00:01:00"
            });

        using var factory = CreateFactory();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            factory.CreateClient());

        Assert.Contains(
            "Client rate limit permit limit must be greater than zero.",
            exception.Message);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-00:00:01")]
    public void ApiStartup_ShouldFail_WhenClientRateLimitWindowIsNotGreaterThanZero(
        string invalidWindow)
    {
        using var environment = TemporaryEnvironmentVariables.Set(
            new Dictionary<string, string?>
            {
                [PermitLimitEnvironmentVariable] = "5",
                [WindowEnvironmentVariable] = invalidWindow
            });

        using var factory = CreateFactory();

        var exception = Assert.Throws<OptionsValidationException>(() =>
            factory.CreateClient());

        Assert.Contains(
            "Client rate limit window must be greater than zero.",
            exception.Message);
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
            });
    }

    private sealed class TemporaryEnvironmentVariables : IDisposable
    {
        private readonly Dictionary<string, string?> _originalValues;

        private TemporaryEnvironmentVariables(
            IEnumerable<string> variableNames)
        {
            _originalValues = variableNames.ToDictionary(
                variableName => variableName,
                Environment.GetEnvironmentVariable);
        }

        public static TemporaryEnvironmentVariables Set(
            IReadOnlyDictionary<string, string?> values)
        {
            var environment = new TemporaryEnvironmentVariables(values.Keys);

            foreach (var item in values)
            {
                Environment.SetEnvironmentVariable(
                    item.Key,
                    item.Value);
            }

            return environment;
        }

        public static TemporaryEnvironmentVariables Clear(
            params string[] variableNames)
        {
            var environment = new TemporaryEnvironmentVariables(variableNames);

            foreach (var variableName in variableNames)
            {
                Environment.SetEnvironmentVariable(
                    variableName,
                    null);
            }

            return environment;
        }

        public void Dispose()
        {
            foreach (var item in _originalValues)
            {
                Environment.SetEnvironmentVariable(
                    item.Key,
                    item.Value);
            }
        }
    }
}
