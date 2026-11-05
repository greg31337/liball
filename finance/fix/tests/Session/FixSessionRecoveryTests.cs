using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixSessionRecoveryTests
{
    [TestCase(false, 0, 0)]
    [TestCase(true, 0, 5)]
    [TestCase(false, 2, 3)]
    public void GapPoliciesProduceOpenClosedAndChunkedWireRanges(bool closed, int chunkSize, int expectedEnd)
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Settings.ClosedResendInterval = closed;
        fixture.Settings.ResendRequestChunkSize = chunkSize;
        RecreateSession(fixture);
        fixture.Activate();
        FixMessage high = fixture.PeerFactory.CreateStandardMessage("D", 6);
        fixture.Transport.AddInbound(fixture.Encode(high));

        fixture.Session.PollSession();
        fixture.Session.PollSession();
        DecodeResult decoded = fixture.DecodeSingle(fixture.Transport.OutboundBytes.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(decoded.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.ResendRequest));
            Assert.That(Text(decoded.Message, FixTags.BeginSeqNo), Is.EqualTo("2"));
            Assert.That(Text(decoded.Message, FixTags.EndSeqNo), Is.EqualTo(expectedEnd.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        });
    }

    [Test]
    public void OrdinaryResentMessagesReleaseQueuedHighMessageAndCompleteRecovery()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage fourth = fixture.PeerFactory.CreateStandardMessage("D", 4);
        fixture.Transport.AddInbound(fixture.Encode(fourth));
        SessionPollResult gap = fixture.Session.PollSession();
        fixture.Session.PollSession();
        fixture.Transport.OutboundBytes.Clear();

        FixMessage second = fixture.PeerFactory.CreateStandardMessage("D", 2);
        AddDuplicateFields(second);
        fixture.Transport.AddInbound(fixture.Encode(second));
        SessionPollResult firstResent = fixture.Session.PollSession();
        FixMessage third = fixture.PeerFactory.CreateStandardMessage("D", 3);
        AddDuplicateFields(third);
        fixture.Transport.AddInbound(fixture.Encode(third));
        SessionPollResult completed = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(gap.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(firstResent.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(firstResent.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(completed.State, Is.EqualTo(SessionState.Active));
            Assert.That(completed.Events.Exists(item => item.Kind == SessionEventKind.RecoveryCompleted), Is.True);
            Assert.That(completed.Snapshot.NextTargetSeqNum, Is.EqualTo(5));
            Assert.That(completed.Snapshot.InboundQueueCount, Is.Zero);
            Assert.That(fixture.Application.ApplicationContexts.ConvertAll(item => item.SequenceNumber), Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(fixture.Application.ApplicationContexts[0].IsPossibleDuplicate, Is.True);
            Assert.That(fixture.Application.ApplicationContexts[1].IsPossibleDuplicate, Is.True);
            Assert.That(fixture.Application.ApplicationContexts[2].IsPossibleDuplicate, Is.False);
        });
    }

    [Test]
    public void QueuedSequenceResetIsAppliedWhenAnEarlierGapFillMakesItExpected()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage queuedReset = fixture.PeerFactory.CreateSequenceResetMessage(5, 7, true);
        fixture.Transport.AddInbound(fixture.Encode(queuedReset));
        SessionPollResult queued = fixture.Session.PollSession();
        fixture.Session.PollSession();
        fixture.Transport.OutboundBytes.Clear();

        FixMessage firstFill = fixture.PeerFactory.CreateSequenceResetMessage(2, 5, true);
        fixture.Transport.AddInbound(fixture.Encode(firstFill));
        SessionPollResult completed = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(queued.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(queued.Snapshot.InboundQueueCount, Is.EqualTo(1));
            Assert.That(completed.State, Is.EqualTo(SessionState.Active));
            Assert.That(completed.Events.Exists(item => item.Kind == SessionEventKind.RecoveryCompleted), Is.True);
            Assert.That(completed.Snapshot.NextTargetSeqNum, Is.EqualTo(7));
            Assert.That(completed.Snapshot.InboundQueueCount, Is.Zero);
        });
    }

    [Test]
    public void StalePossibleDuplicateGapFillIsIgnoredWithoutChangingTarget()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage stale = fixture.PeerFactory.CreateSequenceResetMessage(1, 2, true);
        AddDuplicateFields(stale);
        fixture.Transport.AddInbound(fixture.Encode(stale));

        SessionPollResult result = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
            Assert.That(result.Events.Exists(item => item.Code == "stale-gap-fill-ignored"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(result.Snapshot.InboundQueueCount, Is.Zero);
        });
    }

    [Test]
    public void ResendRequestQueuesAdministrativeGapFillAndApplicationReplayWithoutConsumingSenderSequence()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage original = new FixMessage { MsgType = "D" };
        original.BodyFields.Add(FixSessionTests.Field(11, "ORDER-1"));
        Assert.That(fixture.Session.SendMessage(original).Accepted, Is.True);
        fixture.Session.PollSession();
        fixture.Transport.OutboundBytes.Clear();
        FixMessage request = fixture.PeerFactory.CreateResendRequestMessage(2, 1, 0);
        fixture.Transport.AddInbound(fixture.Encode(request));

        SessionPollResult replayQueued = fixture.Session.PollSession();
        SessionPollResult gapFillSent = fixture.Session.PollSession();
        DecodeResult gapFill = fixture.DecodeSingle(fixture.Transport.OutboundBytes.ToArray());
        fixture.Transport.OutboundBytes.Clear();
        SessionPollResult applicationSent = fixture.Session.PollSession();
        DecodeResult application = fixture.DecodeSingle(fixture.Transport.OutboundBytes.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(replayQueued.Snapshot.OutboundQueueCount, Is.EqualTo(2));
            Assert.That(gapFillSent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(gapFill.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.SequenceReset));
            Assert.That(Text(gapFill.Message, FixTags.MsgSeqNum), Is.EqualTo("1"));
            Assert.That(Text(gapFill.Message, FixTags.GapFillFlag), Is.EqualTo("Y"));
            Assert.That(Text(gapFill.Message, FixTags.NewSeqNo), Is.EqualTo("2"));
            Assert.That(applicationSent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(application.Message!.MsgType, Is.EqualTo("D"));
            Assert.That(Text(application.Message, FixTags.MsgSeqNum), Is.EqualTo("2"));
            Assert.That(Text(application.Message, FixTags.PossDupFlag), Is.EqualTo("Y"));
            Assert.That(EnvelopeValidator.FindFirstMessageField(application.Message, FixTags.OrigSendingTime), Is.Not.Null);
            Assert.That(applicationSent.Snapshot.NextSenderSeqNum, Is.EqualTo(3));
            Assert.That(fixture.Application.CallbackOrder, Does.Contain("replay"));
        });
    }

    private static void AddDuplicateFields(FixMessage message)
    {
        message.HeaderFields.Add(FixSessionTests.Field(FixTags.PossDupFlag, "Y"));
        message.HeaderFields.Add(FixSessionTests.Field(FixTags.OrigSendingTime, "20260830-17:59:59.000"));
    }

    private static void RecreateSession(FixSessionTests.SessionFixture fixture)
    {
        fixture.Session = new FixSession(fixture.Settings, fixture.Transport, fixture.Store, fixture.Log, fixture.Clock, fixture.Throttle, fixture.Application, fixture.Framer, fixture.Decoder, fixture.Encoder, fixture.Validator, fixture.Provider, fixture.Authenticator);
    }

    private static string Text(FixMessage message, int tag)
    {
        FixField field = EnvelopeValidator.FindFirstMessageField(message, tag)!;
        return System.Text.Encoding.Latin1.GetString(field.Value);
    }
}
