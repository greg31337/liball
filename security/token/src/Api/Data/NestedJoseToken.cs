using System;
namespace token;

/// <summary>Represents one parsed inner JOSE layer selected by an authenticated outer content type.</summary>
public class NestedJoseToken
{
    /// <summary>Contains the authenticated outer <c>cty</c> value.</summary>
    public string? ContentType;

    /// <summary>Contains the canonical registered media type for <see cref="ContentType"/>.</summary>
    public string? NormalizedContentType;

    /// <summary>Identifies the parsed inner JOSE representation.</summary>
    public TokenKind Kind;

    /// <summary>Contains an inner compact JWS, or <c>null</c>.</summary>
    public JwsCompactToken? CompactJws;

    /// <summary>Contains an inner compact JWE, or <c>null</c>.</summary>
    public JweCompactToken? CompactJwe;

    /// <summary>Contains an inner flattened JWS JSON serialization, or <c>null</c>.</summary>
    public JwsFlattenedToken? FlattenedJws;

    /// <summary>Contains an inner general JWS JSON serialization, or <c>null</c>.</summary>
    public JwsGeneralToken? GeneralJws;

    /// <summary>Contains an inner flattened JWE JSON serialization, or <c>null</c>.</summary>
    public JweFlattenedToken? FlattenedJwe;

    /// <summary>Contains an inner general JWE JSON serialization, or <c>null</c>.</summary>
    public JweGeneralToken? GeneralJwe;

    /// <summary>Initializes an empty nested JOSE model.</summary>
    public NestedJoseToken()
    {
        ContentType = null;
        NormalizedContentType = null;
        Kind = TokenKind.Unknown;
        CompactJws = null;
        CompactJwe = null;
        FlattenedJws = null;
        GeneralJws = null;
        FlattenedJwe = null;
        GeneralJwe = null;
    }
}
