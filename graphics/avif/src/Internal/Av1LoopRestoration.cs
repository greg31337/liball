using System;

namespace avif;

internal class Av1RestorationUnit
{
    internal Av1RestorationType Type;
    internal int[] WienerVertical;
    internal int[] WienerHorizontal;
    internal int SgrSet;
    internal int[] SgrWeights;

    internal Av1RestorationUnit()
    {
        Type = Av1RestorationType.None;
        WienerVertical = new int[3];
        WienerHorizontal = new int[3];
        SgrWeights = new int[2];
    }
}

internal class Av1LoopRestorationRequest
{
    internal ParsedAv1FrameHeader? Header;
    internal PlaneBuffer[] CurrentPlanes;
    internal PlaneBuffer[] CdefPlanes;
    internal PlaneBuffer[] DestinationPlanes;
    internal Av1RestorationUnit[][] Units;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;

    internal Av1LoopRestorationRequest()
    {
        CurrentPlanes = System.Array.Empty<PlaneBuffer>();
        CdefPlanes = System.Array.Empty<PlaneBuffer>();
        DestinationPlanes = System.Array.Empty<PlaneBuffer>();
        Units = System.Array.Empty<Av1RestorationUnit[]>();
        BitDepth = 8;
    }
}

// AV1 specification sections 7.17.1 through 7.17.6.
internal static class Av1LoopRestoration
{
    static readonly int[] sgrParameters = new int[]
    {
        2,12,1,4, 2,15,1,6, 2,18,1,8, 2,21,1,9,
        2,24,1,10, 2,29,1,11, 2,36,1,12, 2,45,1,13,
        2,56,1,14, 2,68,1,15, 0,0,1,5, 0,0,1,8,
        0,0,1,11, 0,0,1,14, 2,30,0,0, 2,75,0,0
    };

