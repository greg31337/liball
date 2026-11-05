namespace tftp;

internal class TftpRetransmissionTimer
{
    internal ITftpTimeSource TimeSource;
    internal long TransferStartedMilliseconds;
    internal long IntervalStartedMilliseconds;
    internal long InitialIntervalMilliseconds;
    internal long CurrentIntervalMilliseconds;
    internal long MaximumIntervalMilliseconds;
    internal long MaximumDurationMilliseconds;
    internal int MaximumRetries;
    internal int ConsecutiveRetransmissionCount = 0;
    internal int TotalRetransmissionCount = 0;

    internal TftpRetransmissionTimer(
        ITftpTimeSource timeSource,
        int initialTimeoutSeconds,
        int maximumTimeoutSeconds,
        int maximumRetries,
        int maximumDurationSeconds,
        bool hasNegotiatedTimeout,
        int negotiatedTimeoutSeconds)
    {
        if (timeSource == null)
        {
            throw new TftpProtocolException(
                "The retransmission time source cannot be null.",
                false,
                0);
        }

        if (initialTimeoutSeconds < 1)
        {
            throw new TftpProtocolException(
                "The initial retransmission timeout must be positive.",
                false,
                0);
        }

        if (maximumTimeoutSeconds < initialTimeoutSeconds)
        {
            throw new TftpProtocolException(
                "The maximum retransmission timeout cannot be below the initial timeout.",
                false,
                0);
        }

        if (maximumRetries < 0)
        {
            throw new TftpProtocolException(
                "The maximum retransmission count cannot be negative.",
                false,
                0);
        }

        if (maximumDurationSeconds < 1)
        {
            throw new TftpProtocolException(
                "The maximum transfer duration must be positive.",
                false,
                0);
        }

        if (hasNegotiatedTimeout &&
            (negotiatedTimeoutSeconds < TftpProtocolValues.MinimumTimeoutSeconds ||
            negotiatedTimeoutSeconds > TftpProtocolValues.MaximumTimeoutSeconds))
        {
            throw new TftpProtocolException(
                "The negotiated retransmission timeout is outside the RFC 2349 range.",
                false,
                0);
        }

        int effectiveInitialSeconds = initialTimeoutSeconds;
        if (hasNegotiatedTimeout)
        {
            effectiveInitialSeconds = negotiatedTimeoutSeconds;
        }

        int effectiveMaximumSeconds = maximumTimeoutSeconds;
        if (effectiveMaximumSeconds < effectiveInitialSeconds)
        {
            effectiveMaximumSeconds = effectiveInitialSeconds;
        }

        TimeSource = timeSource;
        InitialIntervalMilliseconds = effectiveInitialSeconds * 1000L;
        CurrentIntervalMilliseconds = InitialIntervalMilliseconds;
        MaximumIntervalMilliseconds = effectiveMaximumSeconds * 1000L;
        MaximumDurationMilliseconds = maximumDurationSeconds * 1000L;
        MaximumRetries = maximumRetries;
        long now = TimeSource.GetTimestampMilliseconds();
        TransferStartedMilliseconds = now;
        IntervalStartedMilliseconds = now;
    }

    internal TftpRetransmissionResult EvaluateTimer()
    {
        long now = TimeSource.GetTimestampMilliseconds();
        long totalElapsed = GetNonnegativeElapsed(
            TransferStartedMilliseconds,
            now);
        if (totalElapsed >= MaximumDurationMilliseconds)
        {
            return CreateTimerResult(
                TftpRetransmissionAction.TotalDurationReached,
                0,
                totalElapsed);
        }

        long intervalElapsed = GetNonnegativeElapsed(
            IntervalStartedMilliseconds,
            now);
        if (intervalElapsed < CurrentIntervalMilliseconds)
        {
            long intervalRemaining = CurrentIntervalMilliseconds - intervalElapsed;
            long totalRemaining = MaximumDurationMilliseconds - totalElapsed;
            long waitMilliseconds = intervalRemaining;
            if (waitMilliseconds > totalRemaining)
            {
                waitMilliseconds = totalRemaining;
            }

            return CreateTimerResult(
                TftpRetransmissionAction.Wait,
                waitMilliseconds,
                totalElapsed);
        }

        if (ConsecutiveRetransmissionCount >= MaximumRetries)
        {
            return CreateTimerResult(
                TftpRetransmissionAction.RetryLimitReached,
                0,
                totalElapsed);
        }

        ConsecutiveRetransmissionCount = ConsecutiveRetransmissionCount + 1;
        TotalRetransmissionCount = TotalRetransmissionCount + 1;
        IntervalStartedMilliseconds = now;
        if (CurrentIntervalMilliseconds >= MaximumIntervalMilliseconds / 2)
        {
            CurrentIntervalMilliseconds = MaximumIntervalMilliseconds;
        }
        else
        {
            CurrentIntervalMilliseconds = CurrentIntervalMilliseconds * 2;
        }

        return CreateTimerResult(
            TftpRetransmissionAction.Retransmit,
            0,
            totalElapsed);
    }

