using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Defines the public operations implemented by <see cref = "EmailOpenPgpSigningCredential"/>.
/// </summary>
public interface IEmailOpenPgpSigningCredentialOperationsContract
{
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    byte[] Copy(byte[] value);
}
