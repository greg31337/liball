using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Provides limits for RSA key import operations.
/// </summary>
public static class RsaKeyImporter
{
    /// <summary>The default minimum accepted RSA modulus size in bits.</summary>
    public const int MinimumKeySizeInBits = 2048;
}
