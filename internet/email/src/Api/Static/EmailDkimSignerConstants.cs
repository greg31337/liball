using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace email;
/// <summary>
/// Exposes the public constants associated with this API.
/// </summary>
public static class EmailDkimSignerConstants
{
    /// <summary>
    /// The RSA sha 1 prohibited reason value exposed by this API.
    /// </summary>
    public const string RsaSha1ProhibitedReason = "RSA-SHA1 DKIM signing is prohibited because SHA-1 is a historic DKIM algorithm and new signatures must use a modern algorithm.";
    /// <summary>
    /// The ed 25519 sha 256 unsupported reason value exposed by this API.
    /// </summary>
    public const string Ed25519Sha256UnsupportedReason = "Ed25519-SHA256 DKIM signing is unavailable because the target .NET 10 runtime does not expose a built-in Ed25519 signing primitive, and this library does not add an external cryptography dependency.";
}
