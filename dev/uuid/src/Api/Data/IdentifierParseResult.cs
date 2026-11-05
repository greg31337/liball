using System;

namespace uuid;

/// <summary>Reports the value, format, and normalized text produced by a format-independent parse operation.</summary>
/// <remarks>Check <see cref="Ok"/> before using <see cref="Value"/>, <see cref="Kind"/>, or <see cref="NormalizedText"/>. Failed results describe the problem in <see cref="Code"/> and <see cref="Error"/> and use <see cref="IdentifierKind.Unknown"/>.</remarks>
public class IdentifierParseResult
{
    /// <summary>Indicates whether parsing succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the parsed format-independent value, or <c>null</c> when parsing failed.</summary>
    public IIdentifierValue? Value;

    /// <summary>Contains the recognized format, or <see cref="IdentifierKind.Unknown"/> when parsing failed.</summary>
    public IdentifierKind Kind;

    /// <summary>Contains canonical text for the identifier, or an empty string when parsing failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when parsing succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when parsing succeeded.</summary>
    public IdentifierErrorCode Code;

    internal IdentifierParseResult(bool ok, IIdentifierValue? value, IdentifierKind kind, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Value = value;
        Kind = kind;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static IdentifierParseResult Success(IdentifierKind kind, string normalizedText)
    {
        return Success(new TextIdentifierValue(kind, normalizedText), normalizedText);
    }

    internal static IdentifierParseResult Success(IIdentifierValue value, string normalizedText)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return new IdentifierParseResult(true, value, value.Kind(), normalizedText, "", IdentifierErrorCode.None);
    }

    internal static IdentifierParseResult Failure(IdentifierErrorCode code, string error)
    {
        return new IdentifierParseResult(false, null, IdentifierKind.Unknown, "", error, code);
    }
}
