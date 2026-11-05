namespace cbor;

/// <summary>
/// Writes back-to-back CBOR values to a constructor-supplied stream.
/// </summary>
public interface ICborStreamWriter
{
    /// <summary>Writes the next complete CBOR value to the stream.</summary>
    /// <param name="value">The value to write.</param>
    /// <returns>The write outcome and size information.</returns>
    CborWriteResult WriteNext(CborValue value);

    /// <summary>Flushes buffered encoded output to the stream.</summary>
    void FlushOutput();
}
