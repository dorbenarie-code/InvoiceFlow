namespace InvoiceFlow.Tests.DeveloperExperience;

public sealed class InvoiceFlowReadmeDeveloperExperienceTests
{
    private const string ReadmeRelativePath = "README.md";

    [Fact]
    public void Readme_ShouldExist()
    {
        var path = GetReadmePath();

        Assert.True(
            File.Exists(path),
            $"README was not found at {ReadmeRelativePath}.");
    }

    [Fact]
    public void Readme_ShouldDocumentPostmanCollectionPath()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "docs/postman/InvoiceFlow.postman_collection.json",
            readme);
    }

    [Fact]
    public void Readme_ShouldDocumentPostmanRateLimitScenario()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "Process invoice - rate limit exceeded",
            readme);

        Assert.DoesNotContain(
            "The collection does not yet include a dedicated rate-limit scenario.",
            readme);
    }

    [Fact]
    public void Readme_ShouldDocumentManualRateLimitVerificationScript()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "scripts/manual/verify-rate-limit.sh",
            readme);

        Assert.Contains(
            "INVOICEFLOW_BASE_URL",
            readme);

        Assert.Contains(
            "INVOICEFLOW_API_KEY",
            readme);

        Assert.Contains(
            "INVOICEFLOW_INVOICE_FILE",
            readme);
    }

    [Fact]
    public void Readme_ShouldDocumentRateLimitErrorContract()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "429 Too Many Requests",
            readme);

        Assert.Contains(
            "RATE_LIMIT_EXCEEDED",
            readme);
    }

    [Fact]
    public void Readme_ShouldDocumentRateLimitEvidencePath()
    {
        var readme = ReadReadme();

        Assert.Contains(
            "docs/evidence/rate-limiting",
            readme);
    }

    [Fact]
    public void Readme_ShouldNotListPerClientRateLimitingAsNotImplemented()
    {
        var readme = ReadReadme();

        var notImplementedSection = ExtractSection(
            readme,
            "## Not Implemented Yet");

        Assert.DoesNotContain(
            "per-client rate limiting",
            notImplementedSection,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractSection(
        string markdown,
        string heading)
    {
        var startIndex = markdown.IndexOf(
            heading,
            StringComparison.Ordinal);

        if (startIndex < 0)
        {
            throw new InvalidOperationException(
                $"README section '{heading}' was not found.");
        }

        var nextHeadingIndex = markdown.IndexOf(
            "\n## ",
            startIndex + heading.Length,
            StringComparison.Ordinal);

        return nextHeadingIndex < 0
            ? markdown[startIndex..]
            : markdown[startIndex..nextHeadingIndex];
    }

    private static string ReadReadme()
    {
        return File.ReadAllText(GetReadmePath());
    }

    private static string GetReadmePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                ReadmeRelativePath);

            if (File.Exists(candidate)
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find repository README from {AppContext.BaseDirectory}.");
    }
}
