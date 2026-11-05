using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>
/// Provides limits for JOSE ECDH-ES Concat KDF operations.
/// </summary>
public static class ConcatKdf
{
    /// <summary>The maximum derived-key size accepted by this implementation.</summary>
    public const int MaximumKeySizeInBits = 65536;
}
