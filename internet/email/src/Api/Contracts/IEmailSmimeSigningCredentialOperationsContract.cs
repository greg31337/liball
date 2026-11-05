using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeSigningCredential"/>.
/// </summary>
public interface IEmailSmimeSigningCredentialOperationsContract
{
    /// <summary>Creates an S/MIME signing credential that uses the certificate's private key.</summary>
    /// <param name = "certificate">The signing certificate.</param>
    /// <param name = "digestAlgorithm">The digest algorithm.</param>
    /// <returns>The configured signing credential.</returns>
    EmailSmimeSigningCredential CreateWithCertificateAndDigestAlgorithm(X509Certificate2 certificate, EmailSmimeDigestAlgorithm digestAlgorithm);
    /// <summary>
    /// Creates a new S/MIME signing credential.
    /// </summary>
    /// <param name = "certificate">The certificate used by the operation.</param>
    /// <param name = "privateKey">The private key used by the operation.</param>
    /// <param name = "digestAlgorithm">The digest algorithm used by the operation.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    /// <returns>The configured signing credential.</returns>
    EmailSmimeSigningCredential CreateWithCertificateAndPrivateKeyAndDigestAlgorithm(X509Certificate2 certificate, AsymmetricAlgorithm privateKey, EmailSmimeDigestAlgorithm digestAlgorithm);
}
