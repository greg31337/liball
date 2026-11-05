using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace fix;

internal class FileStoreRecord
{
    internal long Offset;
    internal int Length;
    internal int Checksum;
    internal StoredMessage Message = new StoredMessage();
}

internal class FileStoreRecordReadResult
{
    internal bool Success;
    internal bool IsEndOfFile;
    internal bool IsPartialTail;
    internal bool IsCorrupt;
    internal string Code = "";
    internal FileStoreRecord? Record;
}

internal class FileStoreScanResult
{
    internal bool Success;
    internal bool IsPartialTail;
    internal bool IsCorrupt;
    internal long ValidLength;
    internal string Code = "";
    internal List<FileStoreRecord> Records = [];
}

internal static class FileStoreRecordFormat
{
    internal const int RecordMagic = 0x31525346;
    internal const int RecordVersion = 1;
    internal const int RecordHeaderSize = 36;
    internal const int RecordChecksumSize = 4;
    internal const int MinimumRecordSize = RecordHeaderSize + RecordChecksumSize;
    internal const int MaximumMessageTypeLength = 64;
    internal const int AdministrativeFlag = 1;

    internal static FileStoreRecordReadResult WriteAppendRecord(Stream stream, StoredMessage message)
    {
        FileStoreRecordReadResult result = new FileStoreRecordReadResult();
        string validationCode = ValidateAppendMessage(message);
        if (validationCode.Length != 0)
        {
            SetCorruptRecordResult(result, validationCode);
            return result;
        }

        byte[] messageType = Encoding.ASCII.GetBytes(message.MsgType);
        int totalLength = RecordHeaderSize + messageType.Length + message.Bytes.Length + RecordChecksumSize;
        byte[] bytes = new byte[totalLength];
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(0, 4), RecordMagic);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(4, 4), RecordVersion);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(8, 4), totalLength);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(12, 4), message.SequenceNumber);
        BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(16, 8), message.SendingTimeUtc.Ticks);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(24, 4), message.IsAdministrative ? AdministrativeFlag : 0);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(28, 4), messageType.Length);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(32, 4), message.Bytes.Length);
        Array.Copy(messageType, 0, bytes, RecordHeaderSize, messageType.Length);
        Array.Copy(message.Bytes, 0, bytes, RecordHeaderSize + messageType.Length, message.Bytes.Length);
        int checksum = CalculateStoreChecksum(bytes, totalLength - RecordChecksumSize);
        BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(totalLength - RecordChecksumSize, RecordChecksumSize), checksum);

        long offset = stream.Length;
        stream.Position = offset;
        stream.Write(bytes, 0, bytes.Length);
        FileStoreRecord record = new FileStoreRecord();
        record.Offset = offset;
        record.Length = totalLength;
        record.Checksum = checksum;
        record.Message = CloneRecordMessage(message);
        result.Success = true;
        result.Record = record;
        return result;
    }

    internal static FileStoreRecordReadResult ReadAppendRecord(Stream stream, long offset)
    {
        FileStoreRecordReadResult result = new FileStoreRecordReadResult();
        if (offset < 0 || !stream.CanRead || !stream.CanSeek)
        {
            SetCorruptRecordResult(result, "invalid-record-read-position");
            return result;
        }
        if (offset == stream.Length)
        {
            result.IsEndOfFile = true;
            return result;
        }
        if (offset > stream.Length)
        {
            SetCorruptRecordResult(result, "record-offset-out-of-bounds");
            return result;
        }
        long available = stream.Length - offset;
        if (available < RecordHeaderSize)
        {
            SetPartialRecordResult(result, "partial-record-header");
            return result;
        }

        byte[] header = new byte[RecordHeaderSize];
        stream.Position = offset;
        if (!ReadExactRecordBytes(stream, header, RecordHeaderSize))
        {
            SetPartialRecordResult(result, "partial-record-header");
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(0, 4)) != RecordMagic)
        {
            SetCorruptRecordResult(result, "invalid-record-magic");
            return result;
        }
        if (BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(4, 4)) != RecordVersion)
        {
            SetCorruptRecordResult(result, "unsupported-record-version");
            return result;
        }
        int totalLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));
        int sequenceNumber = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(12, 4));
        long sendingTicks = BinaryPrimitives.ReadInt64LittleEndian(header.AsSpan(16, 8));
        int flags = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(24, 4));
        int messageTypeLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(28, 4));
        int payloadLength = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(32, 4));
        if (!ValidateRecordHeader(totalLength, sequenceNumber, sendingTicks, flags, messageTypeLength, payloadLength))
        {
            SetCorruptRecordResult(result, "invalid-record-header");
            return result;
        }
        if (available < totalLength)
        {
            SetPartialRecordResult(result, "partial-record-tail");
            return result;
        }

        byte[] bytes = new byte[totalLength];
        stream.Position = offset;
        if (!ReadExactRecordBytes(stream, bytes, totalLength))
        {
            SetPartialRecordResult(result, "partial-record-tail");
            return result;
        }
        int expectedChecksum = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(totalLength - RecordChecksumSize, RecordChecksumSize));
        int actualChecksum = CalculateStoreChecksum(bytes, totalLength - RecordChecksumSize);
        if (expectedChecksum != actualChecksum)
        {
            SetCorruptRecordResult(result, "record-checksum-mismatch");
            return result;
        }
        if (!IsAsciiMessageType(bytes, RecordHeaderSize, messageTypeLength))
        {
            SetCorruptRecordResult(result, "invalid-record-message-type");
            return result;
        }

        StoredMessage message = new StoredMessage();
        message.SequenceNumber = sequenceNumber;
        message.SendingTimeUtc = new DateTime(sendingTicks, DateTimeKind.Utc);
        message.MsgType = Encoding.ASCII.GetString(bytes, RecordHeaderSize, messageTypeLength);
        message.IsAdministrative = flags == AdministrativeFlag;
        message.Bytes = new byte[payloadLength];
        Array.Copy(bytes, RecordHeaderSize + messageTypeLength, message.Bytes, 0, payloadLength);
        FileStoreRecord record = new FileStoreRecord();
        record.Offset = offset;
        record.Length = totalLength;
        record.Checksum = actualChecksum;
        record.Message = message;
        result.Success = true;
        result.Record = record;
        return result;
    }

    internal static FileStoreScanResult ScanAppendFile(Stream stream)
    {
        FileStoreScanResult scan = new FileStoreScanResult();
        long offset = 0;
        int lastSequence = 0;
        while (offset < stream.Length)
        {
            FileStoreRecordReadResult read = ReadAppendRecord(stream, offset);
            if (!read.Success || read.Record == null)
            {
                scan.IsPartialTail = read.IsPartialTail;
                scan.IsCorrupt = read.IsCorrupt;
                scan.Code = read.Code;
                scan.ValidLength = offset;
                return scan;
            }
            if (read.Record.Message.SequenceNumber <= lastSequence)
            {
                scan.IsCorrupt = true;
                scan.Code = "record-sequence-not-increasing";
                scan.ValidLength = offset;
                return scan;
            }
            scan.Records.Add(read.Record);
            lastSequence = read.Record.Message.SequenceNumber;
            offset += read.Record.Length;
        }
        scan.Success = true;
        scan.ValidLength = offset;
        return scan;
    }

    internal static int CalculateStoreChecksum(byte[] bytes, int count)
    {
        long checksum = 1;
        for (int index = 0; index < count; index++)
        {
            checksum = (checksum * 31L + bytes[index]) % 2147483647L;
        }
        return (int)checksum;
    }

    internal static string ValidateAppendMessage(StoredMessage message)
    {
        if (message == null) return "missing-record-message";
        if (message.SequenceNumber < FixDefaults.InitialSequenceNumber) return "invalid-record-sequence-number";
        if (message.SendingTimeUtc.Kind != DateTimeKind.Utc) return "invalid-record-sending-time";
        if (message.MsgType == null || message.MsgType.Length == 0 || message.MsgType.Length > MaximumMessageTypeLength) return "invalid-record-message-type";
        for (int index = 0; index < message.MsgType.Length; index++)
        {
            if (message.MsgType[index] < 33 || message.MsgType[index] > 126) return "invalid-record-message-type";
        }
        if (message.Bytes == null || message.Bytes.Length == 0 || message.Bytes.Length > FixDefaults.MaxMessageSize) return "invalid-record-payload";
        return "";
    }

    internal static bool ValidateRecordHeader(int totalLength, int sequenceNumber, long sendingTicks, int flags, int messageTypeLength, int payloadLength)
    {
        if (sequenceNumber < FixDefaults.InitialSequenceNumber) return false;
        if (sendingTicks < DateTime.MinValue.Ticks || sendingTicks > DateTime.MaxValue.Ticks) return false;
        if (flags != 0 && flags != AdministrativeFlag) return false;
        if (messageTypeLength < 1 || messageTypeLength > MaximumMessageTypeLength) return false;
        if (payloadLength < 1 || payloadLength > FixDefaults.MaxMessageSize) return false;
        long expectedLength = (long)RecordHeaderSize + messageTypeLength + payloadLength + RecordChecksumSize;
        return totalLength == expectedLength;
    }

    internal static bool IsAsciiMessageType(byte[] bytes, int start, int count)
    {
        for (int index = start; index < start + count; index++)
        {
            if (bytes[index] < 33 || bytes[index] > 126) return false;
        }
        return true;
    }

    internal static bool ReadExactRecordBytes(Stream stream, byte[] bytes, int count)
    {
        int total = 0;
        while (total < count)
        {
            int read = stream.Read(bytes, total, count - total);
            if (read == 0) return false;
            total += read;
        }
        return true;
    }

    internal static StoredMessage CloneRecordMessage(StoredMessage source)
    {
        StoredMessage copy = new StoredMessage();
        copy.SequenceNumber = source.SequenceNumber;
        copy.SendingTimeUtc = source.SendingTimeUtc;
        copy.MsgType = source.MsgType;
        copy.IsAdministrative = source.IsAdministrative;
        copy.Bytes = new byte[source.Bytes.Length];
        Array.Copy(source.Bytes, copy.Bytes, source.Bytes.Length);
        return copy;
    }

    internal static void SetPartialRecordResult(FileStoreRecordReadResult result, string code)
    {
        result.IsPartialTail = true;
        result.Code = code;
    }

    internal static void SetCorruptRecordResult(FileStoreRecordReadResult result, string code)
    {
        result.IsCorrupt = true;
        result.Code = code;
    }
}
