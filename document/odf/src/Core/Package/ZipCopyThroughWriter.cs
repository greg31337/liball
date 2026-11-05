using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace odf;

internal class ZipCopyEntryRecord
{
    internal byte[] LocalRecord = [];
    internal byte[] CentralRecord = [];
}

internal static class ZipCopyThroughWriter
{
    const uint LocalHeaderSignature = 67324752;
    const uint CentralHeaderSignature = 33639248;
    const uint EndRecordSignature = 101010256;
    const ushort Utf8NameFlag = 2048;

    internal static bool CanWrite(
        EditablePackageEntry[] entries,
        OdfSaveOptions options)
    {
        if (options.Deterministic
            || !options.PreserveTimestamps
            || entries.Length >= ushort.MaxValue)
        {
            return false;
        }
        long localSize = 0;
        long centralSize = 0;
        foreach (EditablePackageEntry item in entries)
        {
            if (item.IsModified)
            {
                if (!item.IsContentAvailable)
                {
                    return false;
                }
                localSize = localSize + 30 + Encoding.UTF8.GetByteCount(
                    item.Entry.Path) + item.Data.LongLength + 65536;
                centralSize = centralSize + 46 + Encoding.UTF8.GetByteCount(
                    item.Entry.Path);
            }
            else
            {
                if (item.Entry.UsesZip64
                    || item.OriginalLocalRecord.Length == 0
                    || item.OriginalCentralRecord.Length < 46)
                {
                    return false;
                }
                if (!HasMatchingCompressionMetadata(item))
                {
                    return false;
                }
                if (!item.IsContentAvailable
                    && item.Entry.CompressionMethod
                        != OdfCompressionMethod.Unsupported
                    && !item.Entry.IsEncrypted)
                {
                    return false;
                }
                localSize = localSize + item.OriginalLocalRecord.LongLength;
                centralSize = centralSize + item.OriginalCentralRecord.LongLength;
            }
            if (localSize > uint.MaxValue || centralSize > uint.MaxValue)
            {
                return false;
            }
        }
        return localSize + centralSize + 22 <= uint.MaxValue;
    }

    internal static void WriteStream(
        EditablePackageEntry[] entries,
        Stream output,
        OdfSaveOptions options)
    {
        output.SetLength(0);
        List<ZipCopyEntryRecord> records = new List<ZipCopyEntryRecord>();
        long localOffset = 0;
        foreach (EditablePackageEntry item in entries)
        {
            if (options.Cancellation.IsCancellationRequested)
            {
                throw new OdfPackageException("Package writing was cancelled.");
            }
            ZipCopyEntryRecord record = item.IsModified
                ? CreateChangedRecord(item, options, localOffset)
                : CreateCopiedRecord(item, localOffset);
            output.Write(record.LocalRecord, 0, record.LocalRecord.Length);
            records.Add(record);
            localOffset = localOffset + record.LocalRecord.LongLength;
        }

        long directoryOffset = localOffset;
        long directorySize = 0;
        foreach (ZipCopyEntryRecord record in records)
        {
            output.Write(record.CentralRecord, 0, record.CentralRecord.Length);
            directorySize = directorySize + record.CentralRecord.LongLength;
        }
        WriteEndRecord(output, records.Count, directorySize, directoryOffset);
        output.Flush();
    }

    static ZipCopyEntryRecord CreateCopiedRecord(
        EditablePackageEntry item,
        long localOffset)
    {
        if (localOffset > uint.MaxValue)
        {
            throw new OdfPackageException(
                "The copy-through package exceeds the standard ZIP offset range.");
        }
        ZipCopyEntryRecord result = new ZipCopyEntryRecord();
        result.LocalRecord = CloneBytes(item.OriginalLocalRecord);
        result.CentralRecord = CloneBytes(item.OriginalCentralRecord);
        WriteUInt32ToArray(result.CentralRecord, 42, (uint)localOffset);
        return result;
    }

    static ZipCopyEntryRecord CreateChangedRecord(
        EditablePackageEntry item,
        OdfSaveOptions options,
        long localOffset)
    {
        if (localOffset > uint.MaxValue)
        {
            throw new OdfPackageException(
                "The copy-through package exceeds the standard ZIP offset range.");
        }
        ushort method = SelectMethodCode(item.Entry, options);
        byte[] compressed = method == 0
            ? CloneBytes(item.Data)
            : DeflateBytes(item.Data);
        if (compressed.LongLength > uint.MaxValue
            || item.Data.LongLength > uint.MaxValue)
        {
            throw new OdfPackageException(
                "A package entry exceeds the standard ZIP size range.");
        }
        byte[] name = Encoding.UTF8.GetBytes(item.Entry.Path);
        ushort flags = IsAscii(item.Entry.Path) ? (ushort)0 : Utf8NameFlag;
        ushort time = ReadPreservedTime(item);
        ushort date = ReadPreservedDate(item);
        uint crc = ComputeCrc32(item.Data);

        ZipCopyEntryRecord result = new ZipCopyEntryRecord();
        result.LocalRecord = CreateLocalRecord(
            name,
            flags,
            method,
            time,
            date,
            crc,
            compressed,
            item.Data.Length);
        result.CentralRecord = CreateCentralRecord(
            name,
            flags,
            method,
            time,
            date,
            crc,
            compressed.Length,
            item.Data.Length,
            (uint)localOffset,
            item.Entry.IsDirectory);
        return result;
    }

