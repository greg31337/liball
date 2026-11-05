namespace uspto;

internal interface IRateLimitClock
{
    global::System.DateTimeOffset GetUtcNow();
}

internal interface IRateLimitWait
{
    void Wait(object sync, int milliseconds);
}

internal class SystemRateLimitClock : IRateLimitClock
{
    public global::System.DateTimeOffset GetUtcNow()
    {
        return global::System.DateTimeOffset.UtcNow;
    }
}

internal class MonitorRateLimitWait : IRateLimitWait
{
    public void Wait(object sync, int milliseconds)
    {
        global::System.Threading.Monitor.Wait(sync, milliseconds);
    }
}

internal class RateLimitHeaderValues
{
    internal long Limit = 0L;
    internal bool HasLimit = false;
    internal long Remaining = 0L;
    internal bool HasRemaining = false;
    internal long ResetSeconds = 0L;
    internal bool HasResetSeconds = false;
    internal bool ResetIsUnixTime = false;
    internal bool IsRateLimited = false;
    internal long RetryAfterMilliseconds = 0L;
    internal bool HasRetryAfterMilliseconds = false;
}

internal class RateLimitObservation
{
    internal ApiService Service = ApiService.Unknown;
    internal int ConfiguredLimit = 0;
    internal int EffectiveLimit = 0;
    internal int Used = 0;
    internal int WaitingCount = 0;
    internal long ObservedLimit = 0L;
    internal bool HasObservedLimit = false;
    internal long ObservedRemaining = 0L;
    internal bool HasObservedRemaining = false;
    internal global::System.DateTimeOffset ObservedResetAt = default(global::System.DateTimeOffset);
    internal bool HasObservedResetAt = false;
}

internal class ServiceRateBucket
{
    internal object Sync = new object();
    internal global::System.Collections.Generic.HashSet<long> CanceledTickets = new global::System.Collections.Generic.HashSet<long>();
    internal ApiService Service = ApiService.Unknown;
    internal int ConfiguredLimit = 0;
    internal int EffectiveLimit = 0;
    internal int WindowMilliseconds = 0;
    internal int QueueCapacity = 0;
    internal bool HasQueueCapacity = false;
    internal bool ObserveServerHeaders = false;
    internal global::System.DateTimeOffset WindowStarted = default(global::System.DateTimeOffset);
    internal int Used = 0;
    internal long NextTicket = 0L;
    internal long ServingTicket = 0L;
    internal int WaitingCount = 0;
    internal long ServerRemaining = 0L;
    internal bool HasServerRemaining = false;
    internal global::System.DateTimeOffset ServerResetAt = default(global::System.DateTimeOffset);
    internal bool HasServerResetAt = false;
    internal long ObservedLimit = 0L;
    internal bool HasObservedLimit = false;
    internal long ObservedRemaining = 0L;
    internal bool HasObservedRemaining = false;
    internal global::System.DateTimeOffset ObservedResetAt = default(global::System.DateTimeOffset);
    internal bool HasObservedResetAt = false;
    internal IRateLimitClock Clock;
    internal IRateLimitWait Waiter;

    internal ServiceRateBucket(RateLimitPolicy policy, IRateLimitClock clock, IRateLimitWait waiter)
    {
        Service = policy.Service;
        ConfiguredLimit = policy.RequestsPerWindow;
        EffectiveLimit = policy.RequestsPerWindow;
        WindowMilliseconds = policy.WindowMilliseconds;
        QueueCapacity = policy.QueueCapacity;
        HasQueueCapacity = policy.HasQueueCapacity;
        ObserveServerHeaders = policy.ObserveServerHeaders;
        Clock = clock;
        Waiter = waiter;
        WindowStarted = Clock.GetUtcNow();
    }

