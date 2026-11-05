namespace ttf;

internal class SfntColrLayerRecord
{
    const int ForegroundPaletteEntryIndex = 0xFFFF;
    int glyphId;
    int paletteEntryIndex;

    internal SfntColrLayerRecord(int glyphId, int paletteEntryIndex)
    {
        this.glyphId = glyphId;
        this.paletteEntryIndex = paletteEntryIndex;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetPaletteEntryIndex()
    {
        return paletteEntryIndex;
    }

    internal bool UsesForegroundColor()
    {
        return paletteEntryIndex == ForegroundPaletteEntryIndex;
    }
}

internal class SfntColrBaseGlyphRecord
{
    int glyphId;
    int firstLayerIndex;
    int layerCount;
    SfntColrLayerRecord[] layerRecords;

    internal SfntColrBaseGlyphRecord(
        int glyphId,
        int firstLayerIndex,
        int layerCount,
        SfntColrLayerRecord[] layerRecords)
    {
        this.glyphId = glyphId;
        this.firstLayerIndex = firstLayerIndex;
        this.layerCount = layerCount;
        this.layerRecords = layerRecords;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetFirstLayerIndex()
    {
        return firstLayerIndex;
    }

    internal int GetLayerCount()
    {
        return layerCount;
    }

    internal SfntColrLayerRecord GetLayer(int index)
    {
        if (index < 0 || index >= layerCount)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return layerRecords[firstLayerIndex + index];
    }
}

internal class SfntColrResolvedLayer
{
    SfntColrLayerRecord record;
    SfntCpalColor color;

    internal SfntColrResolvedLayer(
        SfntColrLayerRecord record,
        SfntCpalColor color)
    {
        this.record = record;
        this.color = color;
    }

    internal int GetGlyphId()
    {
        return record.GetGlyphId();
    }

    internal int GetPaletteEntryIndex()
    {
        return record.GetPaletteEntryIndex();
    }

    internal bool UsesForegroundColor()
    {
        return record.UsesForegroundColor();
    }

    internal SfntCpalColor GetColor()
    {
        return color;
    }
}

internal class SfntColrLayerGlyphClosure
{
    int baseGlyphId;
    int paletteIndex;
    SfntColrResolvedLayer[] layers;
    int[] referencedGlyphIds;

    internal SfntColrLayerGlyphClosure(
        int baseGlyphId,
        int paletteIndex,
        SfntColrResolvedLayer[] layers,
        int[] referencedGlyphIds)
    {
        this.baseGlyphId = baseGlyphId;
        this.paletteIndex = paletteIndex;
        this.layers = layers;
        this.referencedGlyphIds = referencedGlyphIds;
    }

    internal int GetBaseGlyphId()
    {
        return baseGlyphId;
    }

    internal int GetPaletteIndex()
    {
        return paletteIndex;
    }

    internal int GetLayerCount()
    {
        return layers.Length;
    }

