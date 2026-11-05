namespace barcode;

internal static class ValidationIssueFactory
{
    internal static BarcodeValidationIssue Create(
        BarcodeValidationSeverity severity,
        string code,
        string message,
        string standard,
        string field,
        int offset,
        string? expected,
        string? observed)
    {
        ValidateSeverity(severity);
        ValidateCode(code);
        ValidateRequiredText(message, "Validation issue message");
        ValidateRequiredText(standard, "Validation issue standard or profile");
        ValidateRequiredText(field, "Validation issue field or application identifier");
        if (offset < -1)
        {
            throw new BarcodeValidationException(
                "Validation issue source offset must be negative one or greater.");
        }

        BarcodeValidationIssue issue = new BarcodeValidationIssue();
        issue.Severity = severity;
        issue.Code = code;
        issue.Message = message;
        issue.Standard = standard;
        issue.Field = field;
        issue.Offset = offset;
        issue.Expected = expected;
        issue.Observed = observed;
        return issue;
    }

    internal static BarcodeValidationIssue CopyWithOffset(
        BarcodeValidationIssue source,
        int offset)
    {
        if (source == null)
        {
            throw new BarcodeValidationException(
                "A validation issue is required for copying.");
        }

        return Create(
            source.Severity,
            source.Code == null ? "" : source.Code,
            source.Message == null ? "" : source.Message,
            source.Standard == null ? "" : source.Standard,
            source.Field == null ? "" : source.Field,
            offset,
            source.Expected,
            source.Observed);
    }

    static void ValidateSeverity(BarcodeValidationSeverity severity)
    {
        if (severity != BarcodeValidationSeverity.Information &&
            severity != BarcodeValidationSeverity.Warning &&
            severity != BarcodeValidationSeverity.Error)
        {
            throw new BarcodeValidationException(
                "Validation issue severity is invalid.");
        }
    }

    static void ValidateCode(string code)
    {
        ValidateRequiredText(code, "Validation issue code");
        int index = 0;
        while (index < code.Length)
        {
            char character = code[index];
            bool valid = character >= 'A' && character <= 'Z';
            if (character >= '0' && character <= '9')
            {
                valid = true;
            }

            if (character == '_')
            {
                valid = true;
            }

            if (!valid)
            {
                throw new BarcodeValidationException(
                    "Validation issue codes must use uppercase ASCII letters, digits, and underscores.");
            }

            index = index + 1;
        }

        if (code[0] == '_' || code[code.Length - 1] == '_')
        {
            throw new BarcodeValidationException(
                "Validation issue codes must not begin or end with an underscore.");
        }
    }

    static void ValidateRequiredText(string value, string name)
    {
        if (value == null || value.Length == 0)
        {
            throw new BarcodeValidationException($"{name} must not be empty.");
        }
    }
}
