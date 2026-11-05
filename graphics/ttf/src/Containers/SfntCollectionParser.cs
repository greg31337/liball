namespace ttf;

internal static class SfntCollectionParser
{
    internal static SfntCollection Parse(FontData fontData, FontLoadOptions options)
    {
        if (fontData == null)
        {
            throw new System.ArgumentNullException("fontData");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();

        BigEndianReader reader = new BigEndianReader(
            fontData.GetSlice(0, fontData.GetLength()));
        uint signature = reader.ReadUInt32();

        if (signature != 0x74746366U)
        {
            throw FontFormatException.Create("The data does not begin with a TTC collection signature.");
        }

        uint version = reader.ReadUInt32();

        if (version != 0x00010000U && version != 0x00020000U)
        {
            throw FontFormatException.Create("The TTC header version is not supported.");
        }

        long declaredFaceCount = reader.ReadUInt32();

        if (declaredFaceCount == 0)
        {
            throw FontFormatException.Create("The TTC collection does not contain any faces.");
        }

        if (declaredFaceCount > options.GetLimits().GetMaxFaces())
        {
            throw FontLimitException.Create("The TTC face count exceeds the configured limit.");
        }

        int faceCount = (int)declaredFaceCount;
        long headerLength = 12L + faceCount * 4L;

        if (version == 0x00020000U)
        {
            headerLength += 12;
        }

        if (headerLength > fontData.GetLength())
        {
            throw FontFormatException.Create("The TTC header is truncated.");
        }

        long[] faceOffsets = new long[faceCount];
        int faceIndex = 0;

        while (faceIndex < faceCount)
        {
            long faceOffset = reader.ReadOffset32();
            ValidateFaceOffset(fontData, faceOffset, headerLength);

            int earlierFaceIndex = 0;

            while (earlierFaceIndex < faceIndex)
            {
                if (faceOffsets[earlierFaceIndex] == faceOffset)
                {
                    throw FontFormatException.Create("The TTC header contains duplicate face offsets.");
                }

                earlierFaceIndex += 1;
            }

            faceOffsets[faceIndex] = faceOffset;
            faceIndex += 1;
        }

        SfntCollectionDsig dsig = ParseDsig(fontData, reader, version);
        System.Collections.Generic.Dictionary<long, SfntCollectionTableRegion> regionsByOffset =
            new System.Collections.Generic.Dictionary<long, SfntCollectionTableRegion>();
        System.Collections.Generic.List<SfntCollectionTableRegion> regions =
            new System.Collections.Generic.List<SfntCollectionTableRegion>();
        SfntCollectionFace[] faces = new SfntCollectionFace[faceCount];
        faceIndex = 0;

        while (faceIndex < faceCount)
        {
            options.GetCancellationToken().ThrowIfCancellationRequested();
            int faceOffset = (int)faceOffsets[faceIndex];
            FontDataSlice faceData = fontData.GetSlice(
                faceOffset,
                fontData.GetLength() - faceOffset);
            SfntDirectory directory = SfntDirectoryParser.Parse(
                faceData,
                options.GetLimits());

            SfntCollectionTableRegion[] faceRegions = ParseFaceTableRegions(
                fontData,
                directory,
                regionsByOffset,
                regions,
                faceIndex);
            faces[faceIndex] = new SfntCollectionFace(
                faceOffsets[faceIndex],
                directory,
                faceRegions);
            faceIndex += 1;
        }

        SfntCollectionTableRegion[] orderedRegions = regions.ToArray();
        SortRegions(orderedRegions);
        ValidateCollectionLayout(headerLength, faces, orderedRegions, dsig);
        SfntCollectionTableRegion[] sharedRegions = GetSharedRegions(orderedRegions);
        return new SfntCollection(version, faces, orderedRegions, sharedRegions, dsig);
    }

    static void ValidateFaceOffset(FontData fontData, long faceOffset, long headerLength)
    {
        if (faceOffset < headerLength || faceOffset > fontData.GetLength() - 12L)
        {
            throw FontFormatException.Create("A TTC face offset is outside the collection data.");
        }

        if (faceOffset % 4 != 0)
        {
            throw FontFormatException.Create("A TTC face offset is not four-byte aligned.");
        }
    }

    static SfntCollectionDsig ParseDsig(
        FontData fontData,
        BigEndianReader reader,
        uint version)
    {
        if (version == 0x00010000U)
        {
            return new SfntCollectionDsig(
                false,
                0,
                fontData.GetSlice(0, 0));
        }

        uint tag = reader.ReadUInt32();
        long length = reader.ReadUInt32();
        long offset = reader.ReadOffset32();

        if (tag == 0 && length == 0 && offset == 0)
        {
            return new SfntCollectionDsig(
                false,
                0,
                fontData.GetSlice(0, 0));
        }

        if (tag != (uint)FontTags.Dsig ||
            length == 0 ||
            offset > fontData.GetLength() ||
            length > fontData.GetLength() - offset)
        {
            throw FontFormatException.Create("The TTC DSIG metadata is malformed or out of bounds.");
        }

        FontDataSlice dsigData = fontData.GetSlice((int)offset, (int)length);
        return new SfntCollectionDsig(true, tag, dsigData);
    }

    static SfntCollectionTableRegion[] ParseFaceTableRegions(
        FontData fontData,
        SfntDirectory directory,
        System.Collections.Generic.Dictionary<long, SfntCollectionTableRegion> regionsByOffset,
        System.Collections.Generic.List<SfntCollectionTableRegion> regions,
        int faceIndex)
    {
        SfntCollectionTableRegion[] faceRegions =
            new SfntCollectionTableRegion[directory.GetTableCount()];
        int tableIndex = 0;

        while (tableIndex < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(tableIndex);
            long offset = record.GetOffset();
            long length = record.GetLength();

            if (offset > fontData.GetLength() || length > fontData.GetLength() - offset)
            {
                throw FontFormatException.Create("A TTC table range extends outside the collection data.");
            }

            SfntCollectionTableRegion region;

            if (regionsByOffset.ContainsKey(offset))
            {
                region = regionsByOffset[offset];

                if (region.GetLength() != length)
                {
                    throw FontFormatException.Create(
                        "TTC table records use conflicting lengths for one shared offset.");
                }
            }
            else
            {
                region = new SfntCollectionTableRegion(fontData.GetSlice(
                    (int)offset,
                    (int)length));
                regionsByOffset.Add(offset, region);
                regions.Add(region);
            }

            region.AddFaceReference(faceIndex);
            faceRegions[tableIndex] = region;
            tableIndex += 1;
        }

        return faceRegions;
    }

    static void SortRegions(SfntCollectionTableRegion[] regions)
    {
        int index = 1;

        while (index < regions.Length)
        {
            SfntCollectionTableRegion current = regions[index];
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                regions[insertionIndex - 1].GetOffset() > current.GetOffset())
            {
                regions[insertionIndex] = regions[insertionIndex - 1];
                insertionIndex -= 1;
            }

            regions[insertionIndex] = current;
            index += 1;
        }
    }

