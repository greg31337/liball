using System;
using System.Collections.Generic;
using System.Text;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailProtectedHeaderSecurityContext"/>.
/// </summary>
public interface IEmailProtectedHeaderSecurityContextContract
{
    /// <summary>
    /// Gets the protocol.
    /// </summary>
    /// <returns>The current protocol.</returns>
    EmailProtectedHeaderProtocol GetProtocol();
    /// <summary>
    /// Determines whether the current value or state is encrypted.
    /// </summary>
    /// <returns><c>true</c> when the value or state is encrypted; otherwise, <c>false</c>.</returns>
    bool IsEncrypted();
    /// <summary>
    /// Determines whether the valid signature is present or available.
    /// </summary>
    /// <returns><c>true</c> when the valid signature is available; otherwise, <c>false</c>.</returns>
    bool HasValidSignature();
    /// <summary>
    /// Determines whether the current value or state is protected from signer bound.
    /// </summary>
    /// <returns><c>true</c> when the value or state is protected from signer bound; otherwise, <c>false</c>.</returns>
    bool IsProtectedFromSignerBound();
}
