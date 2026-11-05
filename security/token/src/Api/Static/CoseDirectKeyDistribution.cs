using System;
using System.Security.Cryptography;

namespace token;

/// <summary>Provides constants for COSE direct and direct-with-HKDF key distribution.</summary>
public static class CoseDirectKeyDistribution
{
    /// <summary>The maximum derived-key size accepted by this implementation.</summary>
    public const int MaximumKeySizeInBits = 65536;
}
