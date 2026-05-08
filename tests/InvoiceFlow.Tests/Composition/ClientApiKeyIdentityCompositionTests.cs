using InvoiceFlow.Api.ClientIdentity;
using InvoiceFlow.Application.ClientIdentity;
using InvoiceFlow.Application.ProcessingRuns;
using InvoiceFlow.Infrastructure.ClientIdentity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Composition;

public sealed class ClientApiKeyIdentityCompositionTests
{
    private static readonly Guid ClientId =
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private const string KeyPrefix = "if_dev_";

    private const string KeyHash =
        "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

    [Fact]
    public void UseApiKeyClientIdentity_ShouldReturnSameBuilder_ForMethodChaining()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow();

        var returnedBuilder = builder.UseApiKeyClientIdentity(options =>
        {
            options.AddClient(
                clientId: ClientId,
                keyHash: KeyHash,
                keyPrefix: KeyPrefix);
        });

        Assert.Same(builder, returnedBuilder);
        Assert.Same(services, returnedBuilder.Services);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldRegisterConfiguredClientApiKeyValidator()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            });

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IClientApiKeyValidator)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(ConfiguredClientApiKeyValidator),
            descriptor.ImplementationType);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldRegisterHttpProcessingClientContext()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            });

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IProcessingClientContext)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(HttpProcessingClientContext),
            descriptor.ImplementationType);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldOverrideDefaultProcessingClientContext_WhenRegisteredAfterInMemoryInfrastructure()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseInMemoryInfrastructure()
            .UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            });

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IProcessingClientContext)));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.Equal(
            typeof(HttpProcessingClientContext),
            descriptor.ImplementationType);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldRegisterOptionsValidator()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            });

        var descriptor = Assert.Single(services.Where(service =>
            service.ServiceType == typeof(IValidateOptions<ClientApiKeyIdentityOptions>)));

        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
        Assert.Equal(
            typeof(ClientApiKeyIdentityOptionsValidator),
            descriptor.ImplementationType);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldConfigureClientApiKeyIdentityOptions()
    {
        var services = new ServiceCollection();

        services
            .AddInvoiceFlow()
            .UseInMemoryInfrastructure()
            .UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            });

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        var options = provider
            .GetRequiredService<IOptions<ClientApiKeyIdentityOptions>>()
            .Value;

        var client = Assert.Single(options.Clients);

        Assert.Equal(ClientId, client.ClientId);
        Assert.Equal(KeyPrefix, client.KeyPrefix);
        Assert.Equal(KeyHash, client.KeyHash);
        Assert.True(client.IsActive);
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldThrow_WhenBuilderIsNull()
    {
        IInvoiceFlowBuilder builder = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseApiKeyClientIdentity(options =>
            {
                options.AddClient(
                    clientId: ClientId,
                    keyHash: KeyHash,
                    keyPrefix: KeyPrefix);
            }));
    }

    [Fact]
    public void UseApiKeyClientIdentity_ShouldThrow_WhenConfigureOptionsIsNull()
    {
        var services = new ServiceCollection();

        var builder = services.AddInvoiceFlow();

        Action<ClientApiKeyIdentityOptions> configureOptions = null!;

        Assert.Throws<ArgumentNullException>(() =>
            builder.UseApiKeyClientIdentity(configureOptions));
    }
}
