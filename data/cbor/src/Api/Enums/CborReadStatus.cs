namespace cbor;

/// <summary>
/// Describes the outcome of a CBOR read operation.
/// </summary>
public enum CborReadStatus
{
    /// <summary>No read outcome is available.</summary>
    None = 0,
    /// <summary>A complete value or token was read.</summary>
    Done = 1,
    /// <summary>More input is required to finish the current value or token.</summary>
    NeedMoreData = 2,
    /// <summary>The input ended between complete CBOR items.</summary>
    EndOfData = 3,
    /// <summary>The input or reader state is invalid.</summary>
    Error = 4
}