    internal SfntColrResolvedLayer GetLayer(int index)
    {
        if (index < 0 || index >= layers.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return layers[index];
    }

    internal int GetReferencedGlyphCount()
    {
        return referencedGlyphIds.Length;
    }

    internal int GetReferencedGlyphId(int index)
    {
        if (index < 0 || index >= referencedGlyphIds.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return referencedGlyphIds[index];
    }
}

internal class SfntColrTable
{
    int version;
    int glyphCount;
    SfntColrBaseGlyphRecord[] baseGlyphRecords;
    SfntColrLayerRecord[] layerRecords;
    SfntCpalTable cpal;
    SfntColrBaseGlyphPaintRecord[] baseGlyphPaintRecords;
    SfntColrLayerList? layerList;
    SfntColrClipList? clipList;
    SfntColrPaint[] paints;
    SfntColrVariationData variationData;

    internal static SfntColrTable Create(
        int version,
        int glyphCount,
        SfntColrBaseGlyphRecord[] baseGlyphRecords,
        SfntColrLayerRecord[] layerRecords,
        SfntCpalTable cpal)
    {
        return new SfntColrTable(
            version,
            glyphCount,
            baseGlyphRecords,
            layerRecords,
            cpal,
            new SfntColrBaseGlyphPaintRecord[0],
            null,
            null,
            new SfntColrPaint[0],
            new SfntColrVariationData(null, null));
    }

    internal SfntColrTable(
        int version,
        int glyphCount,
        SfntColrBaseGlyphRecord[] baseGlyphRecords,
        SfntColrLayerRecord[] layerRecords,
        SfntCpalTable cpal,
        SfntColrBaseGlyphPaintRecord[] baseGlyphPaintRecords,
        SfntColrLayerList? layerList,
        SfntColrClipList? clipList,
        SfntColrPaint[] paints,
        SfntColrVariationData variationData)
    {
        this.version = version;
        this.glyphCount = glyphCount;
        this.baseGlyphRecords = baseGlyphRecords;
        this.layerRecords = layerRecords;
        this.cpal = cpal;
        this.baseGlyphPaintRecords = baseGlyphPaintRecords;
        this.layerList = layerList;
        this.clipList = clipList;
        this.paints = paints;
        this.variationData = variationData;
    }

    internal static SfntColrTable Create(
        int version,
        int glyphCount,
        SfntColrBaseGlyphRecord[] baseGlyphRecords,
        SfntColrLayerRecord[] layerRecords,
        SfntCpalTable cpal,
        SfntColrBaseGlyphPaintRecord[] baseGlyphPaintRecords,
        SfntColrLayerList? layerList,
        SfntColrClipList? clipList,
        SfntColrPaint[] paints,
        SfntColrVariationData variationData)
    {
        return new SfntColrTable(
            version,
            glyphCount,
            baseGlyphRecords,
            layerRecords,
            cpal,
            baseGlyphPaintRecords,
            layerList,
            clipList,
            paints,
            variationData);
    }

    internal int GetVersion()
    {
        return version;
    }

    internal int GetBaseGlyphRecordCount()
    {
        return baseGlyphRecords.Length;
    }

    internal SfntColrBaseGlyphRecord GetBaseGlyphRecord(int index)
    {
        if (index < 0 || index >= baseGlyphRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return baseGlyphRecords[index];
    }

    internal int GetLayerRecordCount()
    {
        return layerRecords.Length;
    }

    internal SfntColrLayerRecord GetLayerRecord(int index)
    {
        if (index < 0 || index >= layerRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return layerRecords[index];
    }

    internal SfntColrBaseGlyphRecord? FindBaseGlyphRecord(int glyphId)
    {
        ValidateGlyphId(glyphId);
        int low = 0;
        int high = baseGlyphRecords.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntColrBaseGlyphRecord candidate = baseGlyphRecords[middle];

            if (candidate.GetGlyphId() == glyphId)
            {
                return candidate;
            }

            if (candidate.GetGlyphId() < glyphId)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    internal SfntColrLayerGlyphClosure? ResolveLayerGlyphClosure(
        int glyphId,
        int paletteIndex,
        SfntCpalColor foregroundColor)
    {
        SfntColrBaseGlyphRecord? baseGlyph = FindBaseGlyphRecord(glyphId);

        if (baseGlyph == null)
        {
            return null;
        }

        cpal.GetPalette(paletteIndex);
        SfntColrResolvedLayer[] resolvedLayers =
            new SfntColrResolvedLayer[baseGlyph.GetLayerCount()];
        bool[] referencedGlyphs = new bool[glyphCount];
        System.Collections.Generic.List<int> referencedGlyphIds =
            new System.Collections.Generic.List<int>();
        int index = 0;

        while (index < resolvedLayers.Length)
        {
            SfntColrLayerRecord layer = baseGlyph.GetLayer(index);
            int layerGlyphId = layer.GetGlyphId();
            SfntCpalColor color = cpal.ResolveColor(
                paletteIndex,
                layer.GetPaletteEntryIndex(),
                foregroundColor);
            resolvedLayers[index] = new SfntColrResolvedLayer(layer, color);

            if (!referencedGlyphs[layerGlyphId])
            {
                referencedGlyphs[layerGlyphId] = true;
                referencedGlyphIds.Add(layerGlyphId);
            }

            index += 1;
        }

        return new SfntColrLayerGlyphClosure(
            glyphId,
            paletteIndex,
            resolvedLayers,
            referencedGlyphIds.ToArray());
    }

    internal int GetBaseGlyphPaintRecordCount()
    {
        return baseGlyphPaintRecords.Length;
    }

    internal SfntColrBaseGlyphPaintRecord GetBaseGlyphPaintRecord(int index)
    {
        if (index < 0 || index >= baseGlyphPaintRecords.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return baseGlyphPaintRecords[index];
    }

    internal SfntColrBaseGlyphPaintRecord? FindBaseGlyphPaintRecord(int glyphId)
    {
        ValidateGlyphId(glyphId);
        int low = 0;
        int high = baseGlyphPaintRecords.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntColrBaseGlyphPaintRecord candidate =
                baseGlyphPaintRecords[middle];

            if (candidate.GetGlyphId() == glyphId)
            {
                return candidate;
            }

            if (candidate.GetGlyphId() < glyphId)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    internal SfntColrLayerList? GetLayerList()
    {
        return layerList;
    }

    internal SfntColrClipList? GetClipList()
    {
        return clipList;
    }

    internal int GetPaintCount()
    {
        return paints.Length;
    }

    internal SfntColrPaint GetPaint(int index)
    {
        if (index < 0 || index >= paints.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return paints[index];
    }

    internal SfntColrPaint? FindPaintByOffset(int offset)
    {
        int low = 0;
        int high = paints.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntColrPaint candidate = paints[middle];

            if (candidate.GetOffset() == offset)
            {
                return candidate;
            }

            if (candidate.GetOffset() < offset)
            {
                low = middle + 1;
            }
            else
            {
                high = middle - 1;
            }
        }

        return null;
    }

    internal bool HasVariationData()
    {
        return variationData.HasStore();
    }

    internal SfntDeltaSetIndexMap? GetVariationIndexMap()
    {
        return variationData.GetIndexMap();
    }

    internal SfntItemVariationStore? GetItemVariationStore()
    {
        return variationData.GetStore();
    }

    internal SfntVariationLocation CreateVariationLocation(
        double[] normalizedCoordinates)
    {
        return variationData.CreateLocation(normalizedCoordinates);
    }

    void ValidateGlyphId(int glyphId)
    {
        if (glyphId < 0 || glyphId >= glyphCount)
        {
            throw new System.ArgumentOutOfRangeException("glyphId");
        }
    }
}

internal static class SfntColrTableParser
{
    const int HeaderLength = 14;
    const int BaseGlyphRecordLength = 6;
    const int LayerRecordLength = 4;
    const int ForegroundPaletteEntryIndex = 0xFFFF;
    static readonly SfntTableParser parser = new SfntTableParser(
        FontTags.Colr,
        0,
        1,
        0,
        ReadVersion,
        Parse);

    internal static SfntTableParser GetParser()
    {
        return parser;
    }

    internal static SfntParsedTableView? FindView(SfntTableCache cache)
    {
        if (cache == null)
        {
            throw new System.ArgumentNullException("cache");
        }

        return cache.FindParsedTable(parser);
    }

    internal static SfntColrTable? FindTable(SfntTableCache cache)
    {
        SfntParsedTableView? view = FindView(cache);

        if (view == null || !view.HasParsedData())
        {
            return null;
        }

        return (SfntColrTable)view.GetParsedData();
    }

    static SfntTableVersion ReadVersion(SfntTableData table)
    {
        BigEndianReader reader = new BigEndianReader(table.GetDataSlice());
        return new SfntTableVersion(reader.ReadUInt16(), 0);
    }

    static object Parse(
        SfntTableData table,
        SfntTableVersion version,
        SfntTableVersionSupport support,
        FontLimits limits,
        SfntTableCache cache)
    {
        FontDataSlice tableData = table.GetDataSlice();

        if (tableData.GetLength() < HeaderLength)
        {
            throw FontFormatException.Create("The COLR version 0 header is truncated.");
        }

        BigEndianReader reader = new BigEndianReader(tableData);
        int encodedVersion = reader.ReadUInt16();
        int baseGlyphRecordCount = reader.ReadUInt16();
        long baseGlyphRecordsOffset = reader.ReadOffset32();
        long layerRecordsOffset = reader.ReadOffset32();
        int layerRecordCount = reader.ReadUInt16();

        if (encodedVersion != version.GetMajorVersion())
        {
            throw FontFormatException.Create("The COLR table version is inconsistent.");
        }

        if (encodedVersion == 1)
        {
            return SfntColrV1Parser.Parse(
                tableData,
                reader,
                baseGlyphRecordCount,
                baseGlyphRecordsOffset,
                layerRecordsOffset,
                layerRecordCount,
                limits,
                cache);
        }

        ValidateCounts(baseGlyphRecordCount, layerRecordCount, limits);
        ValidateArrayOffset(
            tableData,
            baseGlyphRecordsOffset,
            baseGlyphRecordCount,
            BaseGlyphRecordLength,
            "The COLR base-glyph-records array offset is invalid.");
        ValidateArrayOffset(
            tableData,
            layerRecordsOffset,
            layerRecordCount,
            LayerRecordLength,
            "The COLR layer-records array offset is invalid.");

        SfntMaxpTable? maxp = SfntMaxpTableParser.FindTable(cache);

        if (maxp == null)
        {
            throw FontFormatException.Create(
                "The COLR table requires a supported maxp table.");
        }

        SfntCpalTable? cpal = SfntCpalTableParser.FindTable(cache);

        if (cpal == null)
        {
            throw FontFormatException.Create(
                "The COLR table requires a supported CPAL table.");
        }

        int glyphCount = maxp.GetGlyphCount();
        SfntColrLayerRecord[] layerRecords = ParseLayerRecords(
            tableData,
            layerRecordsOffset,
            layerRecordCount,
            glyphCount,
            cpal.GetPaletteEntryCount());
        SfntColrBaseGlyphRecord[] baseGlyphRecords = ParseBaseGlyphRecords(
            tableData,
            baseGlyphRecordsOffset,
            baseGlyphRecordCount,
            glyphCount,
            layerRecords);
        return SfntColrTable.Create(
            encodedVersion,
            glyphCount,
            baseGlyphRecords,
            layerRecords,
            cpal);
    }

    static void ValidateCounts(
        int baseGlyphRecordCount,
        int layerRecordCount,
        FontLimits limits)
    {
        if (baseGlyphRecordCount == 0 || layerRecordCount == 0)
        {
            throw FontFormatException.Create(
                "A COLR version 0 table must contain base-glyph and layer records.");
        }

        int limit = limits.GetMaxCoverageEntries();

        if (baseGlyphRecordCount > limit || layerRecordCount > limit)
        {
            throw FontLimitException.Create(
                "A COLR record count exceeds the configured coverage-entry limit.");
        }
    }

    internal static SfntColrLayerRecord[] ParseLayerRecords(
        FontDataSlice tableData,
        long offset,
        int count,
        int glyphCount,
        int paletteEntryCount)
    {
        int length = CheckedFontMath.MultiplyCount(count, LayerRecordLength);
        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice((int)offset, length));
        SfntColrLayerRecord[] records = new SfntColrLayerRecord[count];
        int index = 0;

        while (index < records.Length)
        {
            int glyphId = reader.ReadUInt16();
            int paletteEntryIndex = reader.ReadUInt16();

            if (glyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A COLR layer record references a missing glyph.");
            }

            if (paletteEntryIndex != ForegroundPaletteEntryIndex &&
                paletteEntryIndex >= paletteEntryCount)
            {
                throw FontFormatException.Create(
                    "A COLR layer record references a missing CPAL entry.");
            }

            records[index] = new SfntColrLayerRecord(
                glyphId,
                paletteEntryIndex);
            index += 1;
        }

        return records;
    }

    internal static SfntColrBaseGlyphRecord[] ParseBaseGlyphRecords(
        FontDataSlice tableData,
        long offset,
        int count,
        int glyphCount,
        SfntColrLayerRecord[] layerRecords)
    {
        int length = CheckedFontMath.MultiplyCount(count, BaseGlyphRecordLength);
        BigEndianReader reader = new BigEndianReader(
            tableData.GetSlice((int)offset, length));
        SfntColrBaseGlyphRecord[] records =
            new SfntColrBaseGlyphRecord[count];
        int previousGlyphId = -1;
        int index = 0;

        while (index < records.Length)
        {
            int glyphId = reader.ReadUInt16();
            int firstLayerIndex = reader.ReadUInt16();
            int layerCount = reader.ReadUInt16();
            long endLayerIndex = (long)firstLayerIndex + layerCount;

            if (glyphId >= glyphCount)
            {
                throw FontFormatException.Create(
                    "A COLR base glyph record references a missing glyph.");
            }

            if (glyphId <= previousGlyphId)
            {
                throw FontFormatException.Create(
                    "The COLR base glyph records are not strictly sorted.");
            }

            if (endLayerIndex > layerRecords.Length)
            {
                throw FontFormatException.Create(
                    "A COLR base glyph record references missing layers.");
            }

            records[index] = new SfntColrBaseGlyphRecord(
                glyphId,
                firstLayerIndex,
                layerCount,
                layerRecords);
            previousGlyphId = glyphId;
            index += 1;
        }

        return records;
    }

    static void ValidateArrayOffset(
        FontDataSlice tableData,
        long offset,
        int count,
        int recordLength,
        string message)
    {
        long length = (long)count * recordLength;

        if (offset < HeaderLength || offset > int.MaxValue ||
            length > int.MaxValue || offset > tableData.GetLength() - length)
        {
            throw FontFormatException.Create(message);
        }
    }
}