    internal static AvifResultCode Filter(Av1LoopRestorationRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[][] current = Snapshot(request.CurrentPlanes);
        int[][] cdef = Snapshot(request.CdefPlanes);
        CopyToDestination(request, cdef);
        if (!((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).LoopRestoration ?? throw new InvalidOperationException("Invalid internal state.")).UsesRestoration)
        {
            return AvifResultCode.Success;
        }

        int lumaY = 0;
        while (lumaY < (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight)
        {
            int lumaX = 0;
            while (lumaX < request.Header.Size.UpscaledWidth)
            {
                int plane = 0;
                while (plane < request.CdefPlanes.Length)
                {
                    if (((request.Header.LoopRestoration ?? throw new InvalidOperationException("Invalid internal state.")).Types ?? throw new InvalidOperationException("Invalid internal state."))[plane]
                        != Av1RestorationType.None)
                    {
                        RestoreBlock(
                            request, current, cdef, plane, lumaX, lumaY);
                    }
                    plane++;
                }
                lumaX += Av1SyntaxConstants.MiSize;
            }
            lumaY += Av1SyntaxConstants.MiSize;
        }
        return AvifResultCode.Success;
    }

    internal static int CountUnits(int unitSize, int frameSize)
    {
        if (unitSize <= 0 || frameSize <= 0)
        {
            return 0;
        }
        int count = (frameSize + (unitSize >> 1)) / unitSize;
        return count < 1 ? 1 : count;
    }

    internal static int GetSgrParameter(int set, int index)
    {
        if (set < 0 || set >= 16 || index < 0 || index >= 4)
        {
            return 0;
        }
        return sgrParameters[set * 4 + index];
    }

    static void RestoreBlock(
        Av1LoopRestorationRequest request,
        int[][] current,
        int[][] cdef,
        int plane,
        int lumaX,
        int lumaY)
    {
        int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
        int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
        int planeWidth = Round2(((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, subX);
        int planeHeight = Round2((request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight, subY);
        int x = lumaX >> subX;
        int y = lumaY >> subY;
        int width = Av1SyntaxConstants.MiSize >> subX;
        int height = Av1SyntaxConstants.MiSize >> subY;
        if (x + width > planeWidth)
        {
            width = planeWidth - x;
        }
        if (y + height > planeHeight)
        {
            height = planeHeight - y;
        }
        if (width <= 0 || height <= 0)
        {
            return;
        }

        int unitSize = ((request.Header.LoopRestoration ?? throw new InvalidOperationException("Invalid internal state.")).UnitSizes ?? throw new InvalidOperationException("Invalid internal state."))[plane];
        int unitRows = CountUnits(unitSize, planeHeight);
        int unitColumns = CountUnits(unitSize, planeWidth);
        int unitRow = ((lumaY + 8) >> subY) / unitSize;
        int unitColumn = (lumaX >> subX) / unitSize;
        if (unitRow >= unitRows)
        {
            unitRow = unitRows - 1;
        }
        if (unitColumn >= unitColumns)
        {
            unitColumn = unitColumns - 1;
        }
        Av1RestorationUnit unit = request.Units[plane][
            unitRow * unitColumns + unitColumn];
        if (unit.Type == Av1RestorationType.None)
        {
            return;
        }

        int stripeNumber = (lumaY + 8) / 64;
        int stripeStart = (-8 + stripeNumber * 64) >> subY;
        int stripeEnd = stripeStart + (64 >> subY) - 1;
        if (unit.Type == Av1RestorationType.Wiener)
        {
            ApplyWiener(
                request, current, cdef, unit, plane,
                x, y, width, height, planeWidth, planeHeight,
                stripeStart, stripeEnd);
        }
        else
        {
            ApplySelfGuided(
                request, current, cdef, unit, plane,
                x, y, width, height, planeWidth, planeHeight,
                stripeStart, stripeEnd);
        }
    }

    static void ApplyWiener(
        Av1LoopRestorationRequest request,
        int[][] current,
        int[][] cdef,
        Av1RestorationUnit unit,
        int plane,
        int x,
        int y,
        int width,
        int height,
        int planeWidth,
        int planeHeight,
        int stripeStart,
        int stripeEnd)
    {
        int[] vertical = BuildWienerFilter(unit.WienerVertical);
        int[] horizontal = BuildWienerFilter(unit.WienerHorizontal);
        int round0 = request.BitDepth == 12 ? 5 : 3;
        int round1 = request.BitDepth == 12 ? 9 : 11;
        int offset = 1 << (request.BitDepth
            + Av1SyntaxConstants.FilterBits - round0 - 1);
        int limit = (1 << (request.BitDepth + 1
            + Av1SyntaxConstants.FilterBits - round0)) - 1;
        int[] intermediate = new int[(height + 6) * width];
        int row = 0;
        while (row < height + 6)
        {
            int column = 0;
            while (column < width)
            {
                long sum = 0;
                int tap = 0;
                while (tap < 7)
                {
                    int sample = GetSourceSample(
                        request, current, cdef, plane,
                        x + column + tap - 3, y + row - 3,
                        planeWidth, planeHeight, stripeStart, stripeEnd);
                    sum += (long)horizontal[tap] * sample;
                    tap++;
                }
                intermediate[row * width + column] = Clip(
                    -offset, limit - offset, Round2(sum, round0));
                column++;
            }
            row++;
        }

        int maximum = (1 << request.BitDepth) - 1;
        row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                long sum = 0;
                int tap = 0;
                while (tap < 7)
                {
                    sum += (long)vertical[tap]
                        * intermediate[(row + tap) * width + column];
                    tap++;
                }
                int value = Clip(0, maximum, Round2(sum, round1));
                WriteSample(
                    request.DestinationPlanes[plane],
                    x + column, y + row, value);
                column++;
            }
            row++;
        }
    }

    static void ApplySelfGuided(
        Av1LoopRestorationRequest request,
        int[][] current,
        int[][] cdef,
        Av1RestorationUnit unit,
        int plane,
        int x,
        int y,
        int width,
        int height,
        int planeWidth,
        int planeHeight,
        int stripeStart,
        int stripeEnd)
    {
        int radius0 = GetSgrParameter(unit.SgrSet, 0);
        int radius1 = GetSgrParameter(unit.SgrSet, 2);
        int[] filtered0 = radius0 == 0
            ? System.Array.Empty<int>()
            : BoxFilter(
                request, current, cdef, plane, x, y, width, height,
                planeWidth, planeHeight, stripeStart, stripeEnd,
                radius0, GetSgrParameter(unit.SgrSet, 1), 0);
        int[] filtered1 = radius1 == 0
            ? System.Array.Empty<int>()
            : BoxFilter(
                request, current, cdef, plane, x, y, width, height,
                planeWidth, planeHeight, stripeStart, stripeEnd,
                radius1, GetSgrParameter(unit.SgrSet, 3), 1);
        int weight0 = unit.SgrWeights[0];
        int weight1 = unit.SgrWeights[1];
        int weight2 = (1 << Av1SyntaxConstants.SgrprojProjectionBits)
            - weight0 - weight1;
        int maximum = (1 << request.BitDepth) - 1;
        int sourceStride = request.CdefPlanes[plane].Width;
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int source = cdef[plane][
                    (y + row) * sourceStride + x + column];
                long unfiltered = (long)source
                    << Av1SyntaxConstants.SgrprojRestorationBits;
                long value = weight1 * unfiltered;
                value += weight0 * (radius0 == 0
                    ? unfiltered
                    : filtered0[row * width + column]);
                value += weight2 * (radius1 == 0
                    ? unfiltered
                    : filtered1[row * width + column]);
                int restored = Round2(
                    value,
                    Av1SyntaxConstants.SgrprojRestorationBits
                        + Av1SyntaxConstants.SgrprojProjectionBits);
                WriteSample(
                    request.DestinationPlanes[plane],
                    x + column, y + row,
                    Clip(0, maximum, restored));
                column++;
            }
            row++;
        }
    }

