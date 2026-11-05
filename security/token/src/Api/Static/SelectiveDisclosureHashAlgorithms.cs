using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace token;

/// <summary>
/// Provides hash algorithm identifiers supported for SD-JWT Disclosure digests.
/// </summary>
public static class SelectiveDisclosureHashAlgorithms
{
    /// <summary>The SHA-256 hash algorithm identifier.</summary>
    public const string Sha256 = "sha-256";

    /// <summary>The SHA-384 hash algorithm identifier.</summary>
    public const string Sha384 = "sha-384";

    /// <summary>The SHA-512 hash algorithm identifier.</summary>
    public const string Sha512 = "sha-512";
}
