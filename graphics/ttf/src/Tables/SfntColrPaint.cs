namespace ttf;

internal enum SfntColrExtendMode
{
    Pad = 0,
    Repeat = 1,
    Reflect = 2
}

internal enum SfntColrCompositeMode
{
    Clear = 0,
    Source = 1,
    Destination = 2,
    SourceOver = 3,
    DestinationOver = 4,
    SourceIn = 5,
    DestinationIn = 6,
    SourceOut = 7,
    DestinationOut = 8,
    SourceAtop = 9,
    DestinationAtop = 10,
    Xor = 11,
    Plus = 12,
    Screen = 13,
    Overlay = 14,
    Darken = 15,
    Lighten = 16,
    ColorDodge = 17,
    ColorBurn = 18,
    HardLight = 19,
    SoftLight = 20,
    Difference = 21,
    Exclusion = 22,
    Multiply = 23,
    Hue = 24,
    Saturation = 25,
    Color = 26,
    Luminosity = 27
}

internal class SfntColrVariationData
{
    SfntDeltaSetIndexMap? indexMap;
    SfntItemVariationStore? store;

    internal SfntColrVariationData(
        SfntDeltaSetIndexMap? indexMap,
        SfntItemVariationStore? store)
    {
        this.indexMap = indexMap;
        this.store = store;
    }

    internal bool HasStore()
    {
        return store != null;
    }

    internal SfntDeltaSetIndexMap? GetIndexMap()
    {
        return indexMap;
    }

    internal SfntItemVariationStore? GetStore()
    {
        return store;
    }

    internal SfntVariationLocation CreateLocation(double[] coordinates)
    {
        if (store == null)
        {
            throw new System.InvalidOperationException(
                "This COLR table has no variation store.");
        }

        return store.CreateLocation(coordinates);
    }

    internal void ValidateIndex(long index)
    {
        if (index < 0 || index > uint.MaxValue)
        {
            throw FontFormatException.Create(
                "A COLR variation index overflows its 32-bit sequence.");
        }

        if (store == null)
        {
            throw FontFormatException.Create(
                "A variable COLR structure requires an ItemVariationStore.");
        }

        store.ValidateDeltaSetIndex(ResolveIndex(index));
    }

    internal double CalculateDelta(
        long index,
        SfntVariationLocation location)
    {
        if (location == null)
        {
            throw new System.ArgumentNullException("location");
        }

        ValidateIndex(index);
        return location.CalculateDelta(ResolveIndex(index));
    }

    SfntDeltaSetIndex ResolveIndex(long index)
    {
        if (indexMap == null)
        {
            return new SfntDeltaSetIndex(
                (int)(index >> 16),
                (int)(index & 0xFFFF));
        }

        return indexMap.Resolve(index);
    }
}

internal class SfntColrVariableValue
{
    long rawValue;
    double divisor;
    double minimum;
    double maximum;
    bool hasMinimum;
    bool hasMaximum;
    long variationIndex;
    SfntColrVariationData variationData;

    internal SfntColrVariableValue(
        long rawValue,
        double divisor,
        double minimum,
        double maximum,
        bool hasMinimum,
        bool hasMaximum,
        long variationIndex,
        SfntColrVariationData variationData)
    {
        this.rawValue = rawValue;
        this.divisor = divisor;
        this.minimum = minimum;
        this.maximum = maximum;
        this.hasMinimum = hasMinimum;
        this.hasMaximum = hasMaximum;
        this.variationIndex = variationIndex;
        this.variationData = variationData;

        if (variationIndex >= 0)
        {
            variationData.ValidateIndex(variationIndex);
        }
    }

    internal long GetRawValue()
    {
        return rawValue;
    }

    internal bool IsVariable()
    {
        return variationIndex >= 0;
    }

    internal long GetVariationIndex()
    {
        if (variationIndex < 0)
        {
            throw new System.InvalidOperationException(
                "This COLR value is not variable.");
        }

        return variationIndex;
    }

    internal double GetValue()
    {
        return Clamp(rawValue / divisor);
    }

    internal double GetValueAtLocation(SfntVariationLocation location)
    {
        double variedRawValue = rawValue;

        if (variationIndex >= 0)
        {
            variedRawValue += variationData.CalculateDelta(
                variationIndex,
                location);
        }

        return Clamp(variedRawValue / divisor);
    }

    double Clamp(double value)
    {
        if (hasMinimum && value < minimum)
        {
            return minimum;
        }

        if (hasMaximum && value > maximum)
        {
            return maximum;
        }

        return value;
    }
}

internal class SfntColrColorStop
{
    int paletteEntryIndex;
    SfntColrVariableValue stopOffset;
    SfntColrVariableValue alpha;
    long variationIndexBase;

    internal SfntColrColorStop(
        int paletteEntryIndex,
        SfntColrVariableValue stopOffset,
        SfntColrVariableValue alpha,
        long variationIndexBase)
    {
        this.paletteEntryIndex = paletteEntryIndex;
        this.stopOffset = stopOffset;
        this.alpha = alpha;
        this.variationIndexBase = variationIndexBase;
    }