    internal void AcquirePermit(global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (Sync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            global::System.DateTimeOffset now = Clock.GetUtcNow();
            RefreshWindow(now);
            if (WaitingCount == 0 && HasPermit())
            {
                ConsumePermit();
                return;
            }

            if (HasQueueCapacity && WaitingCount >= QueueCapacity)
            {
                throw new RateLimitException("The local service rate-limit queue is full.", Service, 0, false, null, 0L, false, null);
            }

            long ticket = NextTicket;
            NextTicket += 1L;
            WaitingCount += 1;
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    CancelTicket(ticket);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                now = Clock.GetUtcNow();
                RefreshWindow(now);
                if (ticket == ServingTicket && HasPermit())
                {
                    ConsumePermit();
                    WaitingCount -= 1;
                    ServingTicket += 1L;
                    SkipCanceledTickets();
                    global::System.Threading.Monitor.PulseAll(Sync);
                    return;
                }

                int waitMilliseconds = GetWaitMilliseconds(now, ticket == ServingTicket);
                Waiter.Wait(Sync, waitMilliseconds);
            }
        }
    }

    internal void ApplyObservation(RateLimitHeaderValues values)
    {
        if (!ObserveServerHeaders)
        {
            return;
        }

        lock (Sync)
        {
            global::System.DateTimeOffset now = Clock.GetUtcNow();
            RefreshWindow(now);
            if (values.HasLimit)
            {
                ObservedLimit = values.Limit;
                HasObservedLimit = true;
                EffectiveLimit = ConfiguredLimit;
                if (values.Limit < EffectiveLimit)
                {
                    EffectiveLimit = (int)values.Limit;
                }
            }

            if (values.HasRemaining)
            {
                ObservedRemaining = values.Remaining;
                HasObservedRemaining = true;
                ServerRemaining = values.Remaining;
                HasServerRemaining = true;
                long inferredUsed = EffectiveLimit - values.Remaining;
                if (inferredUsed > Used)
                {
                    Used = (int)inferredUsed;
                }
            }

            if (values.HasResetSeconds)
            {
                global::System.DateTimeOffset resetAt = now;
                if (values.ResetIsUnixTime)
                {
                    resetAt = global::System.DateTimeOffset.FromUnixTimeSeconds(values.ResetSeconds);
                }
                else
                {
                    resetAt = now.AddSeconds(values.ResetSeconds);
                }

                ServerResetAt = resetAt;
                HasServerResetAt = true;
                ObservedResetAt = resetAt;
                HasObservedResetAt = true;
            }

            if (values.IsRateLimited)
            {
                Used = EffectiveLimit;
                ServerRemaining = 0L;
                HasServerRemaining = true;
                if (values.HasRetryAfterMilliseconds)
                {
                    ServerResetAt = now.AddMilliseconds(values.RetryAfterMilliseconds);
                    HasServerResetAt = true;
                    ObservedResetAt = ServerResetAt;
                    HasObservedResetAt = true;
                }
            }

            global::System.Threading.Monitor.PulseAll(Sync);
        }
    }

    internal RateLimitObservation CaptureObservation()
    {
        lock (Sync)
        {
            RefreshWindow(Clock.GetUtcNow());
            RateLimitObservation result = new RateLimitObservation();
            result.Service = Service;
            result.ConfiguredLimit = ConfiguredLimit;
            result.EffectiveLimit = EffectiveLimit;
            result.Used = Used;
            result.WaitingCount = WaitingCount;
            result.ObservedLimit = ObservedLimit;
            result.HasObservedLimit = HasObservedLimit;
            result.ObservedRemaining = ObservedRemaining;
            result.HasObservedRemaining = HasObservedRemaining;
            result.ObservedResetAt = ObservedResetAt;
            result.HasObservedResetAt = HasObservedResetAt;
            return result;
        }
    }

    internal void RefreshWindow(global::System.DateTimeOffset now)
    {
        global::System.DateTimeOffset localResetAt = WindowStarted.AddMilliseconds(WindowMilliseconds);
        if (now >= localResetAt)
        {
            WindowStarted = now;
            Used = 0;
            if (!HasServerResetAt)
            {
                HasServerRemaining = false;
            }
        }

        if (HasServerResetAt && now >= ServerResetAt)
        {
            WindowStarted = now;
            Used = 0;
            HasServerResetAt = false;
            HasServerRemaining = false;
        }
    }

    internal bool HasPermit()
    {
        if (EffectiveLimit <= 0 || Used >= EffectiveLimit)
        {
            return false;
        }

        if (HasServerRemaining && ServerRemaining <= 0L)
        {
            return false;
        }

        return true;
    }

    internal void ConsumePermit()
    {
        Used += 1;
        if (HasServerRemaining && ServerRemaining > 0L)
        {
            ServerRemaining -= 1L;
        }
    }

    internal int GetWaitMilliseconds(global::System.DateTimeOffset now, bool isHead)
    {
        if (!isHead)
        {
            return 25;
        }

        global::System.DateTimeOffset wakeAt = WindowStarted.AddMilliseconds(WindowMilliseconds);
        if (HasServerRemaining && ServerRemaining <= 0L && HasServerResetAt && ServerResetAt < wakeAt)
        {
            wakeAt = ServerResetAt;
        }

        global::System.TimeSpan remaining = wakeAt - now;
        if (remaining <= global::System.TimeSpan.Zero)
        {
            return 1;
        }

        if (remaining.TotalMilliseconds > 25.0)
        {
            return 25;
        }

        int waitMilliseconds = (int)global::System.Math.Ceiling(remaining.TotalMilliseconds);
        if (waitMilliseconds < 1)
        {
            waitMilliseconds = 1;
        }

        return waitMilliseconds;
    }

    internal void CancelTicket(long ticket)
    {
        WaitingCount -= 1;
        if (ticket == ServingTicket)
        {
            ServingTicket += 1L;
            SkipCanceledTickets();
        }
        else
        {
            CanceledTickets.Add(ticket);
        }

        global::System.Threading.Monitor.PulseAll(Sync);
    }

    internal void SkipCanceledTickets()
    {
        while (CanceledTickets.Contains(ServingTicket))
        {
            CanceledTickets.Remove(ServingTicket);
            ServingTicket += 1L;
        }
    }
}

