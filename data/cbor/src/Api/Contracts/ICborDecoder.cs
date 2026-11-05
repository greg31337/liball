using System.Collections.Generic;

namespace cbor;

/// <summary>
/// Decodes CBOR data into generic values.
/// </summary>
public interface ICborDecoder
{
    /// <summary>Decodes exactly one CBOR data item.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding policies and limits.</param>
    /// <returns>The read outcome and decoded value.</returns>
    CborReadResult Decode(byte[] source, CborDecodeOptions options);

    /// <summary>Decodes the first CBOR data item and reports its consumption.</summary>
    /// <param name="source">The encoded CBOR bytes.</param>
    /// <param name="options">The decoding policies and limits.</param>
    /// <returns>The read outcome, decoded value, and consumed byte count.</returns>
    CborReadResult DecodeFirst(byte[] source, CborDecodeOptions options);

    /// <summary>Decodes all concatenated items in an RFC 8742 CBOR Sequence.</summary>
    /// <param name="source">The encoded CBOR Sequence bytes.</param>
    /// <param name="options">The decoding policies and sequence limits.</param>
    /// <returns>One result per decoded item, or a final error result for an invalid item or limit.</returns>
    List<CborReadResult> DecodeSequence(byte[] source, CborDecodeOptions options);
}
