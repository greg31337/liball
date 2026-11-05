namespace uspto;

/// <summary>
/// Defines a digest algorithm that may be supplied by a bulk catalog.
/// </summary>
public enum ChecksumAlgorithm
{
    /// <summary>No checksum algorithm is available.</summary>
    None = 0,

    /// <summary>The SHA-256 digest algorithm.</summary>
    Sha256 = 1,

    /// <summary>The SHA-384 digest algorithm.</summary>
    Sha384 = 2,

    /// <summary>The SHA-512 digest algorithm.</summary>
    Sha512 = 3,

    /// <summary>The legacy SHA-1 digest algorithm.</summary>
    Sha1 = 4,

    /// <summary>The legacy MD5 digest algorithm.</summary>
    Md5 = 5
}
