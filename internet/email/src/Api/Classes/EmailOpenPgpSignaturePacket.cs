using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents an openPGP signature packet.
/// </summary>
public class EmailOpenPgpSignaturePacket : IEmailOpenPgpSignaturePacketContract
{
    int version;
    int signatureType;
    int publicKeyAlgorithm;
    int hashAlgorithm;
    byte[] hashedSubpackets;
    byte[] unhashedSubpackets;
    byte[] hashPrefix;
    byte[] salt;
    byte[] signatureMaterial;
    byte[] issuerKeyIdentifier;
    List<EmailOpenPgpSignatureSubpacket> parsedHashedSubpackets;
    List<EmailOpenPgpSignatureSubpacket> parsedUnhashedSubpackets;
    /// <summary>
    /// Creates a new openPGP signature packet.
    /// </summary>
    /// <param name = "version">The version used by the operation.</param>
    /// <param name = "signatureType">The signature type used by the operation.</param>
    /// <param name = "publicKeyAlgorithm">The public key algorithm used by the operation.</param>
    /// <param name = "hashAlgorithm">The hash algorithm used by the operation.</param>
    /// <param name = "hashedSubpackets">The hashed subpackets used by the operation.</param>
    /// <param name = "unhashedSubpackets">The unhashed subpackets used by the operation.</param>
    /// <param name = "hashPrefix">The hash prefix used by the operation.</param>
    /// <param name = "salt">The salt used by the operation.</param>
    /// <param name = "signatureMaterial">The signature material used by the operation.</param>
    /// <param name = "issuerKeyIdentifier">The issuer key identifier used by the operation.</param>
    /// <param name = "parsedHashedSubpackets">The parsed hashed subpackets used by the operation.</param>
    /// <param name = "parsedUnhashedSubpackets">The parsed unhashed subpackets used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailOpenPgpSignaturePacket(int version, int signatureType, int publicKeyAlgorithm, int hashAlgorithm, byte[] hashedSubpackets, byte[] unhashedSubpackets, byte[] hashPrefix, byte[] salt, byte[] signatureMaterial, byte[] issuerKeyIdentifier, List<EmailOpenPgpSignatureSubpacket>? parsedHashedSubpackets, List<EmailOpenPgpSignatureSubpacket>? parsedUnhashedSubpackets)
    {
        if (hashedSubpackets == null || unhashedSubpackets == null || hashPrefix == null || salt == null || signatureMaterial == null || issuerKeyIdentifier == null || parsedHashedSubpackets == null || parsedUnhashedSubpackets == null)
            throw new ArgumentNullException("hashedSubpackets");
        this.version = version;
        this.signatureType = signatureType;
        this.publicKeyAlgorithm = publicKeyAlgorithm;
        this.hashAlgorithm = hashAlgorithm;
        this.hashedSubpackets = new EmailOpenPgpArmorResultOperations().Copy(hashedSubpackets);
        this.unhashedSubpackets = new EmailOpenPgpArmorResultOperations().Copy(unhashedSubpackets);
        this.hashPrefix = new EmailOpenPgpArmorResultOperations().Copy(hashPrefix);
        this.salt = new EmailOpenPgpArmorResultOperations().Copy(salt);
        this.signatureMaterial = new EmailOpenPgpArmorResultOperations().Copy(signatureMaterial);
        this.issuerKeyIdentifier = new EmailOpenPgpArmorResultOperations().Copy(issuerKeyIdentifier);
        this.parsedHashedSubpackets = new List<EmailOpenPgpSignatureSubpacket>(parsedHashedSubpackets);
        this.parsedUnhashedSubpackets = new List<EmailOpenPgpSignatureSubpacket>(parsedUnhashedSubpackets);
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
    /// Gets the signature type.
    /// </summary>
    /// <returns>The current signature type.</returns>
    public int GetSignatureType()
    {
        return signatureType;
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
    /// Gets the hashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hashed subpackets.</returns>
    public byte[] GetHashedSubpackets()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(hashedSubpackets);
    }

    /// <summary>
    /// Gets the unhashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current unhashed subpackets.</returns>
    public byte[] GetUnhashedSubpackets()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(unhashedSubpackets);
    }

    /// <summary>
    /// Gets the hash prefix.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current hash prefix.</returns>
    public byte[] GetHashPrefix()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(hashPrefix);
    }

    /// <summary>
    /// Gets the salt.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current salt.</returns>
    public byte[] GetSalt()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(salt);
    }

    /// <summary>
    /// Gets the signature material.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current signature material.</returns>
    public byte[] GetSignatureMaterial()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(signatureMaterial);
    }

    /// <summary>
    /// Gets the issuer key identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current issuer key identifier.</returns>
    public byte[] GetIssuerKeyIdentifier()
    {
        return new EmailOpenPgpArmorResultOperations().Copy(issuerKeyIdentifier);
    }

    /// <summary>
    /// Gets the parsed hashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parsed hashed subpackets.</returns>
    public List<EmailOpenPgpSignatureSubpacket> GetParsedHashedSubpackets()
    {
        return new List<EmailOpenPgpSignatureSubpacket>(parsedHashedSubpackets);
    }

    /// <summary>
    /// Gets the parsed unhashed subpackets.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current parsed unhashed subpackets.</returns>
    public List<EmailOpenPgpSignatureSubpacket> GetParsedUnhashedSubpackets()
    {
        return new List<EmailOpenPgpSignatureSubpacket>(parsedUnhashedSubpackets);
    }
}
