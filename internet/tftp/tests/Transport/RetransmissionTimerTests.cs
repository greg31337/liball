using tftp;

namespace tftp.Tests;

public class RetransmissionTimerTests
{
    [Test]
    public void WaitsForInitialIntervalThenRequestsRetransmission()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 8, 3, 60);

        TftpRetransmissionResult initial = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(999);
        TftpRetransmissionResult almostDue = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(1);
        TftpRetransmissionResult due = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(initial.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(initial.WaitMilliseconds, Is.EqualTo(1000));
            Assert.That(almostDue.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(almostDue.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(due.Action, Is.EqualTo(TftpRetransmissionAction.Retransmit));
            Assert.That(due.ElapsedMilliseconds, Is.EqualTo(1000));
            Assert.That(due.ConsecutiveRetransmissionCount, Is.EqualTo(1));
            Assert.That(due.TotalRetransmissionCount, Is.EqualTo(1));
            Assert.That(due.NextIntervalMilliseconds, Is.EqualTo(2000));
        });
    }

    [Test]
    public void DoublesIntervalsAndCapsAtConfiguredMaximum()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 2, 5, 5, 60);

        timeSource.AdvanceMilliseconds(2000);
        TftpRetransmissionResult first = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(4000);
        TftpRetransmissionResult second = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(5000);
        TftpRetransmissionResult third = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(first.NextIntervalMilliseconds, Is.EqualTo(4000));
            Assert.That(second.NextIntervalMilliseconds, Is.EqualTo(5000));
            Assert.That(third.NextIntervalMilliseconds, Is.EqualTo(5000));
            Assert.That(third.ConsecutiveRetransmissionCount, Is.EqualTo(3));
            Assert.That(third.TotalRetransmissionCount, Is.EqualTo(3));
        });
    }

    [Test]
    public void StopsAfterConfiguredRetryCount()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 1, 2, 60);

        timeSource.AdvanceMilliseconds(1000);
        Assert.That(timer.EvaluateTimer().Action,
            Is.EqualTo(TftpRetransmissionAction.Retransmit));
        timeSource.AdvanceMilliseconds(1000);
        Assert.That(timer.EvaluateTimer().Action,
            Is.EqualTo(TftpRetransmissionAction.Retransmit));
        timeSource.AdvanceMilliseconds(1000);
        TftpRetransmissionResult exhausted = timer.EvaluateTimer();
        TftpTimeoutException exception = timer.CreateTimeoutFailure(exhausted);

        Assert.Multiple(() =>
        {
            Assert.That(exhausted.Action,
                Is.EqualTo(TftpRetransmissionAction.RetryLimitReached));
            Assert.That(exhausted.ConsecutiveRetransmissionCount, Is.EqualTo(2));
            Assert.That(exhausted.TotalRetransmissionCount, Is.EqualTo(2));
            Assert.That(exception.RetryLimitReached, Is.True);
            Assert.That(exception.RetransmissionCount, Is.EqualTo(2));
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(3000));
        });
    }

    [Test]
    public void ZeroRetryLimitFailsWhenFirstIntervalExpires()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 2, 0, 60);

        timeSource.AdvanceMilliseconds(1000);
        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.Action,
                Is.EqualTo(TftpRetransmissionAction.RetryLimitReached));
            Assert.That(result.ConsecutiveRetransmissionCount, Is.Zero);
            Assert.That(result.TotalRetransmissionCount, Is.Zero);
        });
    }

    [Test]
    public void TotalDurationLimitsWaitAndPreemptsRetransmission()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 10, 10, 3, 3);

        TftpRetransmissionResult initial = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(2999);
        TftpRetransmissionResult almostExpired = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(1);
        TftpRetransmissionResult expired = timer.EvaluateTimer();
        TftpTimeoutException exception = timer.CreateTimeoutFailure(expired);

        Assert.Multiple(() =>
        {
            Assert.That(initial.WaitMilliseconds, Is.EqualTo(3000));
            Assert.That(almostExpired.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(expired.Action,
                Is.EqualTo(TftpRetransmissionAction.TotalDurationReached));
            Assert.That(expired.TotalRetransmissionCount, Is.Zero);
            Assert.That(exception.RetryLimitReached, Is.False);
            Assert.That(exception.RetransmissionCount, Is.Zero);
            Assert.That(exception.ElapsedMilliseconds, Is.EqualTo(3000));
        });
    }

    [Test]
    public void ValidProgressResetsBackoffAndConsecutiveRetriesOnly()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 8, 3, 60);

        timeSource.AdvanceMilliseconds(1000);
        Assert.That(timer.EvaluateTimer().Action,
            Is.EqualTo(TftpRetransmissionAction.Retransmit));
        timeSource.AdvanceMilliseconds(500);
        timer.RecordValidProgress();
        timeSource.AdvanceMilliseconds(999);
        TftpRetransmissionResult waiting = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(1);
        TftpRetransmissionResult retransmit = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(waiting.ConsecutiveRetransmissionCount, Is.Zero);
            Assert.That(waiting.TotalRetransmissionCount, Is.EqualTo(1));
            Assert.That(waiting.NextIntervalMilliseconds, Is.EqualTo(1000));
            Assert.That(retransmit.ConsecutiveRetransmissionCount, Is.EqualTo(1));
            Assert.That(retransmit.TotalRetransmissionCount, Is.EqualTo(2));
            Assert.That(retransmit.NextIntervalMilliseconds, Is.EqualTo(2000));
        });
    }

    [Test]
    public void RestartCurrentIntervalPreservesBackoffAndRetryCounts()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 8, 3, 60);
        timeSource.AdvanceMilliseconds(1000);
        timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(1500);

        timer.RestartCurrentInterval();
        TftpRetransmissionResult restarted = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(restarted.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(restarted.WaitMilliseconds, Is.EqualTo(2000));
            Assert.That(restarted.ConsecutiveRetransmissionCount, Is.EqualTo(1));
            Assert.That(restarted.TotalRetransmissionCount, Is.EqualTo(1));
            Assert.That(restarted.NextIntervalMilliseconds, Is.EqualTo(2000));
        });
    }

    [Test]
    public void NegotiatedTimeoutBecomesInitialInterval()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = new TftpRetransmissionTimer(
            timeSource,
            1,
            16,
            3,
            60,
            true,
            5);

        timeSource.AdvanceMilliseconds(4999);
        TftpRetransmissionResult waiting = timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(1);
        TftpRetransmissionResult retransmit = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(waiting.WaitMilliseconds, Is.EqualTo(1));
            Assert.That(retransmit.Action,
                Is.EqualTo(TftpRetransmissionAction.Retransmit));
            Assert.That(retransmit.NextIntervalMilliseconds, Is.EqualTo(10000));
        });
    }

    [Test]
    public void AppliesNegotiatedTimeoutAfterTransferHasStarted()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 2, 3, 60);
        timeSource.AdvanceMilliseconds(1000);
        timer.EvaluateTimer();
        timeSource.AdvanceMilliseconds(500);

        timer.ApplyNegotiatedTimeout(5);
        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(result.WaitMilliseconds, Is.EqualTo(5000));
            Assert.That(result.ElapsedMilliseconds, Is.EqualTo(1500));
            Assert.That(result.ConsecutiveRetransmissionCount, Is.Zero);
            Assert.That(result.TotalRetransmissionCount, Is.EqualTo(1));
            Assert.That(result.NextIntervalMilliseconds, Is.EqualTo(5000));
            Assert.That(timer.MaximumIntervalMilliseconds, Is.EqualTo(5000));
        });
    }

    [TestCase(0)]
    [TestCase(256)]
    public void RejectsInvalidRuntimeNegotiatedTimeout(int timeoutSeconds)
    {
        TftpRetransmissionTimer timer = CreateTimer(
            new ManualTimeSource(),
            1,
            2,
            3,
            60);

        Assert.Throws<TftpProtocolException>(() =>
            timer.ApplyNegotiatedTimeout(timeoutSeconds));
    }

    [Test]
    public void NegotiatedTimeoutAboveLocalCapDoesNotBackOffDownward()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = new TftpRetransmissionTimer(
            timeSource,
            1,
            16,
            3,
            120,
            true,
            30);

        timeSource.AdvanceMilliseconds(30000);
        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpRetransmissionAction.Retransmit));
            Assert.That(result.NextIntervalMilliseconds, Is.EqualTo(30000));
            Assert.That(timer.MaximumIntervalMilliseconds, Is.EqualTo(30000));
        });
    }

    [Test]
    public void IgnoresNegotiatedValueWhenItWasNotNegotiated()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = new TftpRetransmissionTimer(
            timeSource,
            1,
            4,
            3,
            60,
            false,
            -1);

        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.WaitMilliseconds, Is.EqualTo(1000));
            Assert.That(result.NextIntervalMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void RejectsInvalidConfiguration()
    {
        ManualTimeSource timeSource = new ManualTimeSource();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(null!, 1, 2, 1, 3, false, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 0, 2, 1, 3, false, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 2, 1, 1, 3, false, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 1, 2, -1, 3, false, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 1, 2, 1, 0, false, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 1, 2, 1, 3, true, 0));
            Assert.Throws<TftpProtocolException>(() =>
                new TftpRetransmissionTimer(timeSource, 1, 2, 1, 3, true, 256));
        });
    }

    [Test]
    public void RejectsTimeoutFailureForNonFailureResult()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 2, 1, 60);
        TftpRetransmissionResult waiting = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.Throws<TftpProtocolException>(() => timer.CreateTimeoutFailure(null!));
            Assert.Throws<TftpProtocolException>(() => timer.CreateTimeoutFailure(waiting));
        });
    }

    [Test]
    public void ClockRegressionDoesNotProduceNegativeElapsedTime()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        timeSource.SetMilliseconds(1000);
        TftpRetransmissionTimer timer = CreateTimer(timeSource, 1, 2, 1, 60);
        timeSource.SetMilliseconds(500);

        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.Action, Is.EqualTo(TftpRetransmissionAction.Wait));
            Assert.That(result.ElapsedMilliseconds, Is.Zero);
            Assert.That(result.WaitMilliseconds, Is.EqualTo(1000));
        });
    }

    [Test]
    public void SupportsLargeSecondValuesWithoutIntegerOverflow()
    {
        ManualTimeSource timeSource = new ManualTimeSource();
        TftpRetransmissionTimer timer = CreateTimer(
            timeSource,
            int.MaxValue,
            int.MaxValue,
            1,
            int.MaxValue);

        TftpRetransmissionResult result = timer.EvaluateTimer();

        Assert.Multiple(() =>
        {
            Assert.That(result.WaitMilliseconds,
                Is.EqualTo((long)int.MaxValue * 1000));
            Assert.That(result.NextIntervalMilliseconds,
                Is.EqualTo((long)int.MaxValue * 1000));
        });
    }

    [Test]
    public void SystemTimeSourceReturnsMonotonicProcessTime()
    {
        TftpSystemTimeSource timeSource = new TftpSystemTimeSource();

        long first = timeSource.GetTimestampMilliseconds();
        long second = timeSource.GetTimestampMilliseconds();

        Assert.That(second, Is.GreaterThanOrEqualTo(first));
    }

    private static TftpRetransmissionTimer CreateTimer(
        ManualTimeSource timeSource,
        int initialTimeoutSeconds,
        int maximumTimeoutSeconds,
        int maximumRetries,
        int maximumDurationSeconds)
    {
        return new TftpRetransmissionTimer(
            timeSource,
            initialTimeoutSeconds,
            maximumTimeoutSeconds,
            maximumRetries,
            maximumDurationSeconds,
            false,
            0);
    }
}

internal class ManualTimeSource : ITftpTimeSource
{
    internal long CurrentMilliseconds = 0;

    public long GetTimestampMilliseconds()
    {
        return CurrentMilliseconds;
    }

    internal void AdvanceMilliseconds(long milliseconds)
    {
        CurrentMilliseconds = CurrentMilliseconds + milliseconds;
    }

    internal void SetMilliseconds(long milliseconds)
    {
        CurrentMilliseconds = milliseconds;
    }
}
