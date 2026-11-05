using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace token;

/// <summary>
/// Provides registered key-use names used by <see cref="TokenKey"/>.
/// </summary>
public static class TokenKeyUses
{
    /// <summary>Signature or MAC use.</summary>
    public const string Signature = "sig";

    /// <summary>Encryption or key-management use.</summary>
    public const string Encryption = "enc";
}
