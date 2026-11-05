namespace uuid;

/// <summary>Reports the outcome of parsing KSUID text.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>. Invalid input is reported through <see cref="Error"/> rather than an exception.</remarks>
public class KsuidParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed KSUID, or <c>null</c> when parsing failed.</summary>
    public KsuidValue? Value;

    /// <summary>Contains the canonical 27-character Base62 text, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal KsuidParseResult(bool ok, KsuidValue? value, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static KsuidParseResult Success(KsuidValue value, string normalizedText)
    {
        return new KsuidParseResult(true, value, normalizedText, "", IdentifierErrorCode.None);
    }

    internal static KsuidParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new KsuidParseResult(false, null, "", error, code);
    }
}
