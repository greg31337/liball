using System;

namespace avif;

internal class Av1LoopFilterRequest
{
    internal ParsedAv1FrameHeader? Header;
    internal PlaneBuffer[] Planes;
    internal Av1ModeInfo[] Modes;
    internal Av1TransformSize[][] TransformSizes;
    internal int MiRows;
    internal int MiColumns;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;

    internal Av1LoopFilterRequest()
    {
        Planes = System.Array.Empty<PlaneBuffer>();
        Modes = System.Array.Empty<Av1ModeInfo>();
        TransformSizes = System.Array.Empty<Av1TransformSize[]>();
        BitDepth = 8;
    }
}

internal class Av1LoopFilterMask
{
    internal bool HighEdgeVariance;
    internal bool Filter;
    internal bool Flat;
    internal bool Flat2;
}

// AV1 specification section 7.14.
internal static class Av1LoopFilter
{
    internal static AvifResultCode Filter(Av1LoopFilterRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int plane = 0;
        while (plane < request.Planes.Length)
        {
            int pass = 0;
            while (pass < 2)
            {
                int rowStep = plane == 0 || !request.SubsamplingY ? 1 : 2;
                int columnStep = plane == 0 || !request.SubsamplingX ? 1 : 2;
                int row = 0;
                while (row < request.MiRows)
                {
                    int column = 0;
                    while (column < request.MiColumns)
                    {
                        FilterEdge(request, plane, pass, row, column);
                        column += columnStep;
                    }
                    row += rowStep;
                }
                pass++;
            }
            plane++;
        }
        return AvifResultCode.Success;
    }

    internal static int GetLevel(
        Av1LoopFilterRequest request,
        Av1ModeInfo mode,
        int plane,
        int pass)
    {
        if (request == null || request.Header == null
            || request.Header.LoopFilter == null || mode == null
            || plane < 0 || plane > 2 || pass < 0 || pass > 1)
        {
            return 0;
        }

        Av1LoopFilterParameters parameters = request.Header.LoopFilter;
        int filterIndex = plane == 0 ? pass : plane + 1;
        int deltaIndex = request.Header.Quantization != null
            && request.Header.Quantization.DeltaLoopFilterMulti
            ? filterIndex
            : 0;
        int delta = mode.RawDeltaLoopFilters != null
            && deltaIndex < mode.RawDeltaLoopFilters.Length
            ? mode.RawDeltaLoopFilters[deltaIndex]
            : 0;
        int level = Clip(0, Av1SyntaxConstants.MaxLoopFilter,
            (parameters.Levels ?? throw new InvalidOperationException("Invalid internal state."))[filterIndex] + delta);

        Av1SegmentFeature feature = (Av1SegmentFeature)(
            (int)Av1SegmentFeature.AlternateLoopFilterYVertical + filterIndex);
        if (IsSegmentFeatureActive(request.Header, mode.SegmentId, feature))
        {
            level = Clip(0, Av1SyntaxConstants.MaxLoopFilter,
                level + GetSegmentFeatureData(request.Header, mode.SegmentId, feature));
        }

        if (parameters.DeltaEnabled)
        {
            int reference = mode.IsInter && mode.ReferenceFrames != null
                && mode.ReferenceFrames.Length > 0
                ? (int)mode.ReferenceFrames[0]
                : (int)Av1ReferenceFrame.Intra;
            if (reference < 0 || reference >= (parameters.ReferenceDeltas ?? throw new InvalidOperationException("Invalid internal state.")).Length)
            {
                reference = (int)Av1ReferenceFrame.Intra;
            }
            int shift = level >> 5;
            level += (parameters.ReferenceDeltas ?? throw new InvalidOperationException("Invalid internal state."))[reference] << shift;
            if (reference != (int)Av1ReferenceFrame.Intra)
            {
                int modeType = GetModeType(mode.YMode);
                level += (parameters.ModeDeltas ?? throw new InvalidOperationException("Invalid internal state."))[modeType] << shift;
            }
            level = Clip(0, Av1SyntaxConstants.MaxLoopFilter, level);
        }
        return level;
    }

