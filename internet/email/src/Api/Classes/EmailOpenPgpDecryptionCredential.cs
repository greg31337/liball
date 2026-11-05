using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents an openPGP decryption credential.
/// </summary>
public class EmailOpenPgpDecryptionCredential : IEmailOpenPgpDecryptionCredentialContract
{
    byte[] fingerprint;
    int keyVersion;
    RSA privateKey;
    /// <summary>
    /// Creates a new openPGP decryption credential.
    /// </summary>
    /// <param name = "keyVersion">The key version used by the operation.</param>
    /// <param name = "fingerprint">The fingerprint used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpDecryptionCredential(int keyVersion, byte[] fingerprint, RSA privateKey)
    {
        if (fingerprint == null || privateKey == null)
            throw new ArgumentNullException("fingerprint");
        if (keyVersion != 4 && keyVersion != 6)
            throw new ArgumentOutOfRangeException("keyVersion");
        if (fingerprint.Length != (keyVersion == 4 ? 20 : 32))
            throw new ArgumentException("The fingerprint length does not match the OpenPGP key version.", "fingerprint");
        this.keyVersion = keyVersion;
        this.fingerprint = new EmailOpenPgpSigningCredentialOperations().Copy(fingerprint);
        this.privateKey = privateKey;
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
    /// Gets the private key.
    /// </summary>
    /// <returns>The current private key.</returns>
    public RSA GetPrivateKey()
    {
        return privateKey;
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
        return keyVersion == 4 ? Slice(fingerprint, fingerprint.Length - 8, 8) : Slice(fingerprint, 0, 8);
    }

    static byte[] Slice(byte[] value, int offset, int count)
    {
        byte[] result = new byte[count];
        Array.Copy(value, offset, result, 0, count);
        return result;
    }
}
