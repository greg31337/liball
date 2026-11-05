using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpSessionKeyPacket"/>.
/// </summary>
public interface IEmailOpenPgpSessionKeyPacketContract
{
    /// <summary>
    /// Gets the version.
    /// </summary>
    /// <returns>The current version.</returns>
    int GetVersion();
    /// <summary>
    /// Determines whether the current value or state is public key encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is public key encrypted; otherwise, <c>false</c>.</returns>
    bool IsPublicKeyEncrypted();
    /// <summary>
    /// Gets the public key algorithm.
    /// </summary>
    /// <returns>The current public key algorithm.</returns>
    int GetPublicKeyAlgorithm();
    /// <summary>
    /// Gets the symmetric algorithm.
    /// </summary>
    /// <returns>The current symmetric algorithm.</returns>
    int GetSymmetricAlgorithm();
    /// <summary>
    /// Gets the aead algorithm.
    /// </summary>
    /// <returns>The current aead algorithm.</returns>
    int GetAeadAlgorithm();
    /// <summary>
    /// Gets the recipient identifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current recipient identifier.</returns>
    byte[] GetRecipientIdentifier();
    /// <summary>
    /// Gets the string to key specifier.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current string to key specifier.</returns>
    byte[] GetStringToKeySpecifier();
    /// <summary>
    /// Gets the encrypted session key.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current encrypted session key.</returns>
    byte[] GetEncryptedSessionKey();
}
