namespace uuid;

/// <summary>Reports whether identifier text is valid and, when possible, which format it uses.</summary>
/// <remarks>Successful results include canonical text in <see cref="NormalizedText"/>. Failed results place the stable code in <see cref="Code"/> and the reason in <see cref="Error"/> without throwing for ordinary invalid input.</remarks>
public class IdentifierValidationResult
{
    /// <summary>Indicates whether validation succeeded.</summary>
    public bool Ok;

    /// <summary>Contains the validated format, or <see cref="IdentifierKind.Unknown"/> when validation failed.</summary>
    public IdentifierKind Kind;

    /// <summary>Contains canonical identifier text, or an empty string when validation failed.</summary>
    public string NormalizedText;

    /// <summary>Contains a plain-English failure reason, or an empty string when validation succeeded.</summary>
    public string Error;

    /// <summary>Contains a stable failure code, or <see cref="IdentifierErrorCode.None"/> when validation succeeded.</summary>
    public IdentifierErrorCode Code;

    internal IdentifierValidationResult(bool ok, IdentifierKind kind, string normalizedText, string error, IdentifierErrorCode code)
    {
        Ok = ok;
        Kind = kind;
        NormalizedText = normalizedText;
        Error = error;
        Code = code;
    }

    internal static IdentifierValidationResult Success(IdentifierKind kind, string normalizedText)
    {
        return new IdentifierValidationResult(true, kind, normalizedText, "", IdentifierErrorCode.None);
    }

    internal static IdentifierValidationResult Failure(IdentifierErrorCode code, string error)
    {
        return new IdentifierValidationResult(false, IdentifierKind.Unknown, "", error, code);
    }
}