    static void ValidateCollectionLayout(
        long headerLength,
        SfntCollectionFace[] faces,
        SfntCollectionTableRegion[] regions,
        SfntCollectionDsig dsig)
    {
        long[] directoryStarts = new long[faces.Length];
        long[] directoryEnds = new long[faces.Length];
        int faceIndex = 0;

        while (faceIndex < faces.Length)
        {
            SfntDirectory directory = faces[faceIndex].GetDirectory();
            directoryStarts[faceIndex] = directory.GetDirectoryOffset();
            directoryEnds[faceIndex] = directory.GetDirectoryOffset() +
                12L + directory.GetTableCount() * 16L;
            faceIndex += 1;
        }

        SortRanges(directoryStarts, directoryEnds);
        ValidateDirectoryRanges(directoryStarts, directoryEnds);
        ValidateTableRanges(
            headerLength,
            regions,
            directoryStarts,
            directoryEnds);
        ValidateDsigRange(
            headerLength,
            dsig,
            regions,
            directoryStarts,
            directoryEnds);
    }

    static void SortRanges(long[] starts, long[] ends)
    {
        int index = 1;

        while (index < starts.Length)
        {
            long currentStart = starts[index];
            long currentEnd = ends[index];
            int insertionIndex = index;

            while (insertionIndex > 0 && starts[insertionIndex - 1] > currentStart)
            {
                starts[insertionIndex] = starts[insertionIndex - 1];
                ends[insertionIndex] = ends[insertionIndex - 1];
                insertionIndex -= 1;
            }

            starts[insertionIndex] = currentStart;
            ends[insertionIndex] = currentEnd;
            index += 1;
        }
    }

