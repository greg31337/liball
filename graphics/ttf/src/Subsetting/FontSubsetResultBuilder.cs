namespace ttf;

internal class FontSubsetMetricData
{
    byte[] header;
    byte[] metrics;

    internal FontSubsetMetricData(byte[] header, byte[] metrics)
    {
        this.header = CopyBytes(header);
        this.metrics = CopyBytes(metrics);
    }

    internal byte[] CopyHeader() { return CopyBytes(header); }
    internal byte[] CopyMetrics() { return CopyBytes(metrics); }

    static byte[] CopyBytes(byte[] values)
    {
        byte[] copy = new byte[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal static class FontSubsetResultBuilder
{
    internal static byte[] CreatePdfOpenType(
        FontSubset subset,
        byte[]? cffData)
    {
        if (subset == null)
        {
            throw new System.ArgumentNullException("subset");
        }

        FontInstance instance = subset.GetInstance();
        FontFace face = instance.GetFace();
        FontSubsetNameData nameData = subset.CreateNameData();
        System.Collections.Generic.List<SfntWriteTable> tables =
            new System.Collections.Generic.List<SfntWriteTable>();
        FontOutlineKind kind = face.GetOutlineKind();

        if (kind == FontOutlineKind.TrueType)
        {
            if (cffData != null)
            {
                throw new System.ArgumentException(
                    "TrueType OpenType output cannot contain CFF override data.",
                    "cffData");
            }

            AddTrueTypeTables(subset, tables);
        }
        else if (kind == FontOutlineKind.Cff1 ||
            kind == FontOutlineKind.Cff2)
        {
            if (cffData == null)
            {
                throw new System.ArgumentNullException("cffData");
            }

            AddCffTables(subset, tables, cffData);
        }
        else
        {
            throw FontFormatException.Create(
                "OpenType PDF output requires TrueType or CFF outlines.");
        }

        AddCommonTables(subset, nameData, tables);
        AddLayoutTables(subset, tables);
        AddAuxiliaryTables(subset, tables);
        FontSubsetUnknownTables unknown = subset.CreateUnknownTables();
        AddUnknownTables(unknown, tables);
        return SfntWriter.Write(
            GetOutputScalerType(face),
            tables.ToArray(),
            face.GetTableCache().GetLimits());
    }

    internal static FontSubsetResult Create(FontSubset subset)
    {
        if (subset == null)
        {
            throw new System.ArgumentNullException("subset");
        }

        FontInstance instance = subset.GetInstance();
        FontFace face = instance.GetFace();
        FontSubsetOptions options = subset.CopyOptions();
        int[] oldToNewGlyphIds = subset.GetOldToNewGlyphMap();
        int[] newToOldGlyphIds = subset.GetNewToOldGlyphMap();
        FontSubsetNameData nameData = subset.CreateNameData();
        FontSubsetUnicodeMap[] unicodeMappings = CreateUnicodeMappings(
            subset,
            oldToNewGlyphIds);
        byte[] bytes;
        int[] retainedTags;
        FontDiagnostic[] outputDiagnostics = new FontDiagnostic[0];

        if (options.GetEmbeddingMode() == FontSubsetEmbeddingMode.FullFont)
        {
            bytes = subset.CreateFullFontData();
            retainedTags = ReadTableTags(bytes);
        }
        else
        {
            System.Collections.Generic.List<SfntWriteTable> tables =
                new System.Collections.Generic.List<SfntWriteTable>();
            AddOutlineTables(subset, tables);
            AddCommonTables(subset, nameData, tables);
            AddLayoutTables(subset, tables);
            AddAuxiliaryTables(subset, tables);
            FontSubsetUnknownTables unknown = subset.CreateUnknownTables();
            AddUnknownTables(unknown, tables);
            outputDiagnostics = CopyDiagnostics(unknown);
            SfntWriteTable[] outputTables = tables.ToArray();
            bytes = SfntWriter.Write(
                GetOutputScalerType(face),
                outputTables,
                face.GetTableCache().GetLimits());
            retainedTags = CopyAndSortTags(outputTables);
        }

        int[] droppedTags = FindDroppedTags(face, retainedTags);
        FontSubsetTableReport tableReport = CreateTableReport(
            face,
            bytes,
            retainedTags,
            droppedTags);
        FontOutlineKind sourceOutlineKind = face.GetOutlineKind();
        FontOutlineKind outputOutlineKind =
            sourceOutlineKind == FontOutlineKind.Cff2 ?
            FontOutlineKind.Cff1 : sourceOutlineKind;
        bool hadVariationData = HasVariationData(face);
        bool variationFrozen = hadVariationData &&
            !ContainsVariationData(retainedTags);
        FontSubsetPrefixReport prefixReport = new FontSubsetPrefixReport(
            instance,
            options,
            oldToNewGlyphIds,
            nameData.GetPrefix(),
            nameData.GetSourcePostScriptName());
        FontDiagnostic[] repairs = CollectRepairs(face);
        FontDiagnostic[] diagnostics = MergeDiagnostics(
            subset.GetDiagnostics(),
            outputDiagnostics,
            repairs,
            face.GetTableCache().GetLimits());
        return new FontSubsetResult(
            bytes,
            nameData.GetPostScriptName(),
            oldToNewGlyphIds,
            newToOldGlyphIds,
            unicodeMappings,
            tableReport,
            repairs,
            diagnostics,
            subset.GetEmbeddingDecision(),
            subset.GetClosureReport(),
            instance,
            options,
            outputOutlineKind,
            hadVariationData,
            variationFrozen,
            prefixReport);
    }

    static void AddOutlineTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        FontOutlineKind kind = subset.GetInstance().GetFace().GetOutlineKind();

        if (kind == FontOutlineKind.TrueType)
        {
            AddTrueTypeTables(subset, tables);
            return;
        }

        if (kind == FontOutlineKind.Cff1 || kind == FontOutlineKind.Cff2)
        {
            AddCffTables(subset, tables);
            return;
        }

        throw FontFormatException.Create(
            "Subset output requires supported TrueType or CFF outlines.");
    }

    static void AddTrueTypeTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        SfntSubsetTrueTypeData data = subset.CreateTrueTypeTables();
        AddTable(tables, FontTags.Head, data.CopyHead());
        AddTable(tables, FontTags.Maxp, data.CopyMaxp());
        AddTable(tables, FontTags.Hhea, data.CopyHhea());
        AddTable(tables, FontTags.Hmtx, data.CopyHmtx());
        AddTable(tables, FontTags.Loca, data.CopyLoca());
        AddTable(tables, FontTags.Glyf, data.CopyGlyf());

        if (data.HasVerticalMetrics())
        {
            AddTable(tables, FontTags.Vhea, data.CopyVhea());
            AddTable(tables, FontTags.Vmtx, data.CopyVmtx());
        }

        if (data.HasFpgm())
        {
            AddTable(tables, FontTags.Fpgm, data.CopyFpgm());
        }

        if (data.HasPrep())
        {
            AddTable(tables, FontTags.Prep, data.CopyPrep());
        }

        if (data.HasCvt())
        {
            AddTable(tables, FontTags.Cvt, data.CopyCvt());
        }

        FontSubsetOptions options = subset.CopyOptions();
        FontFace face = subset.GetInstance().GetFace();

        if (options.GetHintPolicy() == FontSubsetHintPolicy.Retain &&
            face.HasTable(FontTags.Gasp))
        {
            AddRawTable(face, FontTags.Gasp, tables);
        }
    }

    static void AddCffTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        AddCffTables(subset, tables, null);
    }

