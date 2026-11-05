using System;
namespace token;

/// <summary>Contains text or binary output from universal token creation.</summary>
public class UniversalTokenCreationResult
{
    /// <summary>Indicates whether creation succeeded.</summary>
    public bool Success;

    /// <summary>Contains failure details, or <c>null</c> on success.</summary>
    public TokenError? Error;

    /// <summary>Identifies the created representation.</summary>
    public TokenKind Kind;

    /// <summary>Contains created text for JOSE representations, or <c>null</c>.</summary>
    public string? Text;

    /// <summary>Contains created bytes for COSE and CWT representations, or <c>null</c>.</summary>
    public byte[]? Bytes;

    /// <summary>Contains separately transported content for detached output, or <c>null</c>.</summary>
    public byte[]? DetachedContent;

    /// <summary>Initializes an unsuccessful result with no output.</summary>
    public UniversalTokenCreationResult()
    {
        Success = false;
        Error = null;
        Kind = TokenKind.Unknown;
        Text = null;
        Bytes = null;
        DetachedContent = null;
    }
}