    static void ValidateDirectoryRanges(long[] starts, long[] ends)
    {
        int index = 1;

        while (index < starts.Length)
        {
            if (starts[index] < ends[index - 1])
            {
                throw FontFormatException.Create("Two TTC face directories overlap.");
            }

            index += 1;
        }
    }

    static void ValidateTableRanges(
        long headerLength,
        SfntCollectionTableRegion[] regions,
        long[] directoryStarts,
        long[] directoryEnds)
    {
        long precedingEnd = 0;
        int directoryIndex = 0;
        int regionIndex = 0;

        while (regionIndex < regions.Length)
        {
            SfntCollectionTableRegion region = regions[regionIndex];
            long regionStart = region.GetOffset();
            long regionEnd = regionStart + region.GetLength();

            if (region.GetLength() > 0)
            {
                if (regionStart < headerLength)
                {
                    throw FontFormatException.Create("A TTC table range overlaps the collection header.");
                }

                if (regionStart < precedingEnd)
                {
                    throw FontFormatException.Create("Two distinct TTC table ranges overlap.");
                }

                while (directoryIndex < directoryStarts.Length &&
                    directoryEnds[directoryIndex] <= regionStart)
                {
                    directoryIndex += 1;
                }

                if (directoryIndex < directoryStarts.Length &&
                    directoryStarts[directoryIndex] < regionEnd)
                {
                    throw FontFormatException.Create("A TTC table range overlaps a face directory.");
                }

                precedingEnd = regionEnd;
            }

            regionIndex += 1;
        }
    }

    static void ValidateDsigRange(
        long headerLength,
        SfntCollectionDsig dsig,
        SfntCollectionTableRegion[] regions,
        long[] directoryStarts,
        long[] directoryEnds)
    {
        if (!dsig.IsPresent())
        {
            return;
        }

        long dsigStart = dsig.GetOffset();
        long dsigEnd = dsigStart + dsig.GetLength();

        if (dsigStart < headerLength)
        {
            throw FontFormatException.Create("The TTC DSIG data overlaps the collection header.");
        }

        int directoryIndex = 0;

        while (directoryIndex < directoryStarts.Length)
        {
            if (directoryStarts[directoryIndex] < dsigEnd &&
                dsigStart < directoryEnds[directoryIndex])
            {
                throw FontFormatException.Create("The TTC DSIG data overlaps a face directory.");
            }

            directoryIndex += 1;
        }

        int regionIndex = 0;

        while (regionIndex < regions.Length)
        {
            long regionStart = regions[regionIndex].GetOffset();
            long regionEnd = regionStart + regions[regionIndex].GetLength();

            if (regions[regionIndex].GetLength() > 0 &&
                regionStart < dsigEnd &&
                dsigStart < regionEnd)
            {
                throw FontFormatException.Create("The TTC DSIG data overlaps a table range.");
            }

            regionIndex += 1;
        }
    }

    static SfntCollectionTableRegion[] GetSharedRegions(
        SfntCollectionTableRegion[] regions)
    {
        int sharedCount = 0;
        int index = 0;

        while (index < regions.Length)
        {
            if (regions[index].GetFaceReferenceCount() > 1)
            {
                sharedCount += 1;
            }

            index += 1;
        }

        SfntCollectionTableRegion[] sharedRegions =
            new SfntCollectionTableRegion[sharedCount];
        int sharedIndex = 0;
        index = 0;

        while (index < regions.Length)
        {
            if (regions[index].GetFaceReferenceCount() > 1)
            {
                sharedRegions[sharedIndex] = regions[index];
                sharedIndex += 1;
            }

            index += 1;
        }

        return sharedRegions;
    }
}