internal class RateLimiter
{
    internal global::System.Collections.Generic.Dictionary<ApiService, ServiceRateBucket> Buckets = new global::System.Collections.Generic.Dictionary<ApiService, ServiceRateBucket>();
    internal IRateLimitClock Clock = new SystemRateLimitClock();
    internal IRateLimitWait Waiter = new MonitorRateLimitWait();

    internal RateLimiter(RateLimitPolicy[] policies)
    {
        if (policies == null)
        {
            throw new ValidationException("Rate-limit policies are required.", "policies", "REQUIRED", null);
        }

        foreach (RateLimitPolicy policy in policies)
        {
            if (policy == null)
            {
                throw new ValidationException("Rate-limit policies cannot contain null entries.", "policies", "REQUIRED", null);
            }

            if (policy.Service == ApiService.Unknown)
            {
                throw new ValidationException("A rate-limit policy must select a service bucket.", "Service", "REQUIRED", null);
            }

            if (policy.RequestsPerWindow <= 0)
            {
                throw new ValidationException("The request allowance must be positive.", "RequestsPerWindow", "INVALID_RATE_LIMIT", null);
            }

            if (policy.WindowMilliseconds <= 0)
            {
                throw new ValidationException("The rate window must be positive.", "WindowMilliseconds", "INVALID_RATE_LIMIT", null);
            }

            if (policy.HasQueueCapacity && policy.QueueCapacity < 0)
            {
                throw new ValidationException("The queue capacity cannot be negative.", "QueueCapacity", "INVALID_RATE_LIMIT", null);
            }

            if (Buckets.ContainsKey(policy.Service))
            {
                throw new ValidationException("Only one rate-limit policy may configure a service bucket.", "Service", "DUPLICATE_RATE_LIMIT", null);
            }

            Buckets.Add(policy.Service, new ServiceRateBucket(policy, Clock, Waiter));
        }
    }

    internal void ConfigureTimingHooks(IRateLimitClock clock, IRateLimitWait waiter)
    {
        if (clock == null || waiter == null)
        {
            throw new ValidationException("Rate-limit timing hooks are required.", "timingHooks", "REQUIRED", null);
        }

        Clock = clock;
        Waiter = waiter;
        global::System.DateTimeOffset now = Clock.GetUtcNow();
        foreach (global::System.Collections.Generic.KeyValuePair<ApiService, ServiceRateBucket> entry in Buckets)
        {
            entry.Value.Clock = clock;
            entry.Value.Waiter = waiter;
            entry.Value.WindowStarted = now;
        }
    }

    internal void Acquire(ApiService service, global::System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!Buckets.ContainsKey(service))
        {
            return;
        }

