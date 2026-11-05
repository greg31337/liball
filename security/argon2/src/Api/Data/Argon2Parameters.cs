namespace argon2;

/// <summary>
/// Contains the algorithm parameters of an Argon2 operation.
/// </summary>
public class Argon2Parameters
{
    /// <summary>
    /// The Argon2 algorithm variant.
    /// </summary>
    public Argon2Variant Variant = Argon2Profiles.SecondRecommendedVariant;

    /// <summary>
    /// The Argon2 algorithm version.
    /// </summary>
    public Argon2Version Version = Argon2Profiles.SecondRecommendedVersion;

    /// <summary>
    /// The number of passes over memory.
    /// </summary>
    public long Iterations = Argon2Profiles.SecondRecommendedIterations;

    /// <summary>
    /// The requested memory size in KiB.
    /// </summary>
    public long MemorySizeKiB = Argon2Profiles.SecondRecommendedMemorySizeKiB;

    /// <summary>
    /// The number of Argon2 lanes.
    /// </summary>
    public int DegreeOfParallelism = Argon2Profiles.SecondRecommendedDegreeOfParallelism;
}
