namespace argon2;

/// <summary>
/// Contains resource and algorithm limits applied to untrusted encoded hashes.
/// </summary>
public class Argon2VerificationPolicy
{
    /// <summary>
    /// The maximum accepted encoded-hash length in characters.
    /// </summary>
    public int MaximumEncodedHashLength = 4096;

    /// <summary>
    /// The maximum accepted requested memory size in KiB.
    /// </summary>
    public long MaximumMemorySizeKiB = Argon2Profiles.SecondRecommendedMemorySizeKiB;

    /// <summary>
    /// The maximum accepted number of iterations.
    /// </summary>
    public long MaximumIterations = Argon2Profiles.SecondRecommendedIterations;

    /// <summary>
    /// The maximum accepted degree of parallelism.
    /// </summary>
    public int MaximumDegreeOfParallelism = Argon2Profiles.SecondRecommendedDegreeOfParallelism;

    /// <summary>
    /// The maximum accepted salt length in bytes.
    /// </summary>
    public int MaximumSaltLength = 64;

    /// <summary>
    /// The maximum accepted tag length in bytes.
    /// </summary>
    public int MaximumTagLength = 1024;

    /// <summary>
    /// Indicates whether Argon2d hashes are accepted.
    /// </summary>
    public bool AllowVariantD = true;

    /// <summary>
    /// Indicates whether Argon2i hashes are accepted.
    /// </summary>
    public bool AllowVariantI = true;

    /// <summary>
    /// Indicates whether Argon2id hashes are accepted.
    /// </summary>
    public bool AllowVariantId = true;

    /// <summary>
    /// Indicates whether Argon2 version 1.0 hashes are accepted.
    /// </summary>
    public bool AllowVersion10 = true;

    /// <summary>
    /// Indicates whether Argon2 version 1.3 hashes are accepted.
    /// </summary>
    public bool AllowVersion13 = true;
}
