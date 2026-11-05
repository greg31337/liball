using System;

namespace fix;

/// <summary>Applies deterministic token-bucket rate limiting and application queue bounds.</summary>
public class TokenBucketMessageThrottle : IMessageThrottle
{
    internal object ThrottleLock = new object();
    internal ISessionClock Clock;
    internal bool IsInitialized;
    internal int ConfiguredMaxMessages;
    internal int ConfiguredIntervalMilliseconds;
    internal int ConfiguredBurstMessages;
    internal decimal AvailableTokens;
    internal DateTime LastRefillUtc;

    /// <summary>Creates a token bucket driven exclusively by a caller-supplied session clock.</summary>
    /// <param name="clock">The deterministic clock used when throttle decisions are requested.</param>
    /// <exception cref="FixSessionException">Thrown when the clock is missing.</exception>
    public TokenBucketMessageThrottle(ISessionClock clock)
    {
        if (clock == null)
        {
            throw new FixSessionException("The message throttle clock is missing.");
        }
        Clock = clock;
    }

    /// <summary>Checks configuration, queue capacity, and currently available rate tokens.</summary>
    /// <param name="context">The message and current pending-queue context.</param>
    /// <param name="settings">The token rate, interval, burst, and queue bounds.</param>
    /// <returns>An explicit accept, drop, or reject decision.</returns>
    public ApplicationDecision CheckMessageThrottle(ApplicationMessageContext context, SessionThrottleSettings settings)
    {
        lock (ThrottleLock)
        {
            if (context == null)
            {
                return CreateDecision(MessageDisposition.Reject, "missing-throttle-context", "The message throttle context is missing.");
            }
            if (settings == null)
            {
                return CreateDecision(MessageDisposition.Reject, "missing-throttle-settings", "The message throttle settings are missing.");
            }
            if (settings.MaxMessages <= 0 || settings.IntervalMilliseconds <= 0 || settings.BurstMessages < 0 || settings.QueueCapacity < 0)
            {
                return CreateDecision(MessageDisposition.Reject, "invalid-throttle-settings", "Message throttle rate, interval, burst, or queue settings are invalid.");
            }
            if (context.PendingQueueCount < 0)
            {
                return CreateDecision(MessageDisposition.Reject, "invalid-pending-queue-count", "The pending application-message queue count is negative.");
            }
            if (context.IsAdministrative)
            {
                return CreateDecision(MessageDisposition.Accept, "administrative-message-bypass", "Administrative messages bypass application throttling.");
            }
            DateTime now = Clock.GetUtcNow();
            ConfigureOrRefill(settings, now);
            if (context.PendingQueueCount >= settings.QueueCapacity)
            {
                return CreateDecision(MessageDisposition.Reject, "message-queue-full", "The pending application-message queue is full.");
            }
            if (AvailableTokens < 1m)
            {
                return CreateDecision(MessageDisposition.Drop, "message-rate-exceeded", "The configured application-message rate has been exceeded.");
            }
            AvailableTokens -= 1m;
            return CreateDecision(MessageDisposition.Accept, "throttle-accepted", "The application message is within rate and queue bounds.");
        }
    }

    internal void ConfigureOrRefill(SessionThrottleSettings settings, DateTime now)
    {
        if (!IsInitialized || ConfiguredMaxMessages != settings.MaxMessages || ConfiguredIntervalMilliseconds != settings.IntervalMilliseconds || ConfiguredBurstMessages != settings.BurstMessages)
        {
            ConfiguredMaxMessages = settings.MaxMessages;
            ConfiguredIntervalMilliseconds = settings.IntervalMilliseconds;
            ConfiguredBurstMessages = settings.BurstMessages;
            AvailableTokens = (decimal)settings.MaxMessages + settings.BurstMessages;
            LastRefillUtc = now;
            IsInitialized = true;
            return;
        }
        if (now <= LastRefillUtc)
        {
            return;
        }
        long elapsedTicks = now.Ticks - LastRefillUtc.Ticks;
        decimal elapsedMilliseconds = (decimal)elapsedTicks / TimeSpan.TicksPerMillisecond;
        decimal replenished = elapsedMilliseconds * ConfiguredMaxMessages / ConfiguredIntervalMilliseconds;
        decimal capacity = (decimal)ConfiguredMaxMessages + ConfiguredBurstMessages;
        AvailableTokens += replenished;
        if (AvailableTokens > capacity)
        {
            AvailableTokens = capacity;
        }
        LastRefillUtc = now;
    }

    internal static ApplicationDecision CreateDecision(MessageDisposition disposition, string code, string text)
    {
        ApplicationDecision decision = new ApplicationDecision();
        decision.Disposition = disposition;
        decision.Code = code;
        decision.Text = text;
        return decision;
    }
}
