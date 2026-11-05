namespace sdp;

/// <summary>
/// Defines hash-function tokens registered for SDP certificate fingerprints.
/// </summary>
public static class SdpFingerprintAlgorithms
{
    /// <summary>The SHA-1 hash-function token.</summary>
    public const string Sha1 = "sha-1";

    /// <summary>The SHA-224 hash-function token.</summary>
    public const string Sha224 = "sha-224";

    /// <summary>The SHA-256 hash-function token.</summary>
    public const string Sha256 = "sha-256";

    /// <summary>The SHA-384 hash-function token.</summary>
    public const string Sha384 = "sha-384";

    /// <summary>The SHA-512 hash-function token.</summary>
    public const string Sha512 = "sha-512";
}
