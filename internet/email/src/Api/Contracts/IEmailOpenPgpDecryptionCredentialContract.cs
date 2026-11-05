using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpDecryptionCredential"/>.
/// </summary>
public interface IEmailOpenPgpDecryptionCredentialContract
{
    /// <summary>
    /// Gets the key version.
    /// </summary>
    /// <returns>The current key version.</returns>
    int GetKeyVersion();
    /// <summary>
    /// Gets the fingerprint.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current fingerprint.</returns>
    byte[] GetFingerprint();
    /// <summary>
    /// Gets the private key.
    /// </summary>
    /// <returns>The current private key.</returns>
    RSA GetPrivateKey();
    /// <summary>
    /// Gets the key identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current key identifier.</returns>
    byte[] GetKeyIdentifier();
}
