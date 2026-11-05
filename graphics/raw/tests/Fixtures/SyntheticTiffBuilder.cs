using raw;

namespace raw.Tests;

internal class SyntheticTiffBuilder
{
    internal RawByteOrder ByteOrder;
    internal bool BigTiff;
    internal List<SyntheticTiffIfd> Ifds;
    internal SyntheticTiffIfd? FirstIfd;
    internal ulong FirstIfdOffsetOverride;
    internal bool HasFirstIfdOffsetOverride;
    internal int HeaderPadding;

    internal SyntheticTiffBuilder(RawByteOrder byteOrder, bool bigTiff)
    {
        ByteOrder = byteOrder;
        BigTiff = bigTiff;
        Ifds = new List<SyntheticTiffIfd>();
        FirstIfd = null;
        FirstIfdOffsetOverride = 0;
        HasFirstIfdOffsetOverride = false;
        HeaderPadding = 0;
    }

    internal SyntheticTiffIfd AddIfd()
    {
        SyntheticTiffIfd ifd = new SyntheticTiffIfd();
        Ifds.Add(ifd);
        if (FirstIfd == null)
        {
            FirstIfd = ifd;
        }
        return ifd;
    }

    internal SyntheticTiffEntry AddField(SyntheticTiffIfd? ifd, ushort tag, TiffFieldType fieldType, ulong count, byte[] rawBytes)
    {
        RequireIfd(ifd);
        if (rawBytes == null)
        {
            throw new ArgumentNullException(nameof(rawBytes));
        }
        ulong expectedLength = checked(count * (ulong)TiffFieldTypes.GetElementSize(fieldType));
        if (expectedLength != (ulong)rawBytes.Length)
        {
            throw new ArgumentException("The raw field length does not match its TIFF type and count.", nameof(rawBytes));
        }
        SyntheticTiffEntry entry = new SyntheticTiffEntry(tag, fieldType, count, rawBytes.ToArray());
        (ifd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Entries.Add(entry);
        return entry;
    }

    internal SyntheticTiffEntry AddPointer(SyntheticTiffIfd ifd, ushort tag, TiffFieldType fieldType, params SyntheticTiffIfd[] targets)
    {
        RequireIfd(ifd);
        if (targets == null)
        {
            throw new ArgumentNullException(nameof(targets));
        }
        bool fourByte = fieldType == TiffFieldType.Long || fieldType == TiffFieldType.Ifd;
        bool eightByte = fieldType == TiffFieldType.Long8 || fieldType == TiffFieldType.Ifd8;
        if (!fourByte && !eightByte)
        {
            throw new ArgumentException("A pointer field must use LONG, IFD, LONG8, or IFD8.", nameof(fieldType));
        }
        SyntheticTiffIfd[] targetCopy = targets.ToArray();
        for (int index = 0; index < targetCopy.Length; index++)
        {
            RequireIfd(targetCopy[index]);
        }
        SyntheticTiffEntry entry = new SyntheticTiffEntry(tag, fieldType, (ulong)targetCopy.Length, new byte[0]);
        entry.Targets = targetCopy;
        ifd.Entries.Add(entry);
        return entry;
    }

    internal void SetNextIfd(SyntheticTiffIfd ifd, SyntheticTiffIfd? nextIfd)
    {
        RequireIfd(ifd);
        if (nextIfd != null)
        {
            RequireIfd(nextIfd);
        }
        ifd.NextIfd = nextIfd;
    }

    internal void SetFirstIfd(SyntheticTiffIfd ifd)
    {
        RequireIfd(ifd);
        FirstIfd = ifd;
        HasFirstIfdOffsetOverride = false;
    }

    internal void SetMalformedFirstIfdOffset(ulong offset)
    {
        FirstIfdOffsetOverride = offset;
        HasFirstIfdOffsetOverride = true;
    }

    internal void SetHeaderPadding(int length)
    {
        if (BigTiff || length < 0)
        {
            throw new ArgumentException("Only classic TIFF supports nonnegative synthetic header padding.", nameof(length));
        }
        HeaderPadding = length;
    }

    internal void SetMalformedValueOffset(SyntheticTiffEntry entry, ulong offset)
    {
        RequireEntry(entry);
        if (entry.RawBytes.Length <= GetSlotSize())
        {
            throw new ArgumentException("Only out-of-line fields can have a malformed value offset.", nameof(entry));
        }
        entry.ValueOffsetOverride = offset;
        entry.HasValueOffsetOverride = true;
    }

    internal byte[] Build()
    {
        int alignment = GetAlignment();
        int position = Align(GetHeaderSize(), alignment);
        for (int index = 0; index < Ifds.Count; index++)
        {
            SyntheticTiffIfd ifd = Ifds[index];
            ifd.Offset = checked((ulong)position);
            position = checked(position + GetIfdSize(ifd));
            position = Align(position, alignment);
        }

        for (int ifdIndex = 0; ifdIndex < Ifds.Count; ifdIndex++)
        {
            SyntheticTiffIfd ifd = Ifds[ifdIndex];
            for (int entryIndex = 0; entryIndex < ifd.Entries.Count; entryIndex++)
            {
                SyntheticTiffEntry entry = ifd.Entries[entryIndex];
                entry.ResolvedBytes = ResolveBytes(entry);
                if (entry.ResolvedBytes.Length > GetSlotSize() && !entry.HasValueOffsetOverride)
                {
                    position = Align(position, alignment);
                    entry.ValueOffset = checked((ulong)position);
                    position = checked(position + entry.ResolvedBytes.Length);
                }
            }
        }

        byte[] bytes = new byte[position];
        WriteHeader(bytes);
        for (int index = 0; index < Ifds.Count; index++)
        {
            WriteIfd(bytes, Ifds[index]);
        }
        return bytes;
    }

    internal int GetSlotSize()
    {
        return BigTiff ? 8 : 4;
    }

    void RequireIfd(SyntheticTiffIfd? ifd)
    {
        if (ifd == null || !Ifds.Contains(ifd))
        {
            throw new ArgumentException("The IFD must belong to this builder.", nameof(ifd));
        }
    }

    void RequireEntry(SyntheticTiffEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }
        for (int ifdIndex = 0; ifdIndex < Ifds.Count; ifdIndex++)
        {
            if (Ifds[ifdIndex].Entries.Contains(entry))
            {
                return;
            }
        }
        throw new ArgumentException("The entry must belong to this builder.", nameof(entry));
    }

