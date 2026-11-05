using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpSignaturePacket"/>.
/// </summary>
public interface IEmailOpenPgpSignaturePacketContract
{
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    int GetVersion();
    /// <summary>
    /// Gets the signature type.
    /// </summary>
    /// <returns>The current signature type.</returns>
    int GetSignatureType();
    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    int GetPublicKeyAlgorithm();
    /// <summary>
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    int GetHashAlgorithm();
    /// <summary>
    /// Gets the hashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hashed subpackets.</returns>
    byte[] GetHashedSubpackets();
    /// <summary>
    /// Gets the unhashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unhashed subpackets.</returns>
    byte[] GetUnhashedSubpackets();
    /// <summary>
    /// Gets the hash prefix.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hash prefix.</returns>
    byte[] GetHashPrefix();
    /// <summary>
    /// Gets the salt.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current salt.</returns>
    byte[] GetSalt();
    /// <summary>
    /// Gets the signature material.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signature material.</returns>
    byte[] GetSignatureMaterial();
    /// <summary>
    /// Gets the issuer key identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current issuer key identifier.</returns>
    byte[] GetIssuerKeyIdentifier();
    /// <summary>
    /// Gets the parsed hashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parsed hashed subpackets.</returns>
    List<EmailOpenPgpSignatureSubpacket> GetParsedHashedSubpackets();
    /// <summary>
    /// Gets the parsed unhashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parsed unhashed subpackets.</returns>
    List<EmailOpenPgpSignatureSubpacket> GetParsedUnhashedSubpackets();
}
