using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP packet.
/// </summary>
public class EmailOpenPgpPacket : IEmailOpenPgpPacketContract
{
    int typeIdentifier;
    EmailOpenPgpPacketKind kind;
    byte[] body;
    byte[] encodedBytes;
    bool legacyHeader;
    bool partialLengths;
    long offset;
    object? parsedValue;
    /// <summary>
    /// Creates a new openPGP packet.
    /// </summary>
    /// <param name = "typeIdentifier">The type identifier used by the operation.</param>
    /// <param name = "body">The body used by the operation.</param>
    /// <param name = "encodedBytes">The source bytes or data to process.</param>
    /// <param name = "legacyHeader">Whether to enable the legacy header behavior.</param>
    /// <param name = "partialLengths">Whether to enable the partial lengths behavior.</param>
    /// <param name = "offset">The zero-based byte offset.</param>
    /// <param name = "parsedValue">The parsed value used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpPacket(int typeIdentifier, byte[] body, byte[] encodedBytes, bool legacyHeader, bool partialLengths, long offset, object? parsedValue)
    {
        if (typeIdentifier < 0 || typeIdentifier > 63)
            throw new ArgumentOutOfRangeException("typeIdentifier");
        if (body == null || encodedBytes == null)
            throw new ArgumentNullException("body");
        this.typeIdentifier = typeIdentifier;
        kind = IsKnownPacketKind(typeIdentifier) ? (EmailOpenPgpPacketKind)typeIdentifier : EmailOpenPgpPacketKind.Unknown;
        this.body = new EmailOpenPgpArmorResultOperations().Copy(body);
        this.encodedBytes = new EmailOpenPgpArmorResultOperations().Copy(encodedBytes);
        this.legacyHeader = legacyHeader;
        this.partialLengths = partialLengths;
        this.offset = offset;
        this.parsedValue = parsedValue;
    }

    /// <summary>
    /// Gets the type identifier.
    /// </summary>
    /// <returns>The current type identifier.</returns>
    public int GetTypeIdentifier()
    {
        return typeIdentifier;
    }

    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    public EmailOpenPgpPacketKind GetKind()
    {
        return kind;
    }

    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current body.</returns>
    public byte[] GetBody()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(body);
    }

    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    public byte[] GetEncodedBytes()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(encodedBytes);
    }

    /// <summary>
    /// Determines whether the legacy header is present or available.
    /// </summary>
    /// <returns><c>true</c> when the legacy header is available; otherwise, <c>false</c>.</returns>
    public bool HasLegacyHeader()
    {
        return legacyHeader;
    }

    /// <summary>
    /// Determines whether the current value or state uses partial lengths.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses partial lengths; otherwise, <c>false</c>.</returns>
    public bool UsesPartialLengths()
    {
        return partialLengths;
    }

    static bool IsKnownPacketKind(int value)
    {
        return value >= 1 && value <= 14 || value >= 17 && value <= 21;
    }

    /// <summary>
    /// Gets the offset.
    /// </summary>
    /// <returns>The current offset.</returns>
    public long GetOffset()
    {
        return offset;
    }

    /// <summary>
    /// Gets the literal data.
    /// </summary>
    /// <returns>The current literal data.</returns>
    public EmailOpenPgpLiteralData? GetLiteralData()
    {
        return parsedValue as EmailOpenPgpLiteralData;
    }

    /// <summary>
    /// Gets the compressed data.
    /// </summary>
    /// <returns>The current compressed data.</returns>
    public EmailOpenPgpCompressedData? GetCompressedData()
    {
        return parsedValue as EmailOpenPgpCompressedData;
    }

    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    public EmailOpenPgpSignaturePacket? GetSignature()
    {
        return parsedValue as EmailOpenPgpSignaturePacket;
    }

    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <returns>The current key.</returns>
    public EmailOpenPgpKeyPacket? GetKey()
    {
        return parsedValue as EmailOpenPgpKeyPacket;
    }

    /// <summary>
    /// Gets the session key.
    /// </summary>
    /// <returns>The current session key.</returns>
    public EmailOpenPgpSessionKeyPacket? GetSessionKey()
    {
        return parsedValue as EmailOpenPgpSessionKeyPacket;
    }
}
