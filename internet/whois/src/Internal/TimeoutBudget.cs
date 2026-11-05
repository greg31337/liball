namespace whois;

internal class TimeoutBudget
{
    System.TimeProvider clock;
    System.Threading.CancellationToken callerCancellationToken;
    System.TimeSpan resolveTimeout;
    System.TimeSpan connectTimeout;
    System.TimeSpan writeTimeout;
    System.TimeSpan readIdleTimeout;
    System.TimeSpan overallTimeout;
    long overallStartedTimestamp;
    long stageStartedTimestamp;
    WhoisOperationStage currentStage;

    internal TimeoutBudget(WhoisTimeoutOptions timeouts, System.Threading.CancellationToken cancellationToken, System.TimeProvider timeProvider)
    {
        InputGuard.RequireReference(timeouts, "timeouts");
        InputGuard.RequireReference(timeProvider, "timeProvider");
        InputGuard.ValidateTimeouts(timeouts, string.Empty, WhoisQueryKind.FreeForm);

        clock = timeProvider;
        callerCancellationToken = cancellationToken;
        resolveTimeout = timeouts.Resolve;
        connectTimeout = timeouts.Connect;
        writeTimeout = timeouts.Write;
        readIdleTimeout = timeouts.ReadIdle;
        overallTimeout = timeouts.Overall;
        overallStartedTimestamp = clock.GetTimestamp();
        stageStartedTimestamp = overallStartedTimestamp;
        currentStage = WhoisOperationStage.None;
    }

    internal void BeginStage(WhoisOperationStage stage)
    {
        ValidateStage(stage);
        currentStage = stage;
        stageStartedTimestamp = clock.GetTimestamp();
    }

    internal void ResetReadIdle()
    {
        if (currentStage != WhoisOperationStage.Read)
        {
            throw new System.InvalidOperationException("The read-idle deadline can only be reset during the read stage.");
        }
        stageStartedTimestamp = clock.GetTimestamp();
    }

    internal System.TimeSpan GetRemainingTime()
    {
        long now = clock.GetTimestamp();
        System.TimeSpan overallRemaining = System.Threading.Timeout.InfiniteTimeSpan;
        if (overallTimeout != System.Threading.Timeout.InfiniteTimeSpan)
        {
            overallRemaining = GetRemainingDuration(overallTimeout, overallStartedTimestamp, now);
        }

        if (currentStage == WhoisOperationStage.None)
        {
            return overallRemaining;
        }

        System.TimeSpan stageRemaining = GetRemainingDuration(GetStageDuration(), stageStartedTimestamp, now);
        if (overallRemaining == System.Threading.Timeout.InfiniteTimeSpan || stageRemaining < overallRemaining)
        {
            return stageRemaining;
        }
        return overallRemaining;
    }

    internal int GetRemainingMilliseconds()
    {
        return ToMilliseconds(GetRemainingTime());
    }

    internal WhoisExchangeStatus GetInterruptionStatus()
    {
        if (callerCancellationToken.IsCancellationRequested)
        {
            return WhoisExchangeStatus.Cancelled;
        }

        System.TimeSpan remaining = GetRemainingTime();
        if (remaining != System.Threading.Timeout.InfiniteTimeSpan && remaining <= System.TimeSpan.Zero)
        {
            return WhoisExchangeStatus.TimedOut;
        }
        return WhoisExchangeStatus.Complete;
    }

    internal static int ToMilliseconds(System.TimeSpan timeout)
    {
        if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
        {
            return System.Threading.Timeout.Infinite;
        }
        if (timeout <= System.TimeSpan.Zero)
        {
            return 0;
        }
        if (timeout >= System.TimeSpan.FromMilliseconds(2147483647))
        {
            return 2147483647;
        }

        long milliseconds = (timeout.Ticks + System.TimeSpan.TicksPerMillisecond - 1) / System.TimeSpan.TicksPerMillisecond;
        return (int)milliseconds;
    }

    internal void ValidateStage(WhoisOperationStage stage)
    {
        if (stage != WhoisOperationStage.Resolve && stage != WhoisOperationStage.Connect && stage != WhoisOperationStage.Write && stage != WhoisOperationStage.Read)
        {
            throw new System.ArgumentOutOfRangeException("stage");
        }
    }

    System.TimeSpan GetStageDuration()
    {
        switch (currentStage)
        {
            case WhoisOperationStage.Resolve:
                return resolveTimeout;
            case WhoisOperationStage.Connect:
                return connectTimeout;
            case WhoisOperationStage.Write:
                return writeTimeout;
            case WhoisOperationStage.Read:
                return readIdleTimeout;
            default:
                throw new System.InvalidOperationException("A timed operation stage has not been started.");
        }
    }

    System.TimeSpan GetRemainingDuration(System.TimeSpan duration, long startedTimestamp, long currentTimestamp)
    {
        System.TimeSpan elapsed = clock.GetElapsedTime(startedTimestamp, currentTimestamp);
        if (elapsed <= System.TimeSpan.Zero)
        {
            return duration;
        }
        if (elapsed >= duration)
        {
            return System.TimeSpan.Zero;
        }
        return duration - elapsed;
    }
}
