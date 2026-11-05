using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpVerificationKey"/>.
/// </summary>
public interface IEmailOpenPgpVerificationKeyContract
{
    /// <summary>
    /// Gets the identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current identifier.</returns>
    byte[] GetIdentifier();
    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    int GetPublicKeyAlgorithm();
    /// <summary>
    /// Gets the public key.
    /// </summary>
    /// <returns>The current public key.</returns>
    AsymmetricAlgorithm GetPublicKey();
}
