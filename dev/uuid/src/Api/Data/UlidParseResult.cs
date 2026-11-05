namespace uuid;

/// <summary>Reports the outcome of parsing ULID text.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>. Lowercase input is accepted and normalized to uppercase.</remarks>
public class UlidParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed ULID, or <c>null</c> when parsing failed.</summary>
    public UlidValue? Value;

    /// <summary>Contains canonical uppercase ULID text, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal UlidParseResult(bool ok, UlidValue? value, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static UlidParseResult Success(UlidValue value, string normalizedText)
    {
        return new UlidParseResult(true, value, normalizedText, "", IdentifierErrorCode.None);
    }

    internal static UlidParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new UlidParseResult(false, null, "", error, code);
    }
}
