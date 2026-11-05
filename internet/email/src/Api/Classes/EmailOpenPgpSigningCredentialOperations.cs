using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Provides the public factory and type-level operations associated with <see cref = "EmailOpenPgpSigningCredential"/>.
/// </summary>
public class EmailOpenPgpSigningCredentialOperations : IEmailOpenPgpSigningCredentialOperationsContract
{
    /// <summary>
    /// Copies the value to a separate destination.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns>A new array containing the produced bytes or values.</returns>
    public byte[] Copy(byte[] value)
    {
        return EmailOpenPgpSigningCredential.CopyImplementation(value);
    }
}
