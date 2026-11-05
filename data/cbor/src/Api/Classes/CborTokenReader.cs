namespace cbor;

/// <summary>
/// Provides incremental low-level CBOR token reading.
/// </summary>
public class CborTokenReader : ICborTokenReader
{
    internal CborDecodeOptions Options;
    internal CborTokenizerCore Core;

    /// <summary>Creates a token reader with decoding policies and limits.</summary>
    /// <param name="options">The decoding policies and limits.</param>
    public CborTokenReader(CborDecodeOptions options)
    {
        Options = options;
        Core = new CborTokenizerCore(options, null);
    }

    /// <summary>Reads the next token from the supplied input fragment.</summary>
    /// <param name="source">The next encoded CBOR bytes.</param>
    /// <returns>The token read outcome.</returns>
    public CborTokenReadResult ReadToken(byte[] source)
    {
        return Core.ReadToken(source);
    }

    /// <summary>Skips one complete value from the supplied input.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <returns>The read outcome and consumed byte count.</returns>
    public CborReadResult SkipValue(byte[] source)
    {
        return Core.SkipValue(source);
    }

    /// <summary>Resets the reader to its initial state.</summary>
    public void ResetReader()
    {
        Core.Reset();
    }
}
