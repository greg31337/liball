namespace cbor;

/// <summary>
/// Reads and skips low-level CBOR tokens.
/// </summary>
public interface ICborTokenReader
{
    /// <summary>Reads the next token from the supplied input fragment.</summary>
    /// <param name="source">The next encoded CBOR bytes.</param>
    /// <returns>The token read outcome.</returns>
    CborTokenReadResult ReadToken(byte[] source);

    /// <summary>Skips one complete value from the supplied input.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <returns>The read outcome and consumed byte count.</returns>
    CborReadResult SkipValue(byte[] source);

    /// <summary>Resets the reader to its initial state.</summary>
    void ResetReader();
}
