using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailSmimeDecryptionCredential"/>.
/// </summary>
public interface IEmailSmimeDecryptionCredentialContract
{
    /// <summary>
    /// Gets the certificate.
    /// </summary>
    /// <returns>The current certificate.</returns>
    X509Certificate2 GetCertificate();
    /// <summary>
    /// Determines whether the private key is present or available.
    /// </summary>
    /// <returns><c>true</c> when the private key is available; otherwise, <c>false</c>.</returns>
    bool HasPrivateKey();
    /// <summary>
    /// Gets the private key.
    /// </summary>
    /// <returns>The current private key.</returns>
    RSA? GetPrivateKey();
}
