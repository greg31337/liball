using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpKeyPacket"/>.
/// </summary>
public interface IEmailOpenPgpKeyPacketContract
{
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    int GetVersion();
    /// <summary>
    /// Gets the creation time.
    /// </summary>
    /// <returns>The current creation time.</returns>
    uint GetCreationTime();
    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    int GetPublicKeyAlgorithm();
    /// <summary>
    /// Gets the public key material.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current public key material.</returns>
    byte[] GetPublicKeyMaterial();
    /// <summary>
    /// Determines whether the fingerprint is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fingerprint is available; otherwise, <c>false</c>.</returns>
    bool HasFingerprint();
    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fingerprint.</returns>
    byte[] GetFingerprint();
    /// <summary>
    /// Gets the key identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current key identifier.</returns>
    byte[] GetKeyIdentifier();
    /// <summary>
    /// Determines whether the current value or state is secret.
    /// </summary>
    /// <returns><c>true</c> when the value or state is secret; otherwise, <c>false</c>.</returns>
    bool IsSecret();
    /// <summary>
    /// Determines whether the current value or state is subkey.
    /// </summary>
    /// <returns><c>true</c> when the value or state is subkey; otherwise, <c>false</c>.</returns>
    bool IsSubkey();
}
