using System;
using System.Text;

namespace token;

/// <summary>
/// Contains a parsed compact JOSE serialization and its decoded segment bytes.
/// </summary>
public class JoseCompactSerialization
{
    JoseCompactSegments encodedSegments;
    byte[][] decodedSegments;

    /// <summary>Identifies whether the serialization has the JWS or JWE compact shape.</summary>
    public TokenKind Kind;

    /// <summary>Contains the number of compact serialization segments.</summary>
    public int SegmentCount;

    internal JoseCompactSerialization(
        JoseCompactSegments encoded,
        byte[][] decoded)
    {
        encodedSegments = encoded;
        decodedSegments = decoded;
        SegmentCount = decoded.Length;
        Kind = decoded.Length == 3
            ? TokenKind.JwsCompact
            : TokenKind.JweCompact;
    }

    /// <summary>Returns the original compact serialization text.</summary>
    /// <returns>The exact string supplied to the parser.</returns>
    internal string GetText()
    {
        return encodedSegments.GetText();
    }

    /// <summary>Returns an encoded segment as a view over the original compact text.</summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The exact canonical base64url characters for the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal ReadOnlySpan<char> GetEncodedSegment(int index)
    {
        return encodedSegments.GetSegment(index);
    }

    /// <summary>Returns decoded segment bytes as a read-only view.</summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>The decoded bytes for the segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal ReadOnlySpan<byte> GetDecodedSegment(int index)
    {
        if (index < 0 || index >= SegmentCount)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return decodedSegments[index].AsSpan();
    }

    /// <summary>Returns an independent array containing decoded segment bytes.</summary>
    /// <param name="index">The zero-based segment index.</param>
    /// <returns>A new byte array containing the decoded segment.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>index</c> does not identify a segment.</exception>
    internal byte[] GetDecodedSegmentBytes(int index)
    {
        return GetDecodedSegment(index).ToArray();
    }
}
