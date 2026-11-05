using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Represents a S/MIME signing credential.
/// </summary>
public class EmailSmimeSigningCredential : IEmailSmimeSigningCredentialContract
{
    X509Certificate2 certificate;
    AsymmetricAlgorithm? privateKey;
    EmailSmimeDigestAlgorithm digestAlgorithm;
    /// <summary>
    /// Creates an S/MIME signing credential with an explicit private key.
    /// </summary>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <param name = "digestAlgorithm">The digest algorithm used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailSmimeSigningCredential(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm)
    {
        if (certificate == null)
            throw new ArgumentNullException("certificate");
        if (digestAlgorithm < EmailSmimeDigestAlgorithm.Sha1 || digestAlgorithm > EmailSmimeDigestAlgorithm.Sha512)
            throw new ArgumentOutOfRangeException("digestAlgorithm");
        this.certificate = certificate;
        privateKey = null;
        this.digestAlgorithm = digestAlgorithm;
    }

    internal static EmailSmimeSigningCredential CreateImplementationWithCertificateAndDigestAlgorithm(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm)
    {
        return new EmailSmimeSigningCredential(certificate, digestAlgorithm);
    }

    internal static EmailSmimeSigningCredential CreateImplementationWithCertificateAndPrivateKeyAndDigestAlgorithm(X509Certificate2 certificate, AsymmetricAlgorithm privateKey, EmailSmimeDigestAlgorithm digestAlgorithm)
    {
        if (certificate == null)
            throw new ArgumentNullException("certificate");
        if (privateKey == null)
            throw new ArgumentNullException("privateKey");
        if (!(privateKey is RSA) && !(privateKey is ECDsa))
            throw new ArgumentException("Only RSA and ECDSA private keys are supported.", "privateKey");
        if (digestAlgorithm < EmailSmimeDigestAlgorithm.Sha1 || digestAlgorithm > EmailSmimeDigestAlgorithm.Sha512)
            throw new ArgumentOutOfRangeException("digestAlgorithm");
        EmailSmimeSigningCredential value = new EmailSmimeSigningCredential(certificate, digestAlgorithm);
        value.privateKey = privateKey;
        return value;
    }

    /// <summary>
    /// Gets the certificate.
    /// </summary>
    /// <returns>The current certificate.</returns>
    public X509Certificate2 GetCertificate()
    {
        return certificate;
    }

    /// <summary>
    /// Determines whether the private key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the private key is available; otherwise, <c>false</c>.</returns>
    public bool HasPrivateKey()
    {
        return privateKey != null;
    }

    /// <summary>
    /// Gets the private key.
    /// </summary>
    /// <returns>The current private key.</returns>
    public AsymmetricAlgorithm? GetPrivateKey()
    {
        return privateKey;
    }

    /// <summary>
    /// Gets the digest algorithm.
    /// </summary>
    /// <returns>The current digest algorithm.</returns>
    public EmailSmimeDigestAlgorithm GetDigestAlgorithm()
    {
        return digestAlgorithm;
    }
}
