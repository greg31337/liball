using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;

namespace fix;

internal class FileStoreIndexEntry
{
    internal int SequenceNumber;
    internal long RecordOffset;
    internal int RecordLength;
    internal int RecordChecksum;
}

internal class FileStoreIndexReadResult
{
    internal bool Success;
    internal bool IsPartialTail;
    internal bool IsCorrupt;
    internal long ValidLength;
    internal string Code = "";
    internal List<FileStoreIndexEntry> Entries = [];
}

internal class FileStoreSequenceResult
{
    internal bool Success;
    internal bool IsPartialTail;
    internal bool IsCorrupt;
    internal long Generation;
    internal int NextSenderSeqNum = FixDefaults.InitialSequenceNumber;
    internal int NextTargetSeqNum = FixDefaults.InitialSequenceNumber;
    internal string Code = "";
}

internal static class FileStoreIndexFormat
{
    internal const int IndexMagic = 0x31495346;
    internal const int IndexVersion = 1;
    internal const int IndexHeaderSize = 16;
    internal const int IndexEntrySize = 24;
    internal const int SequenceMagic = 0x31515346;
    internal const int SequenceVersion = 1;
    internal const int SequenceFileSize = 32;

    internal static void InitializeIndexFile(Stream stream)
    {
        byte[] header = new byte[IndexHeaderSize];
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(0, 4), IndexMagic);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(4, 4), IndexVersion);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8, 4), IndexEntrySize);
        int checksum = FileStoreRecordFormat.CalculateStoreChecksum(header, IndexHeaderSize - 4);
        BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(12, 4), checksum);
        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(header, 0, header.Length);
    }

    internal static bool AppendIndexEntry(Stream stream, FileStoreRecord record)
    {
        if (record == null || record.Message.SequenceNumber < FixDefaults.InitialSequenceNumber || record.Offset < 0 || record.Length < FileStoreRecordFormat.MinimumRecordSize || record.Checksum < 0)
        {
            return false;
        }
        byte[] entry = new byte[IndexEntrySize];
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(0, 4), record.Message.SequenceNumber);
        BinaryPrimitives.WriteInt64LittleEndian(entry.AsSpan(4, 8), record.Offset);
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(12, 4), record.Length);
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(16, 4), record.Checksum);
        int checksum = FileStoreRecordFormat.CalculateStoreChecksum(entry, IndexEntrySize - 4);
        BinaryPrimitives.WriteInt32LittleEndian(entry.AsSpan(20, 4), checksum);
        stream.Position = stream.Length;
        stream.Write(entry, 0, entry.Length);
        return true;
    }

    internal static FileStoreIndexReadResult ReadIndexFile(Stream stream, long appendLength)
    {
        FileStoreIndexReadResult result = new FileStoreIndexReadResult();
        if (!stream.CanRead || !stream.CanSeek || appendLength < 0)
        {
            SetCorruptIndexResult(result, "invalid-index-read-state", 0);
            return result;
        }
        if (stream.Length < IndexHeaderSize)
        {
            SetPartialIndexResult(result, "partial-index-header", 0);
            return result;
        }
        byte[] header = new byte[IndexHeaderSize];
        stream.Position = 0;
        if (!FileStoreRecordFormat.ReadExactRecordBytes(stream, header, header.Length))
        {
            SetPartialIndexResult(result, "partial-index-header", 0);
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4)) != IndexMagic)
        {
            SetCorruptIndexResult(result, "invalid-index-magic", 0);
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4)) != IndexVersion || BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4)) != IndexEntrySize)
        {
            SetCorruptIndexResult(result, "unsupported-index-format", 0);
            return result;
        }
        int headerChecksum = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12, 4));
        if (headerChecksum != FileStoreRecordFormat.CalculateStoreChecksum(header, IndexHeaderSize - 4))
        {
            SetCorruptIndexResult(result, "index-header-checksum-mismatch", 0);
            return result;
        }

        long offset = IndexHeaderSize;
        int lastSequence = 0;
        while (stream.Length - offset >= IndexEntrySize)
        {
            byte[] bytes = new byte[IndexEntrySize];
            stream.Position = offset;
            if (!FileStoreRecordFormat.ReadExactRecordBytes(stream, bytes, bytes.Length))
            {
                SetPartialIndexResult(result, "partial-index-entry", offset);
                return result;
            }
            int entryChecksum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(20, 4));
            if (entryChecksum != FileStoreRecordFormat.CalculateStoreChecksum(bytes, IndexEntrySize - 4))
            {
                SetCorruptIndexResult(result, "index-entry-checksum-mismatch", offset);
                return result;
            }
            FileStoreIndexEntry entry = new FileStoreIndexEntry();
            entry.SequenceNumber = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4));
            entry.RecordOffset = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(4, 8));
            entry.RecordLength = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(12, 4));
            entry.RecordChecksum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(16, 4));
            if (entry.SequenceNumber <= lastSequence || entry.RecordOffset < 0 || entry.RecordLength < FileStoreRecordFormat.MinimumRecordSize || entry.RecordChecksum < 0)
            {
                SetCorruptIndexResult(result, "invalid-index-entry", offset);
                return result;
            }
            if (entry.RecordOffset > appendLength || entry.RecordLength > appendLength - entry.RecordOffset)
            {
                SetCorruptIndexResult(result, "index-record-out-of-bounds", offset);
                return result;
            }
            result.Entries.Add(entry);
            lastSequence = entry.SequenceNumber;
            offset += IndexEntrySize;
        }
        if (offset != stream.Length)
        {
            SetPartialIndexResult(result, "partial-index-entry", offset);
            return result;
        }
        result.Success = true;
        result.ValidLength = offset;
        return result;
    }

    internal static bool WriteSequenceFile(Stream stream, long generation, int nextSenderSeqNum, int nextTargetSeqNum)
    {
        if (generation < 0 || nextSenderSeqNum < FixDefaults.InitialSequenceNumber || nextTargetSeqNum < FixDefaults.InitialSequenceNumber)
        {
            return false;
        }
        byte[] bytes = new byte[SequenceFileSize];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), SequenceMagic);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), SequenceVersion);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), SequenceFileSize);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(12, 8), generation);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(20, 4), nextSenderSeqNum);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24, 4), nextTargetSeqNum);
        int checksum = FileStoreRecordFormat.CalculateStoreChecksum(bytes, SequenceFileSize - 4);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28, 4), checksum);
        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(bytes, 0, bytes.Length);
        return true;
    }

    internal static FileStoreSequenceResult ReadSequenceFile(Stream stream)
    {
        FileStoreSequenceResult result = new FileStoreSequenceResult();
        if (!stream.CanRead || !stream.CanSeek)
        {
            result.IsCorrupt = true;
            result.Code = "invalid-sequence-read-state";
            return result;
        }
        if (stream.Length < SequenceFileSize)
        {
            result.IsPartialTail = true;
            result.Code = "partial-sequence-file";
            return result;
        }
        if (stream.Length > SequenceFileSize)
        {
            result.IsCorrupt = true;
            result.Code = "invalid-sequence-file-length";
            return result;
        }
        byte[] bytes = new byte[SequenceFileSize];
        stream.Position = 0;
        if (!FileStoreRecordFormat.ReadExactRecordBytes(stream, bytes, bytes.Length))
        {
            result.IsPartialTail = true;
            result.Code = "partial-sequence-file";
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(0, 4)) != SequenceMagic)
        {
            result.IsCorrupt = true;
            result.Code = "invalid-sequence-magic";
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(4, 4)) != SequenceVersion || BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(8, 4)) != SequenceFileSize)
        {
            result.IsCorrupt = true;
            result.Code = "unsupported-sequence-format";
            return result;
        }
        int expectedChecksum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(28, 4));
        if (expectedChecksum != FileStoreRecordFormat.CalculateStoreChecksum(bytes, SequenceFileSize - 4))
        {
            result.IsCorrupt = true;
            result.Code = "sequence-checksum-mismatch";
            return result;
        }
        result.Generation = BinaryPrimitives.ReadInt64LittleEndian(bytes.AsSpan(12, 8));
        result.NextSenderSeqNum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(20, 4));
        result.NextTargetSeqNum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(24, 4));
        if (result.Generation < 0 || result.NextSenderSeqNum < FixDefaults.InitialSequenceNumber || result.NextTargetSeqNum < FixDefaults.InitialSequenceNumber)
        {
            result.IsCorrupt = true;
            result.Code = "invalid-sequence-state";
            return result;
        }
        result.Success = true;
        return result;
    }

    internal static void SetPartialIndexResult(FileStoreIndexReadResult result, string code, long validLength)
    {
        result.IsPartialTail = true;
        result.Code = code;
        result.ValidLength = validLength;
    }

    internal static void SetCorruptIndexResult(FileStoreIndexReadResult result, string code, long validLength)
    {
        result.IsCorrupt = true;
        result.Code = code;
        result.ValidLength = validLength;
    }
}
