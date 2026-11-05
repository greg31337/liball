using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Represents a protected header security context.
/// </summary>
public class EmailProtectedHeaderSecurityContext : IEmailProtectedHeaderSecurityContextContract
{
    EmailProtectedHeaderProtocol protocol;
    bool encrypted;
    bool signatureValid;
    bool protectedFromSignerBound;
    /// <summary>
    /// Creates a new protected header security context.
    /// </summary>
    /// <param name = "protocol">The protocol used by the operation.</param>
    /// <param name = "encrypted">Whether to enable the encrypted behavior.</param>
    /// <param name = "signatureValid">Whether to enable the signature valid behavior.</param>
    /// <param name = "protectedFromSignerBound">Whether to enable the protected from signer bound behavior.</param>
    /// <exception cref = "ArgumentException">Thrown when an argument is empty, malformed, or inconsistent with the other inputs.</exception>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailProtectedHeaderSecurityContext(EmailProtectedHeaderProtocol protocol, bool encrypted, bool signatureValid, bool protectedFromSignerBound)
    {
        if (protocol < EmailProtectedHeaderProtocol.Smime || protocol > EmailProtectedHeaderProtocol.OpenPgpMime)
        {
            throw new ArgumentOutOfRangeException("protocol");
        }

        if (protectedFromSignerBound && !signatureValid)
        {
            throw new ArgumentException("A protected From field cannot be signer-bound without a valid signature.", "protectedFromSignerBound");
        }

        this.protocol = protocol;
        this.encrypted = encrypted;
        this.signatureValid = signatureValid;
        this.protectedFromSignerBound = protectedFromSignerBound;
    }

    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    public EmailProtectedHeaderProtocol GetProtocol()
    {
        return protocol;
    }

    /// <summary>
    /// Determines whether the current value or state is encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted; otherwise, <c>false</c>.</returns>
    public bool IsEncrypted()
    {
        return encrypted;
    }

    /// <summary>
    /// Determines whether the valid signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid signature is available; otherwise, <c>false</c>.</returns>
    public bool HasValidSignature()
    {
        return signatureValid;
    }

    /// <summary>
    /// Determines whether the current value or state is protected from signer bound.
    /// </summary>
    /// <returns><c>true</c> when the value or state is protected from signer bound; otherwise, <c>false</c>.</returns>
    public bool IsProtectedFromSignerBound()
    {
        return protectedFromSignerBound;
    }
}
