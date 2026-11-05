using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents an openPGP signing credential.
/// </summary>
public class EmailOpenPgpSigningCredential : IEmailOpenPgpSigningCredentialContract
{
    byte[] keyIdentifier;
    int publicKeyAlgorithm;
    int hashAlgorithm;
    AsymmetricAlgorithm privateKey;
    /// <summary>
    /// Creates a new openPGP signing credential.
    /// </summary>
    /// <param name = "keyIdentifier">The key identifier used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailOpenPgpSigningCredential(byte[] keyIdentifier, int publicKeyAlgorithm, int hashAlgorithm, AsymmetricAlgorithm privateKey)
    {
        if (keyIdentifier == null || privateKey == null)
            throw new ArgumentNullException("keyIdentifier");
        if (keyIdentifier.Length != 8)
            throw new ArgumentException("A version 4 OpenPGP key identifier must contain eight octets.", "keyIdentifier");
        if (publicKeyAlgorithm != EmailOpenPgpAlgorithmsConstants.PublicKeyRsa && publicKeyAlgorithm != EmailOpenPgpAlgorithmsConstants.PublicKeyRsaSignOnly && publicKeyAlgorithm != EmailOpenPgpAlgorithmsConstants.PublicKeyEcdsa)
            throw new ArgumentOutOfRangeException("publicKeyAlgorithm");
        if (publicKeyAlgorithm == EmailOpenPgpAlgorithmsConstants.PublicKeyEcdsa && !(privateKey is ECDsa) || publicKeyAlgorithm != EmailOpenPgpAlgorithmsConstants.PublicKeyEcdsa && !(privateKey is RSA))
            throw new ArgumentException("The private key type does not match the OpenPGP public-key algorithm.", "privateKey");
        this.keyIdentifier = CopyImplementation(keyIdentifier);
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.hashAlgorithm = hashAlgorithm;
        this.privateKey = privateKey;
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
        return CopyImplementation(keyIdentifier);
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
    /// Gets the hash algorithm.
    /// </summary>
    /// <returns>The current hash algorithm.</returns>
    public int GetHashAlgorithm()
    {
        return hashAlgorithm;
    }

    /// <summary>
    /// Gets the private key.
    /// </summary>
    /// <returns>The current private key.</returns>
    public AsymmetricAlgorithm GetPrivateKey()
    {
        return privateKey;
    }

    internal static byte[] CopyImplementation(byte[] value)
    {
        byte[] copy = new byte[value.Length];
        value.CopyTo(copy, 0);
        return copy;
    }
}
