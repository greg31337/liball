using System;
using System.Linq;
using System.Threading.Tasks;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class TokenBucketMessageThrottleTests
{
    [Test]
    public void ConstructorRequiresCallerSuppliedClock()
    {
        Assert.Throws<FixSessionException>(() => new TokenBucketMessageThrottle(null!));
    }

    [Test]
    public void InitialCapacityIncludesSteadyIntervalAndConfiguredBurst()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(3, 1000, 2, 10);
        ApplicationDecision[] decisions = new ApplicationDecision[6];

        for (int index = 0; index < decisions.Length; index++)
        {
            decisions[index] = throttle.CheckMessageThrottle(Context(0, false), settings);
        }

        Assert.Multiple(() =>
        {
            Assert.That(decisions.Take(5).All(decision => decision.Disposition == MessageDisposition.Accept), Is.True);
            AssertDecision(decisions[5], MessageDisposition.Drop, "message-rate-exceeded");
            Assert.That(throttle.AvailableTokens, Is.Zero);
            Assert.That(clock.CallCount, Is.EqualTo(6));
        });
    }

    [Test]
    public void FractionalTokensReplenishOnlyFromInjectedClockProgress()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(2, 1000, 0, 10);
        throttle.CheckMessageThrottle(Context(0, false), settings);
        throttle.CheckMessageThrottle(Context(0, false), settings);

        clock.AdvanceMilliseconds(499);
        ApplicationDecision tooEarly = throttle.CheckMessageThrottle(Context(0, false), settings);
        clock.AdvanceMilliseconds(1);
        ApplicationDecision halfInterval = throttle.CheckMessageThrottle(Context(0, false), settings);
        ApplicationDecision consumed = throttle.CheckMessageThrottle(Context(0, false), settings);
        clock.AdvanceMilliseconds(500);
        ApplicationDecision fullInterval = throttle.CheckMessageThrottle(Context(0, false), settings);

        Assert.Multiple(() =>
        {
            AssertDecision(tooEarly, MessageDisposition.Drop, "message-rate-exceeded");
            AssertDecision(halfInterval, MessageDisposition.Accept, "throttle-accepted");
            AssertDecision(consumed, MessageDisposition.Drop, "message-rate-exceeded");
            AssertDecision(fullInterval, MessageDisposition.Accept, "throttle-accepted");
        });
    }

    [Test]
    public void IdleRefillNeverExceedsBucketCapacity()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(2, 1000, 1, 10);
        for (int index = 0; index < 3; index++)
        {
            Assert.That(throttle.CheckMessageThrottle(Context(0, false), settings).Disposition, Is.EqualTo(MessageDisposition.Accept));
        }
        clock.AdvanceMilliseconds(10000);

        ApplicationDecision[] decisions = new ApplicationDecision[4];
        for (int index = 0; index < decisions.Length; index++)
        {
            decisions[index] = throttle.CheckMessageThrottle(Context(0, false), settings);
        }

        Assert.Multiple(() =>
        {
            Assert.That(decisions.Take(3).All(decision => decision.Disposition == MessageDisposition.Accept), Is.True);
            AssertDecision(decisions[3], MessageDisposition.Drop, "message-rate-exceeded");
        });
    }

    [Test]
    public void MaximumIntegerRateAndBurstUseAStableNonOverflowingCapacity()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(int.MaxValue, 1, int.MaxValue, 1);

        ApplicationDecision initial = throttle.CheckMessageThrottle(Context(0, false), settings);
        decimal expectedCapacityAfterOneMessage = (decimal)int.MaxValue + int.MaxValue - 1m;
        clock.AdvanceMilliseconds(1);
        ApplicationDecision refilled = throttle.CheckMessageThrottle(Context(0, false), settings);

        Assert.Multiple(() =>
        {
            AssertDecision(initial, MessageDisposition.Accept, "throttle-accepted");
            AssertDecision(refilled, MessageDisposition.Accept, "throttle-accepted");
            Assert.That(throttle.AvailableTokens, Is.EqualTo(expectedCapacityAfterOneMessage));
        });
    }

    [Test]
    public void FullQueueRejectsWithoutConsumingAvailableRateToken()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(1, 1000, 0, 2);

        ApplicationDecision full = throttle.CheckMessageThrottle(Context(2, false), settings);
        ApplicationDecision admitted = throttle.CheckMessageThrottle(Context(1, false), settings);
        ApplicationDecision rateLimited = throttle.CheckMessageThrottle(Context(0, false), settings);

        Assert.Multiple(() =>
        {
            AssertDecision(full, MessageDisposition.Reject, "message-queue-full");
            AssertDecision(admitted, MessageDisposition.Accept, "throttle-accepted");
            AssertDecision(rateLimited, MessageDisposition.Drop, "message-rate-exceeded");
        });
    }

    [Test]
    public void AdministrativeMessagesBypassApplicationQueueAndTokens()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(1, 1000, 0, 0);

        ApplicationDecision firstAdmin = throttle.CheckMessageThrottle(Context(100, true), settings);
        ApplicationDecision secondAdmin = throttle.CheckMessageThrottle(Context(100, true), settings);
        settings.QueueCapacity = 1;
        ApplicationDecision application = throttle.CheckMessageThrottle(Context(0, false), settings);
        ApplicationDecision limited = throttle.CheckMessageThrottle(Context(0, false), settings);

        Assert.Multiple(() =>
        {
            AssertDecision(firstAdmin, MessageDisposition.Accept, "administrative-message-bypass");
            AssertDecision(secondAdmin, MessageDisposition.Accept, "administrative-message-bypass");
            AssertDecision(application, MessageDisposition.Accept, "throttle-accepted");
            AssertDecision(limited, MessageDisposition.Drop, "message-rate-exceeded");
        });
    }

    [Test]
    public void RateConfigurationChangeResetsCapacityAtCurrentClockTime()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings original = Settings(1, 1000, 0, 10);
        throttle.CheckMessageThrottle(Context(0, false), original);
        Assert.That(throttle.CheckMessageThrottle(Context(0, false), original).Disposition, Is.EqualTo(MessageDisposition.Drop));
        SessionThrottleSettings changed = Settings(2, 500, 1, 10);

        ApplicationDecision[] decisions = new ApplicationDecision[4];
        for (int index = 0; index < decisions.Length; index++)
        {
            decisions[index] = throttle.CheckMessageThrottle(Context(0, false), changed);
        }

        Assert.Multiple(() =>
        {
            Assert.That(decisions.Take(3).All(decision => decision.Disposition == MessageDisposition.Accept), Is.True);
            AssertDecision(decisions[3], MessageDisposition.Drop, "message-rate-exceeded");
            Assert.That(throttle.LastRefillUtc, Is.EqualTo(clock.UtcNow));
        });
    }

    [Test]
    public void ClockRegressionCannotMintTokens()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(2, 1000, 0, 10);
        throttle.CheckMessageThrottle(Context(0, false), settings);
        throttle.CheckMessageThrottle(Context(0, false), settings);
        DateTime initial = clock.UtcNow;
        clock.UtcNow = initial.AddMilliseconds(-100);

        ApplicationDecision regressed = throttle.CheckMessageThrottle(Context(0, false), settings);
        clock.UtcNow = initial.AddMilliseconds(500);
        ApplicationDecision recovered = throttle.CheckMessageThrottle(Context(0, false), settings);

        Assert.Multiple(() =>
        {
            AssertDecision(regressed, MessageDisposition.Drop, "message-rate-exceeded");
            AssertDecision(recovered, MessageDisposition.Accept, "throttle-accepted");
        });
    }

    [Test]
    public void MissingAndInvalidInputsReturnStableRejectDecisions()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings invalidRate = Settings(0, 1000, 0, 1);
        SessionThrottleSettings invalidInterval = Settings(1, 0, 0, 1);
        SessionThrottleSettings invalidBurst = Settings(1, 1000, -1, 1);
        SessionThrottleSettings invalidCapacity = Settings(1, 1000, 0, -1);

        Assert.Multiple(() =>
        {
            AssertDecision(throttle.CheckMessageThrottle(null!, Settings(1, 1, 0, 1)), MessageDisposition.Reject, "missing-throttle-context");
            AssertDecision(throttle.CheckMessageThrottle(Context(0, false), null!), MessageDisposition.Reject, "missing-throttle-settings");
            AssertDecision(throttle.CheckMessageThrottle(Context(0, false), invalidRate), MessageDisposition.Reject, "invalid-throttle-settings");
            AssertDecision(throttle.CheckMessageThrottle(Context(0, false), invalidInterval), MessageDisposition.Reject, "invalid-throttle-settings");
            AssertDecision(throttle.CheckMessageThrottle(Context(0, false), invalidBurst), MessageDisposition.Reject, "invalid-throttle-settings");
            AssertDecision(throttle.CheckMessageThrottle(Context(0, false), invalidCapacity), MessageDisposition.Reject, "invalid-throttle-settings");
            AssertDecision(throttle.CheckMessageThrottle(Context(-1, false), Settings(1, 1, 0, 1)), MessageDisposition.Reject, "invalid-pending-queue-count");
        });
    }

    [Test]
    public void ConcurrentDecisionsCannotOverspendTokens()
    {
        FakeClock clock = new FakeClock();
        TokenBucketMessageThrottle throttle = new TokenBucketMessageThrottle(clock);
        SessionThrottleSettings settings = Settings(10, 1000, 0, 100);
        ApplicationDecision[] decisions = new ApplicationDecision[50];

        Parallel.For(0, decisions.Length, index =>
        {
            decisions[index] = throttle.CheckMessageThrottle(Context(0, false), settings);
        });

        Assert.Multiple(() =>
        {
            Assert.That(decisions.Count(decision => decision.Disposition == MessageDisposition.Accept), Is.EqualTo(10));
            Assert.That(decisions.Count(decision => decision.Code == "message-rate-exceeded"), Is.EqualTo(40));
            Assert.That(throttle.AvailableTokens, Is.Zero);
        });
    }

    private static SessionThrottleSettings Settings(int maxMessages, int interval, int burst, int queueCapacity)
    {
        return new SessionThrottleSettings
        {
            MaxMessages = maxMessages,
            IntervalMilliseconds = interval,
            BurstMessages = burst,
            QueueCapacity = queueCapacity
        };
    }

    private static ApplicationMessageContext Context(int pendingQueueCount, bool administrative)
    {
        return new ApplicationMessageContext
        {
            PendingQueueCount = pendingQueueCount,
            IsAdministrative = administrative
        };
    }

    private static void AssertDecision(ApplicationDecision decision, MessageDisposition disposition, string code)
    {
        Assert.That(decision.Disposition, Is.EqualTo(disposition));
        Assert.That(decision.Code, Is.EqualTo(code));
        Assert.That(decision.Text, Is.Not.Empty);
    }

    private class FakeClock : ISessionClock
    {
        public DateTime UtcNow = new DateTime(2026, 8, 30, 12, 0, 0, DateTimeKind.Utc);
        public int CallCount;

        public DateTime GetUtcNow()
        {
            CallCount++;
            return UtcNow;
        }

        public void AdvanceMilliseconds(int milliseconds)
        {
            UtcNow = UtcNow.AddMilliseconds(milliseconds);
        }
    }
}
