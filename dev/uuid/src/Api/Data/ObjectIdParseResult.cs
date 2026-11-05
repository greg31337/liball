namespace uuid;

/// <summary>Reports the outcome of parsing ObjectId text.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>. Invalid input is reported through <see cref="Error"/> rather than an exception.</remarks>
public class ObjectIdParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed ObjectId, or <c>null</c> when parsing failed.</summary>
    public ObjectIdValue? Value;

    /// <summary>Contains canonical lowercase hexadecimal text, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal ObjectIdParseResult(bool ok, ObjectIdValue? value, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static ObjectIdParseResult Success(ObjectIdValue value, string normalizedText)
    {
        return new ObjectIdParseResult(true, value, normalizedText, "", IdentifierErrorCode.None);
    }

    internal static ObjectIdParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new ObjectIdParseResult(false, null, "", error, code);
    }
}
