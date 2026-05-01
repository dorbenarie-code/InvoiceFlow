using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace InvoiceFlow.Tests.Api;

public sealed class InvoiceDocumentUploadOptionsTests
{
    [Fact]
    public void ApiStartup_ShouldFail_WhenUploadMaxFileSizeIsInvalid()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");

                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["InvoiceFlow:Upload:MaxFileSizeInBytes"] = "0"
                        });
                });
            });

        var exception = Assert.Throws<OptionsValidationException>(() =>
            factory.CreateClient());

        Assert.Contains(
            "Maximum invoice document file size must be greater than zero.",
            exception.Message);
    }
}
