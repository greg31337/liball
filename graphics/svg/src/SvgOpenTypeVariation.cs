namespace svg;

internal class SvgItemVariationMetrics
{
    internal double Advance;
    internal double Bearing;

    internal SvgItemVariationMetrics(double advance, double bearing)
    {
        Advance = advance;
        Bearing = bearing;
    }
}

internal class SvgVariationIndex
{
    internal int Outer;
    internal int Inner;

    internal SvgVariationIndex(int outer, int inner)
    {
        Outer = outer;
        Inner = inner;
    }
}

internal class SvgVariationDelta
{
    internal double Value;

    internal SvgVariationDelta(double value)
    {
        Value = value;
    }
}

internal enum SvgGvarApplyResult
{
    Success = 0,
    InvalidFont = 1,
    LimitExceeded = 2,
    CallbackRequired = 3
}

internal class SvgGvarTuple
{
    internal int DataSize;
    internal int Flags;
    internal double[] Peak;
    internal double[] Start;
    internal double[] End;

    internal SvgGvarTuple(int axes)
    {
        DataSize = 0;
        Flags = 0;
        Peak = new double[axes];
        Start = new double[axes];
        End = new double[axes];
    }
}

internal class SvgItemVariationEvaluator
{
    internal SvgItemVariationMetrics? EvaluateMetrics(SvgFont font, SvgOpenTypeTable table, int glyphIndex, double[] coordinates)
    {
        byte[]? bytes = font.Bytes;
        if (bytes == null || table.Offset < 0 || table.Length < 20 || (long)table.Offset + table.Length > bytes.Length) return null;
        int start = table.Offset;
        if (UInt16(bytes, start) != 1 || UInt16(bytes, start + 2) != 0) return null;
        uint storeValue = UInt32(bytes, start + 4);
        uint advanceMapValue = UInt32(bytes, start + 8);
        uint bearingMapValue = UInt32(bytes, start + 12);
        if (storeValue > System.Int32.MaxValue || advanceMapValue > System.Int32.MaxValue || bearingMapValue > System.Int32.MaxValue) return null;
        int store = start + (int)storeValue;
        SvgVariationIndex? advanceIndex = Map(bytes, start, table.Length, (int)advanceMapValue, glyphIndex);
        SvgVariationIndex? bearingIndex = bearingMapValue == 0 ? null : Map(bytes, start, table.Length, (int)bearingMapValue, glyphIndex);
        if (advanceIndex == null) return null;
        SvgVariationDelta? advance = Delta(bytes, start, table.Length, store, advanceIndex, coordinates);
        SvgVariationDelta? bearing = bearingIndex == null ? new SvgVariationDelta(0) : Delta(bytes, start, table.Length, store, bearingIndex, coordinates);
        if (advance == null || bearing == null) return null;
        return new SvgItemVariationMetrics(advance.Value, bearing.Value);
    }

    internal double[]? ScalarsFromStore(byte[] bytes, int store, int end, int outer, System.Collections.Generic.List<double> coordinates)
    {
        if (store < 0 || end > bytes.Length || store + 8 > end || UInt16(bytes, store) != 1) return null;
        uint regionOffsetValue = UInt32(bytes, store + 2);
        int dataCount = UInt16(bytes, store + 6);
        if (regionOffsetValue > System.Int32.MaxValue || outer < 0 || outer >= dataCount || store + 8L + dataCount * 4 > end) return null;
        uint dataOffsetValue = UInt32(bytes, store + 8 + outer * 4);
        if (dataOffsetValue > System.Int32.MaxValue) return null;
        int data = store + (int)dataOffsetValue;
        int regions = store + (int)regionOffsetValue;
        if (data < store || data + 6 > end || regions < store || regions + 4 > end) return null;
        int regionIndexCount = UInt16(bytes, data + 4);
        int axisCount = UInt16(bytes, regions);
        int regionCount = UInt16(bytes, regions + 2);
        if (axisCount != coordinates.Count || data + 6L + regionIndexCount * 2 > end || regions + 4L + (long)axisCount * regionCount * 6 > end) return null;
        double[] values = new double[coordinates.Count];
        int axis = 0;
        while (axis < coordinates.Count)
        {
            values[axis] = coordinates[axis];
            axis = axis + 1;
        }
        double[] result = new double[regionIndexCount];
        int index = 0;
        while (index < regionIndexCount)
        {
            int selected = UInt16(bytes, data + 6 + index * 2);
            if (selected >= regionCount) return null;
            result[index] = RegionScalar(bytes, regions + 4 + selected * axisCount * 6, values);
            if (!SvgFiniteMath.IsFinite(result[index])) return null;
            index = index + 1;
        }
        return result;
    }

