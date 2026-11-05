using whois;

namespace whois.Tests;

public class TimeoutBudgetTests
{
    [Test]
    public void AcceptsFiniteAndInfiniteOverallBudgets()
    {
        ManualTimeProvider finiteClock = new ManualTimeProvider();
        ManualTimeProvider infiniteClock = new ManualTimeProvider();
        WhoisTimeoutOptions finiteTimeouts = CreateTimeouts(TimeSpan.FromMilliseconds(1500), TimeSpan.FromSeconds(5));
        WhoisTimeoutOptions infiniteTimeouts = CreateTimeouts(TimeSpan.FromSeconds(2), Timeout.InfiniteTimeSpan);
        TimeoutBudget finiteBudget = new TimeoutBudget(finiteTimeouts, default(CancellationToken), finiteClock);
        TimeoutBudget infiniteBudget = new TimeoutBudget(infiniteTimeouts, default(CancellationToken), infiniteClock);

        finiteBudget.BeginStage(WhoisOperationStage.Connect);
        Assert.That(finiteBudget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromMilliseconds(1500)));
        Assert.That(finiteBudget.GetRemainingMilliseconds(), Is.EqualTo(1500));

        Assert.That(infiniteBudget.GetRemainingTime(), Is.EqualTo(Timeout.InfiniteTimeSpan));
        Assert.That(infiniteBudget.GetRemainingMilliseconds(), Is.EqualTo(Timeout.Infinite));
        infiniteBudget.BeginStage(WhoisOperationStage.Write);
        Assert.That(infiniteBudget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(2)));
    }

    [TestCase("Resolve", 0L)]
    [TestCase("Connect", -1L)]
    [TestCase("Write", 0L)]
    [TestCase("ReadIdle", -1L)]
    [TestCase("Overall", 0L)]
    [TestCase("Overall", -1L)]
    public void RejectsZeroAndNegativeConfiguredTimeouts(string fieldName, long ticks)
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        SetTimeout(timeouts, fieldName, TimeSpan.FromTicks(ticks));

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => new TimeoutBudget(timeouts, default(CancellationToken), clock));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Timeouts." + fieldName));
    }

    [TestCase("Resolve")]
    [TestCase("Connect")]
    [TestCase("Write")]
    [TestCase("ReadIdle")]
    public void RejectsInfiniteStageTimeouts(string fieldName)
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        SetTimeout(timeouts, fieldName, Timeout.InfiniteTimeSpan);

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => new TimeoutBudget(timeouts, default(CancellationToken), clock));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Timeouts." + fieldName));
    }

    [TestCase("Resolve")]
    [TestCase("Overall")]
    public void RejectsTimeoutsBeyondSocketMillisecondRange(string fieldName)
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2));
        SetTimeout(timeouts, fieldName, TimeSpan.FromMilliseconds(2147483648d));

        WhoisQueryException exception = Assert.Throws<WhoisQueryException>(() => new TimeoutBudget(timeouts, default(CancellationToken), clock));

        Assert.That(exception.InvalidParameter, Is.EqualTo("Timeouts." + fieldName));
    }

    [Test]
    public void AcceptsPositiveSubMillisecondTimeoutAndRoundsForSocketUse()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromTicks(1), TimeSpan.FromTicks(2));
        TimeoutBudget budget = new TimeoutBudget(timeouts, default(CancellationToken), clock);
        budget.BeginStage(WhoisOperationStage.Resolve);

        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromTicks(1)));
        Assert.That(budget.GetRemainingMilliseconds(), Is.EqualTo(1));

        clock.Advance(TimeSpan.FromTicks(1));
        Assert.That(budget.GetRemainingMilliseconds(), Is.Zero);
        Assert.That(budget.GetInterruptionStatus(), Is.EqualTo(WhoisExchangeStatus.TimedOut));
    }

    [Test]
    public void IntersectsEachStageWithMonotonicOverallBudget()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        TimeoutBudget budget = new TimeoutBudget(timeouts, default(CancellationToken), clock);

        budget.BeginStage(WhoisOperationStage.Resolve);
        clock.Advance(TimeSpan.FromSeconds(7));
        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(3)));

        budget.BeginStage(WhoisOperationStage.Connect);
        clock.Advance(TimeSpan.FromSeconds(8));
        budget.BeginStage(WhoisOperationStage.Write);

        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void OverallBudgetExhaustionStopsFreshStage()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(12));
        TimeoutBudget budget = new TimeoutBudget(timeouts, default(CancellationToken), clock);
        budget.BeginStage(WhoisOperationStage.Resolve);
        clock.Advance(TimeSpan.FromSeconds(8));
        budget.BeginStage(WhoisOperationStage.Connect);

        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(4)));

        clock.Advance(TimeSpan.FromSeconds(4));
        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.Zero));
        Assert.That(budget.GetInterruptionStatus(), Is.EqualTo(WhoisExchangeStatus.TimedOut));
    }

    [Test]
    public void ResetReadIdleDoesNotExtendOverallDeadline()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(15));
        TimeoutBudget budget = new TimeoutBudget(timeouts, default(CancellationToken), clock);
        budget.BeginStage(WhoisOperationStage.Read);

        clock.Advance(TimeSpan.FromSeconds(8));
        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(2)));

        budget.ResetReadIdle();
        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(7)));

        clock.Advance(TimeSpan.FromSeconds(6));
        budget.ResetReadIdle();
        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void CallerCancellationTakesPrecedenceOverExpiredTimeout()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        TimeoutBudget budget = new TimeoutBudget(timeouts, cancellation.Token, clock);
        budget.BeginStage(WhoisOperationStage.Resolve);

        Assert.That(budget.GetInterruptionStatus(), Is.EqualTo(WhoisExchangeStatus.Complete));

        clock.Advance(TimeSpan.FromSeconds(10));
        Assert.That(budget.GetInterruptionStatus(), Is.EqualTo(WhoisExchangeStatus.TimedOut));

        cancellation.Cancel();
        Assert.That(budget.GetInterruptionStatus(), Is.EqualTo(WhoisExchangeStatus.Cancelled));
    }

    [Test]
    public void ConvertsDurationsToSafeSocketMilliseconds()
    {
        Assert.That(TimeoutBudget.ToMilliseconds(Timeout.InfiniteTimeSpan), Is.EqualTo(Timeout.Infinite));
        Assert.That(TimeoutBudget.ToMilliseconds(TimeSpan.Zero), Is.Zero);
        Assert.That(TimeoutBudget.ToMilliseconds(TimeSpan.FromTicks(1)), Is.EqualTo(1));
        Assert.That(TimeoutBudget.ToMilliseconds(TimeSpan.FromTicks(TimeSpan.TicksPerMillisecond + 1)), Is.EqualTo(2));
        Assert.That(TimeoutBudget.ToMilliseconds(TimeSpan.MaxValue), Is.EqualTo(int.MaxValue));
    }

    [Test]
    public void CopiesConfiguredTimeoutsAtConstruction()
    {
        ManualTimeProvider clock = new ManualTimeProvider();
        WhoisTimeoutOptions timeouts = CreateTimeouts(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(20));
        TimeoutBudget budget = new TimeoutBudget(timeouts, default(CancellationToken), clock);
        timeouts.Resolve = TimeSpan.FromSeconds(1);
        budget.BeginStage(WhoisOperationStage.Resolve);

        Assert.That(budget.GetRemainingTime(), Is.EqualTo(TimeSpan.FromSeconds(10)));
    }

    private static WhoisTimeoutOptions CreateTimeouts(TimeSpan stage, TimeSpan overall)
    {
        WhoisTimeoutOptions timeouts = new WhoisTimeoutOptions();
        timeouts.Resolve = stage;
        timeouts.Connect = stage;
        timeouts.Write = stage;
        timeouts.ReadIdle = stage;
        timeouts.Overall = overall;
        return timeouts;
    }

    private static void SetTimeout(WhoisTimeoutOptions timeouts, string fieldName, TimeSpan value)
    {
        if (fieldName == "Resolve")
        {
            timeouts.Resolve = value;
        }
        else if (fieldName == "Connect")
        {
            timeouts.Connect = value;
        }
        else if (fieldName == "Write")
        {
            timeouts.Write = value;
        }
        else if (fieldName == "ReadIdle")
        {
            timeouts.ReadIdle = value;
        }
        else
        {
            timeouts.Overall = value;
        }
    }

    private class ManualTimeProvider : TimeProvider
    {
        private long timestamp;

        public override long TimestampFrequency
        {
            get
            {
                return TimeSpan.TicksPerSecond;
            }
        }

        public override long GetTimestamp()
        {
            return timestamp;
        }

        public void Advance(TimeSpan duration)
        {
            timestamp = timestamp + duration.Ticks;
        }
    }
}
