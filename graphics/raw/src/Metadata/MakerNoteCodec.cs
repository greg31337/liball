using System;

namespace raw;

internal class MakerNoteParseState
{
    internal byte[] Bytes;
    internal long MakerOffset;
    internal long BaseAbsolute;
    internal RawByteOrder ByteOrder;
    internal bool AdjustOnRelocation;
    internal RawLimits? Limits;
    internal System.Collections.Generic.List<RawMakerNoteOffsetReference> References;
    internal System.Collections.Generic.List<int> VisitedIfds;
    internal int TagCount;

    internal MakerNoteParseState(byte[] bytes, long makerOffset, long baseAbsolute, RawByteOrder byteOrder, bool adjustOnRelocation, RawLimits? limits)
    {
        Bytes = bytes;
        MakerOffset = makerOffset;
        BaseAbsolute = baseAbsolute;
        ByteOrder = byteOrder;
        AdjustOnRelocation = adjustOnRelocation;
        Limits = limits;
        References = new System.Collections.Generic.List<RawMakerNoteOffsetReference>();
        VisitedIfds = new System.Collections.Generic.List<int>();
        TagCount = 0;
    }
}

internal static class MakerNoteCodec
{
    internal static RawMakerNote[] Discover(TiffIfdGraph graph, RawMetadata metadata, RawLimits? limits)
    {
        System.Collections.Generic.List<RawMakerNote> notes = new System.Collections.Generic.List<RawMakerNote>();
        if (graph == null || graph.Header == null || graph.Ifds == null)
        {
            return notes.ToArray();
        }
        string manufacturer = metadata == null ? string.Empty : ExifCodec.GetText(metadata, "IFD0", 271);
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (entry.Tag == 37500 && entry.RawBytes.Length > 0)
                {
                    long offset = ValueOffset(graph, entry);
                    notes.Add(Parse(entry.RawBytes, offset, entry.EntryOffset, ifd.Path, manufacturer, graph.Header.BaseOffset, graph.Header.ByteOrder, limits));
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        return notes.ToArray();
    }

    internal static RawMakerNote Parse(byte[] rawBytes, long offset, long entryOffset, string? ifdPath, string? manufacturer, long tiffBaseOffset, RawByteOrder tiffByteOrder, RawLimits? limits)
    {
        ValidateInputs(rawBytes, offset, tiffBaseOffset, tiffByteOrder, limits);
        byte[] preserved = RawTextValue.Copy(rawBytes);
        string selectedManufacturer = manufacturer == null ? string.Empty : manufacturer;
        string selectedPath = ifdPath == null ? string.Empty : ifdPath;
        if (NikonSignature(preserved))
        {
            RawMakerNote? nikon = ParseNikon(preserved, offset, entryOffset, selectedPath, selectedManufacturer, limits);
            if (nikon != null)
            {
                return nikon;
            }
            return Unknown(preserved, offset, entryOffset, selectedPath, selectedManufacturer, "Nikon", tiffByteOrder);
        }
        if (CanonManufacturer(selectedManufacturer))
        {
            MakerNoteParseState state = new MakerNoteParseState(preserved, offset, tiffBaseOffset, tiffByteOrder, true, limits);
            if (ParseClassicIfd(state, 0, 0))
            {
                RawMakerNote canon = new RawMakerNote(preserved, selectedManufacturer, "CanonIfd", offset, entryOffset, selectedPath, tiffByteOrder, RawMakerNoteOffsetBase.TiffHeader, tiffBaseOffset, -1, state.References.ToArray(), true);
                canon.CanonTags = CanonMakerNoteCodec.Decode(canon, (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")));
                return canon;
            }
            return Unknown(preserved, offset, entryOffset, selectedPath, selectedManufacturer, "Canon", tiffByteOrder);
        }
        return Unknown(preserved, offset, entryOffset, selectedPath, selectedManufacturer, string.Empty, tiffByteOrder);
    }

    internal static RawMakerNote? ParseNikon(byte[] bytes, long offset, long entryOffset, string ifdPath, string manufacturer, RawLimits? limits)
    {
        if (bytes.Length < 18)
        {
            return null;
        }
        RawByteOrder byteOrder;
        if (bytes[10] == 0x49 && bytes[11] == 0x49)
        {
            byteOrder = RawByteOrder.LittleEndian;
        }
        else if (bytes[10] == 0x4D && bytes[11] == 0x4D)
        {
            byteOrder = RawByteOrder.BigEndian;
        }
        else
        {
            return null;
        }
        if (ReadUInt16(bytes, 12, byteOrder) != 42)
        {
            return null;
        }
        ulong firstOffset = ReadUInt32(bytes, 14, byteOrder);
        if (firstOffset > int.MaxValue || firstOffset > (ulong)(bytes.Length - 10))
        {
            return null;
        }
        int firstPosition = 10 + (int)firstOffset;
        long baseAbsolute = CheckedMath.Add(offset, 10);
        MakerNoteParseState state = new MakerNoteParseState(bytes, offset, baseAbsolute, byteOrder, false, limits);
        if (!ParseClassicIfd(state, firstPosition, 0))
        {
            return null;
        }
        RawMakerNote nikon = new RawMakerNote(bytes, manufacturer, "NikonEmbeddedTiff", offset, entryOffset, ifdPath, byteOrder, RawMakerNoteOffsetBase.EmbeddedTiffHeader, baseAbsolute, 10, state.References.ToArray(), true);
        nikon.NikonTags = NikonMakerNoteCodec.Decode(nikon, (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")));
        nikon.NikonVersion = NikonMakerNoteCodec.Version(nikon.NikonTags);
        nikon.NikonColorBalance = NikonMakerNoteCodec.DecodeColorBalance(nikon, nikon.NikonTags);
        return nikon;
    }

    internal static bool ParseClassicIfd(MakerNoteParseState state, int position, int depth)
    {
        if (depth >= (state.Limits ?? throw new System.InvalidOperationException("The maker-note limits are unavailable.")).MaxRecursionDepth || position < 0 || position > state.Bytes.Length - 2 || Contains(state.VisitedIfds, position))
        {
            return false;
        }
        state.VisitedIfds.Add(position);
        ushort count = ReadUInt16(state.Bytes, position, state.ByteOrder);
        if (count > state.Limits.MaxTagCount - state.TagCount)
        {
            return false;
        }
        state.TagCount += count;
        long entriesLength = (long)count * 12;
        long nextPosition = (long)position + 2 + entriesLength;
        if (nextPosition < 0 || nextPosition > state.Bytes.Length - 4)
        {
            return false;
        }
        int index = 0;
        while (index < count)
        {
            int entryPosition = position + 2 + index * 12;
            ushort type = ReadUInt16(state.Bytes, entryPosition + 2, state.ByteOrder);
            int elementSize = ElementSize(type);
            ulong valueCount = ReadUInt32(state.Bytes, entryPosition + 4, state.ByteOrder);
            if (elementSize == 0 || valueCount > long.MaxValue / (ulong)elementSize)
            {
                return false;
            }
            long valueLength = (long)valueCount * elementSize;
            if (valueLength > state.Limits.MaxTagValueBytes)
            {
                return false;
            }
            if (valueLength > 4)
            {
                ulong value = ReadUInt32(state.Bytes, entryPosition + 8, state.ByteOrder);
                int target = TargetPosition(state, value);
                if (target < 0 || target > state.Bytes.Length || valueLength > state.Bytes.Length - target)
                {
                    return false;
                }
                state.References.Add(new RawMakerNoteOffsetReference(entryPosition + 8, 4, value, target, state.AdjustOnRelocation));
            }
            index++;
        }
        uint next = ReadUInt32(state.Bytes, (int)nextPosition, state.ByteOrder);
        if (next == 0)
        {
            return true;
        }
        int nextIfd = TargetPosition(state, next);
        if (nextIfd < 0 || nextIfd > state.Bytes.Length - 2)
        {
            return false;
        }
        state.References.Add(new RawMakerNoteOffsetReference((int)nextPosition, 4, next, nextIfd, state.AdjustOnRelocation));
        return ParseClassicIfd(state, nextIfd, depth + 1);
    }

    internal static int TargetPosition(MakerNoteParseState state, ulong value)
    {
        if (value > long.MaxValue)
        {
            return -1;
        }
        long targetAbsolute;
        if (state.BaseAbsolute > long.MaxValue - (long)value)
        {
            return -1;
        }
        targetAbsolute = state.BaseAbsolute + (long)value;
        long relative = targetAbsolute - state.MakerOffset;
        if (relative < 0 || relative > int.MaxValue)
        {
            return -1;
        }
        return (int)relative;
    }

    internal static RawMakerNote Unknown(byte[] bytes, long offset, long entryOffset, string ifdPath, string manufacturer, string signature, RawByteOrder byteOrder)
    {
        return new RawMakerNote(bytes, manufacturer, signature, offset, entryOffset, ifdPath, byteOrder, RawMakerNoteOffsetBase.Unknown, -1, -1, System.Array.Empty<RawMakerNoteOffsetReference>(), false);
    }

    internal static void ValidateInputs(byte[] rawBytes, long offset, long tiffBaseOffset, RawByteOrder byteOrder, RawLimits? limits)
    {
        CheckedMath.RequireLimits(limits);
        if (rawBytes == null || rawBytes.Length == 0 || offset < 0 || tiffBaseOffset < 0 || offset < tiffBaseOffset || byteOrder < RawByteOrder.LittleEndian || byteOrder > RawByteOrder.BigEndian)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Complete maker-note bytes and valid containing TIFF context are required.", offset);
        }
        if (rawBytes.LongLength > (limits ?? throw new System.InvalidOperationException("A required value is unavailable.")).MaxTagValueBytes || rawBytes.LongLength > limits.MaxMetadataBytes)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "The maker note exceeds configured metadata limits.", offset);
        }
        CheckedMath.AllocationSize(rawBytes.LongLength, 1, limits);
    }

    internal static long ValueOffset(TiffIfdGraph graph, TiffEntry entry)
    {
        if (entry.Inline)
        {
            return CheckedMath.Add(entry.EntryOffset, graph.Header.BigTiff ? 12 : 8);
        }
        return CheckedMath.Add(graph.Header.BaseOffset, CheckedMath.ToInt64(entry.ValueOffset));
    }

    internal static bool NikonSignature(byte[] bytes)
    {
        return bytes.Length >= 6 && bytes[0] == (byte)'N' && bytes[1] == (byte)'i' && bytes[2] == (byte)'k' && bytes[3] == (byte)'o' && bytes[4] == (byte)'n' && bytes[5] == 0;
    }

    internal static bool CanonManufacturer(string manufacturer)
    {
        return manufacturer != null && manufacturer.StartsWith("Canon", System.StringComparison.OrdinalIgnoreCase);
    }

    internal static int ElementSize(ushort type)
    {
        if (type == 1 || type == 2 || type == 6 || type == 7) return 1;
        if (type == 3 || type == 8) return 2;
        if (type == 4 || type == 9 || type == 11) return 4;
        if (type == 5 || type == 10 || type == 12) return 8;
        return 0;
    }

    internal static ushort ReadUInt16(byte[] bytes, int offset, RawByteOrder byteOrder)
    {
        return EndianReader.ReadUInt16(bytes.AsSpan(offset, 2), byteOrder);
    }

    internal static uint ReadUInt32(byte[] bytes, int offset, RawByteOrder byteOrder)
    {
        return EndianReader.ReadUInt32(bytes.AsSpan(offset, 4), byteOrder);
    }

    internal static bool Contains(System.Collections.Generic.List<int> values, int value)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == value)
            {
                return true;
            }
            index++;
        }
        return false;
    }
}
