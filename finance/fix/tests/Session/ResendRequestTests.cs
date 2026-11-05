using System.IO;
using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class ResendRequestTests
{
    [Test]
    public void DefaultGapRequestUsesOpenEndedRangeAndTracksHighestMessage()
    {
        GapTracker tracker = new GapTracker(new SessionSettings());

        ResendRequestActionResult initial = tracker.ObserveGap(2, 5);
        ResendRequestActionResult extended = tracker.ObserveGap(2, 9);

        Assert.Multiple(() =>
        {
            Assert.That(initial.Accepted, Is.True);
            Assert.That(initial.GenerateResendRequest, Is.True);
            Assert.That(initial.BeginSeqNo, Is.EqualTo(2));
            Assert.That(initial.EndSeqNo, Is.Zero);
            Assert.That(initial.GapActive, Is.True);
            Assert.That(initial.GapBeginSeqNo, Is.EqualTo(2));
            Assert.That(initial.GapEndSeqNo, Is.EqualTo(4));
            Assert.That(initial.HighestReceivedSeqNo, Is.EqualTo(5));
            Assert.That(extended.GenerateResendRequest, Is.False);
            Assert.That(extended.Code, Is.EqualTo("resend-request-outstanding"));
            Assert.That(extended.GapEndSeqNo, Is.EqualTo(8));
            Assert.That(extended.HighestReceivedSeqNo, Is.EqualTo(9));
            Assert.That(tracker.OutstandingEndSeqNo, Is.Zero);
        });
    }

    [Test]
    public void ClosedIntervalsSuppressOutstandingRequestsAndRequestNewExtensions()
    {
        SessionSettings settings = new SessionSettings { ClosedResendInterval = true };
        GapTracker tracker = new GapTracker(settings);

        ResendRequestActionResult initial = tracker.ObserveGap(2, 5);
        ResendRequestActionResult extended = tracker.ObserveGap(2, 8);
        ResendRequestActionResult next = tracker.ObserveGap(6, 8);

        Assert.Multiple(() =>
        {
            AssertRange(initial, 2, 4, false);
            Assert.That(extended.GenerateResendRequest, Is.False);
            Assert.That(extended.GapEndSeqNo, Is.EqualTo(7));
            AssertRange(next, 6, 7, false);
            Assert.That(next.GapBeginSeqNo, Is.EqualTo(6));
            Assert.That(tracker.OutstandingBeginSeqNo, Is.EqualTo(6));
            Assert.That(tracker.OutstandingEndSeqNo, Is.EqualTo(7));
        });
    }

    [Test]
    public void RedundantPolicyRepeatsRequestFromCurrentExpectedSequence()
    {
        SessionSettings settings = new SessionSettings
        {
            ClosedResendInterval = true,
            SendRedundantResendRequests = true
        };
        GapTracker tracker = new GapTracker(settings);

        ResendRequestActionResult initial = tracker.ObserveGap(10, 15);
        ResendRequestActionResult repeated = tracker.ObserveGap(10, 15);
        ResendRequestActionResult extended = tracker.ObserveGap(10, 18);

        Assert.Multiple(() =>
        {
            AssertRange(initial, 10, 14, false);
            AssertRange(repeated, 10, 14, true);
            Assert.That(repeated.Code, Is.EqualTo("redundant-resend-request-required"));
            AssertRange(extended, 10, 17, true);
            Assert.That(extended.GapEndSeqNo, Is.EqualTo(17));
        });
    }

    [Test]
    public void ChunkPolicyGeneratesNextBoundedRangeOnlyAfterCurrentChunkAdvances()
    {
        SessionSettings settings = new SessionSettings { ResendRequestChunkSize = 3 };
        GapTracker tracker = new GapTracker(settings);

        ResendRequestActionResult first = tracker.ObserveGap(2, 10);
        ResendRequestActionResult withinFirst = tracker.AdvanceGap(3);
        ResendRequestActionResult second = tracker.AdvanceGap(5);
        ResendRequestActionResult third = tracker.AdvanceGap(8);
        ResendRequestActionResult complete = tracker.AdvanceGap(10);

        Assert.Multiple(() =>
        {
            AssertRange(first, 2, 4, false);
            Assert.That(withinFirst.GenerateResendRequest, Is.False);
            Assert.That(withinFirst.Code, Is.EqualTo("sequence-gap-chunk-pending"));
            AssertRange(second, 5, 7, false);
            AssertRange(third, 8, 9, false);
            Assert.That(complete.Accepted, Is.True);
            Assert.That(complete.GenerateResendRequest, Is.False);
            Assert.That(complete.GapActive, Is.False);
            Assert.That(complete.Code, Is.EqualTo("sequence-gap-complete"));
            Assert.That(tracker.RequestOutstanding, Is.False);
        });
    }

    [Test]
    public void OpenEndedRequestRemainsOutstandingUntilGapCompletes()
    {
        GapTracker tracker = new GapTracker(new SessionSettings());
        tracker.ObserveGap(20, 25);

        ResendRequestActionResult progressed = tracker.AdvanceGap(23);
        ResendRequestActionResult complete = tracker.AdvanceGap(25);
        ResendRequestActionResult noGap = tracker.AdvanceGap(26);

        Assert.Multiple(() =>
        {
            Assert.That(progressed.GenerateResendRequest, Is.False);
            Assert.That(progressed.GapActive, Is.True);
            Assert.That(progressed.GapBeginSeqNo, Is.EqualTo(23));
            Assert.That(progressed.Code, Is.EqualTo("open-resend-request-outstanding"));
            Assert.That(complete.GapActive, Is.False);
            Assert.That(complete.Code, Is.EqualTo("sequence-gap-complete"));
            Assert.That(noGap.Accepted, Is.False);
            Assert.That(noGap.Code, Is.EqualTo("no-sequence-gap"));
        });
    }

    [Test]
    public void ResetClearsEveryTrackedRangeAndAllowsFreshGap()
    {
        GapTracker tracker = new GapTracker(new SessionSettings { ClosedResendInterval = true });
        tracker.ObserveGap(3, 7);

        ResendRequestActionResult reset = tracker.ResetGapTracking();
        ResendRequestActionResult fresh = tracker.ObserveGap(20, 22);

        Assert.Multiple(() =>
        {
            Assert.That(reset.Accepted, Is.True);
            Assert.That(reset.GapActive, Is.False);
            Assert.That(reset.GapBeginSeqNo, Is.Zero);
            Assert.That(reset.GapEndSeqNo, Is.Zero);
            Assert.That(reset.HighestReceivedSeqNo, Is.Zero);
            Assert.That(tracker.GapActive, Is.True);
            AssertRange(fresh, 20, 21, false);
        });
    }

    [Test]
    public void InvalidConfigurationRangesAndProgressAreRejectedWithoutMutation()
    {
        GapTracker tracker = new GapTracker(new SessionSettings());
        ResendProcessor processor = new ResendProcessor(new SessionSettings());

        ResendRequestActionResult equal = tracker.ObserveGap(5, 5);
        ResendRequestActionResult invalidRange = processor.CreateResendRequest(0, 4, false);
        tracker.ObserveGap(5, 8);
        ResendRequestActionResult staleObservation = tracker.ObserveGap(4, 9);
        ResendRequestActionResult staleProgress = tracker.AdvanceGap(4);

        Assert.Multiple(() =>
        {
            Assert.Throws<FixSessionException>(() => new GapTracker(null!));
            Assert.Throws<FixSessionException>(() => new GapTracker(new SessionSettings { ResendRequestChunkSize = -1 }));
            Assert.That(equal.Accepted, Is.False);
            Assert.That(equal.Code, Is.EqualTo("invalid-sequence-gap"));
            Assert.That(invalidRange.Accepted, Is.False);
            Assert.That(invalidRange.Code, Is.EqualTo("invalid-resend-request-range"));
            Assert.That(staleObservation.Accepted, Is.False);
            Assert.That(staleObservation.Code, Is.EqualTo("stale-gap-expectation"));
            Assert.That(staleProgress.Accepted, Is.False);
            Assert.That(staleProgress.Code, Is.EqualTo("stale-gap-progress"));
            Assert.That(tracker.GapBeginSeqNo, Is.EqualTo(5));
            Assert.That(tracker.GapEndSeqNo, Is.EqualTo(7));
        });
    }

    [Test]
    public void ChunkArithmeticCapsAtKnownGapWithoutOverflow()
    {
        SessionSettings settings = new SessionSettings { ResendRequestChunkSize = int.MaxValue };
        GapTracker tracker = new GapTracker(settings);

        ResendRequestActionResult result = tracker.ObserveGap(int.MaxValue - 2, int.MaxValue);

        AssertRange(result, int.MaxValue - 2, int.MaxValue - 1, false);
    }

    [Test]
    public void TextSettingsLoadResendPoliciesAndDefaults()
    {
        SessionSettings configured = LoadSettings("ClosedResendInterval=Y\nSendRedundantResendRequests=true\nResendRequestChunkSize=250");
        SessionSettings defaults = LoadSettings("");

        Assert.Multiple(() =>
        {
            Assert.That(configured.ClosedResendInterval, Is.True);
            Assert.That(configured.SendRedundantResendRequests, Is.True);
            Assert.That(configured.ResendRequestChunkSize, Is.EqualTo(250));
            Assert.That(defaults.ClosedResendInterval, Is.False);
            Assert.That(defaults.SendRedundantResendRequests, Is.False);
            Assert.That(defaults.ResendRequestChunkSize, Is.Zero);
            Assert.That(Assert.Throws<FixSessionException>(() => LoadSettings("ResendRequestChunkSize=-1"))!.Message, Does.StartWith("settings:invalid-integer"));
        });
    }

    private static void AssertRange(ResendRequestActionResult result, int beginSeqNo, int endSeqNo, bool redundant)
    {
        Assert.That(result.Accepted, Is.True);
        Assert.That(result.GenerateResendRequest, Is.True);
        Assert.That(result.BeginSeqNo, Is.EqualTo(beginSeqNo));
        Assert.That(result.EndSeqNo, Is.EqualTo(endSeqNo));
        Assert.That(result.Redundant, Is.EqualTo(redundant));
    }

    private static SessionSettings LoadSettings(string extra)
    {
        string text = "[SESSION]\nConnectionType=initiator\nBeginString=FIX.4.4\nSenderCompID=BUY\nTargetCompID=SELL\nEndpoint=localhost,9000\n" + extra;
        using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        return new TextSessionSettingsLoader().LoadSessionSettings(stream)[0];
    }
}