    internal int GetPaletteEntryIndex()
    {
        return paletteEntryIndex;
    }

    internal bool UsesForegroundColor()
    {
        return paletteEntryIndex == 0xFFFF;
    }

    internal double GetStopOffset()
    {
        return stopOffset.GetValue();
    }

    internal double GetStopOffsetAtLocation(SfntVariationLocation location)
    {
        return stopOffset.GetValueAtLocation(location);
    }

    internal double GetAlpha()
    {
        return alpha.GetValue();
    }

    internal double GetAlphaAtLocation(SfntVariationLocation location)
    {
        return alpha.GetValueAtLocation(location);
    }

    internal bool IsVariable()
    {
        return variationIndexBase >= 0;
    }

    internal long GetVariationIndexBase()
    {
        if (!IsVariable())
        {
            throw new System.InvalidOperationException(
                "This COLR color stop is not variable.");
        }

        return variationIndexBase;
    }
}

internal class SfntColrColorLine
{
    int extendRaw;
    SfntColrExtendMode extendMode;
    bool variable;
    SfntColrColorStop[] stops;

    internal SfntColrColorLine(
        int extendRaw,
        SfntColrExtendMode extendMode,
        bool variable,
        SfntColrColorStop[] stops)
    {
        this.extendRaw = extendRaw;
        this.extendMode = extendMode;
        this.variable = variable;
        this.stops = stops;
    }

    internal int GetExtendRaw()
    {
        return extendRaw;
    }

    internal SfntColrExtendMode GetExtendMode()
    {
        return extendMode;
    }

    internal bool IsVariable()
    {
        return variable;
    }

    internal int GetStopCount()
    {
        return stops.Length;
    }

    internal SfntColrColorStop GetStop(int index)
    {
        if (index < 0 || index >= stops.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return stops[index];
    }
}

internal class SfntColrAffineTransform
{
    bool variable;
    long variationIndexBase;
    SfntColrVariableValue[] values;

    internal SfntColrAffineTransform(
        bool variable,
        long variationIndexBase,
        SfntColrVariableValue[] values)
    {
        this.variable = variable;
        this.variationIndexBase = variationIndexBase;
        this.values = values;
    }

    internal bool IsVariable()
    {
        return variable;
    }

    internal long GetVariationIndexBase()
    {
        if (!variable)
        {
            throw new System.InvalidOperationException(
                "This COLR affine transform is not variable.");
        }

        return variationIndexBase;
    }

    internal double GetValue(int index)
    {
        return GetVariableValue(index).GetValue();
    }

    internal double GetValueAtLocation(
        int index,
        SfntVariationLocation location)
    {
        return GetVariableValue(index).GetValueAtLocation(location);
    }

    SfntColrVariableValue GetVariableValue(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return values[index];
    }
}

internal class SfntColrClipBox
{
    int format;
    bool supported;
    long variationIndexBase;
    SfntColrVariableValue[] coordinates;

