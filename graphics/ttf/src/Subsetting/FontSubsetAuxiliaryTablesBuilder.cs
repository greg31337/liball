namespace ttf;

internal class FontSubsetAuxiliaryTable
{
    int tag;
    byte[] data;

    internal FontSubsetAuxiliaryTable(int tag, byte[] data)
    {
        this.tag = tag;
        this.data = Copy(data);
    }

    internal int GetTag() { return tag; }
    internal byte[] CopyData() { return Copy(data); }

    static byte[] Copy(byte[] value)
    {
        byte[] output = new byte[value.Length];
        System.Array.Copy(value, output, value.Length);
        return output;
    }
}

internal class FontSubsetAuxiliaryTables
{
    FontSubsetAuxiliaryTable[] tables;
    int[] droppedTags;

    internal FontSubsetAuxiliaryTables(
        FontSubsetAuxiliaryTable[] tables,
        int[] droppedTags)
    {
        this.tables = CopyTables(tables);
        this.droppedTags = CopyIntegers(droppedTags);
    }

    internal int GetTableCount() { return tables.Length; }

    internal FontSubsetAuxiliaryTable GetTable(int index)
    {
        if (index < 0 || index >= tables.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
        return new FontSubsetAuxiliaryTable(
            tables[index].GetTag(), tables[index].CopyData());
    }

    internal bool HasTable(int tag)
    {
        return FindTableIndex(tag) >= 0;
    }

    internal byte[]? CopyTableData(int tag)
    {
        int index = FindTableIndex(tag);
        return index < 0 ? null : tables[index].CopyData();
    }

    internal int[] CopyDroppedTags()
    {
        return CopyIntegers(droppedTags);
    }

    int FindTableIndex(int tag)
    {
        int low = 0;
        int high = tables.Length - 1;
        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            int candidate = tables[middle].GetTag();
            uint candidateValue = CheckedFontMath.ReinterpretInt32AsUInt32(
                candidate);
            uint targetValue = CheckedFontMath.ReinterpretInt32AsUInt32(tag);
            if (candidateValue == targetValue) { return middle; }
            if (candidateValue < targetValue) { low = middle + 1; }
            else { high = middle - 1; }
        }
        return -1;
    }

    static FontSubsetAuxiliaryTable[] CopyTables(
        FontSubsetAuxiliaryTable[] values)
    {
        FontSubsetAuxiliaryTable[] output =
            new FontSubsetAuxiliaryTable[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            output[index] = new FontSubsetAuxiliaryTable(
                values[index].GetTag(), values[index].CopyData());
            index += 1;
        }
        return output;
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] output = new int[values.Length];
        System.Array.Copy(values, output, values.Length);
        return output;
    }
}

internal static class FontSubsetAuxiliaryTablesBuilder
{
    static readonly int[] layoutTags = new int[]
    {
        FontTags.Base, FontTags.Jstf, FontTags.Math
    };
    static readonly int[] colorTags = new int[]
    {
        FontTags.Cbdt, FontTags.Cblc, FontTags.Colr, FontTags.Cpal,
        FontTags.Ebdt, FontTags.Eblc, FontTags.Sbix, FontTags.Svg
    };
    static readonly int[] variationTags = new int[]
    {
        FontTags.Avar, FontTags.Cvar, FontTags.Fvar, FontTags.Gvar,
        FontTags.Hvar, FontTags.Mvar, FontTags.Stat, FontTags.Vvar
    };

    internal static FontSubsetAuxiliaryTables Create(
        FontInstance instance,
        FontSubsetOptions options,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        FontSubsetColorClosure colorClosure)
    {
        if (instance == null || options == null ||
            oldToNewGlyphIds == null || newToOldGlyphIds == null ||
            colorClosure == null)
        {
            throw new System.ArgumentNullException("values");
        }
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables =
            new System.Collections.Generic.List<FontSubsetAuxiliaryTable>();
        System.Collections.Generic.HashSet<int> dropped =
            new System.Collections.Generic.HashSet<int>();
        FontFace face = instance.GetFace();
        AddLayoutTables(face, options, tables, dropped);
        AddColorTables(
            face,
            options,
            oldToNewGlyphIds,
            newToOldGlyphIds,
            colorClosure,
            tables,
            dropped);
        DropPresentTables(face, variationTags, dropped);
        FontSubsetAuxiliaryTable[] output = tables.ToArray();
        SortTables(output);
        int[] droppedTags = ToSortedArray(dropped);
        return new FontSubsetAuxiliaryTables(output, droppedTags);
    }

