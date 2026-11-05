using System;
namespace token;

/// <summary>
/// Contains the outcome of reading one CBOR item.
/// </summary>
public class CborReadResult
{
    /// <summary>
    /// Indicates whether a CBOR item was read successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains the decoded item, or <c>null</c> when reading failed.
    /// </summary>
    public CborItem? Item;

    /// <summary>
    /// Initializes an unsuccessful CBOR read result with no details.
    /// </summary>
    public CborReadResult()
    {
        Success = false;
        Error = null;
        Item = null;
    }
}
