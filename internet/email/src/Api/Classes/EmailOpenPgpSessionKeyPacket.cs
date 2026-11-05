using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP session key packet.
/// </summary>
public class EmailOpenPgpSessionKeyPacket : IEmailOpenPgpSessionKeyPacketContract
{
    int version;
    bool publicKeyEncrypted;
    int publicKeyAlgorithm;
    int symmetricAlgorithm;
    int aeadAlgorithm;
    byte[] recipientIdentifier;
    byte[] stringToKeySpecifier;
    byte[] encryptedSessionKey;
    /// <summary>
    /// Creates a new openPGP session key packet.
    /// </summary>
    /// <param name = "version">The version used by the operation.</param>
    /// <param name = "publicKeyEncrypted">Whether to enable the public key encrypted behavior.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "symmetricAlgorithm">The symmetric algorithm used by the operation.</param>
    /// <param name = "aeadAlgorithm">The aead algorithm used by the operation.</param>
    /// <param name = "recipientIdentifier">The recipient identifier used by the operation.</param>
    /// <param name = "stringToKeySpecifier">The string to key specifier used by the operation.</param>
    /// <param name = "encryptedSessionKey">The encrypted session key used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpSessionKeyPacket(int version, bool publicKeyEncrypted, int publicKeyAlgorithm, int symmetricAlgorithm, int aeadAlgorithm, byte[] recipientIdentifier, byte[] stringToKeySpecifier, byte[] encryptedSessionKey)
    {
        if (recipientIdentifier == null || stringToKeySpecifier == null || encryptedSessionKey == null)
            throw new ArgumentNullException("recipientIdentifier");
        this.version = version;
        this.publicKeyEncrypted = publicKeyEncrypted;
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.symmetricAlgorithm = symmetricAlgorithm;
        this.aeadAlgorithm = aeadAlgorithm;
        this.recipientIdentifier = new EmailOpenPgpArmorResultOperations().Copy(recipientIdentifier);
        this.stringToKeySpecifier = new EmailOpenPgpArmorResultOperations().Copy(stringToKeySpecifier);
        this.encryptedSessionKey = new EmailOpenPgpArmorResultOperations().Copy(encryptedSessionKey);
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
    /// Determines whether the current value or state is public key encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is public key encrypted; otherwise, <c>false</c>.</returns>
    public bool IsPublicKeyEncrypted()
    {
        return publicKeyEncrypted;
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
    /// Gets the symmetric algorithm.
    /// </summary>
    /// <returns>The current symmetric algorithm.</returns>
    public int GetSymmetricAlgorithm()
    {
        return symmetricAlgorithm;
    }

    /// <summary>
    /// Gets the aead algorithm.
    /// </summary>
    /// <returns>The current aead algorithm.</returns>
    public int GetAeadAlgorithm()
    {
        return aeadAlgorithm;
    }

    /// <summary>
    /// Gets the recipient identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipient identifier.</returns>
    public byte[] GetRecipientIdentifier()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(recipientIdentifier);
    }

    /// <summary>
    /// Gets the string to key specifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current string to key specifier.</returns>
    public byte[] GetStringToKeySpecifier()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(stringToKeySpecifier);
    }

    /// <summary>
    /// Gets the encrypted session key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encrypted session key.</returns>
    public byte[] GetEncryptedSessionKey()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(encryptedSessionKey);
    }
}
