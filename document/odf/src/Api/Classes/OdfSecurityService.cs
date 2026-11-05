using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace odf;

/// <summary>
/// Provides built-in OpenDocument package encryption and signature operations.
/// </summary>
public class OdfSecurityService : IOdfSecurityService
{
    /// <summary>
    /// Initializes a security service.
    /// </summary>
    public OdfSecurityService()
    {
    }

    /// <summary>
    /// Encrypts one package entry with a password-derived key.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="path">The package entry path.</param>
    /// <param name="password">The encryption password.</param>
    /// <param name="method">The content-encryption method.</param>
    /// <param name="version">The ODF encryption version.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The mutation result.</returns>
    public OdfEditResult EncryptPackageEntry(
        OdfPackage package,
        string path,
        string password,
        OdfEncryptionMethod method,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.EncryptEntry(
            package,
            path,
            password,
            method,
            version,
            limits);
    }

    /// <summary>
    /// Decrypts one password-encrypted package entry.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="path">The encrypted package entry path.</param>
    /// <param name="password">The decryption password.</param>
    /// <param name="version">The ODF encryption version.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The mutation result.</returns>
    public OdfEditResult DecryptPackageEntry(
        OdfPackage package,
        string path,
        string password,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.DecryptEntry(
            package,
            path,
            password,
            version,
            limits);
    }

    /// <summary>
    /// Decrypts one OpenPGP-key-transported package entry with caller-supplied key material.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="path">The encrypted package entry path.</param>
    /// <param name="provider">The decrypted-key provider.</param>
    /// <param name="encryptedKey">The selected recipient key metadata.</param>
    /// <param name="version">The ODF encryption version.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The mutation result.</returns>
    public OdfEditResult DecryptPackageEntryWithOpenPgp(
        OdfPackage package,
        string path,
        IOdfOpenPgpKeyProvider provider,
        OdfEncryptedKey encryptedKey,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.DecryptEntryWithOpenPgp(
            package,
            path,
            provider,
            encryptedKey,
            version,
            limits);
    }

    /// <summary>
    /// Encrypts every eligible unencrypted document entry.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="password">The encryption password.</param>
    /// <param name="method">The content-encryption method.</param>
    /// <param name="version">The ODF encryption version.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The mutation result.</returns>
    public OdfEditResult EncryptPackageDocument(
        OdfPackage package,
        string password,
        OdfEncryptionMethod method,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.EncryptDocument(
            package,
            password,
            method,
            version,
            limits);
    }

    /// <summary>
    /// Decrypts every password-encrypted document entry.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="password">The decryption password.</param>
    /// <param name="version">The ODF encryption version.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The mutation result.</returns>
    public OdfEditResult DecryptPackageDocument(
        OdfPackage package,
        string password,
        OdfVersion version,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.DecryptDocument(
            package,
            password,
            version,
            limits);
    }

    /// <summary>
    /// Parses and verifies every standard signature container present in a package.
    /// </summary>
    /// <param name="package">The package to verify.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The parsed signature containers in standard path order.</returns>
    public OdfSignatureContainer[] VerifyPackageSignatures(
        OdfPackage package,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.VerifySignatures(package, limits);
    }

    /// <summary>
    /// Creates or replaces one standard signature container and signs selected entries.
    /// </summary>
    /// <param name="package">The editable package.</param>
    /// <param name="partPath">The standard signature-container path.</param>
    /// <param name="version">The ODF signature version lexical value.</param>
    /// <param name="entryPaths">The package entries to sign.</param>
    /// <param name="privateKey">The caller-owned signing key.</param>
    /// <param name="certificate">The caller-owned signing certificate.</param>
    /// <param name="signatureMethod">The XML Signature method IRI.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The generated and verified signature container.</returns>
    public OdfSignatureContainer SignPackageEntries(
        OdfPackage package,
        string partPath,
        string version,
        string[] entryPaths,
        AsymmetricAlgorithm privateKey,
        X509Certificate2 certificate,
        string signatureMethod,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.SignEntries(
            package,
            partPath,
            version,
            entryPaths,
            privateKey,
            certificate,
            signatureMethod,
            limits);
    }

    /// <summary>
    /// Collects package, manifest, encryption, and signature integrity diagnostics.
    /// </summary>
    /// <param name="package">The package to inspect.</param>
    /// <param name="limits">The operation limits.</param>
    /// <returns>The collected diagnostics.</returns>
    public OdfDiagnostic[] InspectPackageIntegrity(
        OdfPackage package,
        OdfSecurityLimits limits)
    {
        return SecurityServiceCore.InspectIntegrity(package, limits);
    }
}
