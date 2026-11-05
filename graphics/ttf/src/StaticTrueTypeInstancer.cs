namespace ttf;

internal class SfntInstancedGlyfData
{
    byte[] glyf;
    byte[] loca;
    int[] offsets;
    int[][] bounds;
    bool[] hasOutlines;
    int[] maxima;

    internal SfntInstancedGlyfData(
        byte[] glyf,
        byte[] loca,
        int[] offsets,
        int[][] bounds,
        bool[] hasOutlines,
        int[] maxima)
    {
        this.glyf = glyf;
        this.loca = loca;
        this.offsets = offsets;
        this.bounds = bounds;
        this.hasOutlines = hasOutlines;
        this.maxima = maxima;
    }

    internal byte[] GetGlyf()
    {
        return glyf;
    }

    internal byte[] GetLoca()
    {
        return loca;
    }

    internal int[] CopyOffsets()
    {
        int[] copy = new int[offsets.Length];
        System.Array.Copy(offsets, copy, offsets.Length);
        return copy;
    }

    internal int[] GetBounds(int glyphId)
    {
        return bounds[glyphId];
    }

    internal int[] GetGlobalBounds()
    {
        int[] result = new int[] { 0, 0, 0, 0 };
        bool hasOutline = false;
        int index = 0;

        while (index < bounds.Length)
        {
            int[] value = bounds[index];

            if (hasOutlines[index])
            {
                if (!hasOutline)
                {
                    result = value.AsSpan().ToArray();
                    hasOutline = true;
                }
                else
                {
                    result[0] = System.Math.Min(result[0], value[0]);
                    result[1] = System.Math.Min(result[1], value[1]);
                    result[2] = System.Math.Max(result[2], value[2]);
                    result[3] = System.Math.Max(result[3], value[3]);
                }
            }

            index += 1;
        }

        return result;
    }

    internal int[] GetMaxima()
    {
        return maxima;
    }
}

internal class SfntInstancedMetricData
{
    byte[] metrics;
    byte[] header;

    internal SfntInstancedMetricData(byte[] metrics, byte[] header)
    {
        this.metrics = metrics;
        this.header = header;
    }

    internal byte[] GetMetrics()
    {
        return metrics;
    }

    internal byte[] GetHeader()
    {
        return header;
    }
}

internal class SfntSubsetTrueTypeData
{
    SfntInstancedGlyfData glyphData;
    byte[] head;
    byte[] maxp;
    SfntInstancedMetricData horizontalMetrics;
    SfntInstancedMetricData? verticalMetrics;
    byte[]? fontProgram;
    byte[]? controlValueProgram;
    byte[]? controlValueData;

    internal SfntSubsetTrueTypeData(
        SfntInstancedGlyfData glyphData,
        byte[] head,
        byte[] maxp,
        SfntInstancedMetricData horizontalMetrics,
        SfntInstancedMetricData? verticalMetrics,
        byte[]? fontProgram,
        byte[]? controlValueProgram,
        byte[]? controlValueData)
    {
        this.glyphData = glyphData;
        this.head = CopyBytes(head);
        this.maxp = CopyBytes(maxp);
        this.horizontalMetrics = horizontalMetrics;
        this.verticalMetrics = verticalMetrics;
        this.fontProgram = CopyOptionalBytes(fontProgram);
        this.controlValueProgram = CopyOptionalBytes(controlValueProgram);
        this.controlValueData = CopyOptionalBytes(controlValueData);
    }

    internal byte[] CopyGlyf()
    {
        return glyphData.GetGlyf().AsSpan().ToArray();
    }

    internal byte[] CopyLoca()
    {
        return glyphData.GetLoca().AsSpan().ToArray();
    }

    internal int[] CopyLocaOffsets()
    {
        return glyphData.CopyOffsets();
    }

    internal byte[] CopyHead()
    {
        return CopyBytes(head);
    }

    internal byte[] CopyMaxp()
    {
        return CopyBytes(maxp);
    }

    internal byte[] CopyHhea()
    {
        return horizontalMetrics.GetHeader().AsSpan().ToArray();
    }

    internal byte[] CopyHmtx()
    {
        return horizontalMetrics.GetMetrics().AsSpan().ToArray();
    }

    internal bool HasVerticalMetrics()
    {
        return verticalMetrics != null;
    }

    internal byte[]? CopyVhea()
    {
        return verticalMetrics == null ? null :
            verticalMetrics.GetHeader().AsSpan().ToArray();
    }

    internal byte[]? CopyVmtx()
    {
        return verticalMetrics == null ? null :
            verticalMetrics.GetMetrics().AsSpan().ToArray();
    }

    internal bool HasFpgm()
    {
        return fontProgram != null;
    }

    internal byte[]? CopyFpgm()
    {
        return CopyOptionalBytes(fontProgram);
    }

    internal bool HasPrep()
    {
        return controlValueProgram != null;
    }

    internal byte[]? CopyPrep()
    {
        return CopyOptionalBytes(controlValueProgram);
    }

    internal bool HasCvt()
    {
        return controlValueData != null;
    }

