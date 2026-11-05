namespace sip.Tests;

public class SipTimerSchedulerTests
{
    [Test]
    public void SchedulerCopiesAndExposesConfiguredTimerIntervals()
    {
        SipTimerOptions options = Options(200, 800, 1500, 4);
        SipTimerScheduler scheduler = new SipTimerScheduler(options,
            new SipManualMonotonicClock(100));

        options.T1Milliseconds = 1;
        options.T2Milliseconds = 1;
        options.T4Milliseconds = 1;
        options.MaxTimerCount = 1;

        Assert.Multiple(() =>
        {
            Assert.That(scheduler.GetT1Milliseconds(), Is.EqualTo(200));
            Assert.That(scheduler.GetT2Milliseconds(), Is.EqualTo(800));
            Assert.That(scheduler.GetT4Milliseconds(), Is.EqualTo(1500));
            Assert.That(scheduler.GetMaximumTimerCount(), Is.EqualTo(4));
            Assert.That(scheduler.GetCurrentMilliseconds(), Is.EqualTo(100));
        });
    }

    [Test]
    public void ExplicitAdvancementFiresAtDeadlineAndCleansUpDueTimers()
    {
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 5),
            new SipManualMonotonicClock(1000));
        long last = scheduler.ScheduleTimer("tx-last", SipTimerNames.TimerB, 50);
        long immediate = scheduler.ScheduleTimer("tx-now", SipTimerNames.TimerA, 0);
        long middle = scheduler.ScheduleTimer("tx-middle", SipTimerNames.TimerE, 25);

        SipScheduledTimer[] first = scheduler.AdvanceTime(1000);
        SipScheduledTimer[] early = scheduler.AdvanceTime(1024);
        SipScheduledTimer[] second = scheduler.AdvanceTime(1025);
        SipScheduledTimer[] third = scheduler.AdvanceTime(1050);

        Assert.Multiple(() =>
        {
            Assert.That(first.Select(timer => timer.Id), Is.EqualTo(new long[] { immediate }));
            Assert.That(first[0].DeadlineMilliseconds, Is.EqualTo(1000));
            Assert.That(early, Is.Empty);
            Assert.That(second.Select(timer => timer.Id), Is.EqualTo(new long[] { middle }));
            Assert.That(third.Select(timer => timer.Id), Is.EqualTo(new long[] { last }));
            Assert.That(scheduler.GetTimerCount(), Is.Zero);
            Assert.That(scheduler.CancelTimer(immediate), Is.False);
        });
    }

    [Test]
    public void EqualDeadlinesPreserveSchedulingOrder()
    {
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 8),
            new SipManualMonotonicClock(10));
        long first = scheduler.ScheduleTimer("tx", SipTimerNames.TimerA, 50);
        long second = scheduler.ScheduleTimer("tx", SipTimerNames.TimerB, 50);
        long earlier = scheduler.ScheduleTimer("tx", SipTimerNames.TimerC, 20);
        long third = scheduler.ScheduleTimer("tx", SipTimerNames.TimerD, 50);

        SipScheduledTimer[] firstDue = scheduler.AdvanceTime(30);
        SipScheduledTimer[] due = scheduler.AdvanceTime(60);

        Assert.Multiple(() =>
        {
            Assert.That(firstDue.Select(timer => timer.Id), Is.EqualTo(new long[] { earlier }));
            Assert.That(due.Select(timer => timer.Id),
                Is.EqualTo(new long[] { first, second, third }));
            Assert.That(due.Select(timer => timer.Name),
                Is.EqualTo(new string[] { SipTimerNames.TimerA, SipTimerNames.TimerB,
                    SipTimerNames.TimerD }));
        });
    }

    [Test]
    public void CancellationAndOwnerCleanupReleaseCapacityImmediately()
    {
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 3),
            new SipManualMonotonicClock(0));
        long first = scheduler.ScheduleTimer("tx-one", SipTimerNames.TimerA, 10);
        long second = scheduler.ScheduleTimer("tx-one", SipTimerNames.TimerB, 20);
        long third = scheduler.ScheduleTimer("tx-two", SipTimerNames.TimerC, 30);

        Assert.Throws<InvalidOperationException>(() =>
            scheduler.ScheduleTimer("full", SipTimerNames.TimerD, 40));
        Assert.That(scheduler.CancelTimer(second), Is.True);
        long replacement = scheduler.ScheduleTimer("tx-three", SipTimerNames.TimerD, 40);
        Assert.That(scheduler.CancelOwnerTimers("tx-one"), Is.EqualTo(1));
        Assert.That(scheduler.CancelOwnerTimers("tx-one"), Is.Zero);

        SipScheduledTimer[] remaining = scheduler.AdvanceTime(100);

        Assert.Multiple(() =>
        {
            Assert.That(remaining.Select(timer => timer.Id),
                Is.EqualTo(new long[] { third, replacement }));
            Assert.That(remaining.Select(timer => timer.Id), Does.Not.Contain(first));
            Assert.That(remaining.Select(timer => timer.Id), Does.Not.Contain(second));
            Assert.That(scheduler.GetTimerCount(), Is.Zero);
            Assert.That(scheduler.ClearTimers(), Is.Zero);
        });
    }

    [Test]
    public void TimerSnapshotsAreDetachedAndRemainDeadlineOrdered()
    {
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 3),
            new SipManualMonotonicClock(50));
        scheduler.ScheduleTimer("later", SipTimerNames.TimerB, 20);
        scheduler.ScheduleTimer("earlier", SipTimerNames.TimerA, 10);

        SipScheduledTimer[] snapshot = scheduler.CopyTimers();
        snapshot[0].OwnerId = "changed";
        snapshot[0].Name = "changed";
        snapshot[0].DeadlineMilliseconds = 0;
        SipScheduledTimer[] unchanged = scheduler.CopyTimers();

        Assert.Multiple(() =>
        {
            Assert.That(unchanged.Select(timer => timer.OwnerId),
                Is.EqualTo(new string[] { "earlier", "later" }));
            Assert.That(unchanged.Select(timer => timer.Name),
                Is.EqualTo(new string[] { SipTimerNames.TimerA, SipTimerNames.TimerB }));
            Assert.That(unchanged.Select(timer => timer.DeadlineMilliseconds),
                Is.EqualTo(new long[] { 60, 70 }));
            Assert.That(scheduler.ClearTimers(), Is.EqualTo(2));
        });
    }

    [Test]
    public void ManualClockAdvancementAndDeadlineOverflowRemainMonotonic()
    {
        SipManualMonotonicClock clock = new SipManualMonotonicClock(500);
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 2), clock);
        long timerId = scheduler.ScheduleTimer("tx", SipTimerNames.TimerA, 25);

        clock.AdvanceMilliseconds(24);
        Assert.That(scheduler.AdvanceClock(), Is.Empty);
        clock.AdvanceMilliseconds(1);
        Assert.That(scheduler.AdvanceClock().Select(timer => timer.Id),
            Is.EqualTo(new long[] { timerId }));
        Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.AdvanceTime(524));

        SipTimerScheduler overflow = new SipTimerScheduler(Options(100, 400, 500, 1),
            new SipManualMonotonicClock(long.MaxValue - 5));
        overflow.ScheduleTimer("overflow", SipTimerNames.TimerB, 10);

        Assert.Multiple(() =>
        {
            Assert.That(overflow.CopyTimers()[0].DeadlineMilliseconds, Is.EqualTo(long.MaxValue));
            Assert.That(overflow.AdvanceTime(long.MaxValue - 1), Is.Empty);
            Assert.That(overflow.AdvanceTime(long.MaxValue).Length, Is.EqualTo(1));
        });
    }

    [Test]
    public void CapacityAdmissionIsAtomicAndSameDeadlineOrderUsesTimerIds()
    {
        const int capacity = 40;
        SipTimerScheduler scheduler = new SipTimerScheduler(
            Options(100, 400, 500, capacity), new SipManualMonotonicClock(0));
        System.Collections.Concurrent.ConcurrentBag<long> accepted = new();

        Parallel.For(0, 400, index =>
        {
            try
            {
                accepted.Add(scheduler.ScheduleTimer("tx-" + index, SipTimerNames.TimerA, 10));
            }
            catch (InvalidOperationException)
            {
            }
        });

        SipScheduledTimer[] due = scheduler.AdvanceTime(10);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.Count, Is.EqualTo(capacity));
            Assert.That(due.Length, Is.EqualTo(capacity));
            Assert.That(due.Select(timer => timer.Id), Is.Ordered.Ascending);
            Assert.That(due.Select(timer => timer.Id).Distinct().Count(), Is.EqualTo(capacity));
        });
    }

    [Test]
    public void InjectedClockIsReadWithoutHoldingSchedulerLock()
    {
        BlockingClock clock = new BlockingClock(100);
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 2), clock);
        System.Threading.Volatile.Write(ref clock.Blocking, 1);

        Task<SipScheduledTimer[]> advancing = Task.Run(() => scheduler.AdvanceClock());
        bool entered = clock.Entered.Wait(5000);
        Task<long> scheduling = Task.Run(() =>
            scheduler.ScheduleTimer("tx", SipTimerNames.TimerA, 10));
        bool completedWhileClockBlocked = scheduling.Wait(5000);
        clock.Release.Set();
        bool advancementCompleted = advancing.Wait(5000);

        Assert.Multiple(() =>
        {
            Assert.That(entered, Is.True);
            Assert.That(completedWhileClockBlocked, Is.True);
            Assert.That(advancementCompleted, Is.True);
            Assert.That(scheduling.Result, Is.GreaterThan(0));
        });
    }

    [Test]
    public void InvalidConfigurationAndSchedulingInputsAreRejected()
    {
        SipTimerOptions badT1 = Options(0, 400, 500, 1);
        SipTimerOptions badT2 = Options(500, 400, 500, 1);
        SipTimerOptions badT4 = Options(100, 400, 0, 1);
        SipTimerOptions badCapacity = Options(100, 400, 500, 0);
        SipTimerScheduler scheduler = new SipTimerScheduler(Options(100, 400, 500, 1),
            new SipManualMonotonicClock(0));

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => new SipTimerScheduler(null,
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentNullException>(() => new SipTimerScheduler(
                Options(100, 400, 500, 1), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SipTimerScheduler(badT1,
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SipTimerScheduler(badT2,
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SipTimerScheduler(badT4,
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SipTimerScheduler(badCapacity,
                new SipManualMonotonicClock(0)));
            Assert.Throws<ArgumentException>(() => scheduler.ScheduleTimer(null,
                SipTimerNames.TimerA, 1));
            Assert.Throws<ArgumentException>(() => scheduler.ScheduleTimer("tx", "not a timer", 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => scheduler.ScheduleTimer("tx",
                SipTimerNames.TimerA, -1));
            Assert.Throws<ArgumentException>(() => scheduler.CancelOwnerTimers(""));
            Assert.That(scheduler.CancelTimer(0), Is.False);
        });
    }

    static SipTimerOptions Options(int t1, int t2, int t4, int capacity)
    {
        SipTimerOptions options = new SipTimerOptions();
        options.T1Milliseconds = t1;
        options.T2Milliseconds = t2;
        options.T4Milliseconds = t4;
        options.MaxTimerCount = capacity;
        return options;
    }

    class BlockingClock : ISipMonotonicClock
    {
        internal long Milliseconds;
        internal int Blocking;
        internal System.Threading.ManualResetEventSlim Entered;
        internal System.Threading.ManualResetEventSlim Release;

        internal BlockingClock(long milliseconds)
        {
            Milliseconds = milliseconds;
            Entered = new System.Threading.ManualResetEventSlim(false);
            Release = new System.Threading.ManualResetEventSlim(false);
        }

        public long GetMilliseconds()
        {
            if (System.Threading.Volatile.Read(ref Blocking) != 0)
            {
                Entered.Set();
                Release.Wait();
            }
            return Milliseconds;
        }
    }
}
