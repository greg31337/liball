using System;
namespace token;

/// <summary>
/// Implements strict and deterministic CBOR codec operations.
/// </summary>
public class TokenCborCodec : ITokenCborCodec
{
    /// <summary>Initializes a stateless CBOR codec.</summary>
    public TokenCborCodec()
    {
    }

    /// <summary>Reads exactly one duplicate-safe CBOR item.</summary>
    /// <param name="cbor">The encoded CBOR bytes.</param>
    /// <param name="maximumLength">The maximum accepted byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The decoded item, or stable failure details.</returns>
    public CborReadResult ReadCbor(byte[] cbor, int maximumLength, int maximumDepth)
    {
        ValidateCborArguments(cbor, maximumLength, maximumDepth);
        CborReader reader = CborReader.Create(cbor, maximumLength, maximumDepth);
        CborReadResult result = reader.Read();
        if (result.Success && reader.GetRemainingLength() != 0)
        {
            CborReadResult failure = new CborReadResult();
            failure.Error = new TokenError(
                "trailing_cbor_data",
                "The CBOR input must contain exactly one item.",
                reader.GetPosition());
            return failure;
        }

        return result;
    }

    /// <summary>Writes one CBOR item using definite lengths and preferred primitive encodings.</summary>
    /// <param name="item">The item to write.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The encoded bytes, or stable failure details.</returns>
    public TokenBytesResult WriteCbor(CborItem item, int maximumLength, int maximumDepth)
    {
        ValidateCborItemArguments(item, maximumLength, maximumDepth);
        CborWriter writer = CborWriter.Create(maximumLength, maximumDepth);
        TokenResult writeResult = writer.WriteItem(item);
        return CreateCborBytesResult(writer, writeResult);
    }

    /// <summary>Writes one deterministic CBOR item with canonical map-key ordering.</summary>
    /// <param name="item">The item to write.</param>
    /// <param name="maximumLength">The maximum output byte count.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The deterministic encoded bytes, or stable failure details.</returns>
    public TokenBytesResult WriteDeterministicCbor(CborItem item, int maximumLength, int maximumDepth)
    {
        ValidateCborItemArguments(item, maximumLength, maximumDepth);
        return CborWriter.EncodeDeterministicItem(item, maximumLength, maximumDepth, 0);
    }

    /// <summary>Returns map entries in deterministic encoded-key order without changing the input array.</summary>
    /// <param name="entries">The map entries to sort.</param>
    /// <param name="maximumLength">The maximum encoded size permitted for each key.</param>
    /// <param name="maximumDepth">The maximum number of nested arrays, maps, and tags.</param>
    /// <returns>The sorted entries, or stable failure details.</returns>
    public CborMapSortResult SortCborMap(CborMapEntry[] entries, int maximumLength, int maximumDepth)
    {
        if (entries == null)
        {
            throw new ArgumentNullException("entries");
        }

        ValidateCborLimits(maximumLength, maximumDepth);
        return CborMapSorter.SortCore(entries, maximumLength, maximumDepth, 1);
    }

    internal static void ValidateCborArguments(byte[] cbor, int maximumLength, int maximumDepth)
    {
        if (cbor == null)
        {
            throw new ArgumentNullException("cbor");
        }

        ValidateCborLimits(maximumLength, maximumDepth);
    }

    internal static void ValidateCborItemArguments(CborItem item, int maximumLength, int maximumDepth)
    {
        if (item == null)
        {
            throw new ArgumentNullException("item");
        }

        ValidateCborLimits(maximumLength, maximumDepth);
    }

    internal static void ValidateCborLimits(int maximumLength, int maximumDepth)
    {
        if (maximumLength < 0)
        {
            throw new ArgumentOutOfRangeException("maximumLength");
        }

        if (maximumDepth < 0)
        {
            throw new ArgumentOutOfRangeException("maximumDepth");
        }
    }

    internal static TokenBytesResult CreateCborBytesResult(CborWriter writer, TokenResult writeResult)
    {
        TokenBytesResult result = new TokenBytesResult();
        if (!writeResult.Success)
        {
            result.Error = writeResult.Error;
            return result;
        }

        result.Bytes = writer.ToArray();
        result.Success = true;
        return result;
    }
}
