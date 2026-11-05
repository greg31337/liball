using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Represents an openPGP verification key.
/// </summary>
public class EmailOpenPgpVerificationKey : IEmailOpenPgpVerificationKeyContract
{
    byte[] identifier;
    int publicKeyAlgorithm;
    AsymmetricAlgorithm publicKey;
    /// <summary>
    /// Creates a new openPGP verification key.
    /// </summary>
    /// <param name = "identifier">The identifier used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "publicKey">The public key used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpVerificationKey(byte[] identifier, int publicKeyAlgorithm, AsymmetricAlgorithm publicKey)
    {
        if (identifier == null || publicKey == null)
            throw new ArgumentNullException("identifier");
        if (identifier.Length != 8 && identifier.Length != 20 && identifier.Length != 32)
            throw new ArgumentException("An OpenPGP verification identifier must be a key ID or fingerprint.", "identifier");
        if (publicKeyAlgorithm == EmailOpenPgpAlgorithmsConstants.PublicKeyEcdsa && !(publicKey is ECDsa) || publicKeyAlgorithm != EmailOpenPgpAlgorithmsConstants.PublicKeyEcdsa && !(publicKey is RSA))
            throw new ArgumentException("The public key type does not match the OpenPGP public-key algorithm.", "publicKey");
        this.identifier = new EmailOpenPgpSigningCredentialOperations().Copy(identifier);
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.publicKey = publicKey;
    }

    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current identifier.</returns>
    public byte[] GetIdentifier()
    {
        return new EmailOpenPgpSigningCredentialOperations().Copy(identifier);
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
    /// Gets the public key.
    /// </summary>
    /// <returns>The current public key.</returns>
    public AsymmetricAlgorithm GetPublicKey()
    {
        return publicKey;
    }
}
