namespace uspto;

/// <summary>
/// Configures retry attempts and their total time budget.
/// </summary>
public class RetryPolicy
{
    /// <summary>The total permitted HTTP attempts, including the initial attempt.</summary>
    public int AttemptCount = UsptoDefaults.HttpAttemptCount;

    /// <summary>The initial exponential-backoff delay in milliseconds.</summary>
    public int InitialDelayMilliseconds = UsptoDefaults.InitialRetryDelayMilliseconds;

    /// <summary>The maximum delay before one retry in milliseconds.</summary>
    public int MaximumDelayMilliseconds = UsptoDefaults.MaximumRetryDelayMilliseconds;

    /// <summary>The total retry delay budget in milliseconds.</summary>
    public int BudgetMilliseconds = UsptoDefaults.RetryBudgetMilliseconds;

    /// <summary>Whether bounded random jitter is applied to client-selected delays.</summary>
    public bool UseJitter = true;

    /// <summary>Whether a valid server <c>Retry-After</c> value is honored within the configured limits.</summary>
    public bool HonorRetryAfter = true;
}
