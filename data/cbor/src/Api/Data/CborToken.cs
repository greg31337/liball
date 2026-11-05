using System.Numerics;

namespace cbor;

/// <summary>
/// Holds the public discriminants for a low-level CBOR token.
/// </summary>
public class CborToken
{
    /// <summary>The token kind.</summary>
    public CborTokenKind Kind;
    /// <summary>The encoded width when the token contains a floating-point value.</summary>
    public CborFloatWidth FloatWidth;
    /// <summary>The encoded length form when the token starts a string or container.</summary>
    public CborLengthKind LengthKind;
    /// <summary>The integer value when <see cref="Kind"/> is an integer token kind.</summary>
    public BigInteger IntegerValue;
    /// <summary>The bytes when <see cref="Kind"/> is <see cref="CborTokenKind.ByteString"/>.</summary>
    public byte[]? ByteStringValue;
    /// <summary>The text when <see cref="Kind"/> is <see cref="CborTokenKind.TextString"/>.</summary>
    public string? TextStringValue;
    /// <summary>The byte length for a string or the declared item or pair count for a container.</summary>
    public ulong Length;
    /// <summary>The tag number when <see cref="Kind"/> is <see cref="CborTokenKind.Tag"/>.</summary>
    public ulong TagNumber;
    /// <summary>The simple-value number when <see cref="Kind"/> is <see cref="CborTokenKind.SimpleValue"/>.</summary>
    public byte SimpleValue;
    /// <summary>The floating-point value and retained bits when <see cref="Kind"/> is <see cref="CborTokenKind.Float"/>.</summary>
    public CborFloatValue? FloatValue;
    /// <summary>Optional metadata describing the original token representation.</summary>
    public CborRepresentation? Representation;
}
