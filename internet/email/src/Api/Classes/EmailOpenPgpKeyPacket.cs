using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP key packet.
/// </summary>
public class EmailOpenPgpKeyPacket : IEmailOpenPgpKeyPacketContract
{
    int version;
    uint creationTime;
    int publicKeyAlgorithm;
    byte[] publicKeyMaterial;
    byte[] fingerprint;
    byte[] keyIdentifier;
    bool secret;
    bool subkey;
    /// <summary>
    /// Creates a new openPGP key packet.
    /// </summary>
    /// <param name = "version">The version used by the operation.</param>
    /// <param name = "creationTime">The creation time used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "publicKeyMaterial">The public key material used by the operation.</param>
    /// <param name = "fingerprint">The fingerprint used by the operation.</param>
    /// <param name = "keyIdentifier">The key identifier used by the operation.</param>
    /// <param name = "secret">Whether to enable the secret behavior.</param>
    /// <param name = "subkey">Whether to enable the subkey behavior.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpKeyPacket(int version, uint creationTime, int publicKeyAlgorithm, byte[] publicKeyMaterial, byte[] fingerprint, byte[] keyIdentifier, bool secret, bool subkey)
    {
        if (publicKeyMaterial == null || fingerprint == null || keyIdentifier == null)
            throw new ArgumentNullException("publicKeyMaterial");
        this.version = version;
        this.creationTime = creationTime;
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.publicKeyMaterial = new EmailOpenPgpArmorResultOperations().Copy(publicKeyMaterial);
        this.fingerprint = new EmailOpenPgpArmorResultOperations().Copy(fingerprint);
        this.keyIdentifier = new EmailOpenPgpArmorResultOperations().Copy(keyIdentifier);
        this.secret = secret;
        this.subkey = subkey;
    }

    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    public int GetVersion()
    {
        return version;
    }

    /// <summary>
    /// Gets the creation time.
    /// </summary>
    /// <returns>The current creation time.</returns>
    public uint GetCreationTime()
    {
        return creationTime;
    }

    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    public int GetPublicKeyAlgorithm()
    {
        return publicKeyAlgorithm;
    }

    /// <summary>
    /// Gets the public key material.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current public key material.</returns>
    public byte[] GetPublicKeyMaterial()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(publicKeyMaterial);
    }

    /// <summary>
    /// Determines whether the fingerprint is present or available.
    /// </summary>
    /// <returns><c>true</c> when the fingerprint is available; otherwise, <c>false</c>.</returns>
    public bool HasFingerprint()
    {
        return fingerprint.Length > 0;
    }

    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fingerprint.</returns>
    public byte[] GetFingerprint()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(fingerprint);
    }

    /// <summary>
    /// Gets the key identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current key identifier.</returns>
    public byte[] GetKeyIdentifier()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(keyIdentifier);
    }

    /// <summary>
    /// Determines whether the current value or state is secret.
    /// </summary>
    /// <returns><c>true</c> when the value or state is secret; otherwise, <c>false</c>.</returns>
    public bool IsSecret()
    {
        return secret;
    }

    /// <summary>
    /// Determines whether the current value or state is subkey.
    /// </summary>
    /// <returns><c>true</c> when the value or state is subkey; otherwise, <c>false</c>.</returns>
    public bool IsSubkey()
    {
        return subkey;
    }
}
