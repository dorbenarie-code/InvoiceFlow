using System.Text.Json;

namespace InvoiceFlow.Tests.DeveloperExperience;

public sealed class InvoiceFlowPostmanCollectionTests
{
    private const string CollectionRelativePath =
        "docs/postman/InvoiceFlow.postman_collection.json";

    private const string ExpectedSchema =
        "https://schema.getpostman.com/json/collection/v2.1.0/collection.json";

    [Fact]
    public void PostmanCollection_ShouldExist()
    {
        var path = GetPostmanCollectionPath();

        Assert.True(
            File.Exists(path),
            $"Postman collection was not found at {CollectionRelativePath}.");
    }

    [Fact]
    public void PostmanCollection_ShouldBeValidJson()
    {
        using var collection = ReadPostmanCollection();

        Assert.Equal(
            JsonValueKind.Object,
            collection.RootElement.ValueKind);
    }

    [Fact]
    public void PostmanCollection_ShouldUseExpectedSchema()
    {
        using var collection = ReadPostmanCollection();

        var schema = collection
            .RootElement
            .GetProperty("info")
            .GetProperty("schema")
            .GetString();

        Assert.Equal(ExpectedSchema, schema);
    }

    [Fact]
    public void PostmanCollection_ShouldContainExpectedVariables()
    {
        using var collection = ReadPostmanCollection();

        var variableKeys = collection
            .RootElement
            .GetProperty("variable")
            .EnumerateArray()
            .Select(variable => variable.GetProperty("key").GetString())
            .ToArray();

        Assert.Contains("base_url", variableKeys);
        Assert.Contains("api_key", variableKeys);
        Assert.Contains("invoice_file_path", variableKeys);
    }

    [Fact]
    public void PostmanCollection_ShouldContainExpectedRequests()
    {
        using var collection = ReadPostmanCollection();

        var requestNames = collection
            .RootElement
            .GetProperty("item")
            .EnumerateArray()
            .Select(item => item.GetProperty("name").GetString())
            .ToArray();

        Assert.Contains("Health - public", requestNames);
        Assert.Contains("Process invoice - valid API key", requestNames);
        Assert.Contains("Process invoice - missing API key", requestNames);
        Assert.Contains("Process invoice - invalid content type", requestNames);
        Assert.Contains("Process invoice - rate limit exceeded", requestNames);
    }

    [Fact]
    public void PostmanProcessRequests_ShouldUseMultipartFileFieldNamedFile()
    {
        using var collection = ReadPostmanCollection();

        var root = collection.RootElement;

        AssertUsesMultipartFileFieldNamedFile(
            root,
            "Process invoice - valid API key");

        AssertUsesMultipartFileFieldNamedFile(
            root,
            "Process invoice - missing API key");

        AssertUsesMultipartFileFieldNamedFile(
            root,
            "Process invoice - rate limit exceeded");
    }

    [Fact]
    public void PostmanProcessFileRequests_ShouldUseInvoiceFilePathVariable()
    {
        using var collection = ReadPostmanCollection();

        var root = collection.RootElement;

        AssertUsesInvoiceFilePathVariable(
            root,
            "Process invoice - valid API key");

        AssertUsesInvoiceFilePathVariable(
            root,
            "Process invoice - missing API key");

        AssertUsesInvoiceFilePathVariable(
            root,
            "Process invoice - rate limit exceeded");
    }

    [Fact]
    public void PostmanProtectedRequests_ShouldUseApiKeyHeaderVariable()
    {
        using var collection = ReadPostmanCollection();

        var root = collection.RootElement;

        AssertUsesApiKeyHeaderVariable(
            root,
            "Process invoice - valid API key");

        AssertUsesApiKeyHeaderVariable(
            root,
            "Process invoice - invalid content type");

        AssertUsesApiKeyHeaderVariable(
            root,
            "Process invoice - rate limit exceeded");
    }

    [Fact]
    public void PostmanRateLimitRequest_ShouldDocumentExpectedRateLimitResponse()
    {
        using var collection = ReadPostmanCollection();

        var item = FindItem(
            collection.RootElement,
            "Process invoice - rate limit exceeded");

        var description = item
            .GetProperty("request")
            .GetProperty("description")
            .GetString();

        Assert.NotNull(description);
        Assert.Contains("429", description);
        Assert.Contains("RATE_LIMIT_EXCEEDED", description);
    }

    private static void AssertUsesMultipartFileFieldNamedFile(
        JsonElement root,
        string requestName)
    {
        var item = FindItem(root, requestName);
        var request = item.GetProperty("request");

        var body = request.GetProperty("body");

        Assert.Equal(
            "formdata",
            body.GetProperty("mode").GetString());

        var fileField = body
            .GetProperty("formdata")
            .EnumerateArray()
            .SingleOrDefault(formData =>
                formData.TryGetProperty("key", out var key)
                && key.GetString() == "file"
                && formData.TryGetProperty("type", out var type)
                && type.GetString() == "file");

        Assert.Equal(
            JsonValueKind.Object,
            fileField.ValueKind);
    }

    private static void AssertUsesInvoiceFilePathVariable(
        JsonElement root,
        string requestName)
    {
        var item = FindItem(root, requestName);
        var request = item.GetProperty("request");
        var body = request.GetProperty("body");

        var fileField = body
            .GetProperty("formdata")
            .EnumerateArray()
            .Single(formData =>
                formData.GetProperty("key").GetString() == "file"
                && formData.GetProperty("type").GetString() == "file");

        Assert.Equal(
            "{{invoice_file_path}}",
            fileField.GetProperty("src").GetString());
    }

    private static void AssertUsesApiKeyHeaderVariable(
        JsonElement root,
        string requestName)
    {
        var item = FindItem(root, requestName);
        var request = item.GetProperty("request");

        var apiKeyHeader = request
            .GetProperty("header")
            .EnumerateArray()
            .SingleOrDefault(header =>
                header.TryGetProperty("key", out var key)
                && key.GetString() == "X-API-Key");

        Assert.Equal(
            JsonValueKind.Object,
            apiKeyHeader.ValueKind);

        Assert.Equal(
            "{{api_key}}",
            apiKeyHeader.GetProperty("value").GetString());
    }

    private static JsonElement FindItem(
        JsonElement root,
        string requestName)
    {
        foreach (var item in root.GetProperty("item").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == requestName)
            {
                return item;
            }
        }

        throw new InvalidOperationException(
            $"Postman request '{requestName}' was not found.");
    }

    private static JsonDocument ReadPostmanCollection()
    {
        var path = GetPostmanCollectionPath();

        return JsonDocument.Parse(
            File.ReadAllText(path));
    }

    private static string GetPostmanCollectionPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Combine(
                directory.FullName,
                CollectionRelativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find {CollectionRelativePath} from {AppContext.BaseDirectory}.");
    }
}
