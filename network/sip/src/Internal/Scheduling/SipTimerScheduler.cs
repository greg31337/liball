namespace sip;

internal class SipTimerScheduler
{
    System.Collections.Generic.List<SipScheduledTimer> timers;
    object synchronization;
    ISipMonotonicClock clock;
    int t1Milliseconds;
    int t2Milliseconds;
    int t4Milliseconds;
    int maximumTimerCount;
    long currentMilliseconds;
    long nextTimerId;
    long nextSequence;

    internal SipTimerScheduler(SipTimerOptions? options, ISipMonotonicClock? clock)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }
        if (clock == null)
        {
            throw new System.ArgumentNullException("clock");
        }
        if (options.T1Milliseconds <= 0 || options.T2Milliseconds < options.T1Milliseconds
            || options.T4Milliseconds <= 0 || options.MaxTimerCount <= 0)
        {
            throw new System.ArgumentOutOfRangeException("options");
        }

        long initialMilliseconds = clock.GetMilliseconds();
        if (initialMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("clock");
        }

        timers = new System.Collections.Generic.List<SipScheduledTimer>();
        synchronization = new object();
        this.clock = clock;
        t1Milliseconds = options.T1Milliseconds;
        t2Milliseconds = options.T2Milliseconds;
        t4Milliseconds = options.T4Milliseconds;
        maximumTimerCount = options.MaxTimerCount;
        currentMilliseconds = initialMilliseconds;
        nextTimerId = 1;
        nextSequence = 0;
    }

    internal long ScheduleTimer(string? ownerId, string? timerName, long delayMilliseconds)
    {
        if (ownerId == null || ownerId.Length == 0
            || ownerId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded timer owner identifier is required.",
                "ownerId");
        }
        if (!SipAscii.IsToken(timerName) || timerName == null
            || timerName.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded timer name token is required.",
                "timerName");
        }
        if (delayMilliseconds < 0)
        {
            throw new System.ArgumentOutOfRangeException("delayMilliseconds");
        }

        lock (synchronization)
        {
            if (timers.Count >= maximumTimerCount)
            {
                throw new System.InvalidOperationException("The scheduled timer limit has been reached.");
            }
            if (nextTimerId == long.MaxValue || nextSequence == long.MaxValue)
            {
                throw new System.InvalidOperationException("The timer sequence has been exhausted.");
            }

            SipScheduledTimer timer = new SipScheduledTimer();
            timer.Id = nextTimerId;
            timer.OwnerId = ownerId;
            timer.Name = timerName;
            timer.DeadlineMilliseconds = AddMilliseconds(currentMilliseconds, delayMilliseconds);
            timer.Sequence = nextSequence;
            nextTimerId = nextTimerId + 1;
            nextSequence = nextSequence + 1;
            timers.Insert(FindInsertionIndex(timer), timer);
            return timer.Id;
        }
    }

    internal bool CancelTimer(long timerId)
    {
        if (timerId <= 0)
        {
            return false;
        }

        lock (synchronization)
        {
            int index = 0;
            while (index < timers.Count)
            {
                if (timers[index].Id == timerId)
                {
                    timers.RemoveAt(index);
                    return true;
                }
                index = index + 1;
            }
            return false;
        }
    }

    internal int CancelOwnerTimers(string? ownerId)
    {
        if (ownerId == null || ownerId.Length == 0
            || ownerId.Length > SipDefaults.MaxHeaderLineBytes)
        {
            throw new System.ArgumentException("A bounded timer owner identifier is required.",
                "ownerId");
        }

        lock (synchronization)
        {
            int removed = 0;
            int index = timers.Count - 1;
            while (index >= 0)
            {
                if (timers[index].OwnerId == ownerId)
                {
                    timers.RemoveAt(index);
                    removed = removed + 1;
                }
                index = index - 1;
            }
            return removed;
        }
    }

    internal SipScheduledTimer[] AdvanceTime(long nowMilliseconds)
    {
        lock (synchronization)
        {
            if (nowMilliseconds < currentMilliseconds)
            {
                throw new System.ArgumentOutOfRangeException("nowMilliseconds");
            }
            currentMilliseconds = nowMilliseconds;

            int dueCount = 0;
            while (dueCount < timers.Count
                && timers[dueCount].DeadlineMilliseconds <= currentMilliseconds)
            {
                dueCount = dueCount + 1;
            }
            if (dueCount == 0)
            {
                return new SipScheduledTimer[0];
            }

            SipScheduledTimer[] due = new SipScheduledTimer[dueCount];
            int index = 0;
            while (index < dueCount)
            {
                due[index] = timers[index];
                index = index + 1;
            }
            timers.RemoveRange(0, dueCount);
            return due;
        }
    }

    internal SipScheduledTimer[] AdvanceClock()
    {
        long nowMilliseconds = clock.GetMilliseconds();
        return AdvanceTime(nowMilliseconds);
    }

    internal SipScheduledTimer[] CopyTimers()
    {
        lock (synchronization)
        {
            SipScheduledTimer[] copy = new SipScheduledTimer[timers.Count];
            int index = 0;
            while (index < timers.Count)
            {
                copy[index] = CopyTimer(timers[index]);
                index = index + 1;
            }
            return copy;
        }
    }

    internal int ClearTimers()
    {
        lock (synchronization)
        {
            int removed = timers.Count;
            timers.Clear();
            return removed;
        }
    }

    internal int GetTimerCount()
    {
        lock (synchronization)
        {
            return timers.Count;
        }
    }

    internal long GetCurrentMilliseconds()
    {
        lock (synchronization)
        {
            return currentMilliseconds;
        }
    }

    internal int GetT1Milliseconds()
    {
        return t1Milliseconds;
    }

    internal int GetT2Milliseconds()
    {
        return t2Milliseconds;
    }

    internal int GetT4Milliseconds()
    {
        return t4Milliseconds;
    }

    internal int GetMaximumTimerCount()
    {
        return maximumTimerCount;
    }

    int FindInsertionIndex(SipScheduledTimer timer)
    {
        int low = 0;
        int high = timers.Count;
        while (low < high)
        {
            int middle = low + (high - low) / 2;
            SipScheduledTimer candidate = timers[middle];
            if (candidate.DeadlineMilliseconds < timer.DeadlineMilliseconds
                || candidate.DeadlineMilliseconds == timer.DeadlineMilliseconds
                && candidate.Sequence < timer.Sequence)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }
        return low;
    }

    static long AddMilliseconds(long value, long addition)
    {
        if (addition > long.MaxValue - value)
        {
            return long.MaxValue;
        }
        return value + addition;
    }

    static SipScheduledTimer CopyTimer(SipScheduledTimer source)
    {
        SipScheduledTimer copy = new SipScheduledTimer();
        copy.Id = source.Id;
        copy.OwnerId = source.OwnerId;
        copy.Name = source.Name;
        copy.DeadlineMilliseconds = source.DeadlineMilliseconds;
        copy.Sequence = source.Sequence;
        return copy;
    }
}