    SvgVariationIndex? Map(byte[] bytes, int tableStart, int tableLength, int relative, int glyphIndex)
    {
        if (relative == 0) return new SvgVariationIndex(0, glyphIndex);
        int start = tableStart + relative;
        int end = tableStart + tableLength;
        if (start < tableStart || start + 4 > end) return null;
        int format = bytes[start];
        int entryFormat = bytes[start + 1];
        int countWidth = format == 0 ? 2 : format == 1 ? 4 : 0;
        if (countWidth == 0 || start + 2 + countWidth > end) return null;
        uint countValue = countWidth == 2 ? (uint)UInt16(bytes, start + 2) : UInt32(bytes, start + 2);
        if (countValue > System.Int32.MaxValue) return null;
        int count = (int)countValue;
        int entrySize = (entryFormat >> 4 & 3) + 1;
        int innerBits = (entryFormat & 15) + 1;
        if (innerBits > entrySize * 8 || (long)start + 2 + countWidth + (long)count * entrySize > end) return null;
        int selected = glyphIndex < count ? glyphIndex : count - 1;
        if (selected < 0) return null;
        int position = start + 2 + countWidth + selected * entrySize;
        uint value = 0;
        int index = 0;
        while (index < entrySize)
        {
            value = value << 8 | bytes[position + index];
            index = index + 1;
        }
        uint mask = innerBits == 32 ? System.UInt32.MaxValue : (1u << innerBits) - 1;
        uint outer = value >> innerBits;
        uint inner = value & mask;
        if (outer > System.Int32.MaxValue || inner > System.Int32.MaxValue) return null;
        return new SvgVariationIndex((int)outer, (int)inner);
    }

    SvgVariationDelta? Delta(byte[] bytes, int tableStart, int tableLength, int store, SvgVariationIndex index, double[] coordinates)
    {
        int end = tableStart + tableLength;
        if (store < tableStart || store + 8 > end || UInt16(bytes, store) != 1) return null;
        uint regionOffsetValue = UInt32(bytes, store + 2);
        int dataCount = UInt16(bytes, store + 6);
        if (regionOffsetValue > System.Int32.MaxValue || index.Outer < 0 || index.Outer >= dataCount || store + 8L + dataCount * 4 > end) return null;
        uint dataOffsetValue = UInt32(bytes, store + 8 + index.Outer * 4);
        if (dataOffsetValue > System.Int32.MaxValue) return null;
        int data = store + (int)dataOffsetValue;
        int regions = store + (int)regionOffsetValue;
        if (data < store || data + 6 > end || regions < store || regions + 4 > end) return null;
        int itemCount = UInt16(bytes, data);
        int wordCountRaw = UInt16(bytes, data + 2);
        int regionIndexCount = UInt16(bytes, data + 4);
        if ((wordCountRaw & 32768) != 0) return null;
        int wordCount = wordCountRaw;
        if (index.Inner < 0 || index.Inner >= itemCount || wordCount > regionIndexCount || data + 6L + regionIndexCount * 2 > end) return null;
        int axisCount = UInt16(bytes, regions);
        int regionCount = UInt16(bytes, regions + 2);
        if (axisCount != coordinates.Length || regions + 4L + (long)axisCount * regionCount * 6 > end) return null;
        int rowSize = wordCount * 2 + regionIndexCount - wordCount;
        long row = (long)data + 6 + regionIndexCount * 2L + (long)index.Inner * rowSize;
        if (row < 0 || row + rowSize > end) return null;
        double total = 0;
        int regionIndex = 0;
        int deltaPosition = (int)row;
        while (regionIndex < regionIndexCount)
        {
            int selectedRegion = UInt16(bytes, data + 6 + regionIndex * 2);
            if (selectedRegion >= regionCount) return null;
            int delta;
            if (regionIndex < wordCount)
            {
                delta = Int16(bytes, deltaPosition);
                deltaPosition = deltaPosition + 2;
            }
            else
            {
                delta = bytes[deltaPosition] >= 128 ? bytes[deltaPosition] - 256 : bytes[deltaPosition];
                deltaPosition = deltaPosition + 1;
            }
            double scalar = RegionScalar(bytes, regions + 4 + selectedRegion * axisCount * 6, coordinates);
            if (!SvgFiniteMath.IsFinite(scalar)) return null;
            total = total + delta * scalar;
            regionIndex = regionIndex + 1;
        }
        return SvgFiniteMath.IsFinite(total) ? new SvgVariationDelta(total) : null;
    }

