using System;
using System.Security.Cryptography;

namespace token;

/// <summary>
/// Provides key-type constants for symmetric key import operations.
/// </summary>
public static class SymmetricKeyImporter
{
    /// <summary>The default minimum symmetric key size in bytes.</summary>
    public const int MinimumKeySizeInBytes = 16;
}
