using System;
using System.Security.Cryptography;
using System.Text;

namespace token;

/// <summary>
/// Provides limits for JWE PBES2-HMAC-SHA2 key-management operations.
/// </summary>
public static class Pbes2Algorithms
{
    /// <summary>The minimum accepted JWE PBES2 salt-input size in bytes.</summary>
    public const int MinimumSaltInputSize = 8;

    /// <summary>The generated JWE PBES2 salt-input size in bytes.</summary>
    public const int GeneratedSaltInputSize = 16;

    /// <summary>The largest accepted JWE PBES2 salt-input size in bytes.</summary>
    public const int MaximumSaltInputSize = 1024;

    /// <summary>The minimum accepted PBKDF2 iteration count.</summary>
    public const int MinimumIterationCount = 1000;

    /// <summary>The default PBKDF2 iteration count used by wrapping operations.</summary>
    public const int DefaultIterationCount = 100000;

    /// <summary>The maximum accepted PBKDF2 iteration count.</summary>
    public const int MaximumIterationCount = 1000000;
}