    static void FilterEdge(
        Av1LoopFilterRequest request,
        int plane,
        int pass,
        int inputRow,
        int inputColumn)
    {
        int x = inputColumn * Av1SyntaxConstants.MiSize;
        int y = inputRow * Av1SyntaxConstants.MiSize;
        if (x >= GetFrameWidth(request) || y >= GetFrameHeight(request)
            || pass == 0 && x == 0 || pass == 1 && y == 0)
        {
            return;
        }

        int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
        int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
        int row = inputRow | subY;
        int column = inputColumn | subX;
        if (row >= request.MiRows || column >= request.MiColumns)
        {
            return;
        }
        int dx = pass == 0 ? 1 : 0;
        int dy = pass == 0 ? 0 : 1;
        int previousRow = row - (dy << subY);
        int previousColumn = column - (dx << subX);
        if (previousRow < 0 || previousColumn < 0)
        {
            return;
        }

        Av1ModeInfo? mode = GetMode(request, row, column);
        Av1ModeInfo? previousMode = GetMode(request, previousRow, previousColumn);
        if (mode == null || previousMode == null)
        {
            return;
        }
        Av1TransformSize transformSize = GetTransformSize(
            request, plane, row, column, mode);
        Av1TransformSize previousTransformSize = GetTransformSize(
            request, plane, previousRow, previousColumn, previousMode);
        if (transformSize == Av1TransformSize.Invalid
            || previousTransformSize == Av1TransformSize.Invalid)
        {
            return;
        }

        int xPlane = x >> subX;
        int yPlane = y >> subY;
        int blockWidth = Av1ConversionTables.GetBlockWidth(mode.BlockSize) >> subX;
        int blockHeight = Av1ConversionTables.GetBlockHeight(mode.BlockSize) >> subY;
        if (blockWidth < 4)
        {
            blockWidth = 4;
        }
        if (blockHeight < 4)
        {
            blockHeight = 4;
        }
        int transformWidth = Av1ConversionTables.GetTransformWidth(transformSize) >> subX;
        int transformHeight = Av1ConversionTables.GetTransformHeight(transformSize) >> subY;
        if (transformWidth < 4)
        {
            transformWidth = 4;
        }
        if (transformHeight < 4)
        {
            transformHeight = 4;
        }

        bool blockEdge = pass == 0
            ? xPlane % blockWidth == 0
            : yPlane % blockHeight == 0;
        bool transformEdge = pass == 0
            ? xPlane % transformWidth == 0
            : yPlane % transformHeight == 0;
        bool intra = !mode.IsInter
            || mode.ReferenceFrames == null || mode.ReferenceFrames.Length == 0
            || mode.ReferenceFrames[0] <= Av1ReferenceFrame.Intra;
        bool apply = transformEdge && (blockEdge || !mode.Skip || intra);
        if (!apply)
        {
            return;
        }

        int level = GetLevel(request, mode, plane, pass);
        if (level == 0)
        {
            level = GetLevel(request, previousMode, plane, pass);
        }
        if (level == 0)
        {
            return;
        }

        int filterSize = GetFilterSize(
            transformSize, previousTransformSize, pass, plane, subX, subY);
        int sharpness = ((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopFilter ?? throw new InvalidOperationException("Invalid internal state.")).Sharpness;
        int shift = sharpness > 4 ? 2 : (sharpness > 0 ? 1 : 0);
        int limit = level >> shift;
        if (sharpness > 0)
        {
            limit = Clip(1, 9 - sharpness, limit);
        }
        else if (limit < 1)
        {
            limit = 1;
        }
        int boundaryLimit = 2 * (level + 2) + limit;
        int threshold = level >> 4;
        int index = 0;
        while (index < Av1SyntaxConstants.MiSize)
        {
            int sampleX = xPlane + dy * index;
            int sampleY = yPlane + dx * index;
            if (sampleX < request.Planes[plane].Width
                && sampleY < request.Planes[plane].Height)
            {
                FilterSamples(
                    request.Planes[plane], sampleX, sampleY,
                    request.BitDepth, plane, limit, boundaryLimit,
                    threshold, dx, dy, filterSize);
            }
            index++;
        }
    }

    static void FilterSamples(
        PlaneBuffer plane,
        int x,
        int y,
        int bitDepth,
        int planeIndex,
        int limit,
        int boundaryLimit,
        int threshold,
        int dx,
        int dy,
        int requestedFilterSize)
    {
        int filterSize = GetAvailableFilterSize(
            plane, x, y, dx, dy, planeIndex, requestedFilterSize);
        if (filterSize == 0)
        {
            return;
        }
        Av1LoopFilterMask mask = GetMask(
            plane, x, y, bitDepth, planeIndex, limit,
            boundaryLimit, threshold, dx, dy, filterSize);
        if (!mask.Filter)
        {
            return;
        }
        if (filterSize == 4 || !mask.Flat)
        {
            NarrowFilter(plane, x, y, bitDepth, dx, dy, mask.HighEdgeVariance);
        }
        else if (filterSize == 8 || !mask.Flat2)
        {
            WideFilter(plane, x, y, planeIndex, dx, dy, 3);
        }
        else
        {
            WideFilter(plane, x, y, planeIndex, dx, dy, 4);
        }
    }

    static Av1LoopFilterMask GetMask(
        PlaneBuffer plane,
        int x,
        int y,
        int bitDepth,
        int planeIndex,
        int limit,
        int boundaryLimit,
        int threshold,
        int dx,
        int dy,
        int filterSize)
    {
        int[] p = new int[7];
        int[] q = new int[7];
        int sampleCount = filterSize >= 16
            ? 7
            : (filterSize >= 8 ? (planeIndex == 0 ? 4 : 3) : 2);
        int sample = 0;
        while (sample < sampleCount)
        {
            p[sample] = ReadSample(
                plane, x - dx * (sample + 1), y - dy * (sample + 1));
            q[sample] = ReadSample(
                plane, x + dx * sample, y + dy * sample);
            sample++;
        }

        int depthShift = bitDepth - 8;
        int thresholdDepth = threshold << depthShift;
        int limitDepth = limit << depthShift;
        int boundaryLimitDepth = boundaryLimit << depthShift;
        Av1LoopFilterMask result = new Av1LoopFilterMask();
        result.HighEdgeVariance = Absolute(p[1] - p[0]) > thresholdDepth
            || Absolute(q[1] - q[0]) > thresholdDepth;

        int filterLength = filterSize == 4
            ? 4
            : (planeIndex != 0 ? 6 : (filterSize == 8 ? 8 : 16));
        bool blocked = Absolute(p[1] - p[0]) > limitDepth
            || Absolute(q[1] - q[0]) > limitDepth
            || Absolute(p[0] - q[0]) * 2
                + Absolute(p[1] - q[1]) / 2 > boundaryLimitDepth;
        if (filterLength >= 6)
        {
            blocked = blocked || Absolute(p[2] - p[1]) > limitDepth
                || Absolute(q[2] - q[1]) > limitDepth;
        }
        if (filterLength >= 8)
        {
            blocked = blocked || Absolute(p[3] - p[2]) > limitDepth
                || Absolute(q[3] - q[2]) > limitDepth;
        }
        result.Filter = !blocked;

        if (filterSize >= 8)
        {
            int flatThreshold = 1 << depthShift;
            bool flat = Absolute(p[1] - p[0]) <= flatThreshold
                && Absolute(q[1] - q[0]) <= flatThreshold
                && Absolute(p[2] - p[0]) <= flatThreshold
                && Absolute(q[2] - q[0]) <= flatThreshold;
            if (filterLength >= 8)
            {
                flat = flat
                    && Absolute(p[3] - p[0]) <= flatThreshold
                    && Absolute(q[3] - q[0]) <= flatThreshold;
            }
            result.Flat = flat;
        }
        if (filterSize >= 16)
        {
            int flatThreshold = 1 << depthShift;
            result.Flat2 = Absolute(p[6] - p[0]) <= flatThreshold
                && Absolute(q[6] - q[0]) <= flatThreshold
                && Absolute(p[5] - p[0]) <= flatThreshold
                && Absolute(q[5] - q[0]) <= flatThreshold
                && Absolute(p[4] - p[0]) <= flatThreshold
                && Absolute(q[4] - q[0]) <= flatThreshold;
        }
        return result;
    }

    static void NarrowFilter(
        PlaneBuffer plane,
        int x,
        int y,
        int bitDepth,
        int dx,
        int dy,
        bool highEdgeVariance)
    {
        int q0 = ReadSample(plane, x, y);
        int q1 = ReadSample(plane, x + dx, y + dy);
        int p0 = ReadSample(plane, x - dx, y - dy);
        int p1 = ReadSample(plane, x - dx * 2, y - dy * 2);
        int offset = 128 << bitDepth - 8;
        int ps1 = p1 - offset;
        int ps0 = p0 - offset;
        int qs0 = q0 - offset;
        int qs1 = q1 - offset;
        int filter = highEdgeVariance ? ClampSigned(ps1 - qs1, bitDepth) : 0;
        filter = ClampSigned(filter + 3 * (qs0 - ps0), bitDepth);
        int filter1 = ClampSigned(filter + 4, bitDepth) >> 3;
        int filter2 = ClampSigned(filter + 3, bitDepth) >> 3;
        WriteSample(plane, x, y, ClampSigned(qs0 - filter1, bitDepth) + offset);
        WriteSample(plane, x - dx, y - dy,
            ClampSigned(ps0 + filter2, bitDepth) + offset);
        if (!highEdgeVariance)
        {
            filter = Round2(filter1, 1);
            WriteSample(plane, x + dx, y + dy,
                ClampSigned(qs1 - filter, bitDepth) + offset);
            WriteSample(plane, x - dx * 2, y - dy * 2,
                ClampSigned(ps1 + filter, bitDepth) + offset);
        }
    }

    static void WideFilter(
        PlaneBuffer plane,
        int x,
        int y,
        int planeIndex,
        int dx,
        int dy,
        int log2Size)
    {
        int n = log2Size == 4 ? 6 : (planeIndex == 0 ? 3 : 2);
        int n2 = log2Size == 3 && planeIndex == 0 ? 0 : 1;
        int[] filtered = new int[n * 2];
        int i = -n;
        while (i < n)
        {
            int total = 0;
            int j = -n;
            while (j <= n)
            {
                int position = Clip(-(n + 1), n, i + j);
                int tap = Absolute(j) <= n2 ? 2 : 1;
                total += ReadSample(
                    plane, x + position * dx, y + position * dy) * tap;
                j++;
            }
            filtered[i + n] = Round2(total, log2Size);
            i++;
        }
        i = -n;
        while (i < n)
        {
            WriteSample(plane, x + i * dx, y + i * dy, filtered[i + n]);
            i++;
        }
    }

    static int GetAvailableFilterSize(
        PlaneBuffer plane,
        int x,
        int y,
        int dx,
        int dy,
        int planeIndex,
        int requested)
    {
        int filterSize = requested;
        while (filterSize >= 4)
        {
            int negative = filterSize >= 16
                ? 7
                : (filterSize >= 8 ? (planeIndex == 0 ? 4 : 3) : 2);
            int positive = negative - 1;
            int minimumX = x - dx * negative;
            int minimumY = y - dy * negative;
            int maximumX = x + dx * positive;
            int maximumY = y + dy * positive;
            if (minimumX >= 0 && minimumY >= 0
                && maximumX < plane.Width && maximumY < plane.Height)
            {
                return filterSize;
            }
            filterSize = filterSize >= 16 ? 8 : (filterSize >= 8 ? 4 : 0);
        }
        return 0;
    }

    static int GetFilterSize(
        Av1TransformSize current,
        Av1TransformSize previous,
        int pass,
        int plane,
        int subX,
        int subY)
    {
        int currentSize = pass == 0
            ? Av1ConversionTables.GetTransformWidth(current) >> subX
            : Av1ConversionTables.GetTransformHeight(current) >> subY;
        int previousSize = pass == 0
            ? Av1ConversionTables.GetTransformWidth(previous) >> subX
            : Av1ConversionTables.GetTransformHeight(previous) >> subY;
        if (currentSize < 4)
        {
            currentSize = 4;
        }
        if (previousSize < 4)
        {
            previousSize = 4;
        }
        int baseSize = currentSize < previousSize ? currentSize : previousSize;
        int maximum = plane == 0 ? 16 : 8;
        return baseSize < maximum ? baseSize : maximum;
    }

    static Av1TransformSize GetTransformSize(
        Av1LoopFilterRequest request,
        int plane,
        int row,
        int column,
        Av1ModeInfo mode)
    {
        if (request.TransformSizes != null
            && plane < request.TransformSizes.Length
            && request.TransformSizes[plane] != null
            && request.TransformSizes[plane].Length == request.MiRows * request.MiColumns)
        {
            return request.TransformSizes[plane][row * request.MiColumns + column];
        }
        return mode.TransformSize;
    }

    static Av1ModeInfo? GetMode(
        Av1LoopFilterRequest request,
        int row,
        int column)
    {
        if (row < 0 || row >= request.MiRows
            || column < 0 || column >= request.MiColumns)
        {
            return null;
        }
        return request.Modes[row * request.MiColumns + column];
    }

    static bool IsSegmentFeatureActive(
        ParsedAv1FrameHeader header,
        int segment,
        Av1SegmentFeature feature)
    {
        return header.Segmentation != null && header.Segmentation.Enabled
            && header.Segmentation.FeatureEnabled != null
            && segment >= 0 && segment < header.Segmentation.FeatureEnabled.Length
            && header.Segmentation.FeatureEnabled[segment] != null
            && (int)feature < header.Segmentation.FeatureEnabled[segment].Length
            && header.Segmentation.FeatureEnabled[segment][(int)feature];
    }

    static int GetSegmentFeatureData(
        ParsedAv1FrameHeader header,
        int segment,
        Av1SegmentFeature feature)
    {
        if ((header.Segmentation ?? throw new InvalidOperationException("Invalid internal state.")).FeatureData == null
            || segment < 0 || segment >= (header.Segmentation.FeatureData ?? throw new InvalidOperationException("Invalid internal state.")).Length
            || header.Segmentation.FeatureData[segment] == null
            || (int)feature >= header.Segmentation.FeatureData[segment].Length)
        {
            return 0;
        }
        return header.Segmentation.FeatureData[segment][(int)feature];
    }

    static int GetModeType(Av1PredictionMode mode)
    {
        return mode >= Av1PredictionMode.NearestMv
            && mode != Av1PredictionMode.GlobalMv
            && mode != Av1PredictionMode.GlobalGlobalMv
            ? 1
            : 0;
    }

    static int GetFrameWidth(Av1LoopFilterRequest request)
    {
        return (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size != null && (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth > 0
            ? request.Header.Size.FrameWidth
            : request.Planes[0].Width;
    }

    static int GetFrameHeight(Av1LoopFilterRequest request)
    {
        return (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size != null && (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight > 0
            ? request.Header.Size.FrameHeight
            : request.Planes[0].Height;
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = IntegerConversion.ToByte(value);
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = IntegerConversion.ToByte(value >> 8);
        }
    }

    static int ClampSigned(int value, int bitDepth)
    {
        int minimum = -(1 << bitDepth - 1);
        int maximum = (1 << bitDepth - 1) - 1;
        return Clip(minimum, maximum, value);
    }

    static int Round2(int value, int shift)
    {
        return shift == 0 ? value : (value + (1 << shift - 1)) >> shift;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        return value < minimum ? minimum : (value > maximum ? maximum : value);
    }

    static AvifResultCode Validate(Av1LoopFilterRequest request)
    {
        if (request == null || request.Header == null
            || request.Header.LoopFilter == null
            || request.Header.Quantization == null
            || request.Planes == null || request.Modes == null
            || request.Planes.Length != 1 && request.Planes.Length != 3
            || request.MiRows <= 0 || request.MiColumns <= 0
            || request.Modes.Length != request.MiRows * request.MiColumns
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12)
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1LoopFilterParameters parameters = request.Header.LoopFilter;
        if (parameters.Levels == null || parameters.Levels.Length != 4
            || parameters.Sharpness < 0 || parameters.Sharpness > 7
            || parameters.ReferenceDeltas == null
            || parameters.ReferenceDeltas.Length != 8
            || parameters.ModeDeltas == null || parameters.ModeDeltas.Length != 2)
        {
            return AvifResultCode.InvalidArgument;
        }

        int frameWidth = GetFrameWidth(request);
        int frameHeight = GetFrameHeight(request);
        if (frameWidth <= 0 || frameHeight <= 0
            || request.MiColumns != ((frameWidth + 3) >> 2)
            || request.MiRows != ((frameHeight + 3) >> 2))
        {
            return AvifResultCode.InvalidArgument;
        }
        int plane = 0;
        while (plane < request.Planes.Length)
        {
            if (request.Planes[plane] == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode state = request.Planes[plane].GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int minimumWidth = (frameWidth + (1 << subX) - 1) >> subX;
            int minimumHeight = (frameHeight + (1 << subY) - 1) >> subY;
            if (request.Planes[plane].Width < minimumWidth
                || request.Planes[plane].Height < minimumHeight
                || request.Planes[plane].BytesPerSample
                    != (request.BitDepth > 8 ? 2 : 1))
            {
                return AvifResultCode.InvalidArgument;
            }
            plane++;
        }
        int index = 0;
        while (index < request.Modes.Length)
        {
            if (request.Modes[index] == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        return AvifResultCode.Success;
    }
}
