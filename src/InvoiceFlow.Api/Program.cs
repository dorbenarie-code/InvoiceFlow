using InvoiceFlow.Application.Documents;
using InvoiceFlow.Infrastructure.Documents;
using InvoiceFlow.Api.Health;
using InvoiceFlow.Api.Invoices;
using InvoiceFlow.Application.Invoices;
using InvoiceFlow.Infrastructure.Invoices;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "InvoiceFlow API",
            Version = "v1",
            Description =
                "InvoiceFlow is a developer-facing invoice processing API. " +
                "It receives invoice or receipt documents, extracts invoice data, " +
                "runs deterministic business validation, and returns either a verified invoice " +
                "or a structured result that requires human review."
        });
});

builder.Services
    .AddOptions<InvoiceDocumentUploadOptions>()
    .Bind(builder.Configuration.GetSection(
        InvoiceDocumentUploadOptions.ConfigurationSectionName))
    .Validate(
        options => options.MaxFileSizeInBytes > 0,
        "Maximum invoice document file size must be greater than zero.")
    .ValidateOnStart();

builder.Services
    .AddInvoiceFlow()
    .UseInMemoryInfrastructure()
    .UseAzureDocumentIntelligenceIfConfigured();

builder.Services
    .AddOptions<AzureBlobDocumentStorageOptions>()
    .Bind(builder.Configuration.GetSection(
        AzureBlobDocumentStorageOptions.ConfigurationSectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "Azure Blob Storage connection string is required.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ContainerName),
        "Azure Blob Storage container name is required.");

builder.Services.RemoveAll<IDocumentStorage>();
builder.Services.TryAddSingleton<InMemoryDocumentStorage>();
builder.Services.AddScoped<AzureBlobDocumentStorage>();

builder.Services.AddScoped<IDocumentStorage>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var azureBlobStorageSection = configuration.GetSection(
        AzureBlobDocumentStorageOptions.ConfigurationSectionName);

    if (!azureBlobStorageSection.Exists())
    {
        return serviceProvider.GetRequiredService<InMemoryDocumentStorage>();
    }

    return serviceProvider.GetRequiredService<AzureBlobDocumentStorage>();
});

builder.Services
    .AddOptions<SqlServerInvoiceRepositoryOptions>()
    .Bind(builder.Configuration.GetSection(
        SqlServerInvoiceRepositoryOptions.ConfigurationSectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.ConnectionString),
        "SQL Server invoice repository connection string is required.");

builder.Services.RemoveAll<IInvoiceRepository>();
builder.Services.TryAddSingleton<InMemoryInvoiceRepository>();
builder.Services.AddScoped<SqlServerInvoiceRepository>();

builder.Services.AddScoped<IInvoiceRepository>(serviceProvider =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();

    var sqlServerSection = configuration.GetSection(
        SqlServerInvoiceRepositoryOptions.ConfigurationSectionName);

    if (!sqlServerSection.Exists())
    {
        return serviceProvider.GetRequiredService<InMemoryInvoiceRepository>();
    }

    return serviceProvider.GetRequiredService<SqlServerInvoiceRepository>();
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "InvoiceFlow API v1");

        options.DocumentTitle = "InvoiceFlow API";
    });

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

if (!app.Environment.IsDevelopment()
    && !app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}

app.MapHealthEndpoints();
app.MapInvoiceEndpoints();

app.Run();

public partial class Program
{
}