    static int[] BoxFilter(
        Av1LoopRestorationRequest request,
        int[][] current,
        int[][] cdef,
        int plane,
        int x,
        int y,
        int width,
        int height,
        int planeWidth,
        int planeHeight,
        int stripeStart,
        int stripeEnd,
        int radius,
        int epsilon,
        int pass)
    {
        int arrayWidth = width + 2;
        int arrayHeight = height + 2;
        int[] coefficients = new int[arrayWidth * arrayHeight];
        long[] offsets = new long[arrayWidth * arrayHeight];
        int n = (2 * radius + 1) * (2 * radius + 1);
        int n2e = n * n * epsilon;
        int scale = ((1 << Av1SyntaxConstants.SgrprojMtableBits)
            + n2e / 2) / n2e;
        int oneOverN = ((1 << Av1SyntaxConstants.SgrprojReciprocalBits)
            + n / 2) / n;
        int localY = -1;
        while (localY < height + 1)
        {
            int localX = -1;
            while (localX < width + 1)
            {
                long squares = 0;
                long sum = 0;
                int dy = -radius;
                while (dy <= radius)
                {
                    int dx = -radius;
                    while (dx <= radius)
                    {
                        int sample = GetSourceSample(
                            request, current, cdef, plane,
                            x + localX + dx, y + localY + dy,
                            planeWidth, planeHeight, stripeStart, stripeEnd);
                        sum += sample;
                        squares += (long)sample * sample;
                        dx++;
                    }
                    dy++;
                }
                long a = Round2Long(
                    squares, 2 * (request.BitDepth - 8));
                long d = Round2Long(sum, request.BitDepth - 8);
                long variance = a * n - d * d;
                if (variance < 0)
                {
                    variance = 0;
                }
                long z = Round2Long(
                    variance * scale,
                    Av1SyntaxConstants.SgrprojMtableBits);
                int coefficient;
                if (z >= 255)
                {
                    coefficient = 256;
                }
                else if (z == 0)
                {
                    coefficient = 1;
                }
                else
                {
                    coefficient = (int)((z
                        << Av1SyntaxConstants.SgrprojInternalBits) + z / 2)
                        / (int)(z + 1);
                }
                long b = ((1 << Av1SyntaxConstants.SgrprojInternalBits)
                    - coefficient) * sum * oneOverN;
                int index = (localY + 1) * arrayWidth + localX + 1;
                coefficients[index] = coefficient;
                offsets[index] = Round2Long(
                    b, Av1SyntaxConstants.SgrprojReciprocalBits);
                localX++;
            }
            localY++;
        }

        int[] filtered = new int[width * height];
        localY = 0;
        while (localY < height)
        {
            int shift = pass == 0 && (localY & 1) != 0 ? 4 : 5;
            int localX = 0;
            while (localX < width)
            {
                long coefficientSum = 0;
                long offsetSum = 0;
                int dy = -1;
                while (dy <= 1)
                {
                    int dx = -1;
                    while (dx <= 1)
                    {
                        int weight;
                        if (pass == 0)
                        {
                            weight = ((localY + dy) & 1) != 0
                                ? (dx == 0 ? 6 : 5)
                                : 0;
                        }
                        else
                        {
                            weight = dx == 0 || dy == 0 ? 4 : 3;
                        }
                        int index = (localY + dy + 1) * arrayWidth
                            + localX + dx + 1;
                        coefficientSum += weight * coefficients[index];
                        offsetSum += weight * offsets[index];
                        dx++;
                    }
                    dy++;
                }
                int source = cdef[plane][
                    (y + localY) * request.CdefPlanes[plane].Width
                        + x + localX];
                long value = coefficientSum * source + offsetSum;
                filtered[localY * width + localX] = Round2(
                    value,
                    Av1SyntaxConstants.SgrprojInternalBits + shift
                        - Av1SyntaxConstants.SgrprojRestorationBits);
                localX++;
            }
            localY++;
        }
        return filtered;
    }

