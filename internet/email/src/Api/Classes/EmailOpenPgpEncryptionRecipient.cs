using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents an openPGP encryption recipient.
/// </summary>
public class EmailOpenPgpEncryptionRecipient : IEmailOpenPgpEncryptionRecipientContract
{
    byte[] fingerprint;
    int keyVersion;
    RSA publicKey;
    /// <summary>
    /// Creates a new openPGP encryption recipient.
    /// </summary>
    /// <param name = "keyVersion">The key version used by the operation.</param>
    /// <param name = "fingerprint">The fingerprint used by the operation.</param>
    /// <param name = "publicKey">The public key used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpEncryptionRecipient(int keyVersion, byte[] fingerprint, RSA publicKey)
    {
        if (fingerprint == null || publicKey == null)
            throw new ArgumentNullException("fingerprint");
        if (keyVersion != 4 && keyVersion != 6)
            throw new ArgumentOutOfRangeException("keyVersion");
        if (fingerprint.Length != (keyVersion == 4 ? 20 : 32))
            throw new ArgumentException("The fingerprint length does not match the OpenPGP key version.", "fingerprint");
        this.keyVersion = keyVersion;
        this.fingerprint = new EmailOpenPgpSigningCredentialOperations().Copy(fingerprint);
        this.publicKey = publicKey;
    }

    /// <summary>
    /// Gets the key version.
    /// </summary>
    /// <returns>The current key version.</returns>
    public int GetKeyVersion()
    {
        return keyVersion;
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
        return new EmailOpenPgpSigningCredentialOperations().Copy(fingerprint);
    }

    /// <summary>
    /// Gets the public key.
    /// </summary>
    /// <returns>The current public key.</returns>
    public RSA GetPublicKey()
    {
        return publicKey;
    }
}
