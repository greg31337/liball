namespace cbor;

/// <summary>
/// Reads back-to-back CBOR values from a constructor-supplied stream.
/// </summary>
public interface ICborStreamReader
{
    /// <summary>Reads the next complete CBOR value from the stream.</summary>
    /// <returns>The read outcome and decoded value.</returns>
    CborReadResult ReadNext();
}
