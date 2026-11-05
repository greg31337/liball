namespace ttf;

internal class FontSubsetColorClosure
{
    bool applicable;
    int[] glyphIds;
    int[] colrBaseGlyphIds;
    int[] colrLayerIndices;
    int[] colrPaintOffsets;
    int[] svgDocumentIndices;
    int[] sbixGlyphIds;
    int[] cbdtGlyphIds;
    int[] ebdtGlyphIds;
    int[] paletteIndices;
    int[] paletteEntryIndices;

    internal FontSubsetColorClosure(
        bool applicable,
        int[] glyphIds,
        int[] colrBaseGlyphIds,
        int[] colrLayerIndices,
        int[] colrPaintOffsets,
        int[] svgDocumentIndices,
        int[] sbixGlyphIds,
        int[] cbdtGlyphIds,
        int[] ebdtGlyphIds,
        int[] paletteIndices,
        int[] paletteEntryIndices)
    {
        this.applicable = applicable;
        this.glyphIds = CopyIntegers(glyphIds);
        this.colrBaseGlyphIds = CopyIntegers(colrBaseGlyphIds);
        this.colrLayerIndices = CopyIntegers(colrLayerIndices);
        this.colrPaintOffsets = CopyIntegers(colrPaintOffsets);
        this.svgDocumentIndices = CopyIntegers(svgDocumentIndices);
        this.sbixGlyphIds = CopyIntegers(sbixGlyphIds);
        this.cbdtGlyphIds = CopyIntegers(cbdtGlyphIds);
        this.ebdtGlyphIds = CopyIntegers(ebdtGlyphIds);
        this.paletteIndices = CopyIntegers(paletteIndices);
        this.paletteEntryIndices = CopyIntegers(paletteEntryIndices);
    }

    internal bool IsApplicable()
    {
        return applicable;
    }

    internal int[] CopyGlyphIds()
    {
        return CopyIntegers(glyphIds);
    }

    internal int[] CopyColrBaseGlyphIds()
    {
        return CopyIntegers(colrBaseGlyphIds);
    }

    internal int[] CopyColrLayerIndices()
    {
        return CopyIntegers(colrLayerIndices);
    }

    internal int[] CopyColrPaintOffsets()
    {
        return CopyIntegers(colrPaintOffsets);
    }

    internal int[] CopySvgDocumentIndices()
    {
        return CopyIntegers(svgDocumentIndices);
    }

    internal int[] CopySbixGlyphIds()
    {
        return CopyIntegers(sbixGlyphIds);
    }

    internal int[] CopyCbdtGlyphIds()
    {
        return CopyIntegers(cbdtGlyphIds);
    }

    internal int[] CopyEbdtGlyphIds()
    {
        return CopyIntegers(ebdtGlyphIds);
    }

    internal int[] CopyPaletteIndices()
    {
        return CopyIntegers(paletteIndices);
    }

    internal int[] CopyPaletteEntryIndices()
    {
        return CopyIntegers(paletteEntryIndices);
    }

    static int[] CopyIntegers(int[] values)
    {
        int[] copy = new int[values.Length];
        System.Array.Copy(values, copy, values.Length);
        return copy;
    }
}

internal class FontSubsetColorClosureBuilder
{
    SfntColrTable? colr;
    SfntCpalTable? cpal;
    SfntSvgTable? svg;
    SfntSbixTable? sbix;
    SfntBitmapLocationTable? cbdt;
    SfntBitmapLocationTable? ebdt;
    FontLimits limits;
    double[] normalizedCoordinates;
    int maximumEntries;
    System.Collections.Generic.HashSet<int> glyphIds;
    System.Collections.Generic.List<int> glyphQueue;
    System.Collections.Generic.HashSet<int> colrBaseGlyphIds;
    System.Collections.Generic.HashSet<int> colrLayerIndices;
    System.Collections.Generic.HashSet<int> colrPaintOffsets;
    System.Collections.Generic.HashSet<int> svgDocumentIndices;
    System.Collections.Generic.HashSet<int> sbixGlyphIds;
    System.Collections.Generic.HashSet<int> cbdtGlyphIds;
    System.Collections.Generic.HashSet<int> ebdtGlyphIds;
    System.Collections.Generic.HashSet<int> paletteIndices;
    System.Collections.Generic.HashSet<int> paletteEntryIndices;

