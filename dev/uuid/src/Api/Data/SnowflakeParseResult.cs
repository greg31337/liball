namespace uuid;

/// <summary>Reports the outcome of parsing a decimal Snowflake identifier.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>. The value is decoded with the settings owned by the generator that performed the parse.</remarks>
public class SnowflakeParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed Snowflake value, or <c>null</c> when parsing failed.</summary>
    public SnowflakeValue? Value;

    /// <summary>Contains normalized unsigned decimal text, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal SnowflakeParseResult(bool ok, SnowflakeValue? value, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static SnowflakeParseResult Success(SnowflakeValue value)
    {
        return new SnowflakeParseResult(true, value, value.ToDecimalString(), "", IdentifierErrorCode.None);
    }

    internal static SnowflakeParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new SnowflakeParseResult(false, null, "", error, code);
    }
}
