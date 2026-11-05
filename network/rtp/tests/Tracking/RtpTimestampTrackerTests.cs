using rtp;

namespace rtp.Tests.Tracking;

public class RtpTimestampTrackerTests
{
    [Test]
    public void InitialForwardAndRepeatedTimestampsProduceExpectedUpdates()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();

        RtpTimestampUpdate empty = tracker.GetRtpTimestampState();
        RtpTimestampUpdate initial = tracker.ProcessRtpTimestamp(1000U, 90000);
        RtpTimestampUpdate forward = tracker.ProcessRtpTimestamp(4000U, 90000);
        RtpTimestampUpdate repeated = tracker.ProcessRtpTimestamp(4000U, 90000);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Accepted, Is.False);
            Assert.That(empty.ClockRate, Is.Zero);
            Assert.That(initial.Timestamp, Is.EqualTo(1000U));
            Assert.That(initial.ExtendedTimestamp, Is.EqualTo(1000L));
            Assert.That(initial.Delta, Is.Zero);
            Assert.That(initial.ClockRate, Is.EqualTo(90000));
            Assert.That(initial.Accepted, Is.True);
            Assert.That(initial.IsForward, Is.False);
            Assert.That(forward.ExtendedTimestamp, Is.EqualTo(4000L));
            Assert.That(forward.Delta, Is.EqualTo(3000L));
            Assert.That(forward.IsForward, Is.True);
            Assert.That(forward.IsReordered, Is.False);
            Assert.That(repeated.ExtendedTimestamp, Is.EqualTo(4000L));
            Assert.That(repeated.Delta, Is.Zero);
            Assert.That(repeated.Accepted, Is.True);
            Assert.That(repeated.IsForward, Is.False);
            Assert.That(repeated.IsReordered, Is.False);
        });
    }

    [Test]
    public void TimestampWrapAndPriorCycleReorderingUseOneExtendedTimeline()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0xFFFFFFF0U, 48000);

        RtpTimestampUpdate wrapped = tracker.ProcessRtpTimestamp(0x00000010U, 48000);
        RtpTimestampUpdate reordered = tracker.ProcessRtpTimestamp(0xFFFFFFF8U, 48000);
        RtpTimestampUpdate afterWrap = tracker.ProcessRtpTimestamp(0x00000020U, 48000);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.ExtendedTimestamp, Is.EqualTo(4294967312L));
            Assert.That(wrapped.Delta, Is.EqualTo(32L));
            Assert.That(wrapped.IsForward, Is.True);
            Assert.That(reordered.ExtendedTimestamp, Is.EqualTo(4294967288L));
            Assert.That(reordered.Delta, Is.EqualTo(-24L));
            Assert.That(reordered.IsReordered, Is.True);
            Assert.That(reordered.Accepted, Is.True);
            Assert.That(afterWrap.ExtendedTimestamp, Is.EqualTo(4294967328L));
            Assert.That(afterWrap.Delta, Is.EqualTo(16L));
            Assert.That(afterWrap.IsForward, Is.True);
        });
    }

    [Test]
    public void ReorderedTimestampBeforeInitialBaselineCanExtendNegative()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(10U, 8000);

        RtpTimestampUpdate reordered = tracker.ProcessRtpTimestamp(4294967290U, 8000);
        RtpTimestampUpdate resumed = tracker.ProcessRtpTimestamp(20U, 8000);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.ExtendedTimestamp, Is.EqualTo(-6L));
            Assert.That(reordered.Delta, Is.EqualTo(-16L));
            Assert.That(reordered.IsReordered, Is.True);
            Assert.That(resumed.ExtendedTimestamp, Is.EqualTo(20L));
            Assert.That(resumed.Delta, Is.EqualTo(10L));
            Assert.That(resumed.IsForward, Is.True);
        });
    }

    [Test]
    public void AmbiguousHalfRangeJumpResetsTheTimelineAsADiscontinuity()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(100U, 90000);

        RtpTimestampUpdate discontinuity =
            tracker.ProcessRtpTimestamp(2147483748U, 90000);
        RtpTimestampUpdate forward =
            tracker.ProcessRtpTimestamp(2147483758U, 90000);

        Assert.Multiple(() =>
        {
            Assert.That(discontinuity.Timestamp, Is.EqualTo(2147483748U));
            Assert.That(discontinuity.ExtendedTimestamp, Is.EqualTo(2147483748L));
            Assert.That(discontinuity.Delta, Is.Zero);
            Assert.That(discontinuity.Accepted, Is.True);
            Assert.That(discontinuity.IsDiscontinuity, Is.True);
            Assert.That(discontinuity.IsForward, Is.False);
            Assert.That(discontinuity.IsReordered, Is.False);
            Assert.That(forward.ExtendedTimestamp, Is.EqualTo(2147483758L));
            Assert.That(forward.Delta, Is.EqualTo(10L));
            Assert.That(forward.IsForward, Is.True);
        });
    }

    [Test]
    public void ExplicitResetEstablishesANewTimelineAndClockRate()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(90000U, 90000);
        tracker.ProcessRtpTimestamp(180000U, 90000);

        RtpTimestampUpdate reset = tracker.ResetRtpTimestampTracker(500U, 48000);
        RtpTimestampUpdate forward = tracker.ProcessRtpTimestamp(980U, 48000);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Timestamp, Is.EqualTo(500U));
            Assert.That(reset.ExtendedTimestamp, Is.EqualTo(500L));
            Assert.That(reset.Delta, Is.Zero);
            Assert.That(reset.ClockRate, Is.EqualTo(48000));
            Assert.That(reset.Accepted, Is.True);
            Assert.That(reset.IsDiscontinuity, Is.True);
            Assert.That(reset.ClockRateChanged, Is.True);
            Assert.That(forward.ExtendedTimestamp, Is.EqualTo(980L));
            Assert.That(forward.Delta, Is.EqualTo(480L));
            Assert.That(forward.IsForward, Is.True);
            Assert.That(forward.ClockRateChanged, Is.False);
        });
    }

    [Test]
    public void InvalidClockRatesPreserveState()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(1000U, 90000);

        Assert.Throws<RtpConfigurationException>(() =>
            tracker.ProcessRtpTimestamp(2000U, 0));
        Assert.Throws<RtpConfigurationException>(() =>
            tracker.ResetRtpTimestampTracker(2000U, -1));
        RtpTimestampUpdate state = tracker.GetRtpTimestampState();

        Assert.Multiple(() =>
        {
            Assert.That(state.Timestamp, Is.EqualTo(1000U));
            Assert.That(state.ExtendedTimestamp, Is.EqualTo(1000L));
            Assert.That(state.ClockRate, Is.EqualTo(90000));
            Assert.That(state.Accepted, Is.True);
            Assert.That(state.IsDiscontinuity, Is.False);
        });
    }

    [Test]
    public void ClockRateChangesStartNewIncompatibleTimestampDomains()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0U, 8000);
        tracker.ProcessRtpTimestamp(160U, 8000);
        tracker.ProcessRtpTimestamp(320U, 8000);
        tracker.ProcessRtpTimestamp(480U, 8000);

        RtpTimestampUpdate changedToSixteenKilohertz =
            tracker.ProcessRtpTimestamp(1280U, 16000);
        RtpTimestampUpdate continuedAtSixteenKilohertz =
            tracker.ProcessRtpTimestamp(1600U, 16000);
        RtpTimestampUpdate changedBackToEightKilohertz =
            tracker.ProcessRtpTimestamp(1120U, 8000);
        RtpTimestampUpdate continuedAtEightKilohertz =
            tracker.ProcessRtpTimestamp(1280U, 8000);

        Assert.Multiple(() =>
        {
            Assert.That(changedToSixteenKilohertz.Timestamp, Is.EqualTo(1280U));
            Assert.That(changedToSixteenKilohertz.ExtendedTimestamp, Is.EqualTo(1280L));
            Assert.That(changedToSixteenKilohertz.Delta, Is.Zero);
            Assert.That(changedToSixteenKilohertz.ClockRate, Is.EqualTo(16000));
            Assert.That(changedToSixteenKilohertz.Accepted, Is.True);
            Assert.That(changedToSixteenKilohertz.IsForward, Is.False);
            Assert.That(changedToSixteenKilohertz.IsReordered, Is.False);
            Assert.That(changedToSixteenKilohertz.IsDiscontinuity, Is.True);
            Assert.That(changedToSixteenKilohertz.ClockRateChanged, Is.True);
            Assert.That(continuedAtSixteenKilohertz.ExtendedTimestamp, Is.EqualTo(1600L));
            Assert.That(continuedAtSixteenKilohertz.Delta, Is.EqualTo(320L));
            Assert.That(continuedAtSixteenKilohertz.IsForward, Is.True);
            Assert.That(continuedAtSixteenKilohertz.IsDiscontinuity, Is.False);
            Assert.That(continuedAtSixteenKilohertz.ClockRateChanged, Is.False);
            Assert.That(changedBackToEightKilohertz.ExtendedTimestamp, Is.EqualTo(1120L));
            Assert.That(changedBackToEightKilohertz.Delta, Is.Zero);
            Assert.That(changedBackToEightKilohertz.ClockRate, Is.EqualTo(8000));
            Assert.That(changedBackToEightKilohertz.IsDiscontinuity, Is.True);
            Assert.That(changedBackToEightKilohertz.ClockRateChanged, Is.True);
            Assert.That(continuedAtEightKilohertz.ExtendedTimestamp, Is.EqualTo(1280L));
            Assert.That(continuedAtEightKilohertz.Delta, Is.EqualTo(160L));
            Assert.That(continuedAtEightKilohertz.IsForward, Is.True);
        });
    }

    [Test]
    public void RateTransitionDoesNotReuseWrapOrDirectionStateFromPriorDomain()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        tracker.ProcessRtpTimestamp(0xFFFFFFF0U, 48000);
        RtpTimestampUpdate wrapped = tracker.ProcessRtpTimestamp(0x00000010U, 48000);

        RtpTimestampUpdate changed = tracker.ProcessRtpTimestamp(0xFFFFFF00U, 90000);
        RtpTimestampUpdate continued = tracker.ProcessRtpTimestamp(0xFFFFFF20U, 90000);

        Assert.Multiple(() =>
        {
            Assert.That(wrapped.ExtendedTimestamp, Is.EqualTo(4294967312L));
            Assert.That(changed.ExtendedTimestamp, Is.EqualTo(4294967040L));
            Assert.That(changed.Delta, Is.Zero);
            Assert.That(changed.IsForward, Is.False);
            Assert.That(changed.IsReordered, Is.False);
            Assert.That(changed.IsDiscontinuity, Is.True);
            Assert.That(changed.ClockRateChanged, Is.True);
            Assert.That(continued.ExtendedTimestamp, Is.EqualTo(4294967072L));
            Assert.That(continued.Delta, Is.EqualTo(32L));
            Assert.That(continued.IsForward, Is.True);
            Assert.That(continued.ClockRateChanged, Is.False);
        });
    }

    [Test]
    public void ReturnedTimestampUpdatesAreIndependentSnapshots()
    {
        IRtpTimestampTracker tracker = new RtpTimestampTracker();
        RtpTimestampUpdate update = tracker.ProcessRtpTimestamp(300U, 8000);
        update.Timestamp = 999U;
        update.ExtendedTimestamp = -1L;
        update.Accepted = false;
        RtpTimestampUpdate snapshot = tracker.GetRtpTimestampState();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot, Is.Not.SameAs(update));
            Assert.That(snapshot.Timestamp, Is.EqualTo(300U));
            Assert.That(snapshot.ExtendedTimestamp, Is.EqualTo(300L));
            Assert.That(snapshot.ClockRate, Is.EqualTo(8000));
            Assert.That(snapshot.Accepted, Is.True);
        });
    }
}