    FontSubsetColorClosureBuilder(
        SfntColrTable? colr,
        SfntCpalTable? cpal,
        SfntSvgTable? svg,
        SfntSbixTable? sbix,
        SfntBitmapLocationTable? cbdt,
        SfntBitmapLocationTable? ebdt,
        FontLimits limits,
        double[] normalizedCoordinates,
        int maximumEntries)
    {
        this.colr = colr;
        this.cpal = cpal;
        this.svg = svg;
        this.sbix = sbix;
        this.cbdt = cbdt;
        this.ebdt = ebdt;
        this.limits = limits;
        this.normalizedCoordinates = normalizedCoordinates;
        this.maximumEntries = maximumEntries;
        glyphIds = new System.Collections.Generic.HashSet<int>();
        glyphQueue = new System.Collections.Generic.List<int>();
        colrBaseGlyphIds = new System.Collections.Generic.HashSet<int>();
        colrLayerIndices = new System.Collections.Generic.HashSet<int>();
        colrPaintOffsets = new System.Collections.Generic.HashSet<int>();
        svgDocumentIndices = new System.Collections.Generic.HashSet<int>();
        sbixGlyphIds = new System.Collections.Generic.HashSet<int>();
        cbdtGlyphIds = new System.Collections.Generic.HashSet<int>();
        ebdtGlyphIds = new System.Collections.Generic.HashSet<int>();
        paletteIndices = new System.Collections.Generic.HashSet<int>();
        paletteEntryIndices =
            new System.Collections.Generic.HashSet<int>();
    }

    internal static FontSubsetColorClosure Create(
        FontInstance instance,
        FontSubsetOptions options,
        int[] initialGlyphIds,
        int[] requestedPaletteIndices,
        int maximumEntries)
    {
        if (options.GetColorPolicy() != FontSubsetColorPolicy.Retain)
        {
            return Empty(false, initialGlyphIds);
        }

        SfntTableCache cache = instance.GetFace().GetTableCache();
        SfntColrTable? colr = SfntColrTableParser.FindTable(cache);
        double[] normalizedCoordinates = colr != null &&
            colr.HasVariationData() ?
            instance.GetCoordinates().GetNormalizedValues() : new double[0];
        return CreateForTables(
            colr,
            SfntCpalTableParser.FindTable(cache),
            SfntSvgTableParser.FindTable(cache),
            SfntSbixTableParser.FindTable(cache),
            SfntCblcTableParser.FindTable(cache),
            SfntEblcTableParser.FindTable(cache),
            cache.GetLimits(),
            normalizedCoordinates,
            initialGlyphIds,
            requestedPaletteIndices,
            maximumEntries);
    }