    internal SfntColrClipBox(
        int format,
        bool supported,
        long variationIndexBase,
        SfntColrVariableValue[] coordinates)
    {
        this.format = format;
        this.supported = supported;
        this.variationIndexBase = variationIndexBase;
        this.coordinates = coordinates;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal bool IsSupported()
    {
        return supported;
    }

    internal bool IsVariable()
    {
        return format == 2;
    }

    internal long GetVariationIndexBase()
    {
        if (!IsVariable())
        {
            throw new System.InvalidOperationException(
                "This COLR clip box is not variable.");
        }

        return variationIndexBase;
    }

    internal double GetCoordinate(int index)
    {
        return GetCoordinateValue(index).GetValue();
    }

    internal double GetCoordinateAtLocation(
        int index,
        SfntVariationLocation location)
    {
        return GetCoordinateValue(index).GetValueAtLocation(location);
    }

    SfntColrVariableValue GetCoordinateValue(int index)
    {
        if (!supported || index < 0 || index >= coordinates.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return coordinates[index];
    }
}

internal class SfntColrClipRecord
{
    int startGlyphId;
    int endGlyphId;
    int clipBoxOffset;
    SfntColrClipBox clipBox;

    internal SfntColrClipRecord(
        int startGlyphId,
        int endGlyphId,
        int clipBoxOffset,
        SfntColrClipBox clipBox)
    {
        this.startGlyphId = startGlyphId;
        this.endGlyphId = endGlyphId;
        this.clipBoxOffset = clipBoxOffset;
        this.clipBox = clipBox;
    }

    internal int GetStartGlyphId()
    {
        return startGlyphId;
    }

    internal int GetEndGlyphId()
    {
        return endGlyphId;
    }

    internal int GetClipBoxOffset()
    {
        return clipBoxOffset;
    }

    internal SfntColrClipBox GetClipBox()
    {
        return clipBox;
    }
}

internal class SfntColrClipList
{
    int format;
    SfntColrClipRecord[] records;

    internal SfntColrClipList(int format, SfntColrClipRecord[] records)
    {
        this.format = format;
        this.records = records;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal int GetRecordCount()
    {
        return records.Length;
    }

    internal SfntColrClipRecord GetRecord(int index)
    {
        if (index < 0 || index >= records.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return records[index];
    }

    internal SfntColrClipBox? FindClipBox(int glyphId)
    {
        int low = 0;
        int high = records.Length - 1;

        while (low <= high)
        {
            int middle = low + (high - low) / 2;
            SfntColrClipRecord record = records[middle];

            if (glyphId < record.GetStartGlyphId())
            {
                high = middle - 1;
            }
            else if (glyphId > record.GetEndGlyphId())
            {
                low = middle + 1;
            }
            else
            {
                return record.GetClipBox();
            }
        }

        return null;
    }
}

internal class SfntColrPaint
{
    int offset;
    int format;
    bool supported;
    bool variable;
    int firstLayerIndex;
    int layerCount;
    int glyphId;
    int paletteEntryIndex;
    long variationIndexBase;
    int compositeModeRaw;
    SfntColrCompositeMode compositeMode;
    int[] childPaintOffsets;
    SfntColrVariableValue[] values;
    SfntColrColorLine? colorLine;
    SfntColrAffineTransform? affineTransform;

    internal SfntColrPaint(
        int offset,
        int format,
        bool supported,
        bool variable,
        int firstLayerIndex,
        int layerCount,
        int glyphId,
        int paletteEntryIndex,
        long variationIndexBase,
        int compositeModeRaw,
        SfntColrCompositeMode compositeMode,
        int[] childPaintOffsets,
        SfntColrVariableValue[] values,
        SfntColrColorLine? colorLine,
        SfntColrAffineTransform? affineTransform)
    {
        this.offset = offset;
        this.format = format;
        this.supported = supported;
        this.variable = variable;
        this.firstLayerIndex = firstLayerIndex;
        this.layerCount = layerCount;
        this.glyphId = glyphId;
        this.paletteEntryIndex = paletteEntryIndex;
        this.variationIndexBase = variationIndexBase;
        this.compositeModeRaw = compositeModeRaw;
        this.compositeMode = compositeMode;
        this.childPaintOffsets = childPaintOffsets;
        this.values = values;
        this.colorLine = colorLine;
        this.affineTransform = affineTransform;
    }

    internal int GetOffset()
    {
        return offset;
    }

    internal int GetFormat()
    {
        return format;
    }

    internal bool IsSupported()
    {
        return supported;
    }

    internal bool IsVariable()
    {
        return variable;
    }

    internal int GetFirstLayerIndex()
    {
        return firstLayerIndex;
    }

    internal int GetLayerCount()
    {
        return layerCount;
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
        return paletteEntryIndex == 0xFFFF;
    }

    internal long GetVariationIndexBase()
    {
        if (!variable)
        {
            throw new System.InvalidOperationException(
                "This COLR paint is not variable.");
        }

        return variationIndexBase;
    }

    internal int GetCompositeModeRaw()
    {
        return compositeModeRaw;
    }

    internal SfntColrCompositeMode GetCompositeMode()
    {
        return compositeMode;
    }

    internal int GetChildPaintOffsetCount()
    {
        return childPaintOffsets.Length;
    }

    internal int GetChildPaintOffset(int index)
    {
        if (index < 0 || index >= childPaintOffsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return childPaintOffsets[index];
    }

    internal int GetValueCount()
    {
        return values.Length;
    }

    internal double GetValue(int index)
    {
        return GetVariableValue(index).GetValue();
    }

    internal double GetValueAtLocation(
        int index,
        SfntVariationLocation location)
    {
        return GetVariableValue(index).GetValueAtLocation(location);
    }

    internal SfntColrColorLine? GetColorLine()
    {
        return colorLine;
    }

    internal SfntColrAffineTransform? GetAffineTransform()
    {
        return affineTransform;
    }

    SfntColrVariableValue GetVariableValue(int index)
    {
        if (index < 0 || index >= values.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return values[index];
    }
}

internal class SfntColrBaseGlyphPaintRecord
{
    int glyphId;
    int paintOffset;
    SfntColrPaint paint;

    internal SfntColrBaseGlyphPaintRecord(
        int glyphId,
        int paintOffset,
        SfntColrPaint paint)
    {
        this.glyphId = glyphId;
        this.paintOffset = paintOffset;
        this.paint = paint;
    }

    internal int GetGlyphId()
    {
        return glyphId;
    }

    internal int GetPaintOffset()
    {
        return paintOffset;
    }

    internal SfntColrPaint GetPaint()
    {
        return paint;
    }
}

internal class SfntColrLayerList
{
    int offset;
    SfntColrPaint[] paints;

    internal SfntColrLayerList(int offset, SfntColrPaint[] paints)
    {
        this.offset = offset;
        this.paints = paints;
    }

    internal int GetOffset()
    {
        return offset;
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
}
