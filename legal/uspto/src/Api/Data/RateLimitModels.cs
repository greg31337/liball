namespace uspto;

/// <summary>
/// Configures one independent service rate bucket.
/// </summary>
public class RateLimitPolicy
{
    /// <summary>The service or service bucket governed by this policy.</summary>
    public ApiService Service = ApiService.Unknown;

    /// <summary>The permitted request count in one window.</summary>
    public int RequestsPerWindow = 0;

    /// <summary>The rate window length in milliseconds.</summary>
    public int WindowMilliseconds = 60000;

    /// <summary>An optional maximum number of callers that may wait for this bucket.</summary>
    public int QueueCapacity = 0;

    /// <summary>Whether <see cref="QueueCapacity"/> contains a configured limit.</summary>
    public bool HasQueueCapacity = false;

    /// <summary>Whether response rate headers may refine the locally configured bucket.</summary>
    public bool ObserveServerHeaders = true;
}
