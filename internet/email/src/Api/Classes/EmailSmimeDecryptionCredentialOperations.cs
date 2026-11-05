using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailSmimeDecryptionCredential"/>.
/// </summary>
public class EmailSmimeDecryptionCredentialOperations : IEmailSmimeDecryptionCredentialOperationsContract
{
    /// <summary>Creates an S/MIME decryption credential that uses the certificate's private key.</summary>
    /// <param name = "certificate">The recipient certificate.</param>
    /// <returns>The configured decryption credential.</returns>
    public EmailSmimeDecryptionCredential CreateWithCertificate(X509Certificate2 certificate)
    {
        return EmailSmimeDecryptionCredential.CreateImplementationWithCertificate(certificate);
    }

    /// <summary>
    /// Creates a new S/MIME decryption credential.
    /// </summary>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <returns>The configured decryption credential.</returns>
    public EmailSmimeDecryptionCredential CreateWithCertificateAndPrivateKey(X509Certificate2 certificate, RSA privateKey)
    {
        return EmailSmimeDecryptionCredential.CreateImplementationWithCertificateAndPrivateKey(certificate, privateKey);
    }
}