    int GetAlignment()
    {
        return BigTiff ? 8 : 2;
    }

    int GetHeaderSize()
    {
        return (BigTiff ? 16 : 8) + HeaderPadding;
    }

    int GetIfdSize(SyntheticTiffIfd ifd)
    {
        int countSize = BigTiff ? 8 : 2;
        int entrySize = BigTiff ? 20 : 12;
        int nextSize = BigTiff ? 8 : 4;
        if (!BigTiff && ifd.Entries.Count > ushort.MaxValue)
        {
            throw new InvalidOperationException("Classic TIFF cannot contain more than 65535 entries in one IFD.");
        }
        return checked(countSize + checked(ifd.Entries.Count * entrySize) + nextSize);
    }

    int Align(int value, int alignment)
    {
        int remainder = value % alignment;
        if (remainder == 0)
        {
            return value;
        }
        return checked(value + alignment - remainder);
    }

    byte[] ResolveBytes(SyntheticTiffEntry entry)
    {
        if (entry.Targets == null)
        {
            return entry.RawBytes.ToArray();
        }
        int elementSize = TiffFieldTypes.GetElementSize(entry.FieldType);
        byte[] bytes = new byte[checked(entry.Targets.Length * elementSize)];
        for (int index = 0; index < entry.Targets.Length; index++)
        {
            ulong offset = entry.Targets[index].Offset;
            int byteOffset = index * elementSize;
            if (elementSize == 4)
            {
                if (offset > uint.MaxValue)
                {
                    throw new InvalidOperationException("The generated pointer does not fit in a 32-bit TIFF field.");
                }
                EndianWriter.WriteUInt32(bytes.AsSpan(byteOffset, 4), (uint)offset, ByteOrder);
            }
            else
            {
                EndianWriter.WriteUInt64(bytes.AsSpan(byteOffset, 8), offset, ByteOrder);
            }
        }
        return bytes;
    }

