namespace argon2;

/// <summary>
/// Defines structural constants and input bounds for Argon2 operations.
/// </summary>
public static class Argon2Limits
{
    /// <summary>
    /// The size of one Argon2 memory block in bytes.
    /// </summary>
    public const int BlockSizeInBytes = 1024;

    /// <summary>
    /// The number of 64-bit words in one Argon2 memory block.
    /// </summary>
    public const int WordsPerBlock = 128;

    /// <summary>
    /// The number of synchronization points in each pass.
    /// </summary>
    public const int SynchronizationPoints = 4;

    /// <summary>
    /// The number of blocks initialized before segment filling in each lane.
    /// </summary>
    public const int InitialBlocksPerLane = 2;

    /// <summary>
    /// The length of the Argon2 initial digest in bytes.
    /// </summary>
    public const int PrehashDigestLength = 64;

    /// <summary>
    /// The length of the initial digest plus its block and lane suffix in bytes.
    /// </summary>
    public const int PrehashSeedLength = 72;

    /// <summary>
    /// The BLAKE2b input block size in bytes.
    /// </summary>
    public const int Blake2bBlockSizeInBytes = 128;

    /// <summary>
    /// The maximum BLAKE2b digest length in bytes.
    /// </summary>
    public const int Blake2bMaximumDigestLength = 64;

    /// <summary>
    /// The minimum Argon2 tag length in bytes.
    /// </summary>
    public const int MinimumTagLength = 4;

    /// <summary>
    /// The maximum RFC-representable Argon2 tag length in bytes.
    /// </summary>
    public const long MaximumTagLength = 4294967295L;

    /// <summary>
    /// The minimum interoperable salt length in bytes.
    /// </summary>
    public const int MinimumSaltLength = 8;

    /// <summary>
    /// The maximum RFC-representable salt length in bytes.
    /// </summary>
    public const long MaximumSaltLength = 4294967295L;

    /// <summary>
    /// The minimum password length in bytes.
    /// </summary>
    public const int MinimumPasswordLength = 0;

    /// <summary>
    /// The maximum RFC-representable password length in bytes.
    /// </summary>
    public const long MaximumPasswordLength = 4294967295L;

    /// <summary>
    /// The minimum secret length in bytes.
    /// </summary>
    public const int MinimumSecretLength = 0;

    /// <summary>
    /// The maximum RFC-representable secret length in bytes.
    /// </summary>
    public const long MaximumSecretLength = 4294967295L;

    /// <summary>
    /// The minimum associated-data length in bytes.
    /// </summary>
    public const int MinimumAssociatedDataLength = 0;

    /// <summary>
    /// The maximum RFC-representable associated-data length in bytes.
    /// </summary>
    public const long MaximumAssociatedDataLength = 4294967295L;

    /// <summary>
    /// The minimum number of iterations.
    /// </summary>
    public const long MinimumIterations = 1L;

    /// <summary>
    /// The maximum RFC-representable number of iterations.
    /// </summary>
    public const long MaximumIterations = 4294967295L;

    /// <summary>
    /// The minimum memory size in KiB required per lane.
    /// </summary>
    public const long MinimumMemorySizePerLaneKiB = 8L;

    /// <summary>
    /// The maximum RFC-representable memory size in KiB.
    /// </summary>
    public const long MaximumMemorySizeKiB = 4294967295L;

    /// <summary>
    /// The minimum degree of parallelism.
    /// </summary>
    public const int MinimumDegreeOfParallelism = 1;

    /// <summary>
    /// The maximum degree of parallelism.
    /// </summary>
    public const int MaximumDegreeOfParallelism = 16777215;

    /// <summary>
    /// The minimum worker count.
    /// </summary>
    public const int MinimumWorkerCount = 1;

    /// <summary>
    /// The maximum worker count.
    /// </summary>
    public const int MaximumWorkerCount = 16777215;
}
