namespace uspto;

internal interface IRetryDelay
{
    void Wait(long milliseconds, global::System.Threading.CancellationToken cancellationToken);
}

internal interface IRetryRandom
{
    double NextUnit();
}

internal class RetryExecutionState
{
    internal int AttemptCount = 1;
    internal long TotalDelayMilliseconds = 0L;
    internal long LastDelayMilliseconds = 0L;
}

internal class TaskRetryDelay : IRetryDelay
{
    public void Wait(long milliseconds, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        global::System.Threading.Tasks.Task.Delay((int)milliseconds, cancellationToken).GetAwaiter().GetResult();
    }
}

internal class SharedRetryRandom : IRetryRandom
{
    public double NextUnit()
    {
        return global::System.Random.Shared.NextDouble();
    }
}

internal class RetryEngine
{
    internal RetryPolicy Policy = new RetryPolicy();
    internal IRetryDelay Delay = new TaskRetryDelay();
    internal IRetryRandom Random = new SharedRetryRandom();

    internal RetryEngine(RetryPolicy policy, IRetryDelay delay, IRetryRandom random)
    {
        if (policy == null)
        {
            throw new ValidationException("A retry policy is required.", "policy", "REQUIRED", null);
        }

        if (delay == null)
        {
            throw new ValidationException("A retry delay implementation is required.", "delay", "REQUIRED", null);
        }

        if (random == null)
        {
            throw new ValidationException("A retry random source is required.", "random", "REQUIRED", null);
        }

        if (policy.AttemptCount < 1)
        {
            throw new ValidationException("The retry attempt count must include at least the initial attempt.", "AttemptCount", "INVALID_RETRY_POLICY", null);
        }

        if (policy.InitialDelayMilliseconds < 0)
        {
            throw new ValidationException("The initial retry delay cannot be negative.", "InitialDelayMilliseconds", "INVALID_RETRY_POLICY", null);
        }

        if (policy.MaximumDelayMilliseconds < policy.InitialDelayMilliseconds)
        {
            throw new ValidationException("The maximum retry delay cannot be less than the initial delay.", "MaximumDelayMilliseconds", "INVALID_RETRY_POLICY", null);
        }

        if (policy.BudgetMilliseconds < 0)
        {
            throw new ValidationException("The retry delay budget cannot be negative.", "BudgetMilliseconds", "INVALID_RETRY_POLICY", null);
        }

        Policy = policy;
        Delay = delay;
        Random = random;
    }

    internal static RetryEngine CreateDefault(RetryPolicy policy)
    {
        return new RetryEngine(policy, new TaskRetryDelay(), new SharedRetryRandom());
    }

    internal bool TryWaitForRetry(RetryExecutionState state, global::System.Net.Http.HttpRequestMessage request, bool isSearchOperation, global::System.Exception failure, global::System.Threading.CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ValidationException("Retry execution state is required.", "state", "REQUIRED", null);
        }

        if (state.AttemptCount < 1 || state.TotalDelayMilliseconds < 0L)
        {
            throw new ValidationException("Retry execution state is invalid.", "state", "INVALID_RETRY_STATE", null);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (state.AttemptCount >= Policy.AttemptCount)
        {
            return false;
        }

        if (!RetryClassifier.ShouldRetry(request, isSearchOperation, failure, cancellationToken))
        {
            return false;
        }

        long delayMilliseconds = CalculateDelayMilliseconds(state, failure);
        long remainingBudget = Policy.BudgetMilliseconds - state.TotalDelayMilliseconds;
        if (remainingBudget < 0L || delayMilliseconds > remainingBudget)
        {
            return false;
        }

        Delay.Wait(delayMilliseconds, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        state.LastDelayMilliseconds = delayMilliseconds;
        state.TotalDelayMilliseconds += delayMilliseconds;
        state.AttemptCount += 1;
        return true;
    }

    internal long CalculateDelayMilliseconds(RetryExecutionState state, global::System.Exception failure)
    {
        RetryAfterValue serverDelay = ExtractServerDelay(failure);
        if (Policy.HonorRetryAfter && serverDelay.HasValue)
        {
            long boundedServerDelay = serverDelay.Milliseconds;
            if (boundedServerDelay < 0L)
            {
                boundedServerDelay = 0L;
            }

            if (boundedServerDelay > Policy.MaximumDelayMilliseconds)
            {
                boundedServerDelay = Policy.MaximumDelayMilliseconds;
            }

            return boundedServerDelay;
        }

        long clientDelay = CalculateExponentialDelay(state);
        if (Policy.UseJitter)
        {
            clientDelay = ApplyJitter(clientDelay);
        }

        return clientDelay;
    }

    internal long CalculateExponentialDelay(RetryExecutionState state)
    {
        long delayMilliseconds = Policy.InitialDelayMilliseconds;
        for (int retryIndex = 1; retryIndex < state.AttemptCount; retryIndex += 1)
        {
            if (delayMilliseconds >= Policy.MaximumDelayMilliseconds || delayMilliseconds > global::System.Int64.MaxValue / 2L)
            {
                delayMilliseconds = Policy.MaximumDelayMilliseconds;
                break;
            }

            delayMilliseconds *= 2L;
        }

        if (delayMilliseconds > Policy.MaximumDelayMilliseconds)
        {
            delayMilliseconds = Policy.MaximumDelayMilliseconds;
        }

        return delayMilliseconds;
    }

    internal long ApplyJitter(long delayMilliseconds)
    {
        if (delayMilliseconds <= 1L)
        {
            return delayMilliseconds;
        }

        double unit = Random.NextUnit();
        if (global::System.Double.IsNaN(unit) || unit < 0.0)
        {
            unit = 0.0;
        }
        else if (unit >= 1.0)
        {
            unit = 0.9999999999999999;
        }

        long minimum = delayMilliseconds / 2L;
        long range = delayMilliseconds - minimum;
        long jitter = (long)global::System.Math.Floor(unit * (range + 1L));
        return minimum + jitter;
    }

    internal RetryAfterValue ExtractServerDelay(global::System.Exception failure)
    {
        RetryAfterValue result = new RetryAfterValue();
        if (RetryClassifier.HasExceptionType(failure, "uspto.RateLimitException"))
        {
            RateLimitException rateFailure = (RateLimitException)failure;
            result.Milliseconds = rateFailure.RetryAfterMilliseconds;
            result.HasValue = rateFailure.HasRetryAfterMilliseconds;
        }
        else if (RetryClassifier.HasExceptionType(failure, "uspto.ServiceUnavailableException"))
        {
            ServiceUnavailableException serviceFailure = (ServiceUnavailableException)failure;
            result.Milliseconds = serviceFailure.RetryAfterMilliseconds;
            result.HasValue = serviceFailure.HasRetryAfterMilliseconds;
        }

        return result;
    }
}
