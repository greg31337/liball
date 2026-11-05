namespace ftp.Tests;

public class FtpOperationHelperTests
{
    [Test]
    public void Cancellation_helper_reports_and_throws_for_requested_cancellation()
    {
        CancellationToken active = CancellationToken.None;
        CancellationToken canceled = new CancellationToken(true);

        Assert.Multiple(() =>
        {
            Assert.That(FtpCancellation.IsCancellationRequested(active), Is.False);
            Assert.That(FtpCancellation.IsCancellationRequested(canceled), Is.True);
            Assert.DoesNotThrow(() => FtpCancellation.ThrowIfCancellationRequested(active));
            Assert.Throws<OperationCanceledException>(() => FtpCancellation.ThrowIfCancellationRequested(canceled));
        });
    }

    [Test]
    public void Deadline_reports_deterministic_remaining_time_and_expiration()
    {
        FtpDeadline deadline = new FtpDeadline(1000, 50);
        FtpDeadline infinite = new FtpDeadline(1000, Timeout.Infinite);

        Assert.Multiple(() =>
        {
            Assert.That(deadline.GetRemainingMilliseconds(999), Is.EqualTo(50));
            Assert.That(deadline.GetRemainingMilliseconds(1000), Is.EqualTo(50));
            Assert.That(deadline.GetRemainingMilliseconds(1025), Is.EqualTo(25));
            Assert.That(deadline.GetRemainingMilliseconds(1050), Is.Zero);
            Assert.That(deadline.HasExpired(1049), Is.False);
            Assert.That(deadline.HasExpired(1050), Is.True);
            Assert.That(infinite.GetRemainingMilliseconds(long.MaxValue), Is.EqualTo(Timeout.Infinite));
            Assert.That(infinite.HasExpired(long.MaxValue), Is.False);
        });
    }

    [Test]
    public void Timeout_helper_enforces_an_immediate_deadline()
    {
        FtpDeadline deadline = FtpTimeout.StartDeadline(0);

        Assert.Multiple(() =>
        {
            Assert.That(FtpTimeout.GetRemainingWaitMilliseconds(deadline), Is.Zero);
            Assert.That(FtpTimeout.HasDeadlineExpired(deadline), Is.True);
            TimeoutException? failure = Assert.Throws<TimeoutException>(() =>
                FtpTimeout.ThrowIfDeadlineExpired(deadline, "control read"));
            Assert.That(failure!.Message, Is.EqualTo("The FTP control read timed out."));
        });
    }

    [Test]
    public void Invalid_deadline_and_size_limits_are_rejected()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<FtpConfigurationException>(() => new FtpDeadline(0, -2));
            Assert.Throws<FtpConfigurationException>(() => new FtpSizeLimit(-1));
        });
    }

    [Test]
    public void Size_limit_accepts_the_boundary_and_rejects_overflow_without_wrapping()
    {
        FtpSizeLimit exact = new FtpSizeLimit(10);
        FtpSizeLimit large = new FtpSizeLimit(long.MaxValue);

        bool first = exact.TryAddBytes(4);
        bool boundary = exact.TryAddBytes(6);
        bool exceeded = exact.TryAddBytes(1);
        bool largeFirst = large.TryAddBytes(long.MaxValue - 1);
        bool largeOverflow = large.TryAddBytes(2);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.True);
            Assert.That(boundary, Is.True);
            Assert.That(exceeded, Is.False);
            Assert.That(exact.ConsumedBytes, Is.EqualTo(10));
            Assert.That(exact.GetRemainingBytes(), Is.Zero);
            Assert.That(exact.Exceeded, Is.True);
            Assert.That(largeFirst, Is.True);
            Assert.That(largeOverflow, Is.False);
            Assert.That(large.ConsumedBytes, Is.EqualTo(long.MaxValue - 1));
            Assert.That(large.Exceeded, Is.True);
        });
    }

    [Test]
    public void Resource_lifetime_disposes_once_and_isolates_disposal_faults()
    {
        CountingDisposable disposable = new CountingDisposable(false);
        FtpResourceLifetime lifetime = new FtpResourceLifetime(disposable);

        FtpFaultIsolationResult first = lifetime.DisposeResource();
        FtpFaultIsolationResult second = lifetime.DisposeResource();

        CountingDisposable throwing = new CountingDisposable(true);
        FtpResourceLifetime failingLifetime = new FtpResourceLifetime(throwing);
        FtpFaultIsolationResult failed = failingLifetime.DisposeResource();
        FtpFaultIsolationResult failedAgain = failingLifetime.DisposeResource();

        Assert.Multiple(() =>
        {
            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(disposable.DisposeCalls, Is.EqualTo(1));
            Assert.That(lifetime.HasResource(), Is.False);
            Assert.That(failed.Succeeded, Is.False);
            Assert.That(failed.Failure, Is.TypeOf<InvalidOperationException>());
            Assert.That(throwing.DisposeCalls, Is.EqualTo(1));
            Assert.That(failingLifetime.HasResource(), Is.False);
            Assert.That(failedAgain.Succeeded, Is.True);
        });
    }

    [Test]
    public void Released_resource_remains_caller_owned()
    {
        CountingDisposable disposable = new CountingDisposable(false);
        FtpResourceLifetime lifetime = new FtpResourceLifetime(disposable);

        IDisposable? released = lifetime.ReleaseResource();
        FtpFaultIsolationResult cleanup = lifetime.DisposeResource();

        Assert.Multiple(() =>
        {
            Assert.That(released, Is.SameAs(disposable));
            Assert.That(disposable.DisposeCalls, Is.Zero);
            Assert.That(lifetime.HasResource(), Is.False);
            Assert.That(cleanup.Succeeded, Is.True);
        });
    }

    private class CountingDisposable : IDisposable
    {
        private readonly bool throwOnDispose;

        public int DisposeCalls;

        public CountingDisposable(bool throwOnDispose)
        {
            this.throwOnDispose = throwOnDispose;
        }

        public void Dispose()
        {
            DisposeCalls++;
            if (throwOnDispose)
            {
                throw new InvalidOperationException("dispose failed");
            }
        }
    }
}
