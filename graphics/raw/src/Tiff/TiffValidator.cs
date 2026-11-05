namespace raw;

internal class TiffRange
{
    internal long Start;
    internal long End;
    internal string Context;
    internal ushort Tag;

    internal TiffRange(long start, long end, string context, ushort tag)
    {
        Start = start;
        End = end;
        Context = context;
        Tag = tag;
    }
}

internal static class TiffValidator
{
    internal static RawValidationReport Validate(TiffIfdGraph? graph, bool strict)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (graph == null)
        {
            Add(messages, RawDiagnosticCode.InvalidArgument, RawSeverity.Error, -1, "TIFF", "A TIFF graph is required for validation.");
            return new RawValidationReport(strict, messages.ToArray());
        }

        System.Collections.Generic.List<TiffRange> ranges = new System.Collections.Generic.List<TiffRange>();
        AddRange(graph, ranges, messages, graph.Header.BaseOffset, graph.Header.HeaderSize, "TIFF/Header", 0, true);
        for (int ifdIndex = 0; ifdIndex < graph.Ifds.Length; ifdIndex++)
        {
            ValidateIfd(graph, graph.Ifds[ifdIndex], ranges, messages);
        }
        ValidateLinks(graph, messages);
        ValidateImages(graph, ranges, messages);
        ValidateOverlaps(ranges, messages);
        ValidateTrailingData(graph, ranges, messages, strict);
        return new RawValidationReport(strict, messages.ToArray());
    }

    internal static void ValidateIfd(TiffIfdGraph graph, TiffIfd ifd, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        int countSize = graph.Header.BigTiff ? 8 : 2;
        int entrySize = graph.Header.BigTiff ? 20 : 12;
        int nextSize = graph.Header.BigTiff ? 8 : 4;
        long entriesLength = CheckedLength(ifd.Entries.Length, entrySize, ifd.AbsoluteOffset, ifd.Path, messages);
        long ifdLength = -1;
        if (entriesLength >= 0 && entriesLength <= long.MaxValue - countSize - nextSize)
        {
            ifdLength = countSize + entriesLength + nextSize;
        }
        else if (entriesLength >= 0)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, ifd.AbsoluteOffset, ifd.Path, "The IFD byte length overflows a signed file position.");
        }
        if (ifdLength >= 0)
        {
            AddRange(graph, ranges, messages, ifd.AbsoluteOffset, ifdLength, ifd.Path, 0, true);
        }
        ValidateAlignment(graph, ifd.Offset, ifd.AbsoluteOffset, ifd.Path, messages);

        System.Collections.Generic.HashSet<ushort> tags = new System.Collections.Generic.HashSet<ushort>();
        for (int entryIndex = 0; entryIndex < ifd.Entries.Length; entryIndex++)
        {
            TiffEntry entry = ifd.Entries[entryIndex];
            string context = TagContext(ifd.Path, entry.Tag);
            long entryDelta = (long)entryIndex * entrySize;
            long expectedEntryOffset = -1;
            if (ifd.AbsoluteOffset <= long.MaxValue - countSize && entryDelta <= long.MaxValue - ifd.AbsoluteOffset - countSize)
            {
                expectedEntryOffset = ifd.AbsoluteOffset + countSize + entryDelta;
            }
            else
            {
                Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, ifd.AbsoluteOffset, context, "The TIFF entry position overflows a signed file offset.");
            }
            if (expectedEntryOffset >= 0 && entry.EntryOffset != expectedEntryOffset)
            {
                Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, entry.EntryOffset, context, "The TIFF entry offset does not match its IFD position.");
            }
            if (tags.Contains(entry.Tag))
            {
                Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The IFD contains a duplicate tag.");
            }
            else
            {
                tags.Add(entry.Tag);
            }
            ValidateEntryStorage(graph, entry, context, messages);
            ValidateKnownEntry(graph, ifd, entry, context, messages);
            if (!entry.Inline)
            {
                AddValueRange(graph, ranges, messages, entry, context);
                ValidateAlignment(graph, entry.ValueOffset, GetAbsoluteOffset(graph, entry.ValueOffset, context, messages), context, messages);
            }
        }
        if (ifd.NextIfdOffset != 0 && FindIfdByOffset(graph, ifd.NextIfdOffset) == null)
        {
            Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, GetAbsoluteOffset(graph, ifd.NextIfdOffset, ifd.Path, messages), ifd.Path, "The next-IFD offset does not identify a parsed IFD.");
        }
    }

    internal static void ValidateEntryStorage(TiffIfdGraph graph, TiffEntry entry, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        int elementSize = TiffFieldTypes.GetElementSize(entry.FieldType);
        if (entry.Count > long.MaxValue || (entry.Count != 0 && (ulong)elementSize > ulong.MaxValue / entry.Count))
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, entry.EntryOffset, context, "The TIFF entry value length overflows.");
            return;
        }
        ulong byteLength = entry.Count * (ulong)elementSize;
        if (byteLength > int.MaxValue || entry.RawBytes.Length != (int)byteLength)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, RawSeverity.Error, entry.EntryOffset, context, "The retained TIFF value length does not match its type and count.");
        }
        int slotSize = graph.Header.BigTiff ? 8 : 4;
        bool expectedInline = byteLength <= (ulong)slotSize;
        if (entry.Inline != expectedInline)
        {
            Add(messages, RawDiagnosticCode.InvalidContainer, RawSeverity.Error, entry.EntryOffset, context, "The TIFF value does not use the required inline boundary.");
        }
        if (entry.ValueSlotBytes.Length != slotSize)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, RawSeverity.Error, entry.EntryOffset, context, "The retained TIFF value slot has an invalid length.");
        }
        if (entry.Inline && entry.RawBytes.Length <= entry.ValueSlotBytes.Length)
        {
            for (int index = 0; index < entry.RawBytes.Length; index++)
            {
                if (entry.RawBytes[index] != entry.ValueSlotBytes[index])
                {
                    Add(messages, RawDiagnosticCode.InvalidContainer, RawSeverity.Error, entry.EntryOffset, context, "The retained inline TIFF bytes do not match the value slot.");
                    break;
                }
            }
        }
    }

    internal static void ValidateLinks(TiffIfdGraph graph, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        for (int index = 0; index < graph.Links.Length; index++)
        {
            TiffIfdLink link = graph.Links[index];
            TiffIfd? target = FindIfdByOffset(graph, link.TargetOffset);
            if (target == null)
            {
                Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, GetAbsoluteOffset(graph, link.TargetOffset, link.Path, messages), link.Path, "The IFD link target is not present in the parsed graph.");
            }
            else if (target.Path != link.TargetPath)
            {
                Add(messages, RawDiagnosticCode.InvalidContainer, RawSeverity.Error, target.AbsoluteOffset, link.Path, "The IFD link canonical path is inconsistent.");
            }
        }
    }

    internal static TiffIfd? FindIfdByOffset(TiffIfdGraph graph, ulong offset)
    {
        for (int index = 0; index < graph.Ifds.Length; index++)
        {
            if (graph.Ifds[index].Offset == offset)
            {
                return graph.Ifds[index];
            }
        }
        return null;
    }

    internal static void ValidateKnownEntry(TiffIfdGraph graph, TiffIfd ifd, TiffEntry entry, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        ushort tag = entry.Tag;
        if (tag == TiffImageData.NewSubfileTypeTag)
        {
            RequireTypeAndCount(entry, TiffFieldType.Long, 1, context, messages);
        }
        else if (tag == TiffImageData.OldSubfileTypeTag || tag == TiffImageData.CompressionTag || tag == TiffImageData.PhotometricTag || tag == TiffImageData.FillOrderTag || tag == TiffImageData.OrientationTag || tag == TiffImageData.SamplesPerPixelTag || tag == TiffImageData.PlanarConfigurationTag || tag == TiffImageData.PredictorTag)
        {
            RequireTypeAndCount(entry, TiffFieldType.Short, 1, context, messages);
        }
        else if (tag == TiffImageData.ImageWidthTag || tag == TiffImageData.ImageLengthTag || tag == TiffImageData.RowsPerStripTag || tag == TiffImageData.TileWidthTag || tag == TiffImageData.TileLengthTag)
        {
            RequireShortOrLongScalar(entry, context, messages);
        }
        else if (tag == TiffImageData.BitsPerSampleTag || tag == TiffImageData.ExtraSamplesTag || tag == TiffImageData.SampleFormatTag)
        {
            RequireShortArray(entry, context, messages);
        }
        else if (tag == TiffImageData.StripOffsetsTag || tag == TiffImageData.StripByteCountsTag || tag == TiffImageData.TileOffsetsTag || tag == TiffImageData.TileByteCountsTag)
        {
            RequireOffsetArray(graph, entry, context, messages);
        }
        else if (tag == 330)
        {
            RequirePointerArray(graph, entry, false, context, messages);
        }
        else if (tag == 34665 || tag == 34853 || tag == 40965)
        {
            RequirePointerArray(graph, entry, true, context, messages);
        }

        if (tag == TiffImageData.FillOrderTag)
        {
            RequireCodeRange(ifd, tag, 1, 2, context, messages);
        }
        else if (tag == TiffImageData.OrientationTag)
        {
            RequireCodeRange(ifd, tag, 1, 8, context, messages);
        }
        else if (tag == TiffImageData.PlanarConfigurationTag)
        {
            RequireCodeRange(ifd, tag, 1, 2, context, messages);
        }
        else if (tag == TiffImageData.PredictorTag)
        {
            RequireCodeRange(ifd, tag, 1, 3, context, messages);
        }
        else if (tag == TiffImageData.SampleFormatTag)
        {
            ulong[] values = TiffImageData.GetUnsignedValues(entry);
            for (int index = 0; index < values.Length; index++)
            {
                if (values[index] < 1 || values[index] > 4)
                {
                    Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "A SampleFormat value is outside the defined range.");
                    break;
                }
            }
        }
    }

    internal static void ValidateImages(TiffIfdGraph graph, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        for (int ifdIndex = 0; ifdIndex < graph.Ifds.Length; ifdIndex++)
        {
            TiffIfd ifd = graph.Ifds[ifdIndex];
            if (IsImageCandidate(ifd))
            {
                ValidateRequiredImageTags(ifd, messages);
            }
        }
        for (int imageIndex = 0; imageIndex < graph.Images.Length; imageIndex++)
        {
            TiffImageDescriptor image = graph.Images[imageIndex];
            ValidateImageArrays(image, messages);
            ValidateCoverage(image, messages);
            AddPayloadRanges(graph, image, ranges, messages);
        }
    }

    internal static bool IsImageCandidate(TiffIfd ifd)
    {
        return ifd.FindEntry(TiffImageData.ImageWidthTag) != null || ifd.FindEntry(TiffImageData.ImageLengthTag) != null || ifd.FindEntry(TiffImageData.StripOffsetsTag) != null || ifd.FindEntry(TiffImageData.StripByteCountsTag) != null || ifd.FindEntry(TiffImageData.TileOffsetsTag) != null || ifd.FindEntry(TiffImageData.TileByteCountsTag) != null;
    }

    internal static void ValidateRequiredImageTags(TiffIfd ifd, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        RequireTag(ifd, TiffImageData.ImageWidthTag, "ImageWidth", messages);
        RequireTag(ifd, TiffImageData.ImageLengthTag, "ImageLength", messages);
        RequireTag(ifd, TiffImageData.PhotometricTag, "PhotometricInterpretation", messages);
        bool strips = ifd.FindEntry(TiffImageData.StripOffsetsTag) != null || ifd.FindEntry(TiffImageData.StripByteCountsTag) != null;
        bool tiles = ifd.FindEntry(TiffImageData.TileOffsetsTag) != null || ifd.FindEntry(TiffImageData.TileByteCountsTag) != null;
        bool jpeg = ifd.FindEntry(513) != null || ifd.FindEntry(514) != null;
        if (!strips && !tiles && !jpeg)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, ifd.AbsoluteOffset, ifd.Path, "The image has neither strip, tile, nor JPEG interchange storage tags.");
        }
        if (strips && tiles)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, ifd.AbsoluteOffset, ifd.Path, "The image declares both strip and tile storage.");
        }
        if (jpeg && (strips || tiles))
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, ifd.AbsoluteOffset, ifd.Path, "The image declares JPEG interchange storage together with strip or tile storage.");
        }
        if (strips)
        {
            RequireTag(ifd, TiffImageData.StripOffsetsTag, "StripOffsets", messages);
            RequireTag(ifd, TiffImageData.StripByteCountsTag, "StripByteCounts", messages);
        }
        if (tiles)
        {
            RequireTag(ifd, TiffImageData.TileWidthTag, "TileWidth", messages);
            RequireTag(ifd, TiffImageData.TileLengthTag, "TileLength", messages);
            RequireTag(ifd, TiffImageData.TileOffsetsTag, "TileOffsets", messages);
            RequireTag(ifd, TiffImageData.TileByteCountsTag, "TileByteCounts", messages);
        }
        if (jpeg)
        {
            RequireTag(ifd, 513, "JPEGInterchangeFormat", messages);
            RequireTag(ifd, 514, "JPEGInterchangeFormatLength", messages);
        }
    }

    internal static void ValidateImageArrays(TiffImageDescriptor image, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (image.Width == 0 || image.Height == 0)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, image.Ifd.AbsoluteOffset, image.Path, "Image dimensions must be greater than zero.");
        }
        if (image.SamplesPerPixel == 0)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, image.Ifd.AbsoluteOffset, image.Path, "SamplesPerPixel must be greater than zero.");
        }
        if (image.BitsPerSample.Length != 1 && (ulong)image.BitsPerSample.Length != image.SamplesPerPixel)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, EntryOffset(image.Ifd, TiffImageData.BitsPerSampleTag), TagContext(image.Path, TiffImageData.BitsPerSampleTag), "BitsPerSample must contain one value or one value per sample.");
        }
        if (image.SampleFormatCodes.Length != 1 && (ulong)image.SampleFormatCodes.Length != image.SamplesPerPixel)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, EntryOffset(image.Ifd, TiffImageData.SampleFormatTag), TagContext(image.Path, TiffImageData.SampleFormatTag), "SampleFormat must contain one value or one value per sample.");
        }
        if ((ulong)image.ExtraSamples.Length > image.SamplesPerPixel)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, EntryOffset(image.Ifd, TiffImageData.ExtraSamplesTag), TagContext(image.Path, TiffImageData.ExtraSamplesTag), "ExtraSamples cannot exceed SamplesPerPixel.");
        }
    }

    internal static void ValidateCoverage(TiffImageDescriptor image, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (image.StorageKind == TiffStorageKind.Strips || image.StorageKind == TiffStorageKind.Mixed)
        {
            ValidateStorageCounts(image, false, messages);
        }
        if (image.StorageKind == TiffStorageKind.Tiles || image.StorageKind == TiffStorageKind.Mixed)
        {
            ValidateStorageCounts(image, true, messages);
        }
    }

    internal static void ValidateStorageCounts(TiffImageDescriptor image, bool tiled, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        ulong[] offsets = tiled ? image.TileOffsets : image.StripOffsets;
        ulong[] byteCounts = tiled ? image.TileByteCounts : image.StripByteCounts;
        ushort offsetTag = tiled ? TiffImageData.TileOffsetsTag : TiffImageData.StripOffsetsTag;
        string name = tiled ? "tile" : "strip";
        if (offsets.Length != byteCounts.Length)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, RawSeverity.Error, EntryOffset(image.Ifd, offsetTag), image.Path, "The TIFF " + name + " offset and byte-count arrays have different lengths.");
        }
        ulong perPlane;
        if (tiled)
        {
            ulong across = TiffImageData.CeilingDivide(image.Width, image.TileWidth);
            ulong down = TiffImageData.CeilingDivide(image.Height, image.TileHeight);
            if (across != 0 && down > ulong.MaxValue / across)
            {
                Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, image.Ifd.AbsoluteOffset, image.Path, "The TIFF tile grid size overflows.");
                return;
            }
            perPlane = across * down;
        }
        else
        {
            perPlane = TiffImageData.GetStripsPerPlane(image);
        }
        if (perPlane == 0)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, image.Ifd.AbsoluteOffset, image.Path, "The TIFF " + name + " geometry cannot cover the image.");
            return;
        }
        ulong expected = perPlane;
        if (image.PlanarConfiguration == TiffPlanarConfiguration.Planar)
        {
            if (image.SamplesPerPixel != 0 && expected > ulong.MaxValue / image.SamplesPerPixel)
            {
                Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, image.Ifd.AbsoluteOffset, image.Path, "The planar TIFF block count overflows.");
                return;
            }
            expected *= image.SamplesPerPixel;
        }
        if ((ulong)offsets.Length != expected || (ulong)byteCounts.Length != expected)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, EntryOffset(image.Ifd, offsetTag), image.Path, "The TIFF " + name + " arrays do not cover the declared image geometry.");
        }
    }

    internal static void AddPayloadRanges(TiffIfdGraph graph, TiffImageDescriptor image, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        for (int index = 0; index < image.Blocks.Length; index++)
        {
            TiffDataBlock block = image.Blocks[index];
            string context = image.Path + (block.Tiled ? "/Tile" : "/Strip") + block.Index;
            if (block.ByteCount == 0)
            {
                Add(messages, RawDiagnosticCode.InvalidLength, RawSeverity.Error, OffsetForMessage(graph, block.Offset), context, "A TIFF image data block has zero length.");
                continue;
            }
            if (block.Offset == 0)
            {
                Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, graph.Header.BaseOffset, context, "A TIFF image data block has a null offset.");
            }
            long absoluteOffset = GetAbsoluteOffset(graph, block.Offset, context, messages);
            long length = ToLength(block.ByteCount, absoluteOffset, context, messages);
            if (absoluteOffset >= 0 && length >= 0)
            {
                AddRange(graph, ranges, messages, absoluteOffset, length, context, 0, false);
                ValidateAlignment(graph, block.Offset, absoluteOffset, context, messages);
            }
        }
    }

    internal static void AddValueRange(TiffIfdGraph graph, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages, TiffEntry entry, string context)
    {
        long absoluteOffset = GetAbsoluteOffset(graph, entry.ValueOffset, context, messages);
        if (absoluteOffset >= 0)
        {
            AddRange(graph, ranges, messages, absoluteOffset, entry.RawBytes.Length, context, entry.Tag, false);
        }
    }

    internal static void AddRange(TiffIfdGraph graph, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages, long start, long length, string context, ushort tag, bool allowZeroLength)
    {
        if (length < 0 || (!allowZeroLength && length == 0))
        {
            Add(messages, RawDiagnosticCode.InvalidLength, RawSeverity.Error, start, context, "A TIFF range has an invalid length.");
            return;
        }
        if (start < graph.ContainerStart)
        {
            Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, start, context, "A TIFF range starts before the bounded container.");
            return;
        }
        long containerEnd = ContainerEnd(graph, messages);
        if (containerEnd < 0)
        {
            return;
        }
        if (start > containerEnd || length > containerEnd - start)
        {
            Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, start, context, "A TIFF range extends beyond the bounded container.");
            return;
        }
        ranges.Add(new TiffRange(start, start + length, context, tag));
    }

    internal static void ValidateOverlaps(System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        ranges.Sort((left, right) => CompareRanges(left, right));
        if (ranges.Count == 0)
        {
            return;
        }
        TiffRange previous = ranges[0];
        for (int index = 1; index < ranges.Count; index++)
        {
            TiffRange current = ranges[index];
            if (current.Start < previous.End && current.End > current.Start && previous.End > previous.Start)
            {
                Add(messages, RawDiagnosticCode.InvalidContainer, RawSeverity.Error, current.Start, current.Context, "The TIFF range overlaps " + previous.Context + ".");
            }
            if (current.End > previous.End)
            {
                previous = current;
            }
        }
    }

    internal static int CompareRanges(TiffRange left, TiffRange right)
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
        return string.CompareOrdinal(left.Context, right.Context);
    }

    internal static void ValidateTrailingData(TiffIfdGraph graph, System.Collections.Generic.List<TiffRange> ranges, System.Collections.Generic.List<RawValidationMessage> messages, bool strict)
    {
        long containerEnd = ContainerEnd(graph, messages);
        if (containerEnd < 0)
        {
            return;
        }
        long claimedEnd = graph.ContainerStart;
        for (int index = 0; index < ranges.Count; index++)
        {
            if (ranges[index].End > claimedEnd)
            {
                claimedEnd = ranges[index].End;
            }
        }
        if (claimedEnd < containerEnd)
        {
            RawSeverity severity = strict ? RawSeverity.Error : RawSeverity.Warning;
            Add(messages, RawDiagnosticCode.InvalidContainer, severity, claimedEnd, "TIFF/TrailingData", "The bounded TIFF contains unclaimed trailing data.");
        }
    }

    internal static void ValidateAlignment(TiffIfdGraph graph, ulong relativeOffset, long absoluteOffset, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        int alignment = graph.Header.BigTiff ? 8 : 2;
        if (absoluteOffset >= 0 && relativeOffset % (ulong)alignment != 0)
        {
            Add(messages, RawDiagnosticCode.InvalidOffset, RawSeverity.Error, absoluteOffset, context, "The TIFF offset does not satisfy the required alignment.");
        }
    }

    internal static void RequireTypeAndCount(TiffEntry entry, TiffFieldType fieldType, ulong count, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (entry.FieldType != fieldType)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF tag has an invalid field type.");
        }
        if (entry.Count != count)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF tag has an invalid value count.");
        }
    }

    internal static void RequireShortOrLongScalar(TiffEntry entry, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (entry.FieldType != TiffFieldType.Short && entry.FieldType != TiffFieldType.Long)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF scalar must use SHORT or LONG.");
        }
        if (entry.Count != 1)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF scalar must contain one value.");
        }
    }

    internal static void RequireShortArray(TiffEntry entry, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (entry.FieldType != TiffFieldType.Short)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF array must use SHORT values.");
        }
        if (entry.Count == 0)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF array must not be empty.");
        }
    }

    internal static void RequireOffsetArray(TiffIfdGraph graph, TiffEntry entry, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        bool valid = entry.FieldType == TiffFieldType.Short || entry.FieldType == TiffFieldType.Long;
        if (graph.Header.BigTiff && entry.FieldType == TiffFieldType.Long8)
        {
            valid = true;
        }
        if (!valid)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF offset or byte-count array has an invalid field type.");
        }
        if (entry.Count == 0)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF offset or byte-count array must not be empty.");
        }
    }

    internal static void RequirePointerArray(TiffIfdGraph graph, TiffEntry entry, bool scalar, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        bool valid = entry.FieldType == TiffFieldType.Long || entry.FieldType == TiffFieldType.Ifd;
        if (graph.Header.BigTiff && entry.FieldType == TiffFieldType.Ifd8)
        {
            valid = true;
        }
        if (!valid)
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF IFD pointer has an invalid field type.");
        }
        if (entry.Count == 0 || (scalar && entry.Count != 1))
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, entry.EntryOffset, context, "The TIFF IFD pointer has an invalid value count.");
        }
    }

    internal static void RequireCodeRange(TiffIfd ifd, ushort tag, ulong minimum, ulong maximum, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        TiffEntry? entry = ifd.FindEntry(tag);
        ulong[] values = TiffImageData.GetUnsignedValues(entry);
        if (values.Length != 0 && (values[0] < minimum || values[0] > maximum))
        {
            Add(messages, RawDiagnosticCode.InvalidMetadata, RawSeverity.Error, (entry ?? throw new System.InvalidOperationException("A required value is unavailable.")).EntryOffset, context, "The TIFF tag value is outside the defined range.");
        }
    }

    internal static void RequireTag(TiffIfd ifd, ushort tag, string name, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (ifd.FindEntry(tag) == null)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, ifd.AbsoluteOffset, ifd.Path + "/" + name, "The TIFF image is missing a required tag.");
        }
    }

    internal static long CheckedLength(int count, int elementSize, long offset, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (count < 0 || elementSize < 0 || (count != 0 && elementSize > long.MaxValue / count))
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, offset, context, "The TIFF range length overflows.");
            return -1;
        }
        return (long)count * elementSize;
    }

    internal static long GetAbsoluteOffset(TiffIfdGraph graph, ulong relativeOffset, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (relativeOffset > long.MaxValue)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, -1, context, "The TIFF offset does not fit a signed file position.");
            return -1;
        }
        long signedOffset = (long)relativeOffset;
        if (graph.Header.BaseOffset < 0 || signedOffset > long.MaxValue - graph.Header.BaseOffset)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, graph.Header.BaseOffset, context, "The TIFF absolute offset overflows.");
            return -1;
        }
        return graph.Header.BaseOffset + signedOffset;
    }

    internal static long ToLength(ulong length, long offset, string context, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (length > long.MaxValue)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, offset, context, "The TIFF length does not fit a signed file position.");
            return -1;
        }
        return (long)length;
    }

    internal static long ContainerEnd(TiffIfdGraph graph, System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (graph.ContainerStart < 0 || graph.ContainerLength < 0 || graph.ContainerStart > long.MaxValue - graph.ContainerLength)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, RawSeverity.Error, graph.ContainerStart, "TIFF", "The bounded TIFF container range overflows.");
            return -1;
        }
        return graph.ContainerStart + graph.ContainerLength;
    }

    internal static long OffsetForMessage(TiffIfdGraph graph, ulong relativeOffset)
    {
        if (relativeOffset > long.MaxValue || graph.Header.BaseOffset < 0 || (long)relativeOffset > long.MaxValue - graph.Header.BaseOffset)
        {
            return -1;
        }
        return graph.Header.BaseOffset + (long)relativeOffset;
    }

    internal static long EntryOffset(TiffIfd ifd, ushort tag)
    {
        TiffEntry? entry = ifd.FindEntry(tag);
        if (entry == null)
        {
            return ifd.AbsoluteOffset;
        }
        return entry.EntryOffset;
    }

    internal static string TagContext(string path, ushort tag)
    {
        return path + "/Tag0x" + tag.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void Add(System.Collections.Generic.List<RawValidationMessage> messages, RawDiagnosticCode code, RawSeverity severity, long offset, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Structural, code, severity, RawFormat.Tiff, offset, context, message, false));
    }
}