    static void AddLayoutTables(
        FontFace face,
        FontSubsetOptions options,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        bool retain = options.GetLayoutPolicy() ==
            FontSubsetLayoutPolicy.Retain &&
            options.GetEmbeddingMode() == FontSubsetEmbeddingMode.FullFont &&
            options.GetGlyphRetentionMode() ==
                FontSubsetGlyphRetentionMode.Preserve;
        int index = 0;
        while (index < layoutTags.Length)
        {
            int tag = layoutTags[index];
            if (face.HasTable(tag))
            {
                if (retain) { AddRaw(face, tag, tables); }
                else { dropped.Add(tag); }
            }
            index += 1;
        }
    }

    static void AddColorTables(
        FontFace face,
        FontSubsetOptions options,
        int[] glyphMap,
        int[] newToOldGlyphIds,
        FontSubsetColorClosure closure,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        if (options.GetColorPolicy() != FontSubsetColorPolicy.Retain ||
            !closure.IsApplicable())
        {
            DropPresentTables(face, colorTags, dropped);
            return;
        }
        AddCpal(face, tables);
        AddColr(face, options, glyphMap, closure, tables, dropped);
        AddSvg(face, glyphMap, closure, tables, dropped);
        AddSbix(
            face, glyphMap, newToOldGlyphIds, closure, tables, dropped);
        AddBitmapTables(face, options, tables, dropped);
    }

    static void AddCpal(
        FontFace face,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables)
    {
        if (face.HasTable(FontTags.Cpal))
        {
            AddRaw(face, FontTags.Cpal, tables);
        }
    }

    static void AddColr(
        FontFace face,
        FontSubsetOptions options,
        int[] glyphMap,
        FontSubsetColorClosure closure,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        if (!face.HasTable(FontTags.Colr)) { return; }
        SfntColrTable? colr = face.GetColrTable();
        if (colr != null && colr.GetVersion() == 0)
        {
            byte[]? data = CreateColrVersionZero(
                colr, closure.CopyColrBaseGlyphIds(), glyphMap,
                face.GetTableCache().GetLimits());
            if (data != null)
            {
                tables.Add(new FontSubsetAuxiliaryTable(FontTags.Colr, data));
            }
            else { dropped.Add(FontTags.Colr); }
            return;
        }
        if (options.GetGlyphRetentionMode() ==
            FontSubsetGlyphRetentionMode.Preserve)
        {
            AddRaw(face, FontTags.Colr, tables);
        }
        else { dropped.Add(FontTags.Colr); }
    }

    static byte[]? CreateColrVersionZero(
        SfntColrTable colr,
        int[] retainedBaseGlyphs,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> bases =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<SfntColrLayerRecord> layers =
            new System.Collections.Generic.List<SfntColrLayerRecord>();
        System.Collections.Generic.List<int> firstLayers =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> layerCounts =
            new System.Collections.Generic.List<int>();
        int index = 0;
        while (index < retainedBaseGlyphs.Length)
        {
            int oldGlyph = retainedBaseGlyphs[index];
            int mapped = MapGlyph(oldGlyph, glyphMap);
            SfntColrBaseGlyphRecord? record =
                colr.FindBaseGlyphRecord(oldGlyph);
            if (mapped >= 0 && record != null)
            {
                int start = layers.Count;
                int layerIndex = 0;
                while (layerIndex < record.GetLayerCount())
                {
                    SfntColrLayerRecord layer = record.GetLayer(layerIndex);
                    if (MapGlyph(layer.GetGlyphId(), glyphMap) >= 0)
                    {
                        layers.Add(layer);
                    }
                    layerIndex += 1;
                }
                if (layers.Count != start)
                {
                    bases.Add(mapped);
                    firstLayers.Add(start);
                    layerCounts.Add(layers.Count - start);
                }
            }
            index += 1;
        }
        if (bases.Count == 0) { return null; }
        int baseOffset = 14;
        int layerOffset = baseOffset + bases.Count * 6;
        BigEndianWriter writer = BigEndianWriter.Create(
            layerOffset + layers.Count * 4, limits.GetMaxOutputBytes());
        writer.WriteUInt16(0);
        writer.WriteUInt16((ushort)bases.Count);
        writer.WriteUInt32((uint)baseOffset);
        writer.WriteUInt32((uint)layerOffset);
        writer.WriteUInt16((ushort)layers.Count);
        index = 0;
        while (index < bases.Count)
        {
            writer.WriteUInt16((ushort)bases[index]);
            writer.WriteUInt16((ushort)firstLayers[index]);
            writer.WriteUInt16((ushort)layerCounts[index]);
            index += 1;
        }
        index = 0;
        while (index < layers.Count)
        {
            SfntColrLayerRecord layer = layers[index];
            writer.WriteUInt16((ushort)MapGlyph(
                layer.GetGlyphId(), glyphMap));
            writer.WriteUInt16((ushort)layer.GetPaletteEntryIndex());
            index += 1;
        }
        return writer.CopyBytes();
    }