        Buckets[service].AcquirePermit(cancellationToken);
    }

    internal void ObserveResponse(ApiService service, global::System.Net.Http.HttpResponseMessage response)
    {
        if (response == null)
        {
            throw new ValidationException("An HTTP response is required.", "response", "REQUIRED", null);
        }

        if (!Buckets.ContainsKey(service))
        {
            return;
        }

        RateLimitHeaderValues values = ReadHeaders(response, Clock.GetUtcNow());
        Buckets[service].ApplyObservation(values);
    }

    internal RateLimitObservation GetObservation(ApiService service)
    {
        if (!Buckets.ContainsKey(service))
        {
            throw new ValidationException("The service has no configured rate-limit bucket.", "service", "RATE_LIMIT_NOT_CONFIGURED", null);
        }

        return Buckets[service].CaptureObservation();
    }

    internal static RateLimitHeaderValues ReadHeaders(global::System.Net.Http.HttpResponseMessage response, global::System.DateTimeOffset now)
    {
        RateLimitHeaderValues values = new RateLimitHeaderValues();
        values.IsRateLimited = (int)response.StatusCode == 429;
        string? limit = ReadHeader(response, "RateLimit-Limit", "X-RateLimit-Limit");
        string? remaining = ReadHeader(response, "RateLimit-Remaining", "X-RateLimit-Remaining");
        string? standardReset = ReadHeader(response, "RateLimit-Reset", null);
        string? legacyReset = null;
        if (standardReset == null)
        {
            legacyReset = ReadHeader(response, "X-RateLimit-Reset", null);
        }

        long parsedLimit = ParseNonNegativeInteger(limit);
        if (parsedLimit > 0L)
        {
            if (parsedLimit > global::System.Int32.MaxValue)
            {
                parsedLimit = global::System.Int32.MaxValue;
            }

            values.Limit = parsedLimit;
            values.HasLimit = true;
        }

        long parsedRemaining = ParseNonNegativeInteger(remaining);
        if (parsedRemaining >= 0L)
        {
            values.Remaining = parsedRemaining;
            values.HasRemaining = true;
        }

        string? reset = standardReset;
        if (reset == null)
        {
            reset = legacyReset;
        }

        long parsedReset = ParseNonNegativeInteger(reset);
        if (parsedReset >= 0L)
        {
            long currentUnixTime = now.ToUnixTimeSeconds();
            bool isUnixTime = legacyReset != null && parsedReset > currentUnixTime - 86400L;
            bool isSupportedUnixTime = isUnixTime && parsedReset <= 253402300799L;
            if (isSupportedUnixTime || !isUnixTime && parsedReset <= 315360000L)
            {
                values.ResetSeconds = parsedReset;
                values.HasResetSeconds = true;
                values.ResetIsUnixTime = isSupportedUnixTime;
            }
        }

        if (values.IsRateLimited)
        {
            HttpResponseMetadata metadata = ResponseReader.CaptureMetadata(response, null);
            RetryAfterValue retryAfter = ErrorMapper.ParseRetryAfter(metadata.Headers);
            values.RetryAfterMilliseconds = retryAfter.Milliseconds;
            values.HasRetryAfterMilliseconds = retryAfter.HasValue;
        }

        return values;
    }

    internal static string? ReadHeader(global::System.Net.Http.HttpResponseMessage response, string primaryName, string? fallbackName)
    {
        string? value = ReadResponseHeader(response, primaryName);
        if (value == null && fallbackName != null)
        {
            value = ReadResponseHeader(response, fallbackName);
        }

        return value;
    }

    internal static string? ReadResponseHeader(global::System.Net.Http.HttpResponseMessage response, string name)
    {
        global::System.Collections.Generic.IEnumerable<string>? values = null;
        if (response.Headers.Contains(name))
        {
            values = response.Headers.GetValues(name);
        }
        else if (response.Content.Headers.Contains(name))
        {
            values = response.Content.Headers.GetValues(name);
        }

        if (values == null)
        {
            return null;
        }

        foreach (string value in values)
        {
            return value;
        }

        return null;
    }

    internal static long ParseNonNegativeInteger(string? text)
    {
        if (global::System.String.IsNullOrWhiteSpace(text))
        {
            return -1L;
        }

        string value = text.Trim();
        long result = 0L;
        bool hasDigit = false;
        for (int index = 0; index < value.Length; index += 1)
        {
            char character = value[index];
            if (character < '0' || character > '9')
            {
                break;
            }

            hasDigit = true;
            int digit = character - '0';
            if (result > (global::System.Int64.MaxValue - digit) / 10L)
            {
                return -1L;
            }

            result = result * 10L + digit;
        }

        if (!hasDigit)
        {
            return -1L;
        }

        return result;
    }
}