    double RegionScalar(byte[] bytes, int start, double[] coordinates)
    {
        double result = 1;
        int axis = 0;
        while (axis < coordinates.Length)
        {
            double minimum = F2Dot14(bytes, start + axis * 6);
            double peak = F2Dot14(bytes, start + axis * 6 + 2);
            double maximum = F2Dot14(bytes, start + axis * 6 + 4);
            double value = coordinates[axis];
            if (minimum > peak || peak > maximum || minimum < -1 || maximum > 1) return double.NaN;
            if (peak == 0 || minimum < 0 && maximum > 0)
            {
                axis = axis + 1;
                continue;
            }
            if (value < minimum || value > maximum || value == 0) return 0;
            if (value < peak && peak != minimum) result = result * (value - minimum) / (peak - minimum);
            else if (value > peak && maximum != peak) result = result * (maximum - value) / (maximum - peak);
            axis = axis + 1;
        }
        return result;
    }

    int UInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    int Int16(byte[] bytes, int offset)
    {
        int value = UInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }

    uint UInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }

    double F2Dot14(byte[] bytes, int offset)
    {
        return Int16(bytes, offset) / 16384.0;
    }
}

internal class SvgGvarEvaluator
{
    internal SvgGvarApplyResult Apply(SvgFont font, SvgOpenTypeTable table, int glyphIndex, double[] coordinates, SvgTrueTypeGlyphOutline outline, SvgTrueTypeGlyphOptions options)
    {
        byte[]? bytes = font.Bytes;
        if (bytes == null || table.Offset < 0 || table.Length < 20 || (long)table.Offset + table.Length > bytes.Length) return SvgGvarApplyResult.InvalidFont;
        int start = table.Offset;
        int end = start + table.Length;
        if (UInt16(bytes, start) != 1 || UInt16(bytes, start + 2) != 0) return SvgGvarApplyResult.InvalidFont;
        int axisCount = UInt16(bytes, start + 4);
        int sharedCount = UInt16(bytes, start + 6);
        uint sharedOffsetValue = UInt32(bytes, start + 8);
        int glyphCount = UInt16(bytes, start + 12);
        int flags = UInt16(bytes, start + 14);
        uint dataOffsetValue = UInt32(bytes, start + 16);
        if (axisCount != coordinates.Length || glyphIndex < 0 || glyphIndex >= glyphCount || sharedOffsetValue > System.Int32.MaxValue || dataOffsetValue > System.Int32.MaxValue) return SvgGvarApplyResult.InvalidFont;
        int width = (flags & 1) == 0 ? 2 : 4;
        if (start + 20L + (glyphCount + 1L) * width > end) return SvgGvarApplyResult.InvalidFont;
        long relativeStart = Offset(bytes, start + 20 + glyphIndex * width, width);
        long relativeEnd = Offset(bytes, start + 20 + (glyphIndex + 1) * width, width);
        int dataBase = start + (int)dataOffsetValue;
        if (relativeStart > relativeEnd || (long)dataBase + relativeEnd > end) return SvgGvarApplyResult.InvalidFont;
        if (relativeStart == relativeEnd) return SvgGvarApplyResult.Success;
        if (outline.Composite) return SvgGvarApplyResult.CallbackRequired;
        int glyphStart = dataBase + (int)relativeStart;
        int glyphEnd = dataBase + (int)relativeEnd;
        if (glyphStart + 4 > glyphEnd) return SvgGvarApplyResult.InvalidFont;
        int tupleCountFlags = UInt16(bytes, glyphStart);
        int tupleCount = tupleCountFlags & 4095;
        int dataRelative = UInt16(bytes, glyphStart + 2);
        int header = glyphStart + 4;
        int tupleData = glyphStart + dataRelative;
        if (tupleData < header || tupleData > glyphEnd || (tupleCountFlags & 32768) != 0) return SvgGvarApplyResult.CallbackRequired;
        System.Collections.Generic.List<SvgGvarTuple> tuples = new System.Collections.Generic.List<SvgGvarTuple>();
        int tuple = 0;
        while (tuple < tupleCount)
        {
            if (header + 4 > tupleData) return SvgGvarApplyResult.InvalidFont;
            SvgGvarTuple item = new SvgGvarTuple(axisCount);
            item.DataSize = UInt16(bytes, header);
            item.Flags = UInt16(bytes, header + 2);
            header = header + 4;
            if ((item.Flags & 8192) != 0) return SvgGvarApplyResult.CallbackRequired;
            if ((item.Flags & 32768) != 0)
            {
                if (header + axisCount * 2 > tupleData) return SvgGvarApplyResult.InvalidFont;
                ReadTuple(bytes, header, item.Peak);
                header = header + axisCount * 2;
            }
            else
            {
                int shared = item.Flags & 4095;
                int sharedOffset = start + (int)sharedOffsetValue + shared * axisCount * 2;
                if (shared >= sharedCount || sharedOffset < start || sharedOffset + axisCount * 2 > end) return SvgGvarApplyResult.InvalidFont;
                ReadTuple(bytes, sharedOffset, item.Peak);
            }
            if ((item.Flags & 16384) != 0)
            {
                if (header + axisCount * 4 > tupleData) return SvgGvarApplyResult.InvalidFont;
                ReadTuple(bytes, header, item.Start);
                ReadTuple(bytes, header + axisCount * 2, item.End);
                header = header + axisCount * 4;
            }
            else
            {
                int axis = 0;
                while (axis < axisCount)
                {
                    item.Start[axis] = item.Peak[axis] < 0 ? item.Peak[axis] : 0;
                    item.End[axis] = item.Peak[axis] > 0 ? item.Peak[axis] : 0;
                    axis = axis + 1;
                }
            }
            tuples.Add(item);
            tuple = tuple + 1;
        }
        int totalPoints = outline.Points.Count + outline.PhantomPoints.Count;
        if (totalPoints > options.Limits.MaxPointsPerGlyph + 4) return SvgGvarApplyResult.LimitExceeded;
        tuple = 0;
        int position = tupleData;
        while (tuple < tuples.Count)
        {
            SvgGvarTuple item = tuples[tuple];
            if ((long)position + item.DataSize > glyphEnd) return SvgGvarApplyResult.InvalidFont;
            double scalar = Scalar(item, coordinates);
            if (!SvgFiniteMath.IsFinite(scalar)) return SvgGvarApplyResult.InvalidFont;
            if (scalar != 0)
            {
                int[]? x = ReadDeltas(bytes, position, position + item.DataSize, totalPoints);
                if (x == null) return SvgGvarApplyResult.InvalidFont;
                int consumed = deltaConsumed;
                int[]? y = ReadDeltas(bytes, position + consumed, position + item.DataSize, totalPoints);
                if (y == null) return SvgGvarApplyResult.InvalidFont;
                int point = 0;
                while (point < totalPoints)
                {
                    SvgTrueTypeGlyphPoint target = point < outline.Points.Count ? outline.Points[point] : outline.PhantomPoints[point - outline.Points.Count];
                    target.X = target.X + x[point] * scalar;
                    target.Y = target.Y + y[point] * scalar;
                    if (!SvgFiniteMath.IsFinite(target.X) || !SvgFiniteMath.IsFinite(target.Y)) return SvgGvarApplyResult.InvalidFont;
                    point = point + 1;
                }
            }
            position = position + item.DataSize;
            tuple = tuple + 1;
        }
        SvgPathData? rebuilt = new SvgTrueTypeGlyphDecoder().RebuildPathWithFontAndOutlineAndOptions(font, outline, options);
        if (rebuilt == null) return SvgGvarApplyResult.InvalidFont;
        outline.Path = rebuilt;
        return SvgGvarApplyResult.Success;
    }

