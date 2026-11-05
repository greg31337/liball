using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Represents a S/MIME decryption credential.
/// </summary>
public class EmailSmimeDecryptionCredential : IEmailSmimeDecryptionCredentialContract
{
    X509Certificate2 certificate;
    RSA? privateKey;
    /// <summary>
    /// Creates an S/MIME decryption credential with an explicit private key.
    /// </summary>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailSmimeDecryptionCredential(X509Certificate2 certificate)
    {
        if (certificate == null)
            throw new ArgumentNullException("certificate");
        this.certificate = certificate;
        privateKey = null;
    }

    internal static EmailSmimeDecryptionCredential CreateImplementationWithCertificate(X509Certificate2 certificate)
    {
        return new EmailSmimeDecryptionCredential(certificate);
    }

    internal static EmailSmimeDecryptionCredential CreateImplementationWithCertificateAndPrivateKey(X509Certificate2 certificate, RSA privateKey)
    {
        if (certificate == null)
            throw new ArgumentNullException("certificate");
        if (privateKey == null)
            throw new ArgumentNullException("privateKey");
        EmailSmimeDecryptionCredential value = new EmailSmimeDecryptionCredential(certificate);
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
    public RSA? GetPrivateKey()
    {
        return privateKey;
    }
}