    static int GetSourceSample(
        Av1LoopRestorationRequest request,
        int[][] current,
        int[][] cdef,
        int plane,
        int x,
        int y,
        int planeWidth,
        int planeHeight,
        int stripeStart,
        int stripeEnd)
    {
        x = Clip(0, planeWidth - 1, x);
        y = Clip(0, planeHeight - 1, y);
        int stride;
        if (y < stripeStart)
        {
            y = y < stripeStart - 2 ? stripeStart - 2 : y;
            stride = request.CurrentPlanes[plane].Width;
            return current[plane][y * stride + x];
        }
        if (y > stripeEnd)
        {
            y = y > stripeEnd + 2 ? stripeEnd + 2 : y;
            stride = request.CurrentPlanes[plane].Width;
            return current[plane][y * stride + x];
        }
        stride = request.CdefPlanes[plane].Width;
        return cdef[plane][y * stride + x];
    }

    static int[] BuildWienerFilter(int[] coefficients)
    {
        int[] filter = new int[7];
        filter[3] = 128;
        int index = 0;
        while (index < 3)
        {
            filter[index] = coefficients[index];
            filter[6 - index] = coefficients[index];
            filter[3] -= 2 * coefficients[index];
            index++;
        }
        return filter;
    }

    static int[][] Snapshot(PlaneBuffer[] planes)
    {
        int[][] samples = new int[planes.Length][];
        int plane = 0;
        while (plane < planes.Length)
        {
            samples[plane] = new int[planes[plane].Width * planes[plane].Height];
            int y = 0;
            while (y < planes[plane].Height)
            {
                int x = 0;
                while (x < planes[plane].Width)
                {
                    samples[plane][y * planes[plane].Width + x] =
                        ReadSample(planes[plane], x, y);
                    x++;
                }
                y++;
            }
            plane++;
        }
        return samples;
    }