    static void AddCffTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables,
        byte[]? cffOverride)
    {
        FontInstance instance = subset.GetInstance();
        FontFace face = instance.GetFace();
        int[] newToOldGlyphIds = subset.GetNewToOldGlyphMap();
        int[] bounds = CalculateBounds(instance, newToOldGlyphIds);
        SfntStaticInstanceMetadata metadata =
            new SfntStaticInstanceMetadata(instance, bounds);
        SfntTableData? headTable = face.GetTableStore().FindTable(
            FontTags.Head);

        if (headTable == null || headTable.GetLength() < 54)
        {
            throw FontFormatException.Create(
                "CFF subset output requires a complete head table.");
        }

        byte[] head = metadata.PatchTable(
            FontTags.Head,
            headTable.GetDataSlice().CopyBytes());
        FontSubsetMetricData horizontal = CreateMetricData(
            instance,
            newToOldGlyphIds,
            false,
            metadata);
        byte[] cff = cffOverride == null ?
            subset.CreateCffData().CopyData() : cffOverride;
        AddTable(tables, FontTags.Cff, cff);
        AddTable(tables, FontTags.Head, head);
        AddTable(tables, FontTags.Maxp, CreateCffMaxp(
            newToOldGlyphIds.Length));
        AddTable(tables, FontTags.Hhea, horizontal.CopyHeader());
        AddTable(tables, FontTags.Hmtx, horizontal.CopyMetrics());
        FontSubsetOptions options = subset.CopyOptions();

        if (options.GetVerticalPolicy() == FontSubsetVerticalPolicy.Retain &&
            face.HasTable(FontTags.Vhea) && face.HasTable(FontTags.Vmtx))
        {
            FontSubsetMetricData vertical = CreateMetricData(
                instance,
                newToOldGlyphIds,
                true,
                metadata);
            AddTable(tables, FontTags.Vhea, vertical.CopyHeader());
            AddTable(tables, FontTags.Vmtx, vertical.CopyMetrics());

            if (face.HasTable(FontTags.Vorg))
            {
                AddTable(tables, FontTags.Vorg, CreateVorg(
                    instance,
                    newToOldGlyphIds));
            }
        }
    }

    static void AddCommonTables(
        FontSubset subset,
        FontSubsetNameData nameData,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        AddTable(tables, FontTags.Cmap, subset.CreateCmapData());
        AddTable(tables, FontTags.Name, nameData.CopyTableData());
        byte[]? os2 = subset.CreateOs2Data();
        byte[]? post = subset.CreatePostData();

        if (os2 != null) { AddTable(tables, FontTags.Os2, os2); }
        if (post != null) { AddTable(tables, FontTags.Post, post); }
    }

    static void AddLayoutTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        FontSubsetLayoutData layout = subset.CreateLayoutData();

        if (layout.HasGsub())
        {
            AddTable(tables, FontTags.Gsub, layout.CopyGsub());
        }

        if (layout.HasGpos())
        {
            AddTable(tables, FontTags.Gpos, layout.CopyGpos());
        }

        if (layout.HasGdef())
        {
            AddTable(tables, FontTags.Gdef, layout.CopyGdef());
        }
    }

    static void AddAuxiliaryTables(
        FontSubset subset,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        FontSubsetAuxiliaryTables auxiliary = subset.CreateAuxiliaryTables();
        int index = 0;

        while (index < auxiliary.GetTableCount())
        {
            FontSubsetAuxiliaryTable table = auxiliary.GetTable(index);
            AddTable(tables, table.GetTag(), table.CopyData());
            index++;
        }
    }

    static void AddUnknownTables(
        FontSubsetUnknownTables unknown,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        int index = 0;

        while (index < unknown.GetTableCount())
        {
            FontSubsetAuxiliaryTable table = unknown.GetTable(index);
            AddTable(tables, table.GetTag(), table.CopyData());
            index++;
        }
    }

    static void AddRawTable(
        FontFace face,
        int tag,
        System.Collections.Generic.List<SfntWriteTable> tables)
    {
        SfntTableData? table = face.GetTableStore().FindTable(tag);

        if (table != null)
        {
            AddTable(tables, tag, table.GetDataSlice().CopyBytes());
        }
    }

    static void AddTable(
        System.Collections.Generic.List<SfntWriteTable> tables,
        int tag,
        byte[]? data)
    {
        int index = 0;

        while (index < tables.Count)
        {
            if (tables[index].GetTag() == tag)
            {
                throw FontFormatException.Create(
                    "Subset output attempted to emit a duplicate table tag.");
            }

            index++;
        }

        tables.Add(new SfntWriteTable(tag, data));
    }

    static FontSubsetMetricData CreateMetricData(
        FontInstance instance,
        int[] newToOldGlyphIds,
        bool vertical,
        SfntStaticInstanceMetadata metadata)
    {
        int glyphCount = newToOldGlyphIds.Length;
        int[] advances = new int[glyphCount];
        int[] bearings = new int[glyphCount];
        int maximumAdvance = 0;
        int minimumLeadingBearing = short.MaxValue;
        int minimumTrailingBearing = short.MaxValue;
        int maximumExtent = short.MinValue;
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            int oldGlyphId = newToOldGlyphIds[glyphId];
            int extent = 0;

            if (oldGlyphId >= 0)
            {
                GlyphMetrics metrics = instance.GetGlyphMetrics(oldGlyphId);
                advances[glyphId] = vertical ?
                    metrics.GetAdvanceHeight() : metrics.GetAdvanceWidth();
                bearings[glyphId] = vertical ?
                    metrics.GetTopSideBearing() :
                    metrics.GetLeftSideBearing();
                GlyphBounds? bounds = instance.GetGlyphOutline(oldGlyphId)
                    .GetBounds();

                System.ArgumentNullException.ThrowIfNull(bounds);
                if (!bounds.IsEmpty())
                {
                    extent = vertical ?
                        RoundMaximum(bounds.GetYMaximum()) -
                            RoundMinimum(bounds.GetYMinimum()) :
                        RoundMaximum(bounds.GetXMaximum()) -
                            RoundMinimum(bounds.GetXMinimum());
                }
            }

            ValidateMetric(advances[glyphId], bearings[glyphId]);
            int trailingBearing = advances[glyphId] -
                bearings[glyphId] - extent;
            int metricExtent = bearings[glyphId] + extent;
            ValidateSignedMetric(trailingBearing);
            ValidateSignedMetric(metricExtent);
            maximumAdvance = System.Math.Max(
                maximumAdvance,
                advances[glyphId]);
            minimumLeadingBearing = System.Math.Min(
                minimumLeadingBearing,
                bearings[glyphId]);
            minimumTrailingBearing = System.Math.Min(
                minimumTrailingBearing,
                trailingBearing);
            maximumExtent = System.Math.Max(maximumExtent, metricExtent);
            glyphId++;
        }

        int metricCount = glyphCount;

        while (metricCount > 1 &&
            advances[metricCount - 1] == advances[metricCount - 2])
        {
            metricCount--;
        }

        FontLimits limits = instance.GetFace().GetTableCache().GetLimits();
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        glyphId = 0;

        while (glyphId < metricCount)
        {
            writer.WriteUInt16((ushort)advances[glyphId]);
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId++;
        }

        while (glyphId < glyphCount)
        {
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId++;
        }

        int tag = vertical ? FontTags.Vhea : FontTags.Hhea;
        SfntTableData? source = instance.GetFace().GetTableStore()
            .FindTable(tag);

        if (source == null || source.GetLength() < 36)
        {
            throw FontFormatException.Create(
                "CFF subset output requires a complete metric header.");
        }

        byte[] header = source.GetDataSlice().CopyBytes();
        WriteUInt16(header, 10, maximumAdvance);
        WriteInt16(header, 12, minimumLeadingBearing);
        WriteInt16(header, 14, minimumTrailingBearing);
        WriteInt16(header, 16, maximumExtent);
        WriteUInt16(header, 34, metricCount);
        header = metadata.PatchTable(tag, header);
        return new FontSubsetMetricData(header, writer.CopyBytes());
    }

    static byte[] CreateCffMaxp(int glyphCount)
    {
        byte[] data = new byte[6];
        WriteUInt32(data, 0, 0x00005000U);
        WriteUInt16(data, 4, glyphCount);
        return data;
    }

    static byte[] CreateVorg(
        FontInstance instance,
        int[] newToOldGlyphIds)
    {
        int defaultOrigin = GetVerticalOrigin(instance, newToOldGlyphIds[0]);
        ValidateSignedMetric(defaultOrigin);
        int overrideCount = 0;
        int glyphId = 1;

        while (glyphId < newToOldGlyphIds.Length)
        {
            int origin = GetVerticalOrigin(
                instance,
                newToOldGlyphIds[glyphId]);
            ValidateSignedMetric(origin);

            if (origin != defaultOrigin) { overrideCount++; }
            glyphId++;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            instance.GetFace().GetTableCache().GetLimits()
                .GetMaxOutputBytes());
        writer.WriteUInt16(1);
        writer.WriteUInt16(0);
        writer.WriteInt16((short)defaultOrigin);
        writer.WriteUInt16((ushort)overrideCount);
        glyphId = 1;

        while (glyphId < newToOldGlyphIds.Length)
        {
            int origin = GetVerticalOrigin(
                instance,
                newToOldGlyphIds[glyphId]);

            if (origin != defaultOrigin)
            {
                writer.WriteUInt16((ushort)glyphId);
                writer.WriteInt16((short)origin);
            }

            glyphId++;
        }

        return writer.CopyBytes();
    }

    static int GetVerticalOrigin(FontInstance instance, int oldGlyphId)
    {
        return oldGlyphId < 0 ? 0 : instance.GetGlyphMetrics(oldGlyphId)
            .GetVerticalOriginY();
    }

    static int[] CalculateBounds(
        FontInstance instance,
        int[] newToOldGlyphIds)
    {
        int[] result = new int[] { 0, 0, 0, 0 };
        bool hasBounds = false;
        int index = 0;

        while (index < newToOldGlyphIds.Length)
        {
            int oldGlyphId = newToOldGlyphIds[index];

            if (oldGlyphId >= 0)
            {
                GlyphBounds? bounds = instance.GetGlyphOutline(oldGlyphId)
                    .GetBounds();

                System.ArgumentNullException.ThrowIfNull(bounds);
                if (!bounds.IsEmpty())
                {
                    int xMinimum = RoundMinimum(bounds.GetXMinimum());
                    int yMinimum = RoundMinimum(bounds.GetYMinimum());
                    int xMaximum = RoundMaximum(bounds.GetXMaximum());
                    int yMaximum = RoundMaximum(bounds.GetYMaximum());

                    if (!hasBounds)
                    {
                        result[0] = xMinimum;
                        result[1] = yMinimum;
                        result[2] = xMaximum;
                        result[3] = yMaximum;
                        hasBounds = true;
                    }
                    else
                    {
                        result[0] = System.Math.Min(result[0], xMinimum);
                        result[1] = System.Math.Min(result[1], yMinimum);
                        result[2] = System.Math.Max(result[2], xMaximum);
                        result[3] = System.Math.Max(result[3], yMaximum);
                    }
                }
            }

            index++;
        }

        return result;
    }

    static FontSubsetUnicodeMap[] CreateUnicodeMappings(
        FontSubset subset,
        int[] oldToNewGlyphIds)
    {
        FontSubsetUnicodeMap[] mappings =
            new FontSubsetUnicodeMap[subset.GetUnicodeMappingCount()];
        int index = 0;

        while (index < mappings.Length)
        {
            FontSubsetUnicodeMapping mapping =
                subset.GetUnicodeMapping(index);
            int originalGlyphId = mapping.GetGlyphId();
            mappings[index] = new FontSubsetUnicodeMap(
                originalGlyphId,
                oldToNewGlyphIds[originalGlyphId],
                mapping.CopyUnicodeSequence());
            index++;
        }

        return mappings;
    }

    static FontDiagnostic[] CollectRepairs(FontFace face)
    {
        System.Collections.Generic.List<FontDiagnostic> repairs =
            new System.Collections.Generic.List<FontDiagnostic>();
        int index = 0;

        while (index < face.GetValidationDiagnosticCount())
        {
            AddRepair(face.GetValidationDiagnostic(index), repairs);
            index += 1;
        }

        index = 0;

        while (index < face.GetTableDiagnosticCount())
        {
            AddRepair(face.GetTableDiagnostic(index), repairs);
            index += 1;
        }

        return repairs.ToArray();
    }

    static void AddRepair(
        FontDiagnostic diagnostic,
        System.Collections.Generic.List<FontDiagnostic> repairs)
    {
        if (diagnostic.GetCode() == FontDiagnosticCode.RepairApplied)
        {
            repairs.Add(diagnostic);
        }
    }

    static FontDiagnostic[] CopyDiagnostics(FontSubsetUnknownTables unknown)
    {
        FontDiagnostic[] diagnostics =
            new FontDiagnostic[unknown.GetDiagnosticCount()];
        int index = 0;

        while (index < diagnostics.Length)
        {
            diagnostics[index] = unknown.GetDiagnostic(index);
            index++;
        }

        return diagnostics;
    }

    static FontDiagnostic[] MergeDiagnostics(
        FontDiagnostic[] subsetDiagnostics,
        FontDiagnostic[] outputDiagnostics,
        FontDiagnostic[] repairs,
        FontLimits limits)
    {
        long count = (long)subsetDiagnostics.Length +
            outputDiagnostics.Length + repairs.Length;

        if (count > limits.GetMaxDiagnostics())
        {
            throw FontLimitException.Create(
                "The configured diagnostic count limit was exceeded.");
        }

        FontDiagnostic[] merged = new FontDiagnostic[(int)count];
        int offset = 0;
        System.Array.Copy(
            subsetDiagnostics,
            0,
            merged,
            offset,
            subsetDiagnostics.Length);
        offset += subsetDiagnostics.Length;
        System.Array.Copy(
            outputDiagnostics,
            0,
            merged,
            offset,
            outputDiagnostics.Length);
        offset += outputDiagnostics.Length;
        System.Array.Copy(repairs, 0, merged, offset, repairs.Length);
        return merged;
    }

    static int[] ReadTableTags(byte[] bytes)
    {
        FontData data = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            data.GetSlice(0, data.GetLength()));
        int[] tags = new int[directory.GetTableCount()];
        int index = 0;

        while (index < tags.Length)
        {
            tags[index] = directory.GetTableRecord(index).GetTag();
            index++;
        }

        SortTags(tags);
        return tags;
    }

    static int[] CopyAndSortTags(SfntWriteTable[] tables)
    {
        int[] tags = new int[tables.Length];
        int index = 0;

        while (index < tables.Length)
        {
            tags[index] = tables[index].GetTag();
            index++;
        }

        SortTags(tags);
        return tags;
    }

    static int[] FindDroppedTags(FontFace face, int[] retainedTags)
    {
        System.Collections.Generic.List<int> dropped =
            new System.Collections.Generic.List<int>();
        SfntTableStore source = face.GetTableStore();
        int index = 0;

        while (index < source.GetTableCount())
        {
            int tag = source.GetTable(index).GetTag();

            if (!ContainsTag(retainedTags, tag)) { dropped.Add(tag); }
            index++;
        }

        int[] tags = dropped.ToArray();
        SortTags(tags);
        return tags;
    }

    static FontSubsetTableReport CreateTableReport(
        FontFace face,
        byte[] bytes,
        int[] retainedTags,
        int[] droppedTags)
    {
        FontData outputData = FontData.Create(bytes);
        SfntDirectory directory = SfntDirectoryParser.Parse(
            outputData.GetSlice(0, outputData.GetLength()));
        System.Collections.Generic.List<int> rewritten =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> preserved =
            new System.Collections.Generic.List<int>();
        int index = 0;

        while (index < retainedTags.Length)
        {
            int tag = retainedTags[index];
            SfntTableData? source = face.GetTableStore().FindTable(tag);
            SfntTableRecord? output = FindTableRecord(directory, tag);

            if (source != null && output != null &&
                HaveSameTableData(source, outputData, output))
            {
                preserved.Add(tag);
            }
            else
            {
                rewritten.Add(tag);
            }

            index += 1;
        }

        return new FontSubsetTableReport(
            retainedTags,
            droppedTags,
            rewritten.ToArray(),
            preserved.ToArray());
    }

    static SfntTableRecord? FindTableRecord(
        SfntDirectory directory,
        int tag)
    {
        int index = 0;

        while (index < directory.GetTableCount())
        {
            SfntTableRecord record = directory.GetTableRecord(index);

            if (record.GetTag() == tag)
            {
                return record;
            }

            index += 1;
        }

        return null;
    }

    static bool HaveSameTableData(
        SfntTableData source,
        FontData outputData,
        SfntTableRecord output)
    {
        if (source.GetLength() != output.GetLength())
        {
            return false;
        }

        byte[] sourceBytes = source.GetDataSlice().CopyBytes();
        byte[] outputBytes = outputData.GetSlice(
            (int)output.GetOffset(),
            (int)output.GetLength()).CopyBytes();
        int index = 0;

        while (index < sourceBytes.Length)
        {
            if (sourceBytes[index] != outputBytes[index])
            {
                return false;
            }
            index += 1;
        }

        return true;
    }

    static bool HasVariationData(FontFace face)
    {
        return face.GetOutlineKind() == FontOutlineKind.Cff2 ||
            face.HasTable(FontTags.Fvar) ||
            face.HasTable(FontTags.Avar) ||
            face.HasTable(FontTags.Gvar) ||
            face.HasTable(FontTags.Cvar) ||
            face.HasTable(FontTags.Hvar) ||
            face.HasTable(FontTags.Vvar) ||
            face.HasTable(FontTags.Mvar);
    }

    static bool ContainsVariationData(int[] tags)
    {
        return ContainsTag(tags, FontTags.Cff2) ||
            ContainsTag(tags, FontTags.Fvar) ||
            ContainsTag(tags, FontTags.Avar) ||
            ContainsTag(tags, FontTags.Gvar) ||
            ContainsTag(tags, FontTags.Cvar) ||
            ContainsTag(tags, FontTags.Hvar) ||
            ContainsTag(tags, FontTags.Vvar) ||
            ContainsTag(tags, FontTags.Mvar);
    }

    static bool ContainsTag(int[] tags, int tag)
    {
        int low = 0;
        int high = tags.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            uint candidate = CheckedFontMath.ReinterpretInt32AsUInt32(
                tags[middle]);
            uint target = CheckedFontMath.ReinterpretInt32AsUInt32(tag);

            if (candidate == target) { return true; }
            if (candidate < target) { low = middle + 1; }
            else { high = middle - 1; }
        }

        return false;
    }

    static void SortTags(int[] tags)
    {
        int index = 1;

        while (index < tags.Length)
        {
            int current = tags[index];
            int position = index;

            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    tags[position - 1]) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current))
            {
                tags[position] = tags[position - 1];
                position--;
            }

            tags[position] = current;
            index++;
        }
    }

    static uint GetOutputScalerType(FontFace face)
    {
        FontOutlineKind kind = face.GetOutlineKind();
        return kind == FontOutlineKind.Cff1 || kind == FontOutlineKind.Cff2 ?
            0x4F54544FU : face.GetScalerType();
    }

    static int RoundMinimum(double value)
    {
        return CheckedCoordinate(System.Math.Floor(value));
    }

    static int RoundMaximum(double value)
    {
        return CheckedCoordinate(System.Math.Ceiling(value));
    }

    static int CheckedCoordinate(double value)
    {
        if (!double.IsFinite(value) ||
            value < short.MinValue || value > short.MaxValue)
        {
            throw FontFormatException.Create(
                "A subset outline bound exceeds the FWORD range.");
        }

        return (int)value;
    }

    static void ValidateMetric(int advance, int bearing)
    {
        if (advance < 0 || advance > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "A subset glyph advance exceeds the UInt16 range.");
        }

        ValidateSignedMetric(bearing);
    }

    static void ValidateSignedMetric(int value)
    {
        if (value < short.MinValue || value > short.MaxValue)
        {
            throw FontFormatException.Create(
                "A subset metric exceeds the Int16 range.");
        }
    }

    static void WriteInt16(byte[] data, int offset, int value)
    {
        WriteUInt16(data, offset, value);
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        uint encoded = CheckedFontMath.ReinterpretInt32AsUInt32(value);
        data[offset] = (byte)((encoded >> 8) & 0xFFU);
        data[offset + 1] = (byte)(encoded & 0xFFU);
    }

    static void WriteUInt32(byte[] data, int offset, uint value)
    {
        data[offset] = (byte)((value >> 24) & 0xFFU);
        data[offset + 1] = (byte)((value >> 16) & 0xFFU);
        data[offset + 2] = (byte)((value >> 8) & 0xFFU);
        data[offset + 3] = (byte)(value & 0xFFU);
    }
}
