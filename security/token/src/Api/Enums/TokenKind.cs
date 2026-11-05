using System;
namespace token;

/// <summary>
/// Identifies a token representation recognized by the library.
/// </summary>
public enum TokenKind
{
    /// <summary>
    /// The representation is not recognized.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// A compact JWS or JWT representation.
    /// </summary>
    JwsCompact = 1,

    /// <summary>
    /// A compact JWE representation.
    /// </summary>
    JweCompact = 2,

    /// <summary>
    /// A JWS JSON representation.
    /// </summary>
    JwsJson = 3,

    /// <summary>
    /// A JWE JSON representation.
    /// </summary>
    JweJson = 4,

    /// <summary>
    /// A COSE binary representation.
    /// </summary>
    Cose = 5,

    /// <summary>
    /// A CWT binary representation.
    /// </summary>
    Cwt = 6,

    /// <summary>
    /// A Selective Disclosure JWT representation.
    /// </summary>
    SdJwt = 7
}
