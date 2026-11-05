namespace sip;

internal static class SipUriParser
{
    internal static SipParseResult ParseUriBytes(byte[]? source, SipParseOptions? options)
    {
        if (source == null)
        {
            return CreateInputError("The URI source is required.");
        }

        if (options == null)
        {
            return CreateInputError("The URI parse options are required.");
        }

        if (options.MaxUriBytes < 0)
        {
            return CreateInputError("The maximum URI size cannot be negative.");
        }

        if (options.MaxParameterCount < 0 || options.MaxListValueCount < 0)
        {
            return CreateInputError("URI collection limits cannot be negative.");
        }

        if (source.Length > options.MaxUriBytes)
        {
            SipParseResult limited = CreateInputError("The URI exceeds the configured maximum size.");
            limited.ErrorOffset = options.MaxUriBytes;
            limited.RequiredBytes = source.Length;
            limited.ValidationIssues[0].ByteOffset = options.MaxUriBytes;
            return limited;
        }

        if (source.Length == 0)
        {
            return CreateInputError("The URI is empty.");
        }

        int index = 0;
        while (index < source.Length)
        {
            if (source[index] < 33 || source[index] > 126)
            {
                SipParseResult asciiError = CreateInputError("The URI contains whitespace, control, or unescaped non-ASCII bytes.");
                asciiError.ErrorOffset = index;
                asciiError.Consumed = index;
                asciiError.RequiredBytes = source.Length;
                return asciiError;
            }

            index = index + 1;
        }

        string text = System.Text.Encoding.ASCII.GetString(source);
        SipUriParseState state = new SipUriParseState(text, options);
        return state.ParseUriText();
    }

    static SipParseResult CreateInputError(string message)
    {
        SipParseResult result = new SipParseResult();
        result.Status = SipParseStatus.Error;
        result.ErrorOffset = 0;
        result.ErrorMessage = message;
        SipValidationIssue issue = new SipValidationIssue();
        issue.Severity = SipValidationSeverity.Error;
        issue.Code = "invalid-uri";
        issue.Message = message;
        issue.ByteOffset = result.ErrorOffset;
        issue.Path = "uri";
        result.ValidationIssues = new SipValidationIssue[] { issue };
        return result;
    }
}
