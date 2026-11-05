using System;
using System.IO;
using System.Threading.Tasks;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FileMessageStoreTests
{
    private string StoreDirectory = "";

    [SetUp]
    public void CreateStoreDirectory()
    {
        StoreDirectory = Path.Combine(Path.GetTempPath(), "fix-file-store-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(StoreDirectory);
    }

    [TearDown]
    public void RemoveStoreDirectory()
    {
        if (Directory.Exists(StoreDirectory))
        {
            Directory.Delete(StoreDirectory, true);
        }
    }

    [Test]
    public void MessagesAndAtomicSequenceStateSurviveNewStoreInstances()
    {
        FileMessageStore firstStore = new FileMessageStore(StoreDirectory, "orders", 0);
        StoreRangeResult initial = firstStore.RefreshStore();
        StoreRangeResult saved = firstStore.SaveSequenceState(7, 9);
        StoredMessage firstMessage = Message(1, "D", false, 10, 11);
        StoreRangeResult firstStored = firstStore.StoreMessage(firstMessage);
        StoreRangeResult thirdStored = firstStore.StoreMessage(Message(3, "G", false, 30, 31));
        firstMessage.Bytes[0] = 99;

        FileMessageStore restarted = new FileMessageStore(StoreDirectory, "orders", 0);
        StoreRangeResult refreshed = restarted.RefreshStore();
        StoreRangeResult range = restarted.ReadMessageRange(1, 0);

        Assert.Multiple(() =>
        {
            AssertState(initial, 1, 1);
            AssertState(saved, 7, 9);
            Assert.That(firstStored.Success, Is.True, firstStored.Code);
            Assert.That(thirdStored.Success, Is.True, thirdStored.Code);
            AssertState(refreshed, 7, 9);
            Assert.That(range.Success, Is.True, range.Code);
            Assert.That(range.EndSeqNo, Is.EqualTo(3));
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(range.Messages[0].Bytes, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(range.HasGap, Is.True);
            Assert.That(File.Exists(Path.Combine(StoreDirectory, "orders.messages")), Is.True);
            Assert.That(File.Exists(Path.Combine(StoreDirectory, "orders.index")), Is.True);
            Assert.That(File.Exists(Path.Combine(StoreDirectory, "orders.sequence")), Is.True);
        });
    }

    [Test]
    public void RetentionAndResetAreDurableAcrossRestarts()
    {
        FileMessageStore store = new FileMessageStore(StoreDirectory, "retained", 2);
        store.SaveSequenceState(4, 5);
        store.StoreMessage(Message(1, "D", false, 1));
        store.StoreMessage(Message(2, "F", false, 2));
        store.StoreMessage(Message(3, "G", false, 3));

        FileMessageStore restarted = new FileMessageStore(StoreDirectory, "retained", 2);
        StoreRangeResult retained = restarted.ReadMessageRange(1, 3);
        StoreRangeResult reset = restarted.ResetStore();
        FileMessageStore afterReset = new FileMessageStore(StoreDirectory, "retained", 2);
        StoreRangeResult empty = afterReset.ReadMessageRange(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(retained.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 2, 3 }));
            Assert.That(retained.HasGap, Is.True);
            AssertState(reset, 1, 1);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Messages, Is.Empty);
            Assert.That(empty.HasGap, Is.True);
            Assert.That(new FileInfo(Path.Combine(StoreDirectory, "retained.messages")).Length, Is.Zero);
        });
    }

    [Test]
    public void RefreshTruncatesPartialAppendTailAndRebuildsAStaleIndex()
    {
        FileMessageStore store = new FileMessageStore(StoreDirectory, "recovery", 0);
        store.StoreMessage(Message(1, "D", false, 1));
        long validLength = new FileInfo(store.AppendPath).Length;
        using (FileStream append = new FileStream(store.AppendPath, FileMode.Append, FileAccess.Write, FileShare.None))
        {
            append.Write([1, 2, 3], 0, 3);
            append.Flush(true);
        }

        FileMessageStore partialRestart = new FileMessageStore(StoreDirectory, "recovery", 0);
        StoreRangeResult partial = partialRestart.RefreshStore();
        long recoveredLength = new FileInfo(partialRestart.AppendPath).Length;
        using (FileStream append = new FileStream(partialRestart.AppendPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreRecordFormat.WriteAppendRecord(append, Message(2, "F", false, 2));
            append.Flush(true);
        }
        FileMessageStore staleIndexRestart = new FileMessageStore(StoreDirectory, "recovery", 0);
        StoreRangeResult rebuilt = staleIndexRestart.RefreshStore();
        StoreRangeResult range = staleIndexRestart.ReadMessageRange(1, 2);

        Assert.Multiple(() =>
        {
            Assert.That(partial.Success, Is.True, partial.Text);
            Assert.That(partial.Code, Is.EqualTo("recovered-partial-message-tail"));
            Assert.That(recoveredLength, Is.EqualTo(validLength));
            Assert.That(new FileInfo(partialRestart.AppendPath).Length, Is.GreaterThan(validLength));
            Assert.That(rebuilt.Success, Is.True, rebuilt.Text);
            Assert.That(rebuilt.Code, Is.EqualTo("rebuilt-message-index"));
            Assert.That(range.Messages.ConvertAll(message => message.SequenceNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(range.HasGap, Is.False);
        });
    }

    [Test]
    public void CorruptAppendDataIsReportedAndResetRemainsAvailable()
    {
        FileMessageStore store = new FileMessageStore(StoreDirectory, "corrupt", 0);
        store.SaveSequenceState(7, 9);
        store.StoreMessage(Message(1, "D", false, 10, 11));
        using (FileStream append = new FileStream(store.AppendPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            append.Position = FileStoreRecordFormat.RecordHeaderSize + 1;
            int value = append.ReadByte();
            append.Position--;
            append.WriteByte((byte)(value ^ 1));
            append.Flush(true);
        }

        FileMessageStore restarted = new FileMessageStore(StoreDirectory, "corrupt", 0);
        StoreRangeResult corrupt = restarted.RefreshStore();
        StoreRangeResult reset = restarted.ResetStore();
        StoreRangeResult empty = restarted.ReadMessageRange(1, 0);

        Assert.Multiple(() =>
        {
            Assert.That(corrupt.Success, Is.False);
            Assert.That(corrupt.IsCorrupt, Is.True);
            Assert.That(corrupt.Code, Is.EqualTo("record-checksum-mismatch"));
            Assert.That(corrupt.HasSequenceState, Is.True);
            Assert.That(corrupt.NextSenderSeqNum, Is.EqualTo(7));
            Assert.That(corrupt.NextTargetSeqNum, Is.EqualTo(9));
            AssertState(reset, 1, 1);
            Assert.That(empty.Success, Is.True);
            Assert.That(empty.Messages, Is.Empty);
        });
    }

    [Test]
    public void CorruptSequenceSnapshotDoesNotClaimSequenceStateIsAvailable()
    {
        FileMessageStore store = new FileMessageStore(StoreDirectory, "bad-sequence", 0);
        store.SaveSequenceState(4, 5);
        using (FileStream sequence = new FileStream(store.SequencePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            sequence.Position = 20;
            int value = sequence.ReadByte();
            sequence.Position--;
            sequence.WriteByte((byte)(value ^ 1));
            sequence.Flush(true);
        }

        StoreRangeResult result = new FileMessageStore(StoreDirectory, "bad-sequence", 0).RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.IsCorrupt, Is.True);
            Assert.That(result.Code, Is.EqualTo("sequence-checksum-mismatch"));
            Assert.That(result.HasSequenceState, Is.False);
        });
    }

    [Test]
    public void HigherGenerationTemporarySequenceSnapshotWinsOnRestart()
    {
        FileMessageStore store = new FileMessageStore(StoreDirectory, "sequence", 0);
        store.SaveSequenceState(4, 5);
        using (FileStream temporary = new FileStream(store.SequenceTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreIndexFormat.WriteSequenceFile(temporary, 2, 8, 9);
            temporary.Flush(true);
        }

        FileMessageStore restarted = new FileMessageStore(StoreDirectory, "sequence", 0);
        StoreRangeResult refreshed = restarted.RefreshStore();

        Assert.Multiple(() =>
        {
            AssertState(refreshed, 8, 9);
            Assert.That(File.Exists(store.SequenceTemporaryPath), Is.False);
            Assert.That(new FileInfo(store.SequencePath).Length, Is.EqualTo(FileStoreIndexFormat.SequenceFileSize));
        });
    }

    [Test]
    public void ConcurrentStoreInstancesNeverExposeTornSequencePairs()
    {
        FileMessageStore first = new FileMessageStore(StoreDirectory, "locked", 0);
        FileMessageStore second = new FileMessageStore(StoreDirectory, "locked", 0);
        first.RefreshStore();

        Parallel.For(1, 25, index =>
        {
            FileMessageStore selected = index % 2 == 0 ? first : second;
            StoreRangeResult saved = selected.SaveSequenceState(index, index + 1000);
            Assert.That(saved.Success, Is.True, saved.Code);
        });
        StoreRangeResult final = new FileMessageStore(StoreDirectory, "locked", 0).RefreshStore();

        Assert.Multiple(() =>
        {
            Assert.That(final.Success, Is.True, final.Code);
            Assert.That(final.NextSenderSeqNum, Is.GreaterThanOrEqualTo(1));
            Assert.That(final.NextTargetSeqNum, Is.EqualTo(final.NextSenderSeqNum + 1000));
        });
    }

    [Test]
    public void InvalidNamesConfigurationAndNonIncreasingRecordsAreRejected()
    {
        Assert.Throws<FixStoreException>(() => new FileMessageStore(StoreDirectory, "../escape", 0));
        Assert.Throws<FixStoreException>(() => new FileMessageStore(StoreDirectory, "valid", -1));
        FileMessageStore store = new FileMessageStore(StoreDirectory, "validation", 0);
        StoreRangeResult first = store.StoreMessage(Message(2, "D", false, 2));
        StoreRangeResult duplicate = store.StoreMessage(Message(2, "F", false, 3));
        StoreRangeResult earlier = store.StoreMessage(Message(1, "G", false, 1));

        Assert.Multiple(() =>
        {
            Assert.That(first.Success, Is.True, first.Code);
            Assert.That(duplicate.Success, Is.False);
            Assert.That(duplicate.Code, Is.EqualTo("message-sequence-not-increasing"));
            Assert.That(earlier.Success, Is.False);
            Assert.That(earlier.Code, Is.EqualTo("message-sequence-not-increasing"));
        });
    }

    [Test]
    public void InvalidRangesAndCorruptRefreshesPropagateThroughEveryOperation()
    {
        FileMessageStore ranges = new FileMessageStore(StoreDirectory, "ranges", 0);
        StoreRangeResult invalid = ranges.ReadMessageRange(0, 1);
        StoreRangeResult inverted = ranges.ReadMessageRange(2, 1);
        FileMessageStore corrupt = new FileMessageStore(StoreDirectory, "propagate", 0);
        corrupt.RefreshStore();
        File.WriteAllBytes(corrupt.SequencePath, [1, 2, 3]);

        StoreRangeResult stored = corrupt.StoreMessage(Message(1, "D", false, 1));
        StoreRangeResult read = corrupt.ReadMessageRange(1, 1);
        StoreRangeResult saved = corrupt.SaveSequenceState(2, 2);

        Assert.Multiple(() =>
        {
            Assert.That(invalid.Code, Is.EqualTo("invalid-message-range"));
            Assert.That(inverted.Code, Is.EqualTo("inverted-message-range"));
            Assert.That(stored.Success, Is.False);
            Assert.That(read.Success, Is.False);
            Assert.That(saved.Success, Is.False);
            Assert.That(stored.IsCorrupt, Is.True);
            Assert.That(read.Code, Is.EqualTo(stored.Code));
            Assert.That(saved.Code, Is.EqualTo(stored.Code));
        });
    }

    [Test]
    public void SequenceRecoveryUsesTheOnlyValidSnapshotAndDiscardsOlderTemporaryState()
    {
        FileMessageStore temporaryOnly = new FileMessageStore(StoreDirectory, "temporary-only", 0);
        temporaryOnly.RefreshStore();
        File.Delete(temporaryOnly.SequencePath);
        using (FileStream temporary = new FileStream(temporaryOnly.SequenceTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreIndexFormat.WriteSequenceFile(temporary, 3, 8, 9);
        }
        StoreRangeResult recovered = temporaryOnly.RefreshStore();

        FileMessageStore olderTemporary = new FileMessageStore(StoreDirectory, "older-temporary", 0);
        olderTemporary.SaveSequenceState(5, 6);
        using (FileStream temporary = new FileStream(olderTemporary.SequenceTemporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
        {
            FileStoreIndexFormat.WriteSequenceFile(temporary, 0, 2, 3);
        }
        StoreRangeResult retained = olderTemporary.RefreshStore();

        Assert.Multiple(() =>
        {
            AssertState(recovered, 8, 9);
            Assert.That(File.Exists(temporaryOnly.SequenceTemporaryPath), Is.False);
            AssertState(retained, 5, 6);
            Assert.That(File.Exists(olderTemporary.SequenceTemporaryPath), Is.False);
        });
    }

    private static StoredMessage Message(int sequenceNumber, string msgType, bool administrative, params byte[] bytes)
    {
        return new StoredMessage
        {
            SequenceNumber = sequenceNumber,
            SendingTimeUtc = new DateTime(2026, 8, 30, 12, 0, sequenceNumber, DateTimeKind.Utc),
            MsgType = msgType,
            IsAdministrative = administrative,
            Bytes = bytes
        };
    }

    private static void AssertState(StoreRangeResult result, int sender, int target)
    {
        Assert.That(result.Success, Is.True, result.Code + ":" + result.Text);
        Assert.That(result.HasSequenceState, Is.True);
        Assert.That(result.NextSenderSeqNum, Is.EqualTo(sender));
        Assert.That(result.NextTargetSeqNum, Is.EqualTo(target));
    }
}
