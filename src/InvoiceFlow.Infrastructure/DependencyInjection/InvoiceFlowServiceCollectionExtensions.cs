using System.Globalization;
using Azure;
using Azure.AI.DocumentIntelligence;
using InvoiceFlow.Application.Documents;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Domain.Invoices;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class InvoiceFlowServiceCollectionExtensions
{
    public static IInvoiceFlowBuilder AddInvoiceFlow(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInvoiceFlowCore();

        return new InvoiceFlowBuilder(services);
    }

    public static IInvoiceFlowBuilder AddInvoiceFlow(
        this IServiceCollection services,
        DateOnly validationDate)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddInvoiceFlowCore(validationDate);

        return new InvoiceFlowBuilder(services);
    }

    public static IInvoiceFlowBuilder UseInMemoryInfrastructure(
        this IInvoiceFlowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddInvoiceFlowInMemory();

        return builder;
    }

    public static IInvoiceFlowBuilder UseDocumentExtractor<TDocumentExtractor>(
        this IInvoiceFlowBuilder builder)
        where TDocumentExtractor : class, IDocumentExtractor
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.RemoveAll<IDocumentExtractor>();
        builder.Services.AddSingleton<IDocumentExtractor, TDocumentExtractor>();

        return builder;
    }

    public static IInvoiceFlowBuilder UseAzureDocumentIntelligence(
        this IInvoiceFlowBuilder builder,
        Action<AzureDocumentIntelligenceOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services
            .AddOptions<AzureDocumentIntelligenceOptions>()
            .Configure(configureOptions)
            .ValidateAzureDocumentIntelligenceOptions();

        builder.Services.RemoveAll<DocumentIntelligenceClient>();
        builder.Services.AddSingleton(CreateDocumentIntelligenceClient);

        builder.Services.RemoveAll<IAzureDocumentIntelligenceClient>();
        builder.Services.AddSingleton<
            IAzureDocumentIntelligenceClient,
            AzureDocumentIntelligenceSdkClient>();

        builder.Services.RemoveAll<IDocumentExtractor>();
        builder.Services.AddSingleton<IDocumentExtractor>(CreateAzureDocumentExtractor);

        return builder;
    }

    public static IInvoiceFlowBuilder UseAzureDocumentIntelligenceIfConfigured(
        this IInvoiceFlowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services
            .AddOptions<AzureDocumentIntelligenceOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                var section = configuration.GetSection(
                    AzureDocumentIntelligenceOptions.ConfigurationSectionName);

                if (section.Exists())
                {
                    ConfigureAzureDocumentIntelligenceOptions(
                        options,
                        section);
                }
            })
            .Validate<IConfiguration>(
                (options, configuration) =>
                    !IsAzureDocumentIntelligenceConfigured(configuration)
                    || !string.IsNullOrWhiteSpace(options.Endpoint),
                "Azure Document Intelligence endpoint is required.")
            .Validate<IConfiguration>(
                (options, configuration) =>
                    !IsAzureDocumentIntelligenceConfigured(configuration)
                    || Uri.TryCreate(
                        options.Endpoint,
                        UriKind.Absolute,
                        out _),
                "Azure Document Intelligence endpoint must be an absolute URI.")
            .Validate<IConfiguration>(
                (options, configuration) =>
                    !IsAzureDocumentIntelligenceConfigured(configuration)
                    || !string.IsNullOrWhiteSpace(options.ApiKey),
                "Azure Document Intelligence API key is required.")
            .Validate<IConfiguration>(
                (options, configuration) =>
                    !IsAzureDocumentIntelligenceConfigured(configuration)
                    || !string.IsNullOrWhiteSpace(options.ModelId),
                "Azure Document Intelligence model id is required.")
            .Validate<IConfiguration>(
                (options, configuration) =>
                    !IsAzureDocumentIntelligenceConfigured(configuration)
                    || options.MinimumConfidenceThreshold is >= 0f and <= 1f,
                "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.");

        builder.Services.TryAddSingleton<FakeDocumentExtractor>();

        builder.Services.RemoveAll<DocumentIntelligenceClient>();
        builder.Services.AddSingleton(serviceProvider =>
        {
            var options = serviceProvider
                .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
                .Value;

            return new DocumentIntelligenceClient(
                new Uri(options.Endpoint),
                new AzureKeyCredential(options.ApiKey));
        });

        builder.Services.RemoveAll<IAzureDocumentIntelligenceClient>();
        builder.Services.AddSingleton<
            IAzureDocumentIntelligenceClient,
            AzureDocumentIntelligenceSdkClient>();

        builder.Services.RemoveAll<IDocumentExtractor>();
        builder.Services.AddSingleton<IDocumentExtractor>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();

            if (!IsAzureDocumentIntelligenceConfigured(configuration))
            {
                return serviceProvider.GetRequiredService<FakeDocumentExtractor>();
            }

            return new AzureDocumentIntelligenceDocumentExtractor(
                serviceProvider.GetRequiredService<
                    IOptions<AzureDocumentIntelligenceOptions>>(),
                serviceProvider.GetRequiredService<
                    IAzureDocumentIntelligenceClient>());
        });

        return builder;
    }

    private static void ConfigureAzureDocumentIntelligenceOptions(
        AzureDocumentIntelligenceOptions options,
        IConfigurationSection section)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(section);

        var endpoint = section[nameof(AzureDocumentIntelligenceOptions.Endpoint)];

        if (!string.IsNullOrWhiteSpace(endpoint))
        {
            options.Endpoint = endpoint.Trim();
        }

        var apiKey = section[nameof(AzureDocumentIntelligenceOptions.ApiKey)];

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            options.ApiKey = apiKey.Trim();
        }

        var modelId = section[nameof(AzureDocumentIntelligenceOptions.ModelId)];

        if (!string.IsNullOrWhiteSpace(modelId))
        {
            options.ModelId = modelId.Trim();
        }

        var minimumConfidenceThreshold =
            section[nameof(AzureDocumentIntelligenceOptions.MinimumConfidenceThreshold)];

        if (string.IsNullOrWhiteSpace(minimumConfidenceThreshold))
        {
            return;
        }

        if (float.TryParse(
                minimumConfidenceThreshold,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out var parsedThreshold))
        {
            options.MinimumConfidenceThreshold = parsedThreshold;
            return;
        }

        options.MinimumConfidenceThreshold = float.NaN;
    }

    private static bool IsAzureDocumentIntelligenceConfigured(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return configuration
            .GetSection(AzureDocumentIntelligenceOptions.ConfigurationSectionName)
            .Exists();
    }

    public static IInvoiceFlowBuilder UseAzureBlobDocumentStorage(
        this IInvoiceFlowBuilder builder,
        Action<AzureBlobDocumentStorageOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services
            .AddOptions<AzureBlobDocumentStorageOptions>()
            .Configure(configureOptions)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "Azure Blob Storage connection string is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ContainerName),
                "Azure Blob Storage container name is required.");

        builder.Services.RemoveAll<IDocumentStorage>();
        builder.Services.AddSingleton<IDocumentStorage, AzureBlobDocumentStorage>();

        return builder;
    }

    public static IInvoiceFlowBuilder UseAzureBlobDocumentStorageIfConfigured(
        this IInvoiceFlowBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var configuration = builder.Services
            .LastOrDefault(service =>
                service.ServiceType == typeof(Microsoft.Extensions.Configuration.IConfiguration))
            ?.ImplementationInstance as Microsoft.Extensions.Configuration.IConfiguration;

        if (configuration is null)
        {
            return builder;
        }

        return builder.UseAzureBlobDocumentStorageIfConfigured(configuration);
    }

    public static IInvoiceFlowBuilder UseAzureBlobDocumentStorageIfConfigured(
        this IInvoiceFlowBuilder builder,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(
            AzureBlobDocumentStorageOptions.ConfigurationSectionName);

        if (!section.Exists())
        {
            return builder;
        }

        return builder.UseAzureBlobDocumentStorage(options =>
        {
            options.ConnectionString = section["ConnectionString"] ?? string.Empty;
            options.ContainerName = section["ContainerName"] ?? string.Empty;
        });
    }

    public static IInvoiceFlowBuilder UseSqlServerInvoiceRepository(
        this IInvoiceFlowBuilder builder,
        Action<SqlServerInvoiceRepositoryOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureOptions);

        builder.Services
            .AddOptions<SqlServerInvoiceRepositoryOptions>()
            .Configure(configureOptions)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "SQL Server invoice repository connection string is required.");

        builder.Services.RemoveAll<IInvoiceRepository>();
        builder.Services.AddScoped<IInvoiceRepository, SqlServerInvoiceRepository>();

        return builder;
    }

    public static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddInvoiceFlowCore(
            () => DateOnly.FromDateTime(DateTime.UtcNow));
    }

    public static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services,
        DateOnly validationDate)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddInvoiceFlowCore(() => validationDate);
    }

    public static IServiceCollection AddInvoiceFlowInMemory(
        this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IDocumentStorage, InMemoryDocumentStorage>();
        services.TryAddSingleton<IDocumentExtractor, FakeDocumentExtractor>();
        services.TryAddSingleton<IInvoiceMapper, FieldBasedInvoiceMapper>();
        services.TryAddSingleton<IInvoiceRepository, InMemoryInvoiceRepository>();

        return services;
    }

    private static OptionsBuilder<AzureDocumentIntelligenceOptions>
        ValidateAzureDocumentIntelligenceOptions(
            this OptionsBuilder<AzureDocumentIntelligenceOptions> builder)
    {
        return builder
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.Endpoint),
                "Azure Document Intelligence endpoint is required.")
            .Validate(
                options => Uri.TryCreate(
                    options.Endpoint,
                    UriKind.Absolute,
                    out _),
                "Azure Document Intelligence endpoint must be an absolute URI.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ApiKey),
                "Azure Document Intelligence API key is required.")
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ModelId),
                "Azure Document Intelligence model id is required.")
            .Validate(
                options => options.MinimumConfidenceThreshold is >= 0f and <= 1f,
                "Azure Document Intelligence minimum confidence threshold must be between 0 and 1.");
    }

    private static DocumentIntelligenceClient CreateDocumentIntelligenceClient(
        IServiceProvider serviceProvider)
    {
        var options = serviceProvider
            .GetRequiredService<IOptions<AzureDocumentIntelligenceOptions>>()
            .Value;

        return new DocumentIntelligenceClient(
            new Uri(options.Endpoint),
            new AzureKeyCredential(options.ApiKey));
    }

    private static IDocumentExtractor CreateAzureDocumentExtractor(
        IServiceProvider serviceProvider)
    {
        return CreateConcreteAzureDocumentExtractor(serviceProvider);
    }

    private static AzureDocumentIntelligenceDocumentExtractor
        CreateConcreteAzureDocumentExtractor(
            IServiceProvider serviceProvider)
    {
        return new AzureDocumentIntelligenceDocumentExtractor(
            serviceProvider.GetRequiredService<
                IOptions<AzureDocumentIntelligenceOptions>>(),
            serviceProvider.GetRequiredService<
                IAzureDocumentIntelligenceClient>());
    }

    private static IServiceCollection AddInvoiceFlowCore(
        this IServiceCollection services,
        Func<DateOnly> validationDateProvider)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(validationDateProvider);

        services.TryAddScoped<IInvoiceDocumentProcessor, ProcessInvoiceDocumentService>();

        services.TryAddScoped<IInvoiceValidator>(_ =>
            new DefaultInvoiceValidator(validationDateProvider()));

        return services;
    }
}