    static void CopyToDestination(
        Av1LoopRestorationRequest request,
        int[][] cdef)
    {
        int plane = 0;
        while (plane < request.DestinationPlanes.Length)
        {
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int width = Round2(((request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size ?? throw new InvalidOperationException("Invalid internal state.")).UpscaledWidth, subX);
            int height = Round2((request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight, subY);
            int sourceStride = request.CdefPlanes[plane].Width;
            int y = 0;
            while (y < height)
            {
                int x = 0;
                while (x < width)
                {
                    WriteSample(
                        request.DestinationPlanes[plane], x, y,
                        cdef[plane][y * sourceStride + x]);
                    x++;
                }
                y++;
            }
            plane++;
        }
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

    static int Round2(long value, int shift)
    {
        return (int)Round2Long(value, shift);
    }

    static long Round2Long(long value, int shift)
    {
        if (shift <= 0)
        {
            return value;
        }
        return (value + (1L << (shift - 1))) >> shift;
    }

    static int Round2(int value, int shift)
    {
        return (value + (1 << shift) - 1) >> shift;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        return value < minimum ? minimum : (value > maximum ? maximum : value);
    }

    static AvifResultCode Validate(Av1LoopRestorationRequest request)
    {
        if (request == null || request.Header == null
            || request.Header.Size == null
            || request.Header.LoopRestoration == null
            || request.Header.LoopRestoration.Types == null
            || request.Header.LoopRestoration.UnitSizes == null
            || request.CurrentPlanes == null || request.CdefPlanes == null
            || request.DestinationPlanes == null || request.Units == null
            || request.CurrentPlanes.Length != 1
                && request.CurrentPlanes.Length != 3
            || request.CdefPlanes.Length != request.CurrentPlanes.Length
            || request.DestinationPlanes.Length != request.CurrentPlanes.Length
            || request.Units.Length != request.CurrentPlanes.Length
            || request.Header.LoopRestoration.Types.Length
                < request.CurrentPlanes.Length
            || request.Header.LoopRestoration.UnitSizes.Length
                < request.CurrentPlanes.Length
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.Header.Size.UpscaledWidth <= 0
            || request.Header.Size.FrameHeight <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }

        bool usesRestoration = false;
        int plane = 0;
        while (plane < request.CurrentPlanes.Length)
        {
            PlaneBuffer current = request.CurrentPlanes[plane];
            PlaneBuffer cdef = request.CdefPlanes[plane];
            PlaneBuffer destination = request.DestinationPlanes[plane];
            if (current == null || cdef == null || destination == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode state = current.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            state = cdef.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            state = destination.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int width = Round2(request.Header.Size.UpscaledWidth, subX);
            int height = Round2(request.Header.Size.FrameHeight, subY);
            int bytesPerSample = request.BitDepth > 8 ? 2 : 1;
            if (current.Width < width || current.Height < height
                || cdef.Width < width || cdef.Height < height
                || destination.Width < width || destination.Height < height
                || current.BytesPerSample != bytesPerSample
                || cdef.BytesPerSample != bytesPerSample
                || destination.BytesPerSample != bytesPerSample)
            {
                return AvifResultCode.InvalidArgument;
            }

            Av1RestorationType frameType =
                request.Header.LoopRestoration.Types[plane];
            if (frameType < Av1RestorationType.None
                || frameType > Av1RestorationType.Switchable)
            {
                return AvifResultCode.InvalidArgument;
            }
            if (frameType != Av1RestorationType.None)
            {
                usesRestoration = true;
                int unitSize = request.Header.LoopRestoration.UnitSizes[plane];
                if (unitSize < 32 || unitSize > 256
                    || (unitSize & (unitSize - 1)) != 0)
                {
                    return AvifResultCode.InvalidArgument;
                }
                int unitCount = CountUnits(unitSize, width)
                    * CountUnits(unitSize, height);
                if (request.Units[plane] == null
                    || request.Units[plane].Length != unitCount)
                {
                    return AvifResultCode.InvalidArgument;
                }
                int unitIndex = 0;
                while (unitIndex < unitCount)
                {
                    if (!ValidateUnit(
                        request.Units[plane][unitIndex], frameType, plane))
                    {
                        return AvifResultCode.InvalidArgument;
                    }
                    unitIndex++;
                }
            }
            plane++;
        }
        if (usesRestoration != request.Header.LoopRestoration.UsesRestoration)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool ValidateUnit(
        Av1RestorationUnit unit,
        Av1RestorationType frameType,
        int plane)
    {
        if (unit == null || unit.Type < Av1RestorationType.None
            || unit.Type > Av1RestorationType.Sgrproj
            || frameType == Av1RestorationType.Wiener
                && unit.Type != Av1RestorationType.None
                && unit.Type != Av1RestorationType.Wiener
            || frameType == Av1RestorationType.Sgrproj
                && unit.Type != Av1RestorationType.None
                && unit.Type != Av1RestorationType.Sgrproj)
        {
            return false;
        }
        if (unit.Type == Av1RestorationType.Wiener)
        {
            if (unit.WienerVertical == null || unit.WienerHorizontal == null
                || unit.WienerVertical.Length != 3
                || unit.WienerHorizontal.Length != 3)
            {
                return false;
            }
            int pass = 0;
            while (pass < 2)
            {
                int[] coefficients = pass == 0
                    ? unit.WienerVertical
                    : unit.WienerHorizontal;
                if (coefficients[0] < -5 || coefficients[0] > 10
                    || coefficients[1] < -23 || coefficients[1] > 8
                    || coefficients[2] < -17 || coefficients[2] > 46
                    || plane > 0 && coefficients[0] != 0)
                {
                    return false;
                }
                pass++;
            }
        }
        else if (unit.Type == Av1RestorationType.Sgrproj)
        {
            if (unit.SgrSet < 0 || unit.SgrSet >= 16
                || unit.SgrWeights == null || unit.SgrWeights.Length != 2
                || unit.SgrWeights[0] < -96 || unit.SgrWeights[0] > 31
                || unit.SgrWeights[1] < -32 || unit.SgrWeights[1] > 95
                || GetSgrParameter(unit.SgrSet, 0) == 0
                    && unit.SgrWeights[0] != 0)
            {
                return false;
            }
        }
        return true;
    }
}
