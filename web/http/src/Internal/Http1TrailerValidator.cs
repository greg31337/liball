using System.Text;

namespace http;

internal class Http1TrailerValidationResult
{
    internal HttpOperationStatus Status;
    internal string? ErrorCode;
    internal string? ErrorMessage;
}

internal static class Http1TrailerValidator
{
    internal static Http1TrailerValidationResult ValidateDeclarations(
        HttpHeaders headers,
        HttpLimits limits)
    {
        Http1TrailerValidationResult result = new Http1TrailerValidationResult();
        int remainingNames = limits.MaximumListMemberCount;
        int fieldIndex = 0;
        while (fieldIndex < headers.Fields.Count)
        {
            HttpHeaderField field = headers.Fields[fieldIndex];
            if (Ascii.EqualsStringsIgnoreCase(field.Name, HttpHeaderNames.Trailer))
            {
                FieldValueParseResult parsed = FieldValueParser.ParseTrailer(
                    field.Value,
                    limits.MaximumFieldValueSize,
                    remainingNames);
                if (parsed.IsLimitExceeded)
                {
                    return Reject(
                        result,
                        "http1-trailer-declaration-limit-exceeded",
                        "The Trailer field declares too many field names.");
                }

                if (parsed.Status != HttpOperationStatus.Done)
                {
                    return Reject(
                        result,
                        "invalid-http1-trailer-declaration",
                        "The Trailer field value is malformed.");
                }

                int nameIndex = 0;
                while (nameIndex < parsed.Values.Count)
                {
                    string name = Encoding.ASCII.GetString(parsed.Values[nameIndex]);
                    if (HeaderStore.IsForbiddenTrailerName(name))
                    {
                        return Reject(
                            result,
                            "forbidden-http1-trailer-declaration",
                            "The Trailer field names a field that is forbidden in trailers.");
                    }

                    nameIndex = nameIndex + 1;
                }

                remainingNames = remainingNames - parsed.Values.Count;
            }

            fieldIndex = fieldIndex + 1;
        }

        result.Status = HttpOperationStatus.Done;
        return result;
    }

    static Http1TrailerValidationResult Reject(
        Http1TrailerValidationResult result,
        string code,
        string message)
    {
        result.Status = HttpOperationStatus.Rejected;
        result.ErrorCode = code;
        result.ErrorMessage = message;
        return result;
    }
}
