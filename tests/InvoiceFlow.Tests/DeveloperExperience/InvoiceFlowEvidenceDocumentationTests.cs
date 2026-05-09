namespace InvoiceFlow.Tests.DeveloperExperience;

public sealed class InvoiceFlowEvidenceDocumentationTests
{
    private const string FullPipelineEvidenceReadmeRelativePath =
        "docs/evidence/live-smoke-tests/2026-05-07-full-pipeline-50/README.md";

    [Fact]
    public void FullPipelineEvidenceReadme_ShouldExist()
    {
        var path = GetRepositoryFilePath(
            FullPipelineEvidenceReadmeRelativePath);

        Assert.True(
            File.Exists(path),
            $"Evidence README was not found at {FullPipelineEvidenceReadmeRelativePath}.");
    }

    [Fact]
    public void FullPipelineEvidenceReadme_ShouldNotContainAccidentalSingleCharacterLines()
    {
        var readme = ReadRepositoryFile(
            FullPipelineEvidenceReadmeRelativePath);

        var lines = readme.Split(
            Environment.NewLine,
            StringSplitOptions.None);

        Assert.DoesNotContain(
            lines,
            line => line.Trim() == "ש");
    }

    [Fact]
    public void FullPipelineEvidenceReadme_ShouldHaveBalancedMarkdownCodeFences()
    {
        var readme = ReadRepositoryFile(
            FullPipelineEvidenceReadmeRelativePath);

        var fenceCount = CountOccurrences(
            readme,
            "```");

        Assert.True(
            fenceCount % 2 == 0,
            $"Markdown code fences should be balanced. Found {fenceCount} fence markers.");
    }

    [Fact]
    public void FullPipelineEvidenceReadme_ShouldClosePipelineCodeBlockAfterPipelineFlow()
    {
        var readme = ReadRepositoryFile(
            FullPipelineEvidenceReadmeRelativePath);

        var pipelineHeadingIndex = readme.IndexOf(
            "## Pipeline Covered",
            StringComparison.Ordinal);

        Assert.True(
            pipelineHeadingIndex >= 0,
            "Pipeline Covered section was not found.");

        var openingFenceIndex = readme.IndexOf(
            "```text",
            pipelineHeadingIndex,
            StringComparison.Ordinal);

        Assert.True(
            openingFenceIndex >= 0,
            "Pipeline Covered section should open a text code block.");

        var lastPipelineStepIndex = readme.IndexOf(
            "→ Stable API Response",
            openingFenceIndex,
            StringComparison.Ordinal);

        Assert.True(
            lastPipelineStepIndex >= 0,
            "Pipeline Covered section should include the final stable API response step.");

        var closingFenceIndex = readme.IndexOf(
            "```",
            lastPipelineStepIndex,
            StringComparison.Ordinal);

        Assert.True(
            closingFenceIndex > lastPipelineStepIndex,
            "Pipeline Covered section should close its text code block after the final pipeline step.");
    }

    [Fact]
    public void FullPipelineEvidenceReadme_ShouldDocumentEvidenceScope()
    {
        var readme = ReadRepositoryFile(
            FullPipelineEvidenceReadmeRelativePath);

        Assert.Contains(
            "This was not a load test, stress test, or production readiness test.",
            readme);

        Assert.Contains(
            "50-document live full-pipeline smoke test",
            readme);
    }

    private static string ReadRepositoryFile(
        string relativePath)
    {
        return File.ReadAllText(
            GetRepositoryFilePath(relativePath));
    }

    private static string GetRepositoryFilePath(
        string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                relativePath);

            if (File.Exists(candidate)
                && Directory.Exists(Path.Combine(directory.FullName, "src"))
                && Directory.Exists(Path.Combine(directory.FullName, "tests")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        return Path.Combine(
            Directory.GetCurrentDirectory(),
            relativePath);
    }

    private static int CountOccurrences(
        string value,
        string pattern)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(
                   pattern,
                   index,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += pattern.Length;
        }

        return count;
    }
}