    static byte[] CreateLocalRecord(
        byte[] name,
        ushort flags,
        ushort method,
        ushort time,
        ushort date,
        uint crc,
        byte[] compressed,
        int expandedSize)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(LocalHeaderSignature);
                writer.Write((ushort)20);
                writer.Write(flags);
                writer.Write(method);
                writer.Write(time);
                writer.Write(date);
                writer.Write(crc);
                writer.Write((uint)compressed.Length);
                writer.Write((uint)expandedSize);
                writer.Write((ushort)name.Length);
                writer.Write((ushort)0);
                writer.Write(name);
                writer.Write(compressed);
            }
            return output.ToArray();
        }
    }

    static byte[] CreateCentralRecord(
        byte[] name,
        ushort flags,
        ushort method,
        ushort time,
        ushort date,
        uint crc,
        int compressedSize,
        int expandedSize,
        uint localOffset,
        bool isDirectory)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (BinaryWriter writer = new BinaryWriter(output, Encoding.UTF8, true))
            {
                writer.Write(CentralHeaderSignature);
                writer.Write((ushort)20);
                writer.Write((ushort)20);
                writer.Write(flags);
                writer.Write(method);
                writer.Write(time);
                writer.Write(date);
                writer.Write(crc);
                writer.Write((uint)compressedSize);
                writer.Write((uint)expandedSize);
                writer.Write((ushort)name.Length);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write((ushort)0);
                writer.Write(isDirectory ? 16U : 0U);
                writer.Write(localOffset);
                writer.Write(name);
            }
            return output.ToArray();
        }
    }

    static byte[] DeflateBytes(byte[] data)
    {
        using (MemoryStream output = new MemoryStream())
        {
            using (DeflateStream compressor = new DeflateStream(
                output,
                CompressionLevel.SmallestSize,
                true))
            {
                compressor.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }
    }

    static ushort SelectMethodCode(
        OdfPackageEntry entry,
        OdfSaveOptions options)
    {
        if (entry.Path == OdfPartNames.Mimetype
            || entry.IsDirectory
            || entry.CompressionMethod == OdfCompressionMethod.Stored)
        {
            return 0;
        }
        if (entry.CompressionMethod == OdfCompressionMethod.Deflated)
        {
            return 8;
        }
        return options.CompressionMethod == OdfCompressionMethod.Stored
            ? (ushort)0
            : (ushort)8;
    }

    static ushort ReadPreservedTime(EditablePackageEntry item)
    {
        if (item.OriginalCentralRecord.Length >= 16)
        {
            return ReadUInt16FromArray(item.OriginalCentralRecord, 12);
        }
        DateTime now = DateTime.Now;
        return (ushort)((now.Hour << 11)
            | (now.Minute << 5)
            | now.Second / 2);
    }

    static ushort ReadPreservedDate(EditablePackageEntry item)
    {
        if (item.OriginalCentralRecord.Length >= 16)
        {
            return ReadUInt16FromArray(item.OriginalCentralRecord, 14);
        }
        DateTime now = DateTime.Now;
        int year = now.Year < 1980 ? 1980 : now.Year;
        return (ushort)(((year - 1980) << 9)
            | (now.Month << 5)
            | now.Day);
    }

    static void WriteEndRecord(
        Stream output,
        int entryCount,
        long directorySize,
        long directoryOffset)
    {
        if (directorySize > uint.MaxValue || directoryOffset > uint.MaxValue)
        {
            throw new OdfPackageException(
                "The copy-through package exceeds the standard ZIP directory range.");
        }
        using (BinaryWriter writer = new BinaryWriter(output, Encoding.UTF8, true))
        {
            writer.Write(EndRecordSignature);
            writer.Write((ushort)0);
            writer.Write((ushort)0);
            writer.Write((ushort)entryCount);
            writer.Write((ushort)entryCount);
            writer.Write((uint)directorySize);
            writer.Write((uint)directoryOffset);
            writer.Write((ushort)0);
        }
    }

    static uint ComputeCrc32(byte[] data)
    {
        uint crc = uint.MaxValue;
        int index = 0;
        while (index < data.Length)
        {
            crc = crc ^ data[index];
            int bit = 0;
            while (bit < 8)
            {
                crc = (crc & 1) != 0
                    ? (crc >> 1) ^ 3988292384U
                    : crc >> 1;
                bit = bit + 1;
            }
            index = index + 1;
        }
        return ~crc;
    }

    static bool IsAscii(string value)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] > 127)
            {
                return false;
            }
            index = index + 1;
        }
        return true;
    }

    static bool HasMatchingCompressionMetadata(EditablePackageEntry item)
    {
        ushort originalCode = ReadUInt16FromArray(
            item.OriginalCentralRecord,
            10);
        if (originalCode != item.Entry.CompressionMethodCode)
        {
            return false;
        }
        if (originalCode == 0)
        {
            return item.Entry.CompressionMethod == OdfCompressionMethod.Stored;
        }
        if (originalCode == 8)
        {
            return item.Entry.CompressionMethod == OdfCompressionMethod.Deflated;
        }
        return item.Entry.CompressionMethod == OdfCompressionMethod.Unsupported;
    }

    static byte[] CloneBytes(byte[] source)
    {
        byte[] result = new byte[source.Length];
        Array.Copy(source, result, source.Length);
        return result;
    }

    static ushort ReadUInt16FromArray(byte[] data, int offset)
    {
        return (ushort)(data[offset] | data[offset + 1] << 8);
    }

    static void WriteUInt32ToArray(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)(value & 255U);
        data[offset + 1] = (byte)((value >> 8) & 255U);
        data[offset + 2] = (byte)((value >> 16) & 255U);
        data[offset + 3] = (byte)((value >> 24) & 255U);
    }
}
