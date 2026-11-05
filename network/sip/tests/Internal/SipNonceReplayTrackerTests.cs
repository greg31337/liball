namespace sip.Tests;

public class SipNonceReplayTrackerTests
{
    [Test]
    public void AcceptsIncreasingCountsAndRejectsDuplicatesOrOlderCounts()
    {
        SipNonceReplayTracker tracker = CreateTracker(
            new SipManualMonotonicClock(0), 16, 300, 30);

        SipAuthenticationStatus first = tracker.CheckAndRecord(
            "example.com", "alice", "nonce", "cnonce", 1);
        SipAuthenticationStatus duplicate = tracker.CheckAndRecord(
            "example.com", "alice", "nonce", "cnonce", 1);
        SipAuthenticationStatus increased = tracker.CheckAndRecord(
            "example.com", "alice", "nonce", "cnonce", 3);
        SipAuthenticationStatus decreased = tracker.CheckAndRecord(
            "example.com", "alice", "nonce", "cnonce", 2);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(duplicate,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(increased, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(decreased,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void TracksEveryReplayKeyComponentIndependently()
    {
        SipNonceReplayTracker tracker = CreateTracker(
            new SipManualMonotonicClock(0), 16, 300, 30);

        SipAuthenticationStatus baseline = tracker.CheckAndRecord(
            "realm-one", "alice", "nonce-one", "cnonce-one", 1);
        SipAuthenticationStatus realm = tracker.CheckAndRecord(
            "realm-two", "alice", "nonce-one", "cnonce-one", 1);
        SipAuthenticationStatus identity = tracker.CheckAndRecord(
            "realm-one", "bob", "nonce-one", "cnonce-one", 1);
        SipAuthenticationStatus nonce = tracker.CheckAndRecord(
            "realm-one", "alice", "nonce-two", "cnonce-one", 1);
        SipAuthenticationStatus clientNonce = tracker.CheckAndRecord(
            "realm-one", "alice", "nonce-one", "cnonce-two", 1);

        Assert.Multiple(() =>
        {
            Assert.That(baseline, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(realm, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(identity, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(nonce, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(clientNonce, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(5));
        });
    }

    [Test]
    public void RetainsEntriesForTheEntirePossibleNonceValidityWindow()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipNonceReplayTracker tracker = CreateTracker(clock, 4, 1, 1);
        tracker.CheckAndRecord("realm", "alice", "nonce", "cnonce", 1);

        clock.AdvanceMilliseconds(3000);
        SipAuthenticationStatus atBoundary = tracker.CheckAndRecord(
            "realm", "alice", "nonce", "cnonce", 1);
        clock.AdvanceMilliseconds(1);
        SipAuthenticationStatus afterExpiry = tracker.CheckAndRecord(
            "realm", "alice", "nonce", "cnonce", 1);

        Assert.Multiple(() =>
        {
            Assert.That(atBoundary,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(afterExpiry, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void FailsClosedAtCapacityAndReusesExpiredCapacity()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipNonceReplayTracker tracker = CreateTracker(clock, 2, 1, 0);
        tracker.CheckAndRecord("realm", "alice", "nonce-one", "cnonce", 1);
        tracker.CheckAndRecord("realm", "alice", "nonce-two", "cnonce", 1);

        SipAuthenticationStatus full = tracker.CheckAndRecord(
            "realm", "alice", "nonce-three", "cnonce", 1);
        SipAuthenticationStatus existing = tracker.CheckAndRecord(
            "realm", "alice", "nonce-one", "cnonce", 2);
        int fullCount = tracker.GetEntryCount();
        clock.AdvanceMilliseconds(1001);
        SipAuthenticationStatus afterExpiry = tracker.CheckAndRecord(
            "realm", "alice", "nonce-three", "cnonce", 1);

        Assert.Multiple(() =>
        {
            Assert.That(full, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(existing, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(fullCount, Is.EqualTo(2));
            Assert.That(afterExpiry, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void EvictsOnlyExpiredEntriesWhileRetainingLiveReplayState()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipNonceReplayTracker tracker = CreateTracker(clock, 2, 1, 0);
        tracker.CheckAndRecord(
            "realm", "alice", "old-nonce", "cnonce", 1);
        clock.AdvanceMilliseconds(500);
        tracker.CheckAndRecord(
            "realm", "alice", "live-nonce", "cnonce", 1);
        clock.AdvanceMilliseconds(501);

        SipAuthenticationStatus added = tracker.CheckAndRecord(
            "realm", "alice", "new-nonce", "cnonce", 1);
        SipAuthenticationStatus liveReplay = tracker.CheckAndRecord(
            "realm", "alice", "live-nonce", "cnonce", 1);
        SipAuthenticationStatus oldReused = tracker.CheckAndRecord(
            "realm", "alice", "old-nonce", "cnonce", 1);

        Assert.Multiple(() =>
        {
            Assert.That(added, Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(liveReplay,
                Is.EqualTo(SipAuthenticationStatus.ReplayDetected));
            Assert.That(oldReused, Is.EqualTo(SipAuthenticationStatus.Failed));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(2));
        });
    }

    [Test]
    public void IncreasingCountsDoNotExtendNonceReplayLifetime()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipNonceReplayTracker tracker = CreateTracker(clock, 2, 1, 0);
        tracker.CheckAndRecord("realm", "alice", "nonce", "cnonce", 1);
        clock.AdvanceMilliseconds(900);
        SipAuthenticationStatus increased = tracker.CheckAndRecord(
            "realm", "alice", "nonce", "cnonce", 2);
        clock.AdvanceMilliseconds(101);
        SipAuthenticationStatus afterOriginalExpiry = tracker.CheckAndRecord(
            "realm", "alice", "nonce", "cnonce", 1);

        Assert.Multiple(() =>
        {
            Assert.That(increased,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(afterOriginalExpiry,
                Is.EqualTo(SipAuthenticationStatus.Success));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ConcurrentDuplicatesHaveExactlyOneWinner()
    {
        SipNonceReplayTracker tracker = CreateTracker(
            new SipManualMonotonicClock(0), 64, 300, 30);
        System.Threading.ManualResetEventSlim start =
            new System.Threading.ManualResetEventSlim(false);
        System.Threading.Tasks.Task<SipAuthenticationStatus>[] attempts =
            new System.Threading.Tasks.Task<SipAuthenticationStatus>[64];

        int index = 0;
        while (index < attempts.Length)
        {
            attempts[index] = System.Threading.Tasks.Task.Run(() =>
            {
                start.Wait();
                return tracker.CheckAndRecord(
                    "realm", "alice", "nonce", "cnonce", 1);
            });
            index = index + 1;
        }
        start.Set();
        System.Threading.Tasks.Task.WaitAll(attempts);

        int successes = 0;
        int replays = 0;
        index = 0;
        while (index < attempts.Length)
        {
            if (attempts[index].Result == SipAuthenticationStatus.Success)
            {
                successes = successes + 1;
            }
            if (attempts[index].Result == SipAuthenticationStatus.ReplayDetected)
            {
                replays = replays + 1;
            }
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(successes, Is.EqualTo(1));
            Assert.That(replays, Is.EqualTo(63));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(1));
        });
    }

    [Test]
    public void ConcurrentUniqueRequestsCannotExceedCapacity()
    {
        const int capacity = 8;
        SipNonceReplayTracker tracker = CreateTracker(
            new SipManualMonotonicClock(0), capacity, 300, 30);
        System.Threading.ManualResetEventSlim start =
            new System.Threading.ManualResetEventSlim(false);
        System.Threading.Tasks.Task<SipAuthenticationStatus>[] attempts =
            new System.Threading.Tasks.Task<SipAuthenticationStatus>[64];

        int index = 0;
        while (index < attempts.Length)
        {
            int requestIndex = index;
            attempts[index] = System.Threading.Tasks.Task.Run(() =>
            {
                start.Wait();
                return tracker.CheckAndRecord(
                    "realm", "alice", "nonce-" + requestIndex,
                    "cnonce", 1);
            });
            index = index + 1;
        }
        start.Set();
        System.Threading.Tasks.Task.WaitAll(attempts);

        int successes = 0;
        int failures = 0;
        index = 0;
        while (index < attempts.Length)
        {
            if (attempts[index].Result == SipAuthenticationStatus.Success)
            {
                successes = successes + 1;
            }
            if (attempts[index].Result == SipAuthenticationStatus.Failed)
            {
                failures = failures + 1;
            }
            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(successes, Is.EqualTo(capacity));
            Assert.That(failures, Is.EqualTo(attempts.Length - capacity));
            Assert.That(tracker.GetEntryCount(), Is.EqualTo(capacity));
        });
    }

    [Test]
    public void RejectsInvalidConfigurationAndTrackingInputs()
    {
        SipNonceOptions zeroCapacity = CreateOptions(0, 1, 0);
        SipNonceOptions zeroLifetime = CreateOptions(1, 0, 0);
        SipNonceOptions negativeSkew = CreateOptions(1, 1, -1);
        SipManualMonotonicClock clock = new SipManualMonotonicClock(0);
        SipNonceReplayTracker tracker = CreateTracker(clock, 1, 1, 0);

        Assert.Multiple(() =>
        {
            Assert.Throws<System.ArgumentNullException>(
                () => new SipNonceReplayTracker(null!, zeroCapacity));
            Assert.Throws<System.ArgumentNullException>(
                () => new SipNonceReplayTracker(clock, null));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new SipNonceReplayTracker(clock, zeroCapacity));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new SipNonceReplayTracker(clock, zeroLifetime));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new SipNonceReplayTracker(clock, negativeSkew));
            Assert.Throws<System.ArgumentException>(() => tracker.CheckAndRecord(
                "", "alice", "nonce", "cnonce", 1));
            Assert.Throws<System.ArgumentException>(() => tracker.CheckAndRecord(
                "realm", "", "nonce", "cnonce", 1));
            Assert.Throws<System.ArgumentException>(() => tracker.CheckAndRecord(
                "realm", "alice", "", "cnonce", 1));
            Assert.Throws<System.ArgumentException>(() => tracker.CheckAndRecord(
                "realm", "alice", "nonce", "", 1));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => tracker.CheckAndRecord(
                    "realm", "alice", "nonce", "cnonce", 0));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => tracker.CheckAndRecord(
                    "realm", "alice", "nonce", "cnonce", 4294967296L));
        });
    }

    static SipNonceReplayTracker CreateTracker(ISipMonotonicClock clock,
        int maximumEntries, int lifetimeSeconds, int clockSkewSeconds)
    {
        return new SipNonceReplayTracker(clock,
            CreateOptions(maximumEntries, lifetimeSeconds, clockSkewSeconds));
    }

    static SipNonceOptions CreateOptions(int maximumEntries,
        int lifetimeSeconds, int clockSkewSeconds)
    {
        SipNonceOptions options = new SipNonceOptions();
        options.MaxReplayEntries = maximumEntries;
        options.LifetimeSeconds = lifetimeSeconds;
        options.ClockSkewSeconds = clockSkewSeconds;
        return options;
    }
}
