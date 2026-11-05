using System;
namespace token;

/// <summary>
/// Implements low-level cryptographic byte and signature utilities.
/// </summary>
public class TokenCryptoUtilityService : ITokenCryptoUtilityService
{
    /// <summary>Initializes a stateless cryptographic utility service.</summary>
    public TokenCryptoUtilityService()
    {
    }

    /// <summary>Compares two byte arrays in fixed time for equal lengths.</summary>
    /// <param name="left">The first byte array.</param>
    /// <param name="right">The second byte array.</param>
    /// <returns><c>true</c> when the arrays have equal contents; otherwise, <c>false</c>.</returns>
    public bool FixedTimeEquals(byte[] left, byte[] right)
    {
        return ConstantTime.AreEqual(left, right);
    }

    /// <summary>Compares two byte spans in fixed time for equal lengths.</summary>
    /// <param name="left">The first byte span.</param>
    /// <param name="right">The second byte span.</param>
    /// <returns><c>true</c> when the spans have equal contents; otherwise, <c>false</c>.</returns>
    public bool FixedTimeEqualsSpan(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        return ConstantTime.AreEqual(left, right);
    }

    /// <summary>Generates cryptographically secure random bytes.</summary>
    /// <param name="length">The requested byte count.</param>
    /// <returns>A successful result containing a new random byte array.</returns>
    public TokenBytesResult GenerateRandomBytes(int length)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = RandomBytes.Generate(length);
        result.Success = true;
        return result;
    }

    /// <summary>Fills a destination span with cryptographically secure random bytes.</summary>
    /// <param name="destination">The destination span.</param>
    public void FillRandomBytes(Span<byte> destination)
    {
        RandomBytes.Fill(destination);
    }

    /// <summary>Normalizes an unsigned big-endian integer to an exact width.</summary>
    /// <param name="value">The unsigned big-endian bytes.</param>
    /// <param name="width">The requested width.</param>
    /// <returns>The zero-padded value, or stable failure details when it does not fit.</returns>
    public TokenBytesResult NormalizeBigEndianInteger(byte[] value, int width)
    {
        return BigEndianInteger.ToFixedWidth(value, width);
    }

    /// <summary>Normalizes an unsigned big-endian integer span to an exact width.</summary>
    /// <param name="value">The unsigned big-endian bytes.</param>
    /// <param name="width">The requested width.</param>
    /// <returns>The zero-padded value, or stable failure details when it does not fit.</returns>
    public TokenBytesResult NormalizeBigEndianIntegerSpan(ReadOnlySpan<byte> value, int width)
    {
        return BigEndianInteger.ToFixedWidth(value, width);
    }

    /// <summary>Writes an unsigned big-endian integer into an exact-width destination.</summary>
    /// <param name="value">The unsigned big-endian bytes.</param>
    /// <param name="destination">The destination whose length defines the width.</param>
    /// <returns>A successful result, or stable failure details when the value does not fit.</returns>
    public TokenResult WriteBigEndianIntegerSpan(ReadOnlySpan<byte> value, Span<byte> destination)
    {
        return BigEndianInteger.WriteFixedWidth(value, destination);
    }

    /// <summary>Removes redundant leading zero bytes from an unsigned big-endian integer.</summary>
    /// <param name="value">The unsigned big-endian bytes.</param>
    /// <returns>An independent minimal representation.</returns>
    public TokenBytesResult MinimizeBigEndianInteger(byte[] value)
    {
        return BigEndianInteger.ToMinimal(value);
    }

    /// <summary>Removes redundant leading zero bytes from an unsigned big-endian integer span.</summary>
    /// <param name="value">The unsigned big-endian bytes.</param>
    /// <returns>An independent minimal representation.</returns>
    public TokenBytesResult MinimizeBigEndianIntegerSpan(ReadOnlySpan<byte> value)
    {
        return BigEndianInteger.ToMinimal(value);
    }

    /// <summary>Encodes an unsigned 64-bit integer at an exact big-endian width.</summary>
    /// <param name="value">The unsigned integer.</param>
    /// <param name="width">The requested width from zero through eight.</param>
    /// <returns>The encoded bytes, or stable failure details when the value does not fit.</returns>
    public TokenBytesResult EncodeUInt64BigEndian(ulong value, int width)
    {
        return BigEndianInteger.FromUInt64(value, width);
    }

    /// <summary>Converts a strict DER ECDSA signature to fixed-width raw form.</summary>
    /// <param name="derSignature">The DER signature.</param>
    /// <param name="componentWidth">The exact width of each raw component.</param>
    /// <returns>The raw signature, or stable failure details.</returns>
    public TokenBytesResult ConvertEcdsaDerToRaw(byte[] derSignature, int componentWidth)
    {
        return EcdsaSignatureCodec.DerToRaw(derSignature, componentWidth);
    }

    /// <summary>Converts a strict DER ECDSA signature span to fixed-width raw form.</summary>
    /// <param name="derSignature">The DER signature.</param>
    /// <param name="componentWidth">The exact width of each raw component.</param>
    /// <returns>The raw signature, or stable failure details.</returns>
    public TokenBytesResult ConvertEcdsaDerToRawSpan(ReadOnlySpan<byte> derSignature, int componentWidth)
    {
        return EcdsaSignatureCodec.DerToRaw(derSignature, componentWidth);
    }

    /// <summary>Converts a fixed-width raw ECDSA signature to canonical DER.</summary>
    /// <param name="rawSignature">The raw signature.</param>
    /// <param name="componentWidth">The exact width of each raw component.</param>
    /// <returns>The DER signature, or stable failure details.</returns>
    public TokenBytesResult ConvertEcdsaRawToDer(byte[] rawSignature, int componentWidth)
    {
        return EcdsaSignatureCodec.RawToDer(rawSignature, componentWidth);
    }

    /// <summary>Converts a fixed-width raw ECDSA signature span to canonical DER.</summary>
    /// <param name="rawSignature">The raw signature.</param>
    /// <param name="componentWidth">The exact width of each raw component.</param>
    /// <returns>The DER signature, or stable failure details.</returns>
    public TokenBytesResult ConvertEcdsaRawToDerSpan(ReadOnlySpan<byte> rawSignature, int componentWidth)
    {
        return EcdsaSignatureCodec.RawToDer(rawSignature, componentWidth);
    }
}
