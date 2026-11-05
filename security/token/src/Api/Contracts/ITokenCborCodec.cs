using System;
namespace token;

/// <summary>
/// Provides strict CBOR reading, writing, and deterministic map ordering for token data.
/// </summary>
public interface ITokenCborCodec
{
    /// <summary>Reads exactly one duplicate-safe CBOR item.</summary>
    /// <param name="cbor">The encoded CBOR bytes.</param>
    /// <param name="maximumLength">The maximum accepted byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The decoded item, or stable failure details.</returns>
    CborReadResult ReadCbor(byte[] cbor, int maximumLength, int maximumDepth);

    /// <summary>Writes one CBOR item using definite lengths and preferred primitive encodings.</summary>
    /// <param name="item">The item to write.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The encoded bytes, or stable failure details.</returns>
    TokenBytesResult WriteCbor(CborItem item, int maximumLength, int maximumDepth);

    /// <summary>Writes one deterministic CBOR item with canonical map-key ordering.</summary>
    /// <param name="item">The item to write.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The deterministic encoded bytes, or stable failure details.</returns>
    TokenBytesResult WriteDeterministicCbor(CborItem item, int maximumLength, int maximumDepth);

    /// <summary>Returns map entries in deterministic encoded-key order without changing the input array.</summary>
    /// <param name="entries">The map entries to sort.</param>
    /// <param name="maximumLength">The maximum encoded size permitted for each key.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The sorted entries, or stable failure details.</returns>
    CborMapSortResult SortCborMap(CborMapEntry[] entries, int maximumLength, int maximumDepth);
}
