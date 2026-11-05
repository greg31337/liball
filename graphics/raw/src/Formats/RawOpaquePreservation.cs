namespace raw;

internal class RawPreservedRange
{
    internal long Start;
    internal long End;

    internal RawPreservedRange(long start, long end)
    {
        Start = start;
        End = end;
    }
}

internal static class RawOpaquePreservation
{
    internal static void AddAuthenticityBlocks(System.Collections.Generic.List<RawPayloadBlock> payloads, RawMetadata? metadata, TiffIfdGraph graph, System.ReadOnlyMemory<byte> source)
    {
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (AuthenticityTiffTag(entry.Tag) && entry.RawBytes.Length > 0)
                {
                    long offset = ValueOffset(graph, entry) - graph.ContainerStart;
                    AddAuthenticity(payloads, ifd.Path + "/Authenticity0x" + entry.Tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture), offset, entry.RawBytes.Length, source.Length);
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        if (metadata == null || metadata.MakerNotes == null)
        {
            return;
        }
        int noteIndex = 0;
        while (noteIndex < metadata.MakerNotes.Length)
        {
            RawMakerNote note = metadata.MakerNotes[noteIndex];
            if (note != null)
            {
                AddCanonAuthenticity(payloads, note, noteIndex, graph, source);
                AddNikonAuthenticity(payloads, note, noteIndex, graph, source.Length);
            }
            noteIndex++;
        }
    }

    internal static void AddCanonAuthenticity(System.Collections.Generic.List<RawPayloadBlock> payloads, RawMakerNote note, int noteIndex, TiffIfdGraph graph, System.ReadOnlyMemory<byte> source)
    {
        int index = 0;
        while (note.CanonTags != null && index < note.CanonTags.Length)
        {
            RawCanonMakerNoteTag tag = note.CanonTags[index];
            if (tag != null && tag.Id == 0x0083 && tag.Value != null && tag.Value.Type == RawTagType.Long && tag.Value.Count == 1)
            {
                long offset = CheckedMath.ToInt64(tag.Value.GetUInt32Values()[0]) - graph.ContainerStart;
                int length = CanonOriginalDecisionLength(source, offset, tag.Value.ByteOrder);
                if (length > 0)
                {
                    string path = note.IfdPath + "/MakerNote" + noteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/OriginalDecisionData";
                    AddAuthenticity(payloads, path, offset, length, source.Length);
                    payloads.Add(new RawPayloadBlock(RawPayloadKind.Opaque, path + "/OpaqueCopy", offset, length, -1, -1, -1, RawCompression.None, false, true));
                }
            }
            index++;
        }
    }

    internal static void AddNikonAuthenticity(System.Collections.Generic.List<RawPayloadBlock> payloads, RawMakerNote note, int noteIndex, TiffIfdGraph graph, long sourceLength)
    {
        int index = 0;
        while (note.NikonTags != null && index < note.NikonTags.Length)
        {
            RawNikonMakerNoteTag tag = note.NikonTags[index];
            if (tag != null && tag.Id == 0x0020 && tag.Value != null && tag.Value.RawBytes.Length > 0)
            {
                long offset = CheckedMath.Add(note.Offset, tag.ValuePosition) - graph.ContainerStart;
                string path = note.IfdPath + "/MakerNote" + noteIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + "/ImageAuthentication";
                AddAuthenticity(payloads, path, offset, tag.Value.RawBytes.Length, sourceLength);
            }
            index++;
        }
    }

    internal static int CanonOriginalDecisionLength(System.ReadOnlyMemory<byte> source, long offset, RawByteOrder byteOrder)
    {
        if (offset < 0 || offset > source.Length - 8)
        {
            return 0;
        }
        int start = CheckedMath.ToInt32(offset);
        System.ReadOnlySpan<byte> bytes = source.Span;
        if (bytes[start] != 0xff || bytes[start + 1] != 0xff || bytes[start + 2] != 0xff || bytes[start + 3] != 0xff)
        {
            return 0;
        }
        uint version = EndianReader.ReadUInt32(bytes.Slice(start + 4, 4), byteOrder);
        if (version == 1 || version == 2)
        {
            if (offset > source.Length - 32)
            {
                return 0;
            }
            uint count = EndianReader.ReadUInt32(bytes.Slice(start + 28, 4), byteOrder);
            if (count == 0 || count >= 20)
            {
                return 0;
            }
            long length = CheckedMath.Add(32, CheckedMath.Multiply(count, 32));
            return length > source.Length - offset ? 0 : CheckedMath.ToInt32(length);
        }
        if (version != 3)
        {
            return 0;
        }
        int position = start + 8;
        int part = 0;
        while (part < 3)
        {
            if (position > source.Length - 4)
            {
                return 0;
            }
            uint storedLength = EndianReader.ReadUInt32(bytes.Slice(position, 4), byteOrder);
            long dataLength = part == 2 && storedLength >= 4 ? storedLength - 4 : storedLength;
            if (dataLength > 0x10000 || dataLength > source.Length - position - 4)
            {
                return 0;
            }
            position = CheckedMath.ToInt32(CheckedMath.Add(position, CheckedMath.Add(4, dataLength)));
            part++;
        }
        return position - start;
    }

