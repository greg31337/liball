using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Encodes generic values as CBOR data.
/// </summary>
public interface ICborEncoder
{
    /// <summary>Encodes a value into a new byte array.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The encoded CBOR bytes.</returns>
    byte[] Encode(CborValue value, CborEncodeOptions options);

    /// <summary>Encodes a value into a caller-provided destination.</summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="destination">The destination byte array.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The write outcome and size information.</returns>
    CborWriteResult EncodeInto(CborValue value, byte[] destination, CborEncodeOptions options);

    /// <summary>Measures the encoded size of a value.</summary>
    /// <param name="value">The value to measure.</param>
    /// <param name="options">The encoding policies.</param>
    /// <returns>The required number of bytes.</returns>
    int Measure(CborValue value, CborEncodeOptions options);

    /// <summary>Encodes zero or more values as an RFC 8742 CBOR Sequence.</summary>
    /// <param name="values">The values to concatenate in sequence order.</param>
    /// <param name="options">The encoding policies and sequence limits.</param>
    /// <returns>The concatenated encoded CBOR items.</returns>
    byte[] EncodeSequence(List<CborValue> values, CborEncodeOptions options);
}
