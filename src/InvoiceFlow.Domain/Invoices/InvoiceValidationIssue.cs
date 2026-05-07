namespace InvoiceFlow.Domain.Invoices;

public sealed record InvoiceValidationIssue
{
    private const int MaxCodeLength = 100;
    private const int MaxFieldNameLength = 100;
    private const int MaxMessageLength = 500;

    public string Code { get; }
    public string? FieldName { get; }
    public string Message { get; }
    public InvoiceValidationSeverity Severity { get; }

    private InvoiceValidationIssue(
        string code,
        string? fieldName,
        string message,
        InvoiceValidationSeverity severity)
    {
        Code = NormalizeCode(code);
        FieldName = NormalizeFieldName(fieldName);
        Message = NormalizeMessage(message);
        Severity = severity;
    }

    public static InvoiceValidationIssue Warning(
        string code,
        string? fieldName,
        string message)
    {
        return new InvoiceValidationIssue(
            code,
            fieldName,
            message,
            InvoiceValidationSeverity.Warning);
    }

    public static InvoiceValidationIssue Error(
        string code,
        string? fieldName,
        string message)
    {
        return new InvoiceValidationIssue(
            code,
            fieldName,
            message,
            InvoiceValidationSeverity.Error);
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Validation issue code is required.", nameof(code));
        }

        var normalizedCode = code.Trim().ToUpperInvariant();

        if (normalizedCode.Length > MaxCodeLength)
        {
            throw new ArgumentException(
                $"Validation issue code cannot be longer than {MaxCodeLength} characters.",
                nameof(code));
        }

        if (!normalizedCode.All(character =>
                char.IsAsciiLetterOrDigit(character) || character == '_'))
        {
            throw new ArgumentException(
                "Validation issue code must contain ASCII letters, digits, or underscores only.",
                nameof(code));
        }

        return normalizedCode;
    }

    private static string? NormalizeFieldName(string? fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return null;
        }

        var normalizedFieldName = fieldName.Trim();

        if (normalizedFieldName.Length > MaxFieldNameLength)
        {
            throw new ArgumentException(
                $"Validation issue field name cannot be longer than {MaxFieldNameLength} characters.",
                nameof(fieldName));
        }

        return normalizedFieldName;
    }

    private static string NormalizeMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Validation issue message is required.", nameof(message));
        }

        var normalizedMessage = message.Trim();

        if (normalizedMessage.Length > MaxMessageLength)
        {
            throw new ArgumentException(
                $"Validation issue message cannot be longer than {MaxMessageLength} characters.",
                nameof(message));
        }

        return normalizedMessage;
    }
}