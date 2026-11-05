using System;
using System.IO;
using fix;
using NUnit.Framework;

namespace fix.Tests;

public class FileStoreFormatTests
{
    [Test]
    public void AppendRecordsRoundTripDeterministicallyWithOffsetsAndChecksums()
    {
        StoredMessage first = Message(1, "D", false, 10, 11);
        StoredMessage second = Message(3, FixAdminMessageTypes.SequenceReset, true, 30, 31);
        using MemoryStream stream = new MemoryStream();

        FileStoreRecordReadResult firstWrite = FileStoreRecordFormat.WriteAppendRecord(stream, first);
        FileStoreRecordReadResult secondWrite = FileStoreRecordFormat.WriteAppendRecord(stream, second);
        byte[] encoded = stream.ToArray();
        FileStoreScanResult scan = FileStoreRecordFormat.ScanAppendFile(stream);
        using MemoryStream duplicate = new MemoryStream();
        FileStoreRecordFormat.WriteAppendRecord(duplicate, first);
        FileStoreRecordFormat.WriteAppendRecord(duplicate, second);

        Assert.Multiple(() =>
        {
            Assert.That(firstWrite.Success, Is.True, firstWrite.Code);
            Assert.That(secondWrite.Success, Is.True, secondWrite.Code);
            Assert.That(firstWrite.Record, Is.Not.Null);
            Assert.That(secondWrite.Record, Is.Not.Null);
            Assert.That(firstWrite.Record!.Offset, Is.Zero);
            Assert.That(secondWrite.Record!.Offset, Is.EqualTo(firstWrite.Record.Length));
            Assert.That(scan.Success, Is.True, scan.Code);
            Assert.That(scan.ValidLength, Is.EqualTo(encoded.Length));
            Assert.That(scan.Records.ConvertAll(record => record.Message.SequenceNumber), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(scan.Records[0].Message.Bytes, Is.EqualTo(new byte[] { 10, 11 }));
            Assert.That(scan.Records[1].Message.MsgType, Is.EqualTo(FixAdminMessageTypes.SequenceReset));
            Assert.That(scan.Records[1].Message.IsAdministrative, Is.True);
            Assert.That(scan.Records[0].Checksum, Is.GreaterThan(0));
            Assert.That(duplicate.ToArray(), Is.EqualTo(encoded));
        });
    }

    [Test]
    public void AppendScannerRetainsTheValidPrefixWhenTheTailIsPartial()
    {
        using MemoryStream complete = new MemoryStream();
        FileStoreRecordReadResult first = FileStoreRecordFormat.WriteAppendRecord(complete, Message(1, "D", false, 1));
        FileStoreRecordFormat.WriteAppendRecord(complete, Message(2, "F", false, 2));
        byte[] truncatedBytes = TruncateLastByte(complete.ToArray());
        using MemoryStream truncated = new MemoryStream(truncatedBytes);

        FileStoreScanResult result = FileStoreRecordFormat.ScanAppendFile(truncated);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.IsPartialTail, Is.True);
            Assert.That(result.IsCorrupt, Is.False);
            Assert.That(result.Code, Is.EqualTo("partial-record-tail"));
            Assert.That(result.Records, Has.Count.EqualTo(1));
            Assert.That(result.ValidLength, Is.EqualTo(first.Record!.Length));
        });
    }

    [Test]
    public void AppendScannerDistinguishesChecksumAndSequenceCorruption()
    {
        using MemoryStream complete = new MemoryStream();
        FileStoreRecordFormat.WriteAppendRecord(complete, Message(1, "D", false, 10, 11));
        byte[] damagedBytes = complete.ToArray();
        damagedBytes[FileStoreRecordFormat.RecordHeaderSize + 1] ^= 1;
        using MemoryStream damaged = new MemoryStream(damagedBytes);
        FileStoreScanResult checksum = FileStoreRecordFormat.ScanAppendFile(damaged);

        using MemoryStream duplicateSequence = new MemoryStream();
        FileStoreRecordFormat.WriteAppendRecord(duplicateSequence, Message(2, "D", false, 20));
        FileStoreRecordFormat.WriteAppendRecord(duplicateSequence, Message(2, "F", false, 21));
        FileStoreScanResult sequence = FileStoreRecordFormat.ScanAppendFile(duplicateSequence);

        Assert.Multiple(() =>
        {
            Assert.That(checksum.IsCorrupt, Is.True);
            Assert.That(checksum.IsPartialTail, Is.False);
            Assert.That(checksum.Code, Is.EqualTo("record-checksum-mismatch"));
            Assert.That(checksum.ValidLength, Is.Zero);
            Assert.That(sequence.IsCorrupt, Is.True);
            Assert.That(sequence.Code, Is.EqualTo("record-sequence-not-increasing"));
            Assert.That(sequence.Records, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void FixedWidthIndexRoundTripsAndValidatesAppendBounds()
    {
        using MemoryStream append = new MemoryStream();
        FileStoreRecord first = FileStoreRecordFormat.WriteAppendRecord(append, Message(1, "D", false, 1)).Record!;
        FileStoreRecord second = FileStoreRecordFormat.WriteAppendRecord(append, Message(2, "F", false, 2)).Record!;
        using MemoryStream index = new MemoryStream();
        FileStoreIndexFormat.InitializeIndexFile(index);

        bool firstAdded = FileStoreIndexFormat.AppendIndexEntry(index, first);
        bool secondAdded = FileStoreIndexFormat.AppendIndexEntry(index, second);
        FileStoreIndexReadResult valid = FileStoreIndexFormat.ReadIndexFile(index, append.Length);
        FileStoreIndexReadResult outOfBounds = FileStoreIndexFormat.ReadIndexFile(index, append.Length - 1);

        Assert.Multiple(() =>
        {
            Assert.That(firstAdded, Is.True);
            Assert.That(secondAdded, Is.True);
            Assert.That(index.Length, Is.EqualTo(FileStoreIndexFormat.IndexHeaderSize + 2 * FileStoreIndexFormat.IndexEntrySize));
            Assert.That(valid.Success, Is.True, valid.Code);
            Assert.That(valid.Entries.ConvertAll(entry => entry.SequenceNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(valid.Entries[0].RecordOffset, Is.EqualTo(first.Offset));
            Assert.That(valid.Entries[1].RecordLength, Is.EqualTo(second.Length));
            Assert.That(valid.Entries[1].RecordChecksum, Is.EqualTo(second.Checksum));
            Assert.That(outOfBounds.IsCorrupt, Is.True);
            Assert.That(outOfBounds.Code, Is.EqualTo("index-record-out-of-bounds"));
            Assert.That(outOfBounds.ValidLength, Is.EqualTo(FileStoreIndexFormat.IndexHeaderSize + FileStoreIndexFormat.IndexEntrySize));
        });
    }

    [Test]
    public void IndexReaderRetainsCompleteEntriesBeforePartialOrCorruptTails()
    {
        using MemoryStream append = new MemoryStream();
        FileStoreRecord first = FileStoreRecordFormat.WriteAppendRecord(append, Message(1, "D", false, 1)).Record!;
        FileStoreRecord second = FileStoreRecordFormat.WriteAppendRecord(append, Message(2, "F", false, 2)).Record!;
        using MemoryStream completeIndex = new MemoryStream();
        FileStoreIndexFormat.InitializeIndexFile(completeIndex);
        FileStoreIndexFormat.AppendIndexEntry(completeIndex, first);
        FileStoreIndexFormat.AppendIndexEntry(completeIndex, second);
        byte[] partialBytes = TruncateLastByte(completeIndex.ToArray());
        using MemoryStream partialIndex = new MemoryStream(partialBytes);
        FileStoreIndexReadResult partial = FileStoreIndexFormat.ReadIndexFile(partialIndex, append.Length);

        byte[] corruptBytes = completeIndex.ToArray();
        corruptBytes[FileStoreIndexFormat.IndexHeaderSize + FileStoreIndexFormat.IndexEntrySize + 4] ^= 1;
        using MemoryStream corruptIndex = new MemoryStream(corruptBytes);
        FileStoreIndexReadResult corrupt = FileStoreIndexFormat.ReadIndexFile(corruptIndex, append.Length);

        Assert.Multiple(() =>
        {
            Assert.That(partial.IsPartialTail, Is.True);
            Assert.That(partial.Code, Is.EqualTo("partial-index-entry"));
            Assert.That(partial.Entries, Has.Count.EqualTo(1));
            Assert.That(partial.ValidLength, Is.EqualTo(FileStoreIndexFormat.IndexHeaderSize + FileStoreIndexFormat.IndexEntrySize));
            Assert.That(corrupt.IsCorrupt, Is.True);
            Assert.That(corrupt.Code, Is.EqualTo("index-entry-checksum-mismatch"));
            Assert.That(corrupt.Entries, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void SequenceSnapshotRoundTripsAndDetectsPartialAndCorruptFiles()
    {
        using MemoryStream complete = new MemoryStream();
        bool written = FileStoreIndexFormat.WriteSequenceFile(complete, 12, 7, 9);
        FileStoreSequenceResult valid = FileStoreIndexFormat.ReadSequenceFile(complete);
        using MemoryStream duplicate = new MemoryStream();
        FileStoreIndexFormat.WriteSequenceFile(duplicate, 12, 7, 9);

        using MemoryStream partialStream = new MemoryStream(TruncateLastByte(complete.ToArray()));
        FileStoreSequenceResult partial = FileStoreIndexFormat.ReadSequenceFile(partialStream);
        byte[] corruptBytes = complete.ToArray();
        corruptBytes[20] ^= 1;
        using MemoryStream corruptStream = new MemoryStream(corruptBytes);
        FileStoreSequenceResult corrupt = FileStoreIndexFormat.ReadSequenceFile(corruptStream);

        Assert.Multiple(() =>
        {
            Assert.That(written, Is.True);
            Assert.That(complete.Length, Is.EqualTo(FileStoreIndexFormat.SequenceFileSize));
            Assert.That(valid.Success, Is.True, valid.Code);
            Assert.That(valid.Generation, Is.EqualTo(12));
            Assert.That(valid.NextSenderSeqNum, Is.EqualTo(7));
            Assert.That(valid.NextTargetSeqNum, Is.EqualTo(9));
            Assert.That(duplicate.ToArray(), Is.EqualTo(complete.ToArray()));
            Assert.That(partial.IsPartialTail, Is.True);
            Assert.That(partial.Code, Is.EqualTo("partial-sequence-file"));
            Assert.That(corrupt.IsCorrupt, Is.True);
            Assert.That(corrupt.Code, Is.EqualTo("sequence-checksum-mismatch"));
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

    private static byte[] TruncateLastByte(byte[] source)
    {
        byte[] truncated = new byte[source.Length - 1];
        Array.Copy(source, truncated, truncated.Length);
        return truncated;
    }
}