    internal static FontSubsetColorClosure CreateForTables(
        SfntColrTable? colr,
        SfntCpalTable? cpal,
        SfntSvgTable? svg,
        SfntSbixTable? sbix,
        SfntBitmapLocationTable? cbdt,
        SfntBitmapLocationTable? ebdt,
        FontLimits limits,
        double[] normalizedCoordinates,
        int[] initialGlyphIds,
        int[] requestedPaletteIndices,
        int maximumEntries)
    {
        if (limits == null ||
            normalizedCoordinates == null ||
            initialGlyphIds == null ||
            requestedPaletteIndices == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (maximumEntries <= 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumEntries");
        }

        FontSubsetColorClosureBuilder builder =
            new FontSubsetColorClosureBuilder(
                colr,
                cpal,
                svg,
                sbix,
                cbdt,
                ebdt,
                limits,
                normalizedCoordinates,
                maximumEntries);
        return builder.Build(initialGlyphIds, requestedPaletteIndices);
    }

    FontSubsetColorClosure Build(
        int[] initialGlyphIds,
        int[] requestedPaletteIndices)
    {
        AddRequestedPalettes(requestedPaletteIndices);
        int index = 0;

        while (index < initialGlyphIds.Length)
        {
            AddGlyph(initialGlyphIds[index]);
            index++;
        }

        index = 0;
        while (index < glyphQueue.Count)
        {
            int glyphId = glyphQueue[index];
            ProcessColr(glyphId);
            ProcessSvg(glyphId);
            ProcessSbix(glyphId);
            ProcessBitmap(cbdt, cbdtGlyphIds, glyphId);
            ProcessBitmap(ebdt, ebdtGlyphIds, glyphId);
            index++;
        }

        if (svgDocumentIndices.Count != 0 && cpal != null)
        {
            AddAllPaletteEntries();
        }

        return new FontSubsetColorClosure(
            true,
            ToSortedArray(glyphIds),
            ToSortedArray(colrBaseGlyphIds),
            ToSortedArray(colrLayerIndices),
            ToSortedArray(colrPaintOffsets),
            ToSortedArray(svgDocumentIndices),
            ToSortedArray(sbixGlyphIds),
            ToSortedArray(cbdtGlyphIds),
            ToSortedArray(ebdtGlyphIds),
            ToSortedArray(paletteIndices),
            ToSortedArray(paletteEntryIndices));
    }

    void AddRequestedPalettes(int[] requestedPaletteIndices)
    {
        if (requestedPaletteIndices.Length != 0 && cpal == null)
        {
            throw FontFormatException.Create(
                "Requested color palettes require a CPAL table.");
        }

        if (cpal == null)
        {
            return;
        }

        if (requestedPaletteIndices.Length == 0)
        {
            cpal.GetPalette(0);
            AddEntry(paletteIndices, 0, "palette");
            return;
        }

        int index = 0;
        while (index < requestedPaletteIndices.Length)
        {
            int paletteIndex = requestedPaletteIndices[index];
            cpal.GetPalette(paletteIndex);
            AddEntry(paletteIndices, paletteIndex, "palette");
            index++;
        }
    }

    void ProcessColr(int glyphId)
    {
        if (colr == null || colrBaseGlyphIds.Contains(glyphId))
        {
            return;
        }

        SfntColorGlyphGraph? graph = colr.HasVariationData() ?
            SfntColrGraphNormalizer.Normalize(
                colr,
                glyphId,
                colr.CreateVariationLocation(normalizedCoordinates),
                limits) :
            SfntColrGraphNormalizer.Normalize(colr, glyphId, limits);

        if (graph == null)
        {
            return;
        }

        AddEntry(colrBaseGlyphIds, glyphId, "COLR base glyph");
        int referenceIndex = 0;

        while (referenceIndex < graph.GetReferencedGlyphCount())
        {
            AddGlyph(graph.GetReferencedGlyphId(referenceIndex));
            referenceIndex++;
        }

        CollectPaletteEntries(graph.GetRoot());
        CollectRawColrClosure(glyphId);
    }

    void CollectRawColrClosure(int glyphId)
    {
        System.ArgumentNullException.ThrowIfNull(colr);
        if (colr.GetVersion() == 1)
        {
            SfntColrBaseGlyphPaintRecord? paintRecord =
                colr.FindBaseGlyphPaintRecord(glyphId);

            if (paintRecord != null)
            {
                CollectRawPaint(paintRecord.GetPaint());
                return;
            }
        }

        SfntColrBaseGlyphRecord? layerRecord =
            colr.FindBaseGlyphRecord(glyphId);

        if (layerRecord != null)
        {
            int layerIndex = 0;
            while (layerIndex < layerRecord.GetLayerCount())
            {
                AddEntry(
                    colrLayerIndices,
                    layerRecord.GetFirstLayerIndex() + layerIndex,
                    "COLR layer");
                layerIndex++;
            }
        }
    }

    void CollectRawPaint(SfntColrPaint paint)
    {
        int offset = paint.GetOffset();

        if (colrPaintOffsets.Contains(offset))
        {
            return;
        }

        AddEntry(colrPaintOffsets, offset, "COLR paint");
        int format = paint.GetFormat();

        if (format == 1)
        {
            System.ArgumentNullException.ThrowIfNull(colr);
            SfntColrLayerList? layerList = colr.GetLayerList();
            int layerIndex = 0;

            while (layerIndex < paint.GetLayerCount())
            {
                int retainedLayerIndex =
                    paint.GetFirstLayerIndex() + layerIndex;
                AddEntry(
                    colrLayerIndices,
                    retainedLayerIndex,
                    "COLR layer");
                System.ArgumentNullException.ThrowIfNull(layerList);
                CollectRawPaint(layerList.GetPaint(retainedLayerIndex));
                layerIndex++;
            }
        }

        if (format == 2 || format == 3)
        {
            AddPaletteEntry(paint.GetPaletteEntryIndex());
        }

        if (format >= 4 && format <= 9)
        {
            SfntColrColorLine? line = paint.GetColorLine();
            int stopIndex = 0;

            System.ArgumentNullException.ThrowIfNull(line);
            while (stopIndex < line.GetStopCount())
            {
                AddPaletteEntry(
                    line.GetStop(stopIndex).GetPaletteEntryIndex());
                stopIndex++;
            }
        }

        int childIndex = 0;
        while (childIndex < paint.GetChildPaintOffsetCount())
        {
            System.ArgumentNullException.ThrowIfNull(colr);
            SfntColrPaint? child = colr.FindPaintByOffset(
                paint.GetChildPaintOffset(childIndex));

            if (child == null)
            {
                throw FontFormatException.Create(
                    "A retained COLR paint references a missing child paint.");
            }

            CollectRawPaint(child);
            childIndex++;
        }

        if (format == 11)
        {
            System.ArgumentNullException.ThrowIfNull(colr);
            SfntColrBaseGlyphPaintRecord? record =
                colr.FindBaseGlyphPaintRecord(paint.GetGlyphId());

            if (record == null)
            {
                throw FontFormatException.Create(
                    "A retained PaintColrGlyph references a missing base glyph.");
            }

            CollectRawPaint(record.GetPaint());
        }
    }

    void CollectPaletteEntries(SfntColorPaintNode node)
    {
        if (node.GetKind() == SfntColorPaintKind.PaletteColor)
        {
            AddPaletteEntry(node.GetPaletteEntryIndex());
        }

        int childIndex = 0;
        while (childIndex < node.GetChildCount())
        {
            CollectPaletteEntries(node.GetChild(childIndex));
            childIndex++;
        }

        if (node.GetKind() == SfntColorPaintKind.LinearGradient ||
            node.GetKind() == SfntColorPaintKind.RadialGradient ||
            node.GetKind() == SfntColorPaintKind.SweepGradient)
        {
            SfntColorPaintLine line = node.GetColorLine();
            int stopIndex = 0;

            while (stopIndex < line.GetStopCount())
            {
                CollectPaletteEntries(
                    line.GetStop(stopIndex).GetColor());
                stopIndex++;
            }
        }
    }

    void AddPaletteEntry(int paletteEntryIndex)
    {
        if (paletteEntryIndex == 0xFFFF)
        {
            return;
        }

        if (cpal == null ||
            paletteEntryIndex < 0 ||
            paletteEntryIndex >= cpal.GetPaletteEntryCount())
        {
            throw FontFormatException.Create(
                "A retained COLR paint references an invalid CPAL entry.");
        }

        AddEntry(
            paletteEntryIndices,
            paletteEntryIndex,
            "palette entry");
    }

    void ProcessSvg(int glyphId)
    {
        if (svg == null)
        {
            return;
        }

        SfntSvgDocument? document = svg.FindDocument(glyphId);

        if (document == null)
        {
            return;
        }

        int documentIndex = FindSvgDocumentIndex(document);

        if (!svgDocumentIndices.Contains(documentIndex))
        {
            AddEntry(
                svgDocumentIndices,
                documentIndex,
                "SVG document");
            int rangeGlyphId = document.GetStartGlyphId();

            while (rangeGlyphId <= document.GetEndGlyphId())
            {
                AddGlyph(rangeGlyphId);
                rangeGlyphId++;
            }
        }
    }

    int FindSvgDocumentIndex(SfntSvgDocument document)
    {
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(svg);
        while (index < svg.GetDocumentCount())
        {
            if (object.ReferenceEquals(svg.GetDocument(index), document))
            {
                return index;
            }

            index++;
        }

        throw FontFormatException.Create(
            "An SVG glyph range does not map to its document index.");
    }

    void ProcessSbix(int glyphId)
    {
        if (sbix == null)
        {
            return;
        }

        int strikeIndex = 0;
        while (strikeIndex < sbix.GetStrikeCount())
        {
            SfntSbixStrike strike = sbix.GetStrike(strikeIndex);

            if (strike.HasGlyphData(glyphId))
            {
                AddEntry(sbixGlyphIds, glyphId, "sbix glyph");
                SfntSbixGlyphData glyph = strike.GetGlyphData(glyphId);

                if (glyph.IsDuplicate())
                {
                    AddGlyph(glyph.GetDuplicateGlyphId());
                }
            }

            strikeIndex++;
        }
    }

    void ProcessBitmap(
        SfntBitmapLocationTable? table,
        System.Collections.Generic.HashSet<int> retainedGlyphs,
        int glyphId)
    {
        if (table == null)
        {
            return;
        }

        int strikeIndex = 0;
        while (strikeIndex < table.GetStrikeCount())
        {
            SfntBitmapStrike strike = table.GetStrike(strikeIndex);

            if (strike.HasGlyph(glyphId))
            {
                AddEntry(retainedGlyphs, glyphId, "bitmap glyph");
                SfntBitmapGlyphData glyph = strike.GetGlyph(glyphId);
                int componentIndex = 0;

                while (componentIndex < glyph.GetComponentCount())
                {
                    AddGlyph(glyph.GetComponent(componentIndex).GetGlyphId());
                    componentIndex++;
                }
            }

            strikeIndex++;
        }
    }

    void AddAllPaletteEntries()
    {
        int entryIndex = 0;

        System.ArgumentNullException.ThrowIfNull(cpal);
        while (entryIndex < cpal.GetPaletteEntryCount())
        {
            AddEntry(
                paletteEntryIndices,
                entryIndex,
                "palette entry");
            entryIndex++;
        }
    }

    bool AddGlyph(int glyphId)
    {
        if (glyphIds.Contains(glyphId))
        {
            return false;
        }

        if (glyphIds.Count >= maximumEntries)
        {
            throw FontLimitException.Create(
                "Color glyph closure exceeds the configured entry limit.");
        }

        glyphIds.Add(glyphId);
        glyphQueue.Add(glyphId);
        return true;
    }

    void AddEntry(
        System.Collections.Generic.HashSet<int> values,
        int value,
        string kind)
    {
        if (!values.Contains(value))
        {
            if (values.Count >= maximumEntries)
            {
                throw FontLimitException.Create(
                    "Color " + kind +
                    " closure exceeds the configured entry limit.");
            }

            values.Add(value);
        }
    }

    static int[] ToSortedArray(
        System.Collections.Generic.HashSet<int> values)
    {
        int[] result = new int[values.Count];
        values.CopyTo(result);
        System.Array.Sort(result);
        return result;
    }

    static FontSubsetColorClosure Empty(
        bool applicable,
        int[] glyphIds)
    {
        return new FontSubsetColorClosure(
            applicable,
            glyphIds,
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0],
            new int[0]);
    }
}
