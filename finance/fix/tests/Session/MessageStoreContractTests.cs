using System;
using System.IO;
using System.Threading.Tasks;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class MessageStoreContractTests
{
    internal string StoreDirectory = "";

    [SetUp]
    public void CreateContractDirectory()
    {
        StoreDirectory = Path.Combine(Path.GetTempPath(), "fix-store-contract-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(StoreDirectory);
    }

    [TearDown]
    public void RemoveContractDirectory()
    {
        if (Directory.Exists(StoreDirectory)) Directory.Delete(StoreDirectory, true);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImplementationsShareAtomicSequenceAndResetBehavior(bool fileBacked)
    {
        IMessageStore store = CreateStore(fileBacked, 0, "sequence");
        StoreRangeResult initial = store.RefreshStore();
        StoreRangeResult saved = store.SaveSequenceState(7, 1007);
        StoreRangeResult invalid = store.SaveSequenceState(8, 0);
        StoreRangeResult preserved = store.RefreshStore();

        Parallel.For(1, 33, value =>
        {
            StoreRangeResult concurrent = store.SaveSequenceState(value, value + 1000);
            Assert.That(concurrent.Success, Is.True, concurrent.Code);
        });
        StoreRangeResult atomic = store.RefreshStore();
        StoreRangeResult reset = store.ResetStore();

        Assert.Multiple(() =>
        {
            AssertState(initial, 1, 1);
            AssertState(saved, 7, 1007);
            Assert.That(invalid.Success, Is.False);
            Assert.That(invalid.Code, Is.EqualTo("invalid-sequence-state"));
            AssertState(preserved, 7, 1007);
            Assert.That(atomic.Success, Is.True, atomic.Code);
            Assert.That(atomic.NextTargetSeqNum, Is.EqualTo(atomic.NextSenderSeqNum + 1000));
            AssertState(reset, 1, 1);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImplementationsShareOrderedDetachedRangeBehavior(bool fileBacked)
    {
        IMessageStore store = CreateStore(fileBacked, 0, "ranges");
        StoredMessage first = Message(1, "D", 10);
        StoredMessage third = Message(3, "F", 30);
        StoreRangeResult firstStored = store.StoreMessage(first);
        StoreRangeResult thirdStored = store.StoreMessage(third);
        first.Bytes[0] = 99;
        first.MsgType = "MUTATED";

        StoreRangeResult range = store.ReadMessageRange(1, 0);
        range.Messages[0].Bytes[0] = 88;
        range.Messages[0].MsgType = "MUTATED-RESULT";
        StoreRangeResult reread = store.ReadMessageRange(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(firstStored.Success, Is.True, firstStored.Code);
            Assert.That(thirdStored.Success, Is.True, thirdStored.Code);
            Assert.That(range.BeginSeqNo, Is.EqualTo(1));
            Assert.That(range.EndSeqNo, Is.EqualTo(3));
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(range.HasGap, Is.True);
            Assert.That(reread.Messages[0].Bytes[0], Is.EqualTo(10));
            Assert.That(reread.Messages[0].MsgType, Is.EqualTo("D"));
            Assert.That(reread.Messages[0], Is.Not.SameAs(range.Messages[0]));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImplementationsShareCountRetentionBehavior(bool fileBacked)
    {
        IMessageStore store = CreateStore(fileBacked, 2, "retention");
        store.StoreMessage(Message(1, "D", 10));
        store.StoreMessage(Message(2, "F", 20));
        store.StoreMessage(Message(3, "G", 30));

        StoreRangeResult retained = store.ReadMessageRange(1, 3);

        Assert.Multiple(() =>
        {
            Assert.That(retained.Success, Is.True, retained.Code);
            Assert.That(retained.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(retained.HasGap, Is.True);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ImplementationsShareRecordLimitsAndNonMutationOnFailure(bool fileBacked)
    {
        IMessageStore store = CreateStore(fileBacked, 0, "validation");
        StoredMessage oversized = Message(1, "D", 10);
        oversized.Bytes = new byte[FixDefaults.MaxMessageSize + 1];
        StoredMessage longType = Message(1, new string('D', FileStoreRecordFormat.MaximumMessageTypeLength + 1), 10);
        StoredMessage nonAsciiType = Message(1, "D\u00e9", 10);

        StoreRangeResult missing = store.StoreMessage(null!);
        StoreRangeResult payload = store.StoreMessage(oversized);
        StoreRangeResult messageType = store.StoreMessage(longType);
        StoreRangeResult nonAscii = store.StoreMessage(nonAsciiType);
        StoreRangeResult valid = store.StoreMessage(Message(1, new string('D', FileStoreRecordFormat.MaximumMessageTypeLength), 10));
        StoreRangeResult range = store.ReadMessageRange(1, 1);

        Assert.Multiple(() =>
        {
            Assert.That(missing.Code, Is.EqualTo("missing-stored-message"));
            Assert.That(payload.Code, Is.EqualTo("invalid-record-payload"));
            Assert.That(messageType.Code, Is.EqualTo("invalid-record-message-type"));
            Assert.That(nonAscii.Code, Is.EqualTo("invalid-record-message-type"));
            Assert.That(valid.Success, Is.True, valid.Code);
            Assert.That(range.Messages, Has.Count.EqualTo(1));
            Assert.That(range.Messages[0].MsgType, Has.Length.EqualTo(FileStoreRecordFormat.MaximumMessageTypeLength));
        });
    }

    internal IMessageStore CreateStore(bool fileBacked, int maximumMessages, string name)
    {
        if (fileBacked) return new FileMessageStore(StoreDirectory, name, maximumMessages);
        return new MemoryMessageStore(maximumMessages);
    }

    internal static StoredMessage Message(int sequenceNumber, string messageType, byte firstByte)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, sequenceNumber, DateTimeKind.Utc),
            MsgType = messageType,
            Bytes = [firstByte, 1]
        };
    }

    internal static void AssertState(StoreRangeResult result, int sender, int target)
    {
        Assert.That(result.Success, Is.True, result.Code + ":" + result.Text);
        Assert.That(result.HasSequenceState, Is.True);
        Assert.That(result.NextSenderSeqNum, Is.EqualTo(sender));
        Assert.That(result.NextTargetSeqNum, Is.EqualTo(target));
    }
}