    static void AddSvg(
        FontFace face,
        int[] glyphMap,
        FontSubsetColorClosure closure,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        if (!face.HasTable(FontTags.Svg)) { return; }
        SfntSvgTable? svg = SfntSvgTableParser.FindTable(
            face.GetTableCache());
        byte[]? data = svg == null ? null : CreateSvg(
            svg,
            closure.CopySvgDocumentIndices(),
            glyphMap,
            face.GetTableCache().GetLimits());
        if (data == null) { dropped.Add(FontTags.Svg); }
        else
        {
            tables.Add(new FontSubsetAuxiliaryTable(FontTags.Svg, data));
        }
    }

    static byte[]? CreateSvg(
        SfntSvgTable svg,
        int[] retainedDocuments,
        int[] glyphMap,
        FontLimits limits)
    {
        System.Collections.Generic.List<int> starts =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> ends =
            new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<byte[]> documents =
            new System.Collections.Generic.List<byte[]>();
        int index = 0;
        while (index < retainedDocuments.Length)
        {
            int documentIndex = retainedDocuments[index];
            if (documentIndex >= 0 &&
                documentIndex < svg.GetDocumentCount())
            {
                SfntSvgDocument document = svg.GetDocument(documentIndex);
                int minimum = int.MaxValue;
                int maximum = -1;
                int glyph = document.GetStartGlyphId();
                while (glyph <= document.GetEndGlyphId())
                {
                    int mapped = MapGlyph(glyph, glyphMap);
                    if (mapped >= 0)
                    {
                        if (mapped < minimum) { minimum = mapped; }
                        if (mapped > maximum) { maximum = mapped; }
                    }
                    glyph += 1;
                }
                if (maximum >= 0)
                {
                    starts.Add(minimum);
                    ends.Add(maximum);
                    documents.Add(document.CopyEncodedBytes());
                }
            }
            index += 1;
        }
        if (documents.Count == 0) { return null; }
        int listLength = 2 + documents.Count * 12;
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16(0);
        writer.WriteUInt32(10);
        writer.WriteUInt32(0);
        writer.WriteUInt16((ushort)documents.Count);
        int offset = listLength;
        index = 0;
        while (index < documents.Count)
        {
            writer.WriteUInt16((ushort)starts[index]);
            writer.WriteUInt16((ushort)ends[index]);
            writer.WriteUInt32((uint)offset);
            writer.WriteUInt32((uint)documents[index].Length);
            offset = AddLength(offset, documents[index].Length, limits);
            index += 1;
        }
        index = 0;
        while (index < documents.Count)
        {
            writer.WriteBytes(documents[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static void AddSbix(
        FontFace face,
        int[] glyphMap,
        int[] newToOldGlyphIds,
        FontSubsetColorClosure closure,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        if (!face.HasTable(FontTags.Sbix)) { return; }
        SfntSbixTable? sbix = SfntSbixTableParser.FindTable(
            face.GetTableCache());
        byte[]? data = sbix == null ? null : CreateSbix(
            sbix,
            glyphMap,
            newToOldGlyphIds,
            closure.CopySbixGlyphIds(),
            face.GetTableCache().GetLimits());
        if (data == null) { dropped.Add(FontTags.Sbix); }
        else
        {
            tables.Add(new FontSubsetAuxiliaryTable(FontTags.Sbix, data));
        }
    }

    static byte[]? CreateSbix(
        SfntSbixTable sbix,
        int[] glyphMap,
        int[] newToOldGlyphIds,
        int[] retainedGlyphs,
        FontLimits limits)
    {
        if (retainedGlyphs.Length == 0 || sbix.GetStrikeCount() == 0)
        {
            return null;
        }
        byte[][] strikes = new byte[sbix.GetStrikeCount()][];
        int index = 0;
        while (index < strikes.Length)
        {
            strikes[index] = CreateSbixStrike(
                sbix.GetStrike(index),
                glyphMap,
                newToOldGlyphIds,
                limits);
            index += 1;
        }
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16(1);
        writer.WriteUInt16((ushort)sbix.GetFlags());
        writer.WriteUInt32((uint)strikes.Length);
        int offset = 8 + strikes.Length * 4;
        index = 0;
        while (index < strikes.Length)
        {
            writer.WriteUInt32((uint)offset);
            offset = AddLength(offset, strikes[index].Length, limits);
            index += 1;
        }
        index = 0;
        while (index < strikes.Length)
        {
            writer.WriteBytes(strikes[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateSbixStrike(
        SfntSbixStrike strike,
        int[] glyphMap,
        int[] newToOldGlyphIds,
        FontLimits limits)
    {
        byte[][] glyphData = new byte[newToOldGlyphIds.Length][];
        int index = 0;
        while (index < glyphData.Length)
        {
            int oldGlyph = newToOldGlyphIds[index];
            SfntSbixGlyphData? glyph = oldGlyph < 0 ||
                oldGlyph >= strike.GetGlyphCount() ? null :
                strike.GetGlyphData(oldGlyph);
            glyphData[index] = glyph == null ? new byte[0] :
                CreateSbixGlyph(glyph, glyphMap, limits);
            index += 1;
        }
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteUInt16((ushort)strike.GetPixelsPerEm());
        writer.WriteUInt16((ushort)strike.GetPixelsPerInch());
        int offset = 4 + (glyphData.Length + 1) * 4;
        index = 0;
        while (index < glyphData.Length)
        {
            writer.WriteUInt32((uint)offset);
            offset = AddLength(offset, glyphData[index].Length, limits);
            index += 1;
        }
        writer.WriteUInt32((uint)offset);
        index = 0;
        while (index < glyphData.Length)
        {
            writer.WriteBytes(glyphData[index]);
            index += 1;
        }
        return writer.CopyBytes();
    }

    static byte[] CreateSbixGlyph(
        SfntSbixGlyphData glyph,
        int[] glyphMap,
        FontLimits limits)
    {
        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        writer.WriteInt16((short)glyph.GetOriginOffsetX());
        writer.WriteInt16((short)glyph.GetOriginOffsetY());
        writer.WriteUInt32(CheckedFontMath.ReinterpretInt32AsUInt32(
            glyph.GetGraphicTypeTag()));
        if (glyph.IsDuplicate())
        {
            int duplicate = MapGlyph(glyph.GetDuplicateGlyphId(), glyphMap);
            if (duplicate < 0)
            {
                throw FontFormatException.Create(
                    "A retained sbix duplicate target was omitted.");
            }
            writer.WriteUInt16((ushort)duplicate);
        }
        else { writer.WriteBytes(glyph.CopyData()); }
        return writer.CopyBytes();
    }

    static void AddBitmapTables(
        FontFace face,
        FontSubsetOptions options,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables,
        System.Collections.Generic.HashSet<int> dropped)
    {
        int[] tags = new int[]
        {
            FontTags.Cbdt, FontTags.Cblc, FontTags.Ebdt, FontTags.Eblc
        };
        bool copy = options.GetGlyphRetentionMode() ==
            FontSubsetGlyphRetentionMode.Preserve;
        int index = 0;
        while (index < tags.Length)
        {
            int tag = tags[index];
            if (face.HasTable(tag))
            {
                if (copy) { AddRaw(face, tag, tables); }
                else { dropped.Add(tag); }
            }
            index += 1;
        }
    }

    static void AddRaw(
        FontFace face,
        int tag,
        System.Collections.Generic.List<FontSubsetAuxiliaryTable> tables)
    {
        SfntTableData? table = face.GetTableStore().FindTable(tag);
        if (table != null)
        {
            tables.Add(new FontSubsetAuxiliaryTable(
                tag, table.GetDataSlice().CopyBytes()));
        }
    }

    static void DropPresentTables(
        FontFace face,
        int[] tags,
        System.Collections.Generic.HashSet<int> dropped)
    {
        int index = 0;
        while (index < tags.Length)
        {
            if (face.HasTable(tags[index])) { dropped.Add(tags[index]); }
            index += 1;
        }
    }

    static int MapGlyph(int glyphId, int[] glyphMap)
    {
        return glyphId < 0 || glyphId >= glyphMap.Length ?
            -1 : glyphMap[glyphId];
    }

    static int AddLength(int offset, int length, FontLimits limits)
    {
        long result = (long)offset + length;
        if (result > int.MaxValue || result > limits.GetMaxOutputBytes())
        {
            throw FontLimitException.Create(
                "Subset auxiliary table data exceeds the output limit.");
        }
        return (int)result;
    }

    static void SortTables(FontSubsetAuxiliaryTable[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            FontSubsetAuxiliaryTable current = values[index];
            int position = index;
            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    values[position - 1].GetTag()) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current.GetTag()))
            {
                values[position] = values[position - 1];
                position -= 1;
            }
            values[position] = current;
            index += 1;
        }
    }

    static int[] ToSortedArray(
        System.Collections.Generic.HashSet<int> values)
    {
        int[] output = new int[values.Count];
        values.CopyTo(output);
        int index = 1;
        while (index < output.Length)
        {
            int current = output[index];
            int position = index;
            while (position > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    output[position - 1]) >
                CheckedFontMath.ReinterpretInt32AsUInt32(current))
            {
                output[position] = output[position - 1];
                position -= 1;
            }
            output[position] = current;
            index += 1;
        }
        return output;
    }
}