    internal void RecordValidProgress()
    {
        long now = TimeSource.GetTimestampMilliseconds();
        IntervalStartedMilliseconds = now;
        CurrentIntervalMilliseconds = InitialIntervalMilliseconds;
        ConsecutiveRetransmissionCount = 0;
    }

    internal void RestartCurrentInterval()
    {
        IntervalStartedMilliseconds = TimeSource.GetTimestampMilliseconds();
    }

    internal void ApplyNegotiatedTimeout(int negotiatedTimeoutSeconds)
    {
        if (negotiatedTimeoutSeconds < TftpProtocolValues.MinimumTimeoutSeconds ||
            negotiatedTimeoutSeconds > TftpProtocolValues.MaximumTimeoutSeconds)
        {
            throw new TftpProtocolException(
                "The negotiated retransmission timeout is outside the RFC 2349 range.",
                false,
                0);
        }

        long negotiatedIntervalMilliseconds = negotiatedTimeoutSeconds * 1000L;
        InitialIntervalMilliseconds = negotiatedIntervalMilliseconds;
        CurrentIntervalMilliseconds = negotiatedIntervalMilliseconds;
        if (MaximumIntervalMilliseconds < negotiatedIntervalMilliseconds)
        {
            MaximumIntervalMilliseconds = negotiatedIntervalMilliseconds;
        }

        ConsecutiveRetransmissionCount = 0;
        IntervalStartedMilliseconds = TimeSource.GetTimestampMilliseconds();
    }

    internal TftpTimeoutException CreateTimeoutFailure(
        TftpRetransmissionResult result)
    {
        if (result == null)
        {
            throw new TftpProtocolException(
                "A retransmission result is required to create a timeout failure.",
                false,
                0);
        }

        if (result.Action == TftpRetransmissionAction.RetryLimitReached)
        {
            return new TftpTimeoutException(
                "The retransmission retry limit was reached.",
                true,
                result.TotalRetransmissionCount,
                result.ElapsedMilliseconds);
        }

        if (result.Action == TftpRetransmissionAction.TotalDurationReached)
        {
            return new TftpTimeoutException(
                "The maximum transfer duration was reached.",
                false,
                result.TotalRetransmissionCount,
                result.ElapsedMilliseconds);
        }

        throw new TftpProtocolException(
            "The retransmission result does not represent a timeout failure.",
            false,
            0);
    }

    internal long GetNonnegativeElapsed(long startedMilliseconds, long nowMilliseconds)
    {
        if (nowMilliseconds < startedMilliseconds)
        {
            return 0;
        }

        return nowMilliseconds - startedMilliseconds;
    }

    internal TftpRetransmissionResult CreateTimerResult(
        TftpRetransmissionAction action,
        long waitMilliseconds,
        long elapsedMilliseconds)
    {
        TftpRetransmissionResult result = new TftpRetransmissionResult();
        result.Action = action;
        result.WaitMilliseconds = waitMilliseconds;
        result.ElapsedMilliseconds = elapsedMilliseconds;
        result.ConsecutiveRetransmissionCount = ConsecutiveRetransmissionCount;
        result.TotalRetransmissionCount = TotalRetransmissionCount;
        result.NextIntervalMilliseconds = CurrentIntervalMilliseconds;
        return result;
    }
}