    internal static bool AuthenticityTiffTag(ushort tag)
    {
        return tag == 50969 || tag == 50972 || tag == 50973 || tag == 51111;
    }

    internal static long ValueOffset(TiffIfdGraph graph, TiffEntry entry)
    {
        if (entry.Inline)
        {
            return CheckedMath.Add(entry.EntryOffset, graph.Header.BigTiff ? 12 : 8);
        }
        return CheckedMath.Add(graph.Header.BaseOffset, CheckedMath.ToInt64(entry.ValueOffset));
    }

    internal static void AddAuthenticity(System.Collections.Generic.List<RawPayloadBlock> payloads, string path, long offset, long length, long sourceLength)
    {
        if (offset < 0 || length <= 0 || offset > sourceLength || length > sourceLength - offset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "An authenticity payload exceeds the bounded TIFF source.", offset);
        }
        payloads.Add(new RawPayloadBlock(RawPayloadKind.Authenticity, path, offset, length, -1, -1, -1, RawCompression.None, false, true));
    }

    internal static void AddOpaqueIslands(RawFormat format, TiffIfdGraph graph, System.ReadOnlyMemory<byte> source, System.Collections.Generic.List<RawPayloadBlock> payloads)
    {
        System.Collections.Generic.List<RawPreservedRange> ranges = new System.Collections.Generic.List<RawPreservedRange>();
        long headerStart = graph.Header.BaseOffset - graph.ContainerStart;
        long headerLength = graph.Header.HeaderSize;
        if (format == RawFormat.Cr2)
        {
            long firstIfd = graph.Header.GetFirstIfdAbsoluteOffset() - graph.ContainerStart;
            if (firstIfd > headerStart && firstIfd <= source.Length)
            {
                headerLength = firstIfd - headerStart;
            }
        }
        AddRange(ranges, headerStart, headerLength, source.Length);
        int ifdIndex = 0;
        while (ifdIndex < graph.Ifds.Length)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            int countSize = graph.Header.BigTiff ? 8 : 2;
            int entrySize = graph.Header.BigTiff ? 20 : 12;
            int nextSize = graph.Header.BigTiff ? 8 : 4;
            long directoryLength = countSize + (long)ifd.Entries.Length * entrySize + nextSize;
            AddRange(ranges, ifd.AbsoluteOffset - graph.ContainerStart, directoryLength, source.Length);
            int entryIndex = 0;
            while (entryIndex < ifd.Entries.Length)
            {
                TiffEntry entry = ifd.Entries[entryIndex];
                if (!entry.Inline && entry.RawBytes.Length > 0)
                {
                    AddRange(ranges, ValueOffset(graph, entry) - graph.ContainerStart, entry.RawBytes.Length, source.Length);
                }
                entryIndex++;
            }
            ifdIndex++;
        }
        int blockIndex = 0;
        while (blockIndex < payloads.Count)
        {
            RawPayloadBlock block = payloads[blockIndex];
            if (block != null)
            {
                AddRange(ranges, block.Offset, block.Length, source.Length);
            }
            blockIndex++;
        }
        SortRanges(ranges);
        long cursor = 0;
        int island = 0;
        int rangeIndex = 0;
        while (rangeIndex < ranges.Count)
        {
            RawPreservedRange range = ranges[rangeIndex];
            if (range.Start > cursor)
            {
                island = AddIsland(source, payloads, cursor, range.Start - cursor, island);
            }
            if (range.End > cursor)
            {
                cursor = range.End;
            }
            rangeIndex++;
        }
        if (cursor < source.Length)
        {
            AddIsland(source, payloads, cursor, source.Length - cursor, island);
        }
    }

    internal static void AddRange(System.Collections.Generic.List<RawPreservedRange> ranges, long offset, long length, long sourceLength)
    {
        if (offset < 0 || length < 0 || offset > sourceLength || length > sourceLength - offset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "A preservation range exceeds the bounded TIFF source.", offset);
        }
        if (length != 0)
        {
            ranges.Add(new RawPreservedRange(offset, offset + length));
        }
    }

    internal static void SortRanges(System.Collections.Generic.List<RawPreservedRange> ranges)
    {
        ranges.Sort((left, right) => Compare(left, right));
    }

    internal static int Compare(RawPreservedRange left, RawPreservedRange right)
    {
        if (left.Start < right.Start)
        {
            return -1;
        }
        if (left.Start > right.Start)
        {
            return 1;
        }
        if (left.End > right.End)
        {
            return -1;
        }
        if (left.End < right.End)
        {
            return 1;
        }
        return 0;
    }

    internal static int AddIsland(System.ReadOnlyMemory<byte> source, System.Collections.Generic.List<RawPayloadBlock> payloads, long offset, long length, int island)
    {
        int start = CheckedMath.ToInt32(offset);
        int end = CheckedMath.ToInt32(offset + length);
        while (start < end && source.Span[start] == 0)
        {
            start++;
        }
        while (end > start && source.Span[end - 1] == 0)
        {
            end--;
        }
        if (start == end)
        {
            return island;
        }
        string path = "Opaque/Island" + island.ToString(System.Globalization.CultureInfo.InvariantCulture);
        payloads.Add(new RawPayloadBlock(RawPayloadKind.Opaque, path, start, end - start, -1, -1, -1, RawCompression.None, false, true));
        return island + 1;
    }
}
