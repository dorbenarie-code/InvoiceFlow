using InvoiceFlow.Api.ClientRateLimiting;
using InvoiceFlow.Application.ClientRateLimiting;
using InvoiceFlow.Infrastructure.ClientRateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api.ClientRateLimiting;

public sealed class InvoiceFlowApiRateLimitingBuilderExtensionsTests
{
    [Fact]
    public void UseClientRateLimiting_ShouldRegisterInMemoryClientRateLimiterAsSingleton()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseClientRateLimiting(_ => { });

        var descriptor = Assert.Single(
            services,
            service => service.ServiceType == typeof(IClientRateLimiter));

        Assert.Equal(
            ServiceLifetime.Singleton,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(InMemoryClientRateLimiter),
            descriptor.ImplementationType);
    }

    [Fact]
    public async Task UseClientRateLimiting_ShouldResolveIClientRateLimiter()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseClientRateLimiting(_ => { });

        await using var provider = services.BuildServiceProvider();

        var limiter = provider.GetRequiredService<IClientRateLimiter>();

        Assert.IsType<InMemoryClientRateLimiter>(limiter);
    }

    [Fact]
    public void UseClientRateLimiting_ShouldConfigureClientRateLimitOptions()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseClientRateLimiting(options =>
            {
                options.PermitLimit = 7;
                options.Window = TimeSpan.FromSeconds(30);
            });

        using var provider = services.BuildServiceProvider();

        var options = provider
            .GetRequiredService<IOptions<ClientRateLimitOptions>>()
            .Value;

        Assert.Equal(7, options.PermitLimit);
        Assert.Equal(TimeSpan.FromSeconds(30), options.Window);
    }

    [Fact]
    public void UseClientRateLimiting_ShouldReplaceExistingClientRateLimiterRegistration()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IClientRateLimiter, ExistingClientRateLimiter>();

        services
            .AddInvoiceFlow()
            .UseClientRateLimiting(_ => { });

        var descriptors = services
            .Where(service => service.ServiceType == typeof(IClientRateLimiter))
            .ToArray();

        var descriptor = Assert.Single(descriptors);

        Assert.Equal(
            typeof(InMemoryClientRateLimiter),
            descriptor.ImplementationType);
    }

    [Fact]
    public void UseClientRateLimiting_ShouldReturnSameBuilder()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow();

        var returnedBuilder = builder.UseClientRateLimiting(_ => { });

        Assert.Same(builder, returnedBuilder);
    }

    [Fact]
    public void UseClientRateLimiting_ShouldThrow_WhenBuilderIsNull()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            InvoiceFlowApiRateLimitingBuilderExtensions.UseClientRateLimiting(
                null!,
                _ => { }));

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void UseClientRateLimiting_ShouldThrow_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.UseClientRateLimiting(null!));

        Assert.Equal("configureOptions", exception.ParamName);
    }

    private sealed class ExistingClientRateLimiter : IClientRateLimiter
    {
        public Task<ClientRateLimitResult> AcquireAsync(
            Guid clientId,
            string resource,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(ClientRateLimitResult.Allowed());
        }
    }
}