    void WriteHeader(byte[] bytes)
    {
        if (ByteOrder == RawByteOrder.LittleEndian)
        {
            bytes[0] = 0x49;
            bytes[1] = 0x49;
        }
        else
        {
            bytes[0] = 0x4D;
            bytes[1] = 0x4D;
        }

        ulong firstOffset = 0;
        if (HasFirstIfdOffsetOverride)
        {
            firstOffset = FirstIfdOffsetOverride;
        }
        else if (FirstIfd != null)
        {
            firstOffset = FirstIfd.Offset;
        }

        if (BigTiff)
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(2, 2), 43, ByteOrder);
            EndianWriter.WriteUInt16(bytes.AsSpan(4, 2), 8, ByteOrder);
            EndianWriter.WriteUInt16(bytes.AsSpan(6, 2), 0, ByteOrder);
            EndianWriter.WriteUInt64(bytes.AsSpan(8, 8), firstOffset, ByteOrder);
        }
        else
        {
            if (firstOffset > uint.MaxValue)
            {
                throw new InvalidOperationException("The first IFD offset does not fit in classic TIFF.");
            }
            EndianWriter.WriteUInt16(bytes.AsSpan(2, 2), 42, ByteOrder);
            EndianWriter.WriteUInt32(bytes.AsSpan(4, 4), (uint)firstOffset, ByteOrder);
        }
    }

    void WriteIfd(byte[] bytes, SyntheticTiffIfd ifd)
    {
        int offset = checked((int)ifd.Offset);
        int entrySize;
        int entryOffset;
        if (BigTiff)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(offset, 8), (ulong)ifd.Entries.Count, ByteOrder);
            entrySize = 20;
            entryOffset = offset + 8;
        }
        else
        {
            EndianWriter.WriteUInt16(bytes.AsSpan(offset, 2), (ushort)ifd.Entries.Count, ByteOrder);
            entrySize = 12;
            entryOffset = offset + 2;
        }

        for (int index = 0; index < ifd.Entries.Count; index++)
        {
            WriteEntry(bytes, entryOffset + index * entrySize, ifd.Entries[index]);
        }

        int nextOffsetPosition = entryOffset + ifd.Entries.Count * entrySize;
        ulong nextOffset = ifd.NextIfd == null ? 0 : ifd.NextIfd.Offset;
        if (BigTiff)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(nextOffsetPosition, 8), nextOffset, ByteOrder);
        }
        else
        {
            if (nextOffset > uint.MaxValue)
            {
                throw new InvalidOperationException("The next IFD offset does not fit in classic TIFF.");
            }
            EndianWriter.WriteUInt32(bytes.AsSpan(nextOffsetPosition, 4), (uint)nextOffset, ByteOrder);
        }
    }

    void WriteEntry(byte[] bytes, int offset, SyntheticTiffEntry entry)
    {
        EndianWriter.WriteUInt16(bytes.AsSpan(offset, 2), entry.Tag, ByteOrder);
        EndianWriter.WriteUInt16(bytes.AsSpan(offset + 2, 2), (ushort)entry.FieldType, ByteOrder);
        int slotOffset;
        if (BigTiff)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(offset + 4, 8), entry.Count, ByteOrder);
            slotOffset = offset + 12;
        }
        else
        {
            if (entry.Count > uint.MaxValue)
            {
                throw new InvalidOperationException("The field count does not fit in classic TIFF.");
            }
            EndianWriter.WriteUInt32(bytes.AsSpan(offset + 4, 4), (uint)entry.Count, ByteOrder);
            slotOffset = offset + 8;
        }

        if (entry.ResolvedBytes.Length <= GetSlotSize())
        {
            entry.ResolvedBytes.CopyTo(bytes, slotOffset);
            return;
        }

        ulong valueOffset = entry.HasValueOffsetOverride ? entry.ValueOffsetOverride : entry.ValueOffset;
        if (BigTiff)
        {
            EndianWriter.WriteUInt64(bytes.AsSpan(slotOffset, 8), valueOffset, ByteOrder);
        }
        else
        {
            if (valueOffset > uint.MaxValue)
            {
                throw new InvalidOperationException("The value offset does not fit in classic TIFF.");
            }
            EndianWriter.WriteUInt32(bytes.AsSpan(slotOffset, 4), (uint)valueOffset, ByteOrder);
        }

        if (!entry.HasValueOffsetOverride)
        {
            entry.ResolvedBytes.CopyTo(bytes, checked((int)entry.ValueOffset));
        }
    }
}

internal class SyntheticTiffIfd
{
    internal List<SyntheticTiffEntry> Entries;
    internal SyntheticTiffIfd? NextIfd;
    internal ulong Offset;

    internal SyntheticTiffIfd()
    {
        Entries = new List<SyntheticTiffEntry>();
        NextIfd = null;
        Offset = 0;
    }
}

internal class SyntheticTiffEntry
{
    internal ushort Tag;
    internal TiffFieldType FieldType;
    internal ulong Count;
    internal byte[] RawBytes;
    internal SyntheticTiffIfd[]? Targets;
    internal byte[] ResolvedBytes;
    internal ulong ValueOffset;
    internal ulong ValueOffsetOverride;
    internal bool HasValueOffsetOverride;

    internal SyntheticTiffEntry(ushort tag, TiffFieldType fieldType, ulong count, byte[] rawBytes)
    {
        Tag = tag;
        FieldType = fieldType;
        Count = count;
        RawBytes = rawBytes;
        Targets = null;
        ResolvedBytes = rawBytes;
        ValueOffset = 0;
        ValueOffsetOverride = 0;
        HasValueOffsetOverride = false;
    }
}
