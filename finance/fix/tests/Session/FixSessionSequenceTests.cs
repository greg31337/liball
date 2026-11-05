using System.Text;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FixSessionSequenceTests
{
    [Test]
    public void ExpectedRejectedMessageConsumesDurableTargetAndQueuesSessionReject()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage invalid = fixture.PeerFactory.CreateStandardMessage("D", 2);
        SetText(invalid, FixTags.SenderCompID, "WRONG");
        fixture.Transport.AddInbound(fixture.Encode(invalid));

        SessionPollResult rejected = fixture.Session.PollSession();
        StoreRangeResult persisted = fixture.Store.RefreshStore();
        SessionPollResult sent = fixture.Session.PollSession();
        DecodeResult decoded = fixture.DecodeSingle(fixture.Transport.OutboundBytes.ToArray());

        Assert.Multiple(() =>
        {
            Assert.That(rejected.State, Is.EqualTo(SessionState.Active));
            Assert.That(rejected.Events.Exists(item => item.Code == "sender-comp-id-mismatch"), Is.True);
            Assert.That(rejected.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(rejected.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
            Assert.That(sent.Events.Exists(item => item.Kind == SessionEventKind.MessageSent), Is.True);
            Assert.That(decoded.Status, Is.EqualTo(DecodeStatus.Success));
            Assert.That(decoded.Message!.MsgType, Is.EqualTo(FixAdminMessageTypes.Reject));
        });
    }

    [Test]
    public void HigherMessageStartsRecoveryWithoutConsumingTargetSequence()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage high = fixture.PeerFactory.CreateStandardMessage("D", 4);
        fixture.Transport.AddInbound(fixture.Encode(high));

        SessionPollResult result = fixture.Session.PollSession();
        StoreRangeResult persisted = fixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Recovering));
            Assert.That(result.Events.Exists(item => item.Code == "sequence-gap-queued"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(result.Snapshot.InboundQueueCount, Is.EqualTo(1));
            Assert.That(result.Snapshot.OutboundQueueCount, Is.EqualTo(1));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
        });
    }

    [Test]
    public void LowerUnflaggedMessageDisconnectsWithoutChangingDurableTarget()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage low = fixture.PeerFactory.CreateStandardMessage("D", 1);
        fixture.Transport.AddInbound(fixture.Encode(low));

        SessionPollResult result = fixture.Session.PollSession();
        StoreRangeResult persisted = fixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(result.Events.Exists(item => item.Code == "target-sequence-too-low"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
            Assert.That(fixture.Transport.CloseCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void LowerPossibleDuplicateWithOriginalTimeIsIgnoredWithoutDisconnect()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage duplicate = fixture.PeerFactory.CreateStandardMessage("D", 1);
        AddDuplicateFields(duplicate);
        fixture.Transport.AddInbound(fixture.Encode(duplicate));

        SessionPollResult result = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
            Assert.That(result.Events.Exists(item => item.Code == "possible-duplicate-ignored"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
            Assert.That(fixture.Transport.CloseCount, Is.Zero);
        });
    }

    [Test]
    public void ExpectedPossibleDuplicateConsumesSequenceAndRetainsReplayContext()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage duplicate = fixture.PeerFactory.CreateStandardMessage("D", 2);
        AddDuplicateFields(duplicate);
        fixture.Transport.AddInbound(fixture.Encode(duplicate));

        SessionPollResult result = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(3));
            Assert.That(fixture.Application.ApplicationContexts, Has.Count.EqualTo(1));
            Assert.That(fixture.Application.ApplicationContexts[0].SequenceNumber, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts[0].IsPossibleDuplicate, Is.True);
        });
    }

    [Test]
    public void PossibleDuplicateWithoutOriginalTimeIsRejectedBeforeSequenceHandling()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        fixture.Activate();
        FixMessage invalidDuplicate = fixture.PeerFactory.CreateStandardMessage("D", 1);
        invalidDuplicate.HeaderFields.Add(FixSessionTests.Field(FixTags.PossDupFlag, "Y"));
        fixture.Transport.AddInbound(fixture.Encode(invalidDuplicate));

        SessionPollResult result = fixture.Session.PollSession();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Disconnected));
            Assert.That(result.Events.Exists(item => item.Code == "missing-orig-sending-time"), Is.True);
            Assert.That(result.Events.Exists(item => item.Code == "target-sequence-too-low"), Is.True);
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.ApplicationContexts, Is.Empty);
        });
    }

    [Test]
    public void PeerResetLogonAtomicallyRestartsSenderAndConsumesTargetOne()
    {
        FixSessionTests.SessionFixture fixture = new FixSessionTests.SessionFixture(false);
        Assert.That(fixture.Store.SaveSequenceState(7, 9).Success, Is.True);
        fixture.StartAndSendLogon();
        FixMessage resetLogon = fixture.PeerFactory.CreateLogonMessage(1, 1, true);
        fixture.Transport.AddInbound(fixture.Encode(resetLogon));

        SessionPollResult result = fixture.Session.PollSession();
        StoreRangeResult persisted = fixture.Store.RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.State, Is.EqualTo(SessionState.Active));
            Assert.That(result.Events.Exists(item => item.Code == "peer-logon-sequence-reset"), Is.True);
            Assert.That(result.Snapshot.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(result.Snapshot.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(persisted.NextSenderSeqNum, Is.EqualTo(1));
            Assert.That(persisted.NextTargetSeqNum, Is.EqualTo(2));
            Assert.That(fixture.Application.CallbackOrder, Is.EqualTo(new[] { "created", "administrative", "logon" }));
        });
    }

    private static void AddDuplicateFields(FixMessage message)
    {
        message.HeaderFields.Add(FixSessionTests.Field(FixTags.PossDupFlag, "Y"));
        message.HeaderFields.Add(FixSessionTests.Field(FixTags.OrigSendingTime, "20260830-17:59:59.000"));
    }

    private static void SetText(FixMessage message, int tag, string value)
    {
        FixField field = EnvelopeValidator.FindFirstMessageField(message, tag)!;
        field.Value = Encoding.Latin1.GetBytes(value);
    }
}