    int deltaConsumed;

    int[]? ReadDeltas(byte[] bytes, int start, int end, int count)
    {
        int[] result = new int[count];
        int output = 0;
        int position = start;
        while (output < count)
        {
            if (position >= end) return null;
            int control = bytes[position];
            position = position + 1;
            int run = (control & 63) + 1;
            if (run > count - output) return null;
            int index = 0;
            while (index < run)
            {
                if ((control & 128) != 0) result[output] = 0;
                else if ((control & 64) != 0)
                {
                    if (position + 2 > end) return null;
                    result[output] = Int16(bytes, position);
                    position = position + 2;
                }
                else
                {
                    if (position >= end) return null;
                    result[output] = bytes[position] >= 128 ? bytes[position] - 256 : bytes[position];
                    position = position + 1;
                }
                output = output + 1;
                index = index + 1;
            }
        }
        deltaConsumed = position - start;
        return result;
    }

    double Scalar(SvgGvarTuple tuple, double[] coordinates)
    {
        double result = 1;
        int axis = 0;
        while (axis < coordinates.Length)
        {
            double value = coordinates[axis];
            double peak = tuple.Peak[axis];
            double minimum = tuple.Start[axis];
            double maximum = tuple.End[axis];
            if (minimum > peak || peak > maximum || peak == 0)
            {
                axis = axis + 1;
                continue;
            }
            if (value < minimum || value > maximum || value == 0) return 0;
            if (value < peak && peak != minimum) result = result * (value - minimum) / (peak - minimum);
            else if (value > peak && maximum != peak) result = result * (maximum - value) / (maximum - peak);
            axis = axis + 1;
        }
        return result;
    }

    void ReadTuple(byte[] bytes, int offset, double[] values)
    {
        int index = 0;
        while (index < values.Length)
        {
            values[index] = Int16(bytes, offset + index * 2) / 16384.0;
            index = index + 1;
        }
    }

    long Offset(byte[] bytes, int offset, int width)
    {
        return width == 2 ? UInt16(bytes, offset) * 2L : UInt32(bytes, offset);
    }

    int UInt16(byte[] bytes, int offset)
    {
        return bytes[offset] << 8 | bytes[offset + 1];
    }

    int Int16(byte[] bytes, int offset)
    {
        int value = UInt16(bytes, offset);
        return value >= 32768 ? value - 65536 : value;
    }

    uint UInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] << 24 | (uint)bytes[offset + 1] << 16 | (uint)bytes[offset + 2] << 8 | bytes[offset + 3];
    }
}
