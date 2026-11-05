using System;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class MemoryMessageStoreTests
{
    [Test]
    public void SequenceStateUpdatesAtomicallyAndResetRestoresInitialState()
    {
        IMessageStore store = new MemoryMessageStore(0);

        StoreRangeResult initial = store.RefreshStore();
        StoreRangeResult saved = store.SaveSequenceState(7, 9);
        StoreRangeResult rejected = store.SaveSequenceState(8, 0);
        StoreRangeResult refreshed = store.RefreshStore();
        store.StoreMessage(Message(7, "D", 70));
        StoreRangeResult reset = store.ResetStore();
        StoreRangeResult afterReset = store.ReadMessageRange(1, 0);

        Assert.Multiple(() =>
        {
            AssertSequenceState(initial, 1, 1);
            AssertSequenceState(saved, 7, 9);
            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Code, Is.EqualTo("invalid-sequence-state"));
            Assert.That(rejected.HasSequenceState, Is.True);
            Assert.That(rejected.NextSenderSeqNum, Is.EqualTo(7));
            Assert.That(rejected.NextTargetSeqNum, Is.EqualTo(9));
            AssertSequenceState(refreshed, 7, 9);
            AssertSequenceState(reset, 1, 1);
            Assert.That(afterReset.Success, Is.True);
            Assert.That(afterReset.Messages, Is.Empty);
            Assert.That(afterReset.HasGap, Is.True);
        });
    }

    [Test]
    public void RangeReadsAreInclusiveOrderedAndDetachedFromCallerMutation()
    {
        IMessageStore store = new MemoryMessageStore(0);
        StoredMessage third = Message(3, "G", 30);
        StoredMessage first = Message(1, "D", 10);
        StoredMessage second = Message(2, "F", 20);

        store.StoreMessage(third);
        store.StoreMessage(first);
        store.StoreMessage(second);
        first.Bytes[0] = 99;
        first.MsgType = "MUTATED";
        StoreRangeResult range = store.ReadMessageRange(1, 0);
        range.Messages[0].Bytes[0] = 88;
        StoreRangeResult reread = store.ReadMessageRange(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(range.Success, Is.True);
            Assert.That(range.BeginSeqNo, Is.EqualTo(1));
            Assert.That(range.EndSeqNo, Is.EqualTo(3));
            Assert.That(range.HasGap, Is.False);
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(range.Messages.ConvertAll(message => message.MsgType), Is.EqualTo(new[] { "D", "F", "G" }));
            Assert.That(reread.Messages[0].Bytes[0], Is.EqualTo(10));
            Assert.That(reread.Messages[0], Is.Not.SameAs(range.Messages[0]));
        });
    }

    [Test]
    public void MissingSequencesAndInvalidBoundsHaveExplicitOutcomes()
    {
        IMessageStore store = new MemoryMessageStore(0);
        store.StoreMessage(Message(2, "D", 20));
        store.StoreMessage(Message(4, "F", 40));

        StoreRangeResult gap = store.ReadMessageRange(1, 4);
        StoreRangeResult openEndedGap = store.ReadMessageRange(3, 0);
        StoreRangeResult beyondLatest = store.ReadMessageRange(5, 0);
        StoreRangeResult invalid = store.ReadMessageRange(0, 4);
        StoreRangeResult inverted = store.ReadMessageRange(4, 3);

        Assert.Multiple(() =>
        {
            Assert.That(gap.Success, Is.True);
            Assert.That(gap.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 2, 4 }));
            Assert.That(gap.HasGap, Is.True);
            Assert.That(openEndedGap.EndSeqNo, Is.EqualTo(4));
            Assert.That(openEndedGap.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 4 }));
            Assert.That(openEndedGap.HasGap, Is.True);
            Assert.That(beyondLatest.EndSeqNo, Is.EqualTo(4));
            Assert.That(beyondLatest.Messages, Is.Empty);
            Assert.That(beyondLatest.HasGap, Is.True);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("invalid-message-range"));
            Assert.That(inverted.Success, Is.False);
            Assert.That(inverted.Code, Is.EqualTo("inverted-message-range"));
        });
    }

    [Test]
    public void RetentionKeepsTheHighestConfiguredSequenceNumbers()
    {
        IMessageStore store = new MemoryMessageStore(2);
        store.StoreMessage(Message(2, "D", 20));
        store.StoreMessage(Message(4, "G", 40));
        store.StoreMessage(Message(3, "F", 30));
        store.StoreMessage(Message(1, "A", 10));
        store.StoreMessage(Message(3, "F", 31));

        StoreRangeResult range = store.ReadMessageRange(1, 4);

        Assert.Multiple(() =>
        {
            Assert.That(range.Success, Is.True);
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 3, 4 }));
            Assert.That(range.Messages[0].Bytes[0], Is.EqualTo(31));
            Assert.That(range.HasGap, Is.True);
        });
    }

    [Test]
    public void InvalidConfigurationAndRecordsFailWithoutMutatingTheStore()
    {
        Assert.Throws<FixStoreException>(() => new MemoryMessageStore(-1));
        IMessageStore store = new MemoryMessageStore(0);
        StoredMessage missingBytes = Message(1, "D", 10);
        missingBytes.Bytes = null!;
        StoredMessage emptyBytes = Message(1, "D", 10);
        emptyBytes.Bytes = [];

        StoreRangeResult invalidSequence = store.StoreMessage(Message(0, "D", 10));
        StoreRangeResult invalidTime = store.StoreMessage(new StoredMessage { SequenceNumber = 1, SendingTimeUtc = DateTime.Now, MsgType = "D", Bytes = [1] });
        StoreRangeResult missingType = store.StoreMessage(new StoredMessage { SequenceNumber = 1, SendingTimeUtc = DateTime.UtcNow, Bytes = [1] });
        StoreRangeResult missingPayload = store.StoreMessage(missingBytes);
        StoreRangeResult emptyPayload = store.StoreMessage(emptyBytes);
        StoreRangeResult range = store.ReadMessageRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(invalidSequence.Code, Is.EqualTo("invalid-message-sequence-number"));
            Assert.That(invalidTime.Code, Is.EqualTo("invalid-stored-message-time"));
            Assert.That(missingType.Code, Is.EqualTo("missing-stored-message-type"));
            Assert.That(missingPayload.Code, Is.EqualTo("missing-stored-message-bytes"));
            Assert.That(emptyPayload.Code, Is.EqualTo("missing-stored-message-bytes"));
            Assert.That(range.Messages, Is.Empty);
            Assert.That(range.HasGap, Is.True);
        });
    }

    private static StoredMessage Message(int sequenceNumber, string msgType, byte firstByte)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, sequenceNumber < 60 ? sequenceNumber : 0, DateTimeKind.Utc),
            Bytes = [firstByte, 1],
            MsgType = msgType,
            IsAdministrative = msgType == FixAdminMessageTypes.Logon
        };
    }

    private static void AssertSequenceState(StoreRangeResult result, int sender, int target)
    {
        Assert.That(result.Success, Is.True, result.Code + ":" + result.Text);
        Assert.That(result.HasSequenceState, Is.True);
        Assert.That(result.NextSenderSeqNum, Is.EqualTo(sender));
        Assert.That(result.NextTargetSeqNum, Is.EqualTo(target));
    }
}
