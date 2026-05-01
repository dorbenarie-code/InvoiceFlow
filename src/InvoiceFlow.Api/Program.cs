using InvoiceFlow.Api.Health;
using InvoiceFlow.Api.Invoices;
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
    .UseInMemoryInfrastructure();

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
