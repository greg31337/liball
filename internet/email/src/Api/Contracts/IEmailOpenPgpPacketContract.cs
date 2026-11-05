using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpPacket"/>.
/// </summary>
public interface IEmailOpenPgpPacketContract
{
    /// <summary>
    /// Gets the type identifier.
    /// </summary>
    /// <returns>The current type identifier.</returns>
    int GetTypeIdentifier();
    /// <summary>
    /// Gets the kind.
    /// </summary>
    /// <returns>The current kind.</returns>
    EmailOpenPgpPacketKind GetKind();
    /// <summary>
    /// Gets the body.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current body.</returns>
    byte[] GetBody();
    /// <summary>
    /// Gets the encoded bytes.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encoded bytes.</returns>
    byte[] GetEncodedBytes();
    /// <summary>
    /// Determines whether the legacy header is present or available.
    /// </summary>
    /// <returns><c>true</c> when the legacy header is available; otherwise, <c>false</c>.</returns>
    bool HasLegacyHeader();
    /// <summary>
    /// Determines whether the current value or state uses partial lengths.
    /// </summary>
    /// <returns><c>true</c> when the value or state uses partial lengths; otherwise, <c>false</c>.</returns>
    bool UsesPartialLengths();
    /// <summary>
    /// Gets the offset.
    /// </summary>
    /// <returns>The current offset.</returns>
    long GetOffset();
    /// <summary>
    /// Gets the literal data.
    /// </summary>
    /// <returns>The current literal data.</returns>
    EmailOpenPgpLiteralData? GetLiteralData();
    /// <summary>
    /// Gets the compressed data.
    /// </summary>
    /// <returns>The current compressed data.</returns>
    EmailOpenPgpCompressedData? GetCompressedData();
    /// <summary>
    /// Gets the signature.
    /// </summary>
    /// <returns>The current signature.</returns>
    EmailOpenPgpSignaturePacket? GetSignature();
    /// <summary>
    /// Gets the key.
    /// </summary>
    /// <returns>The current key.</returns>
    EmailOpenPgpKeyPacket? GetKey();
    /// <summary>
    /// Gets the session key.
    /// </summary>
    /// <returns>The current session key.</returns>
    EmailOpenPgpSessionKeyPacket? GetSessionKey();
}
