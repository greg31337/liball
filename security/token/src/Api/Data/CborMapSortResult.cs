using System;
namespace token;

/// <summary>
/// Contains the outcome of sorting CBOR map entries deterministically.
/// </summary>
public class CborMapSortResult
{
    /// <summary>
    /// Indicates whether the entries were sorted successfully.
    /// </summary>
    public bool Success;

    /// <summary>
    /// Contains failure details, or <c>null</c> when no error is present.
    /// </summary>
    public TokenError? Error;

    /// <summary>
    /// Contains a new array of entries in deterministic key order, or <c>null</c> on failure.
    /// </summary>
    public CborMapEntry[]? Entries;

    /// <summary>
    /// Initializes an unsuccessful map sort result with no details.
    /// </summary>
    public CborMapSortResult()
    {
        Success = false;
        Error = null;
        Entries = null;
    }
}
