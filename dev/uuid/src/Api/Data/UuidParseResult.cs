namespace uuid;

/// <summary>Reports the outcome of parsing UUID text.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>. Canonical, compact, and <c>urn:uuid:</c> forms are accepted and normalized to lowercase canonical text.</remarks>
public class UuidParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed UUID, or <c>null</c> when parsing failed.</summary>
    public UuidValue? Value;

    /// <summary>Contains canonical lowercase UUID text, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal UuidParseResult(bool ok, UuidValue? value, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static UuidParseResult Success(UuidValue value)
    {
        return new UuidParseResult(true, value, value.ToCanonicalString(), "", IdentifierErrorCode.None);
    }

    internal static UuidParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new UuidParseResult(false, null, "", error, code);
    }
}
