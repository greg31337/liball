namespace argon2;

/// <summary>
/// Defines the two parameter profiles recommended by RFC 9106.
/// </summary>
public static class Argon2Profiles
{
    /// <summary>
    /// The variant used by the first recommended profile.
    /// </summary>
    public const Argon2Variant FirstRecommendedVariant = Argon2Variant.Id;

    /// <summary>
    /// The version used by the first recommended profile.
    /// </summary>
    public const Argon2Version FirstRecommendedVersion = Argon2Version.Version13;

    /// <summary>
    /// The iteration count used by the first recommended profile.
    /// </summary>
    public const long FirstRecommendedIterations = 1L;

    /// <summary>
    /// The memory size in KiB used by the first recommended profile.
    /// </summary>
    public const long FirstRecommendedMemorySizeKiB = 2097152L;

    /// <summary>
    /// The degree of parallelism used by the first recommended profile.
    /// </summary>
    public const int FirstRecommendedDegreeOfParallelism = 4;

    /// <summary>
    /// The salt length in bytes used by the first recommended profile.
    /// </summary>
    public const int FirstRecommendedSaltLength = 16;

    /// <summary>
    /// The tag length in bytes used by the first recommended profile.
    /// </summary>
    public const int FirstRecommendedTagLength = 32;

    /// <summary>
    /// The variant used by the second recommended profile.
    /// </summary>
    public const Argon2Variant SecondRecommendedVariant = Argon2Variant.Id;

    /// <summary>
    /// The version used by the second recommended profile.
    /// </summary>
    public const Argon2Version SecondRecommendedVersion = Argon2Version.Version13;

    /// <summary>
    /// The iteration count used by the second recommended profile.
    /// </summary>
    public const long SecondRecommendedIterations = 3L;

    /// <summary>
    /// The memory size in KiB used by the second recommended profile.
    /// </summary>
    public const long SecondRecommendedMemorySizeKiB = 65536L;

    /// <summary>
    /// The degree of parallelism used by the second recommended profile.
    /// </summary>
    public const int SecondRecommendedDegreeOfParallelism = 4;

    /// <summary>
    /// The salt length in bytes used by the second recommended profile.
    /// </summary>
    public const int SecondRecommendedSaltLength = 16;

    /// <summary>
    /// The tag length in bytes used by the second recommended profile.
    /// </summary>
    public const int SecondRecommendedTagLength = 32;
}
