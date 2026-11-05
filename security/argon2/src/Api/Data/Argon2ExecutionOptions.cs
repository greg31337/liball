namespace argon2;

/// <summary>
/// Contains execution controls that do not change the derived Argon2 value.
/// </summary>
public class Argon2ExecutionOptions
{
    /// <summary>
    /// The maximum number of worker threads used by an operation.
    /// </summary>
    public int MaximumWorkerCount = Argon2Profiles.SecondRecommendedDegreeOfParallelism;

    /// <summary>
    /// Indicates whether owned cryptographic memory is cleared after an operation.
    /// </summary>
    public bool ClearMemory = true;
}
