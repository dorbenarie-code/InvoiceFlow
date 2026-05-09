namespace InvoiceFlow.Tests.DeveloperExperience;

public sealed class ManualRateLimitVerificationScriptTests
{
    private const string ScriptRelativePath =
        "scripts/manual/verify-rate-limit.sh";

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldExist()
    {
        var path = GetScriptPath();

        Assert.True(
            File.Exists(path),
            $"Manual rate limit verification script was not found at {ScriptRelativePath}.");
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldUseBashStrictMode()
    {
        var script = ReadScript();

        Assert.StartsWith(
            "#!/usr/bin/env bash",
            script);

        Assert.Contains(
            "set -euo pipefail",
            script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldRequireExpectedEnvironmentVariables()
    {
        var script = ReadScript();

        Assert.Contains("INVOICEFLOW_BASE_URL", script);
        Assert.Contains("INVOICEFLOW_API_KEY", script);
        Assert.Contains("INVOICEFLOW_INVOICE_FILE", script);

        Assert.Contains("${INVOICEFLOW_BASE_URL:?", script);
        Assert.Contains("${INVOICEFLOW_API_KEY:?", script);
        Assert.Contains("${INVOICEFLOW_INVOICE_FILE:?", script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldTargetInvoiceProcessingEndpoint()
    {
        var script = ReadScript();

        Assert.Contains(
            "/api/invoices/process",
            script);

        Assert.Contains(
            "X-API-Key",
            script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldVerifyRateLimitContract()
    {
        var script = ReadScript();

        Assert.Contains("429", script);
        Assert.Contains("RATE_LIMIT_EXCEEDED", script);
        Assert.Contains("Too Many Requests", script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldWriteEvidenceArtifacts()
    {
        var script = ReadScript();

        Assert.Contains("docs/evidence/rate-limiting", script);
        Assert.Contains("README.md", script);
        Assert.Contains("summary.json", script);
        Assert.Contains("request-01-response", script);
        Assert.Contains("request-02-response", script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldUseCurlForManualApiVerification()
    {
        var script = ReadScript();

        Assert.Contains("curl", script);
        Assert.Contains("-F", script);
        Assert.Contains("file=@", script);
    }

    [Fact]
    public void ManualRateLimitVerificationScript_ShouldNotContainHardcodedSecrets()
    {
        var script = ReadScript();

        Assert.DoesNotContain("if_dev_valid-secret-key", script);
        Assert.DoesNotContain("if_dev_invalid-secret-key", script);
        Assert.DoesNotContain("AccountKey=", script);
        Assert.DoesNotContain("SharedAccessSignature", script);
        Assert.DoesNotContain("Password=", script);
    }

    private static string ReadScript()
    {
        var path = GetScriptPath();

        return File.ReadAllText(path);
    }

    private static string GetScriptPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                ScriptRelativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(
            GetRepositoryRootFallback(),
            ScriptRelativePath);
    }

    private static string GetRepositoryRootFallback()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return Directory.GetCurrentDirectory();
    }
}