    internal byte[]? CopyCvt()
    {
        return CopyOptionalBytes(controlValueData);
    }

    static byte[] CopyBytes(byte[] data)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        return data.AsSpan().ToArray();
    }

    static byte[]? CopyOptionalBytes(byte[]? data)
    {
        return data == null ? null : data.AsSpan().ToArray();
    }
}

internal static class SfntStaticTrueTypeInstancer
{
    const int ArgumentsAreWords = 0x0001;
    const int ArgumentsAreXYValues = 0x0002;
    const int MoreComponents = 0x0020;
    const int WeHaveInstructions = 0x0100;
    const int UseMyMetrics = 0x0200;
    const int ScaledComponentOffset = 0x0800;
    const int UnscaledComponentOffset = 0x1000;

    internal static byte[] Create(FontInstance instance)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        FontFace face = instance.GetFace();
        FontLimits limits = face.GetTableCache().GetLimits();

        if (!face.IsStructurallyValid())
        {
            throw new System.InvalidOperationException(
                "A structurally invalid face cannot be instantiated.");
        }

        if (face.GetOutlineKind() != FontOutlineKind.TrueType)
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires TrueType outlines.");
        }

        if (!face.HasTable(FontTags.Fvar))
        {
            return SfntFaceWriter.Write(face, limits);
        }

        SfntLocaTable? loca = face.GetLocaTable();
        SfntMaxpTable? maxp = face.GetMaxpTable();

        if (loca == null || maxp == null ||
            loca.GetGlyphCount() != instance.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires matching loca and maxp tables.");
        }

        int[] identityGlyphMap = CreateIdentityMap(
            instance.GetGlyphCount());
        SfntInstancedGlyfData glyphData = CreateGlyphData(
            instance,
            loca,
            face.HasTable(FontTags.Gvar),
            limits,
            identityGlyphMap,
            identityGlyphMap,
            true,
            false);
        SfntInstancedMetricData horizontal = CreateMetricData(
            instance,
            face,
            glyphData,
            identityGlyphMap,
            false,
            limits);
        SfntInstancedMetricData? vertical = null;
        bool hasVhea = face.HasTable(FontTags.Vhea);
        bool hasVmtx = face.HasTable(FontTags.Vmtx);

        if (hasVhea != hasVmtx)
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires paired vertical metric tables.");
        }

        vertical = CreateMetricData(
            instance,
            face,
            glyphData,
            identityGlyphMap,
            true,
            limits);

        SfntStaticInstanceMetadata metadata =
            new SfntStaticInstanceMetadata(
                instance,
                glyphData.GetGlobalBounds());
        byte[] head = metadata.PatchTable(FontTags.Head, CreateHead(face));
        SfntTableData? maxpTable = face.GetTableStore().FindTable(FontTags.Maxp);
        System.ArgumentNullException.ThrowIfNull(maxpTable);
        byte[] maxpData = metadata.CreateTrueTypeMaxp(
            maxpTable
                .GetDataSlice().CopyBytes(),
            glyphData.GetMaxima());
        byte[]? controlValues = null;
        if (face.HasTable(FontTags.Cvt))
        {
            TrueTypeHintingData? hinting = instance.GetTrueTypeHintingData();
            System.ArgumentNullException.ThrowIfNull(hinting);
            controlValues = hinting.CopyControlValueData();
        }
        SfntTableStore store = CreateTableStore(
            face,
            head,
            maxpData,
            glyphData,
            horizontal,
            vertical,
            controlValues,
            metadata);
        return SfntFaceWriter.Write(
            face.GetScalerType(),
            store,
            limits);
    }

    internal static SfntInstancedGlyfData CreateSubsetGlyphData(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        bool retainInstructions)
    {
        if (instance == null)
        {
            throw new System.ArgumentNullException("instance");
        }

        if (oldToNewGlyphIds == null)
        {
            throw new System.ArgumentNullException("oldToNewGlyphIds");
        }

        if (newToOldGlyphIds == null)
        {
            throw new System.ArgumentNullException("newToOldGlyphIds");
        }

        FontFace face = instance.GetFace();

        if (face.GetOutlineKind() != FontOutlineKind.TrueType)
        {
            throw FontFormatException.Create(
                "TrueType glyph subsetting requires TrueType outlines.");
        }

        SfntLocaTable? loca = face.GetLocaTable();

        if (loca == null || loca.GetGlyphCount() != instance.GetGlyphCount())
        {
            throw FontFormatException.Create(
                "TrueType glyph subsetting requires matching loca and maxp tables.");
        }

        ValidateGlyphMaps(
            instance.GetGlyphCount(),
            oldToNewGlyphIds,
            newToOldGlyphIds);
        return CreateGlyphData(
            instance,
            loca,
            face.HasTable(FontTags.Gvar),
            face.GetTableCache().GetLimits(),
            oldToNewGlyphIds,
            newToOldGlyphIds,
            retainInstructions,
            true);
    }

    internal static SfntSubsetTrueTypeData CreateSubsetTables(
        FontInstance instance,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        bool retainInstructions,
        bool retainVerticalMetrics)
    {
        SfntInstancedGlyfData glyphData = CreateSubsetGlyphData(
            instance,
            oldToNewGlyphIds,
            newToOldGlyphIds,
            retainInstructions);
        FontFace face = instance.GetFace();
        FontLimits limits = face.GetTableCache().GetLimits();
        SfntStaticInstanceMetadata metadata =
            new SfntStaticInstanceMetadata(
                instance,
                glyphData.GetGlobalBounds());
        byte[] head = metadata.PatchTable(
            FontTags.Head,
            CreateHead(face));
        SfntTableData? maxpTable = face.GetTableStore().FindTable(
            FontTags.Maxp);

        if (maxpTable == null)
        {
            throw FontFormatException.Create(
                "TrueType subsetting requires a maxp table.");
        }

        byte[] maxp = metadata.CreateTrueTypeMaxp(
            maxpTable.GetDataSlice().CopyBytes(),
            glyphData.GetMaxima());
        WriteUInt16(maxp, 4, newToOldGlyphIds.Length);
        PatchSubsetHintMaxima(maxp, retainInstructions);
        SfntInstancedMetricData horizontal = CreateMetricData(
            instance,
            face,
            glyphData,
            newToOldGlyphIds,
            false,
            limits);
        horizontal = new SfntInstancedMetricData(
            horizontal.GetMetrics(),
            metadata.PatchTable(
                FontTags.Hhea,
                horizontal.GetHeader()));
        SfntInstancedMetricData? vertical = null;

        if (retainVerticalMetrics)
        {
            bool hasVhea = face.HasTable(FontTags.Vhea);
            bool hasVmtx = face.HasTable(FontTags.Vmtx);

            if (hasVhea != hasVmtx)
            {
                throw FontFormatException.Create(
                    "TrueType subsetting requires paired vertical metric tables.");
            }

            if (hasVhea)
            {
                vertical = CreateMetricData(
                    instance,
                    face,
                    glyphData,
                    newToOldGlyphIds,
                    true,
                    limits);
                vertical = new SfntInstancedMetricData(
                    vertical.GetMetrics(),
                    metadata.PatchTable(
                        FontTags.Vhea,
                        vertical.GetHeader()));
            }
        }

        byte[]? fontProgram = null;
        byte[]? controlValueProgram = null;
        byte[]? controlValueData = null;

        if (retainInstructions)
        {
            TrueTypeHintingData? hinting = instance.GetTrueTypeHintingData();
            System.ArgumentNullException.ThrowIfNull(hinting);
            fontProgram = face.HasTable(FontTags.Fpgm) ?
                hinting.CopyFontProgram() : null;
            controlValueProgram = face.HasTable(FontTags.Prep) ?
                hinting.CopyControlValueProgram() : null;
            controlValueData = face.HasTable(FontTags.Cvt) ?
                hinting.CopyControlValueData() : null;
        }

        return new SfntSubsetTrueTypeData(
            glyphData,
            head,
            maxp,
            horizontal,
            vertical,
            fontProgram,
            controlValueProgram,
            controlValueData);
    }

    static void PatchSubsetHintMaxima(byte[] maxp, bool retainInstructions)
    {
        if (retainInstructions)
        {
            return;
        }

        WriteUInt16(maxp, 14, 1);
        int offset = 16;
        while (offset <= 26)
        {
            WriteUInt16(maxp, offset, 0);
            offset += 2;
        }
    }

    static int[] CreateIdentityMap(int glyphCount)
    {
        int[] map = new int[glyphCount];
        int glyphId = 0;

        while (glyphId < glyphCount)
        {
            map[glyphId] = glyphId;
            glyphId += 1;
        }

        return map;
    }

    static void ValidateGlyphMaps(
        int originalGlyphCount,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds)
    {
        if (oldToNewGlyphIds.Length != originalGlyphCount)
        {
            throw new System.ArgumentException(
                "The old-to-new glyph map does not match the font.",
                "oldToNewGlyphIds");
        }

        int oldGlyphId = 0;

        while (oldGlyphId < oldToNewGlyphIds.Length)
        {
            int newGlyphId = oldToNewGlyphIds[oldGlyphId];

            if (newGlyphId < -1 ||
                newGlyphId >= newToOldGlyphIds.Length ||
                (newGlyphId >= 0 &&
                    newToOldGlyphIds[newGlyphId] != oldGlyphId))
            {
                throw new System.ArgumentException(
                    "The glyph maps are not bidirectional.",
                    "oldToNewGlyphIds");
            }

            oldGlyphId += 1;
        }

        int newGlyphIdIndex = 0;

        while (newGlyphIdIndex < newToOldGlyphIds.Length)
        {
            int mappedOldGlyphId = newToOldGlyphIds[newGlyphIdIndex];

            if (mappedOldGlyphId < -1 ||
                mappedOldGlyphId >= oldToNewGlyphIds.Length ||
                (mappedOldGlyphId >= 0 &&
                    oldToNewGlyphIds[mappedOldGlyphId] != newGlyphIdIndex))
            {
                throw new System.ArgumentException(
                    "The glyph maps are not bidirectional.",
                    "newToOldGlyphIds");
            }

            newGlyphIdIndex += 1;
        }
    }

    static SfntInstancedGlyfData CreateGlyphData(
        FontInstance instance,
        SfntLocaTable loca,
        bool useVariations,
        FontLimits limits,
        int[] oldToNewGlyphIds,
        int[] newToOldGlyphIds,
        bool retainInstructions,
        bool retainMetricsFlags)
    {
        BigEndianWriter glyfWriter = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        int[] offsets = new int[newToOldGlyphIds.Length + 1];
        int[][] bounds = new int[newToOldGlyphIds.Length][];
        bool[] hasOutlines = new bool[newToOldGlyphIds.Length];
        int[] maxima = new int[7];
        int newGlyphId = 0;

        while (newGlyphId < newToOldGlyphIds.Length)
        {
            offsets[newGlyphId] = glyfWriter.GetLength();
            bounds[newGlyphId] = new int[] { 0, 0, 0, 0 };
            int oldGlyphId = newToOldGlyphIds[newGlyphId];

            if (oldGlyphId >= 0)
            {
                SfntGlyfRecord record = loca.GetGlyphRecord(oldGlyphId);

                if (!record.IsEmpty())
                {
                    hasOutlines[newGlyphId] = true;
                    SfntVariedGlyph? varied = useVariations ?
                        instance.GetTrueTypeVariedGlyph(oldGlyphId) : null;
                    SfntResolvedGlyph resolved = varied == null ?
                        loca.GetResolvedGlyph(oldGlyphId) :
                        varied.GetResolvedGlyph();
                    bounds[newGlyphId] = CalculateBounds(resolved);
                    SfntSimpleGlyph? simple = loca.GetSimpleGlyph(oldGlyphId);

                    if (simple != null)
                    {
                        maxima[0] = System.Math.Max(
                            maxima[0],
                            simple.GetPointCount());
                        maxima[1] = System.Math.Max(
                            maxima[1],
                            simple.GetContourCount());
                        int simpleInstructionLength = retainInstructions ?
                            simple.GetInstructionLength() : 0;
                        maxima[4] = System.Math.Max(
                            maxima[4],
                            simpleInstructionLength);
                        WriteSimpleGlyph(
                            glyfWriter,
                            simple,
                            resolved,
                            bounds[newGlyphId],
                            retainInstructions);
                    }
                    else
                    {
                        SfntCompositeGlyph? composite =
                            loca.GetCompositeGlyph(oldGlyphId);

                        if (composite == null)
                        {
                            throw FontFormatException.Create(
                                "An instanced glyf record has no supported outline form.");
                        }

                        maxima[2] = System.Math.Max(
                            maxima[2],
                            resolved.GetControlPointCount());
                        maxima[3] = System.Math.Max(
                            maxima[3],
                            resolved.GetOutline().GetContourCount());
                        int compositeInstructionLength = retainInstructions ?
                            composite.GetInstructionLength() : 0;
                        maxima[4] = System.Math.Max(
                            maxima[4],
                            compositeInstructionLength);
                        maxima[5] = System.Math.Max(
                            maxima[5],
                            composite.GetComponentCount());
                        maxima[6] = System.Math.Max(
                            maxima[6],
                            resolved.GetComponentDepth());

                        double[][] componentOffsets = varied == null ?
                            CalculateDefaultComponentOffsets(loca, composite) :
                            CopyVariedComponentOffsets(varied, composite);
                        WriteCompositeGlyph(
                            glyfWriter,
                            composite,
                            componentOffsets[0],
                            componentOffsets[1],
                            bounds[newGlyphId],
                            oldToNewGlyphIds,
                            retainInstructions,
                            retainMetricsFlags);
                    }

                    glyfWriter.WritePaddingToFourBytes();
                }
            }

            newGlyphId += 1;
        }

        offsets[offsets.Length - 1] = glyfWriter.GetLength();
        BigEndianWriter locaWriter = BigEndianWriter.Create(
            offsets.Length * 4,
            limits.GetMaxOutputBytes());
        int index = 0;

        while (index < offsets.Length)
        {
            locaWriter.WriteOffset32(offsets[index]);
            index += 1;
        }

        return new SfntInstancedGlyfData(
            glyfWriter.CopyBytes(),
            locaWriter.CopyBytes(),
            offsets,
            bounds,
            hasOutlines,
            maxima);
    }

    static void WriteSimpleGlyph(
        BigEndianWriter writer,
        SfntSimpleGlyph source,
        SfntResolvedGlyph varied,
        int[] bounds,
        bool retainInstructions)
    {
        if (varied.GetControlPointCount() != source.GetPointCount())
        {
            throw FontFormatException.Create(
                "An instanced simple glyph changed its control-point topology.");
        }

        int[] xs = new int[source.GetPointCount()];
        int[] ys = new int[source.GetPointCount()];
        FillRoundedCoordinates(varied, xs, ys);
        writer.WriteInt16(CheckedShort(source.GetContourCount(),
            "A simple glyph contour count exceeds the Int16 range."));
        WriteBounds(writer, bounds);
        int contourIndex = 0;

        while (contourIndex < source.GetContourCount())
        {
            writer.WriteUInt16(CheckedUShort(
                source.GetContourEndPoint(contourIndex),
                "A simple glyph contour endpoint exceeds the UInt16 range."));
            contourIndex += 1;
        }

        byte[] instructions = retainInstructions ?
            source.CopyInstructions() : new byte[0];
        writer.WriteUInt16(CheckedUShort(
            instructions.Length,
            "A simple glyph instruction program is too long."));
        writer.WriteBytes(instructions);
        int pointIndex = 0;

        while (pointIndex < source.GetPointCount())
        {
            int flags = source.GetPoint(pointIndex).IsOnCurve() ? 1 : 0;

            if (pointIndex == 0 && source.HasOverlappingContours())
            {
                flags |= 0x40;
            }

            writer.WriteUInt8((byte)flags);
            pointIndex += 1;
        }

        WriteCoordinateDeltas(writer, xs);
        WriteCoordinateDeltas(writer, ys);
    }

    static void WriteCompositeGlyph(
        BigEndianWriter writer,
        SfntCompositeGlyph source,
        double[] effectiveXOffsets,
        double[] effectiveYOffsets,
        int[] bounds,
        int[] oldToNewGlyphIds,
        bool retainInstructions,
        bool retainMetricsFlags)
    {
        writer.WriteInt16(-1);
        WriteBounds(writer, bounds);
        int componentIndex = 0;

        while (componentIndex < source.GetComponentCount())
        {
            SfntCompositeComponent component = source.GetComponent(componentIndex);
            int flags = component.GetFlags();
            flags &= ~(MoreComponents |
                WeHaveInstructions |
                ScaledComponentOffset);

            if (!retainMetricsFlags)
            {
                flags &= ~UseMyMetrics;
            }

            flags |= ArgumentsAreWords |
                ArgumentsAreXYValues |
                UnscaledComponentOffset;

            if (componentIndex + 1 < source.GetComponentCount())
            {
                flags |= MoreComponents;
            }
            else if (retainInstructions &&
                source.GetInstructionLength() != 0)
            {
                flags |= WeHaveInstructions;
            }

            writer.WriteUInt16((ushort)flags);
            int oldComponentGlyphId = component.GetGlyphId();
            int newComponentGlyphId = oldToNewGlyphIds[
                oldComponentGlyphId];

            if (newComponentGlyphId < 0)
            {
                throw FontFormatException.Create(
                    "A retained composite component has no output glyph ID.");
            }

            writer.WriteUInt16(CheckedUShort(
                newComponentGlyphId,
                "A composite component glyph ID exceeds the UInt16 range."));
            writer.WriteInt16(RoundCoordinate(
                effectiveXOffsets[componentIndex]));
            writer.WriteInt16(RoundCoordinate(
                effectiveYOffsets[componentIndex]));
            WriteTransform(writer, component.GetTransform());
            componentIndex += 1;
        }

        if (retainInstructions && source.GetInstructionLength() != 0)
        {
            byte[] instructions = source.CopyInstructions();
            writer.WriteUInt16(CheckedUShort(
                instructions.Length,
                "A composite glyph instruction program is too long."));
            writer.WriteBytes(instructions);
        }
    }

    static double[][] CopyVariedComponentOffsets(
        SfntVariedGlyph varied,
        SfntCompositeGlyph composite)
    {
        double[] xs = new double[composite.GetComponentCount()];
        double[] ys = new double[composite.GetComponentCount()];
        int componentIndex = 0;

        while (componentIndex < composite.GetComponentCount())
        {
            xs[componentIndex] = varied.GetEffectiveComponentXOffset(
                componentIndex);
            ys[componentIndex] = varied.GetEffectiveComponentYOffset(
                componentIndex);
            componentIndex += 1;
        }

        return new double[][] { xs, ys };
    }

    static double[][] CalculateDefaultComponentOffsets(
        SfntLocaTable loca,
        SfntCompositeGlyph composite)
    {
        double[] xs = new double[composite.GetComponentCount()];
        double[] ys = new double[composite.GetComponentCount()];
        System.Collections.Generic.List<SfntResolvedPoint> parentPoints =
            new System.Collections.Generic.List<SfntResolvedPoint>();
        int componentIndex = 0;

        while (componentIndex < composite.GetComponentCount())
        {
            SfntCompositeComponent component = composite.GetComponent(
                componentIndex);
            SfntResolvedGlyph child = loca.GetResolvedGlyph(
                component.GetGlyphId());
            SfntResolvedPoint[] transformed = TransformControlPoints(
                child,
                component.GetTransform());

            if (component.GetPlacementKind() ==
                SfntCompositePlacementKind.Offset)
            {
                xs[componentIndex] = component.GetEffectiveXOffset();
                ys[componentIndex] = component.GetEffectiveYOffset();

                if (component.RoundsOffsetToGrid())
                {
                    xs[componentIndex] = RoundToGrid(xs[componentIndex]);
                    ys[componentIndex] = RoundToGrid(ys[componentIndex]);
                }
            }
            else
            {
                int parentIndex = component.GetParentPointIndex();
                int childIndex = component.GetChildPointIndex();

                if (parentIndex >= parentPoints.Count ||
                    childIndex >= transformed.Length)
                {
                    throw FontFormatException.Create(
                        "A composite point attachment is outside the resolved control points.");
                }

                xs[componentIndex] = parentPoints[parentIndex].GetX() -
                    transformed[childIndex].GetX();
                ys[componentIndex] = parentPoints[parentIndex].GetY() -
                    transformed[childIndex].GetY();
            }

            AppendControlPoints(
                parentPoints,
                transformed,
                xs[componentIndex],
                ys[componentIndex]);
            componentIndex += 1;
        }

        return new double[][] { xs, ys };
    }

    static SfntResolvedPoint[] TransformControlPoints(
        SfntResolvedGlyph child,
        SfntCompositeTransform transform)
    {
        SfntResolvedPoint[] points =
            new SfntResolvedPoint[child.GetControlPointCount()];
        int pointIndex = 0;

        while (pointIndex < points.Length)
        {
            SfntResolvedPoint point = child.GetControlPoint(pointIndex);
            points[pointIndex] = new SfntResolvedPoint(
                transform.TransformX(point.GetX(), point.GetY()),
                transform.TransformY(point.GetX(), point.GetY()),
                point.IsOnCurve());
            pointIndex += 1;
        }

        return points;
    }

    static void AppendControlPoints(
        System.Collections.Generic.List<SfntResolvedPoint> destination,
        SfntResolvedPoint[] source,
        double offsetX,
        double offsetY)
    {
        int pointIndex = 0;

        while (pointIndex < source.Length)
        {
            SfntResolvedPoint point = source[pointIndex];
            destination.Add(new SfntResolvedPoint(
                point.GetX() + offsetX,
                point.GetY() + offsetY,
                point.IsOnCurve()));
            pointIndex += 1;
        }
    }

    static double RoundToGrid(double value)
    {
        return System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);
    }

    static void WriteTransform(
        BigEndianWriter writer,
        SfntCompositeTransform transform)
    {
        SfntCompositeTransformKind kind = transform.GetKind();

        if (kind == SfntCompositeTransformKind.UniformScale)
        {
            writer.WriteF2Dot14(transform.GetXScale());
        }
        else if (kind == SfntCompositeTransformKind.SeparateScale)
        {
            writer.WriteF2Dot14(transform.GetXScale());
            writer.WriteF2Dot14(transform.GetYScale());
        }
        else if (kind == SfntCompositeTransformKind.TwoByTwo)
        {
            writer.WriteF2Dot14(transform.GetXScale());
            writer.WriteF2Dot14(transform.GetScale01());
            writer.WriteF2Dot14(transform.GetScale10());
            writer.WriteF2Dot14(transform.GetYScale());
        }
    }

    static void FillRoundedCoordinates(
        SfntResolvedGlyph glyph,
        int[] xs,
        int[] ys)
    {
        int pointIndex = 0;

        while (pointIndex < glyph.GetControlPointCount())
        {
            SfntResolvedPoint point = glyph.GetControlPoint(pointIndex);
            xs[pointIndex] = RoundCoordinate(point.GetX());
            ys[pointIndex] = RoundCoordinate(point.GetY());
            pointIndex += 1;
        }
    }

    static int[] CalculateBounds(int[] xs, int[] ys)
    {
        if (xs.Length == 0)
        {
            return new int[] { 0, 0, 0, 0 };
        }

        int xMinimum = xs[0];
        int yMinimum = ys[0];
        int xMaximum = xs[0];
        int yMaximum = ys[0];
        int index = 1;

        while (index < xs.Length)
        {
            xMinimum = System.Math.Min(xMinimum, xs[index]);
            yMinimum = System.Math.Min(yMinimum, ys[index]);
            xMaximum = System.Math.Max(xMaximum, xs[index]);
            yMaximum = System.Math.Max(yMaximum, ys[index]);
            index += 1;
        }

        return new int[] { xMinimum, yMinimum, xMaximum, yMaximum };
    }

    static int[] CalculateBounds(SfntResolvedGlyph glyph)
    {
        int[] xs = new int[glyph.GetControlPointCount()];
        int[] ys = new int[glyph.GetControlPointCount()];
        FillRoundedCoordinates(glyph, xs, ys);
        return CalculateBounds(xs, ys);
    }

    static void WriteBounds(BigEndianWriter writer, int[] bounds)
    {
        writer.WriteInt16(CheckedShort(bounds[0],
            "An instanced glyph x minimum exceeds the FWORD range."));
        writer.WriteInt16(CheckedShort(bounds[1],
            "An instanced glyph y minimum exceeds the FWORD range."));
        writer.WriteInt16(CheckedShort(bounds[2],
            "An instanced glyph x maximum exceeds the FWORD range."));
        writer.WriteInt16(CheckedShort(bounds[3],
            "An instanced glyph y maximum exceeds the FWORD range."));
    }

    static void WriteCoordinateDeltas(BigEndianWriter writer, int[] values)
    {
        int previous = 0;
        int index = 0;

        while (index < values.Length)
        {
            int delta = values[index] - previous;
            writer.WriteInt16(CheckedShort(delta,
                "An instanced glyph coordinate delta exceeds the Int16 range."));
            previous = values[index];
            index += 1;
        }
    }

    static SfntInstancedMetricData CreateMetricData(
        FontInstance instance,
        FontFace face,
        SfntInstancedGlyfData glyphData,
        int[] newToOldGlyphIds,
        bool vertical,
        FontLimits limits)
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

            if (oldGlyphId >= 0)
            {
                GlyphMetrics metrics = instance.GetGlyphMetrics(oldGlyphId);
                advances[glyphId] = vertical ?
                    metrics.GetAdvanceHeight() : metrics.GetAdvanceWidth();
                bearings[glyphId] = vertical ?
                    metrics.GetTopSideBearing() :
                    metrics.GetLeftSideBearing();
            }

            ValidateMetric(advances[glyphId], bearings[glyphId]);
            int[] bounds = glyphData.GetBounds(glyphId);
            int extent = vertical ?
                bounds[3] - bounds[1] : bounds[2] - bounds[0];
            int trailingBearing = advances[glyphId] -
                bearings[glyphId] - extent;
            int metricExtent = bearings[glyphId] + extent;
            ValidateSignedMetricSummary(trailingBearing, metricExtent);
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
            glyphId += 1;
        }

        int metricCount = glyphCount;

        while (metricCount > 1 &&
            advances[metricCount - 1] == advances[metricCount - 2])
        {
            metricCount -= 1;
        }

        BigEndianWriter writer = BigEndianWriter.Create(
            limits.GetMaxOutputBytes());
        glyphId = 0;

        while (glyphId < metricCount)
        {
            writer.WriteUInt16((ushort)advances[glyphId]);
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId += 1;
        }

        while (glyphId < glyphCount)
        {
            writer.WriteInt16((short)bearings[glyphId]);
            glyphId += 1;
        }

        int headerTag = vertical ? FontTags.Vhea : FontTags.Hhea;
        SfntTableData? headerTable = face.GetTableStore().FindTable(headerTag);

        if (headerTable == null && vertical)
        {
            headerTable = face.GetTableStore().FindTable(FontTags.Hhea);
        }

        if (headerTable == null)
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires a metric header.");
        }

        byte[] header = headerTable.GetDataSlice().CopyBytes();

        if (header.Length < 36)
        {
            throw FontFormatException.Create(
                "An instanced metric header is truncated.");
        }

        WriteUInt16(header, 10, maximumAdvance);
        WriteInt16(header, 12, minimumLeadingBearing);
        WriteInt16(header, 14, minimumTrailingBearing);
        WriteInt16(header, 16, maximumExtent);
        WriteUInt16(header, 34, metricCount);
        return new SfntInstancedMetricData(writer.CopyBytes(), header);
    }

    static void ValidateSignedMetricSummary(int bearing, int extent)
    {
        if (bearing < short.MinValue || bearing > short.MaxValue ||
            extent < short.MinValue || extent > short.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced metric summary exceeds the Int16 range.");
        }
    }

    static void ValidateMetric(int advance, int bearing)
    {
        if (advance < 0 || advance > ushort.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced glyph advance exceeds the UInt16 range.");
        }

        if (bearing < short.MinValue || bearing > short.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced glyph bearing exceeds the Int16 range.");
        }
    }

    static byte[] CreateHead(FontFace face)
    {
        SfntTableData? headTable = face.GetTableStore().FindTable(FontTags.Head);
        System.ArgumentNullException.ThrowIfNull(headTable);
        byte[] head = headTable
            .GetDataSlice().CopyBytes();

        if (head.Length < 54)
        {
            throw FontFormatException.Create(
                "Static TrueType instancing requires a complete head table.");
        }

        WriteUInt16(head, 50, 1);
        return head;
    }

    static SfntTableStore CreateTableStore(
        FontFace face,
        byte[] head,
        byte[] maxp,
        SfntInstancedGlyfData glyphData,
        SfntInstancedMetricData horizontal,
        SfntInstancedMetricData vertical,
        byte[]? controlValues,
        SfntStaticInstanceMetadata metadata)
    {
        SfntTableStore source = face.GetTableStore();
        System.Collections.Generic.List<SfntTableData> tables =
            new System.Collections.Generic.List<SfntTableData>();
        int index = 0;

        while (index < source.GetTableCount())
        {
            SfntTableData table = source.GetTable(index);
            int tag = table.GetTag();

            if (!ShouldDrop(tag))
            {
                byte[] data = SelectTableData(
                    table,
                    head,
                    maxp,
                    glyphData,
                    horizontal,
                    vertical,
                    controlValues);
                data = metadata.PatchTable(tag, data);
                FontData owned = FontData.Create(data);
                tables.Add(new SfntTableData(
                    tag,
                    FontChecksum.CalculateTable(data),
                    owned.GetSlice(0, owned.GetLength()),
                    table.GetClassification(),
                    true));
            }

            index += 1;
        }

        if (vertical != null && !source.HasTable(FontTags.Vhea))
        {
            AddTable(tables, FontTags.Vhea, vertical.GetHeader());
        }

        if (vertical != null && !source.HasTable(FontTags.Vmtx))
        {
            AddTable(tables, FontTags.Vmtx, vertical.GetMetrics());
        }

        SfntTableData[] ordered = tables.ToArray();
        SortTables(ordered);
        return new SfntTableStore(
            ordered,
            Array.Empty<FontDiagnostic>());
    }

    static void AddTable(
        System.Collections.Generic.List<SfntTableData> tables,
        int tag,
        byte[] data)
    {
        FontData owned = FontData.Create(data);
        tables.Add(new SfntTableData(
            tag,
            FontChecksum.CalculateTable(data),
            owned.GetSlice(0, owned.GetLength()),
            SfntTableClassification.Known,
            true));
    }

    static void SortTables(SfntTableData[] tables)
    {
        int index = 1;

        while (index < tables.Length)
        {
            SfntTableData current = tables[index];
            uint currentTag = CheckedFontMath.ReinterpretInt32AsUInt32(
                current.GetTag());
            int insertionIndex = index;

            while (insertionIndex > 0 &&
                CheckedFontMath.ReinterpretInt32AsUInt32(
                    tables[insertionIndex - 1].GetTag()) > currentTag)
            {
                tables[insertionIndex] = tables[insertionIndex - 1];
                insertionIndex -= 1;
            }

            tables[insertionIndex] = current;
            index += 1;
        }
    }

    static byte[] SelectTableData(
        SfntTableData table,
        byte[] head,
        byte[] maxp,
        SfntInstancedGlyfData glyphData,
        SfntInstancedMetricData horizontal,
        SfntInstancedMetricData vertical,
        byte[]? controlValues)
    {
        int tag = table.GetTag();

        if (tag == FontTags.Head) { return head; }
        if (tag == FontTags.Maxp) { return maxp; }
        if (tag == FontTags.Glyf) { return glyphData.GetGlyf(); }
        if (tag == FontTags.Loca) { return glyphData.GetLoca(); }
        if (tag == FontTags.Hhea) { return horizontal.GetHeader(); }
        if (tag == FontTags.Hmtx) { return horizontal.GetMetrics(); }
        if (tag == FontTags.Vhea && vertical != null) { return vertical.GetHeader(); }
        if (tag == FontTags.Vmtx && vertical != null) { return vertical.GetMetrics(); }
        if (tag == FontTags.Cvt && controlValues != null) { return controlValues; }
        return table.GetDataSlice().CopyBytes();
    }

    static bool ShouldDrop(int tag)
    {
        return tag == FontTags.Avar ||
            tag == FontTags.Cvar ||
            tag == FontTags.Dsig ||
            tag == FontTags.Fvar ||
            tag == FontTags.Gvar ||
            tag == FontTags.Hvar ||
            tag == FontTags.Mvar ||
            tag == FontTags.Stat ||
            tag == FontTags.Vvar;
    }

    static short RoundCoordinate(double value)
    {
        if (!double.IsFinite(value))
        {
            throw FontFormatException.Create(
                "An instanced glyph coordinate is not finite.");
        }

        double rounded = System.Math.Round(
            value,
            System.MidpointRounding.AwayFromZero);

        if (rounded < short.MinValue || rounded > short.MaxValue)
        {
            throw FontFormatException.Create(
                "An instanced glyph coordinate exceeds the FWORD range.");
        }

        return (short)rounded;
    }

    static short CheckedShort(int value, string message)
    {
        if (value < short.MinValue || value > short.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return (short)value;
    }

    static ushort CheckedUShort(int value, string message)
    {
        if (value < 0 || value > ushort.MaxValue)
        {
            throw FontFormatException.Create(message);
        }

        return (ushort)value;
    }

    static void WriteUInt16(byte[] data, int offset, int value)
    {
        ushort encoded = CheckedUShort(
            value,
            "An instanced UInt16 field is outside its supported range.");
        data[offset] = (byte)((encoded >> 8) & 0xFF);
        data[offset + 1] = (byte)(encoded & 0xFF);
    }

    static void WriteInt16(byte[] data, int offset, int value)
    {
        short encoded = CheckedShort(
            value,
            "An instanced Int16 field is outside its supported range.");
        int encodedBits = encoded < 0 ? encoded + 65536 : encoded;
        data[offset] = (byte)((encodedBits >> 8) & 0xFF);
        data[offset + 1] = (byte)(encodedBits & 0xFF);
    }
}
