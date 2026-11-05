using System;

namespace avif;

internal class Av1CdefRequest
{
    internal ParsedAv1FrameHeader? Header;
    internal PlaneBuffer[] Planes;
    internal bool[] Skips;
    internal int[] StrengthIndexes;
    internal int[] RegionIds;
    internal int MiRows;
    internal int MiColumns;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;

    internal Av1CdefRequest()
    {
        Planes = System.Array.Empty<PlaneBuffer>();
        Skips = System.Array.Empty<bool>();
        StrengthIndexes = System.Array.Empty<int>();
        RegionIds = System.Array.Empty<int>();
        BitDepth = 8;
    }
}

internal class Av1CdefDirectionResult
{
    internal AvifResultCode Code;
    internal int Direction;
    internal int Variance;

    internal Av1CdefDirectionResult(
        AvifResultCode code,
        int direction,
        int variance)
    {
        Code = code;
        Direction = direction;
        Variance = variance;
    }
}

// AV1 specification sections 7.15.1 through 7.15.3.
internal static class Av1CdefFilter
{
    static readonly int[] divisionTable = new int[]
    {
        0,840,420,280,210,168,140,120,105
    };

    static readonly int[] primaryTaps = new int[]
    {
        4,2, 3,3
    };

    static readonly int[] secondaryTaps = new int[]
    {
        2,1, 2,1
    };

    static readonly int[] directions = new int[]
    {
        -1,1, -2,2,
         0,1, -1,2,
         0,1,  0,2,
         0,1,  1,2,
         1,1,  2,2,
         1,0,  2,1,
         1,0,  2,0,
         1,0,  2,-1
    };

    static readonly int[] chromaDirections = new int[]
    {
        0,1,2,3,4,5,6,7,
        1,2,2,2,3,4,6,0,
        7,0,2,4,5,6,6,6,
        0,1,2,3,4,5,6,7
    };

    internal static AvifResultCode Filter(Av1CdefRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[][] sources = Snapshot(request.Planes);
        int blockRows = (request.MiRows + 15) >> 4;
        int blockColumns = (request.MiColumns + 15) >> 4;
        int row = 0;
        while (row < request.MiRows)
        {
            int column = 0;
            while (column < request.MiColumns)
            {
                int indexRow = row >> 4;
                int indexColumn = column >> 4;
                int strengthIndex = request.StrengthIndexes[
                    indexRow * blockColumns + indexColumn];
                if (strengthIndex >= 0 && !AllSkipped(request, row, column))
                {
                    FilterBlock(
                        request, sources, row, column, strengthIndex);
                }
                column += 2;
            }
            row += 2;
        }
        return AvifResultCode.Success;
    }

    internal static Av1CdefDirectionResult FindDirection(
        PlaneBuffer plane,
        int x,
        int y,
        int bitDepth)
    {
        if (plane == null || plane.GetStateCode() != AvifResultCode.Success
            || bitDepth != 8 && bitDepth != 10 && bitDepth != 12
            || plane.BytesPerSample != (bitDepth > 8 ? 2 : 1)
            || x < 0 || y < 0 || x + 8 > plane.Width || y + 8 > plane.Height)
        {
            return new Av1CdefDirectionResult(
                AvifResultCode.InvalidArgument, 0, 0);
        }
        int[] source = SnapshotPlane(plane);
        return FindDirection(source, plane.Width, x, y, bitDepth);
    }

    internal static int Constrain(
        int difference,
        int threshold,
        int damping)
    {
        if (threshold <= 0)
        {
            return 0;
        }
        int adjustment = damping - FloorLog2(threshold);
        if (adjustment < 0)
        {
            adjustment = 0;
        }
        int magnitude = Absolute(difference);
        int maximum = threshold - (magnitude >> adjustment);
        int constrained = Clip(0, magnitude, maximum);
        return difference < 0 ? -constrained : constrained;
    }

    static void FilterBlock(
        Av1CdefRequest request,
        int[][] sources,
        int row,
        int column,
        int strengthIndex)
    {
        int lumaX = column * Av1SyntaxConstants.MiSize;
        int lumaY = row * Av1SyntaxConstants.MiSize;
        int directionX = lumaX;
        int directionY = lumaY;
        int visibleLumaWidth = GetPlaneWidth(request, 0);
        int visibleLumaHeight = GetPlaneHeight(request, 0);
        if (directionX + 8 > visibleLumaWidth)
        {
            directionX = visibleLumaWidth - 8;
        }
        if (directionY + 8 > visibleLumaHeight)
        {
            directionY = visibleLumaHeight - 8;
        }
        if (directionX < 0)
        {
            directionX = 0;
        }
        if (directionY < 0)
        {
            directionY = 0;
        }
        Av1CdefDirectionResult direction = visibleLumaWidth >= 8
            && visibleLumaHeight >= 8
            ? FindDirection(
                sources[0], request.Planes[0].Width,
                directionX, directionY, request.BitDepth)
            : new Av1CdefDirectionResult(AvifResultCode.Success, 0, 0);
        int coefficientShift = request.BitDepth - 8;
        Av1CdefParameters? parameters = (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Cdef;
        int primary = ((parameters ?? throw new InvalidOperationException("Invalid internal state.")).YPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[strengthIndex]
            << coefficientShift;
        int secondary = (parameters.YSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[strengthIndex]
            << coefficientShift;
        int selectedDirection = primary == 0 ? 0 : direction.Direction;
        int varianceStrength = direction.Variance >> 6;
        varianceStrength = varianceStrength > 0
            ? FloorLog2(varianceStrength)
            : 0;
        if (varianceStrength > 12)
        {
            varianceStrength = 12;
        }
        primary = direction.Variance != 0
            ? (primary * (4 + varianceStrength) + 8) >> 4
            : 0;
        FilterPlaneBlock(
            request, sources, 0, row, column,
            primary, secondary, parameters.Damping + coefficientShift,
            selectedDirection);

        if (request.Planes.Length == 1)
        {
            return;
        }
        primary = (parameters.UvPrimaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[strengthIndex]
            << coefficientShift;
        secondary = (parameters.UvSecondaryStrengths ?? throw new InvalidOperationException("Invalid internal state."))[strengthIndex]
            << coefficientShift;
        selectedDirection = primary == 0
            ? 0
            : GetChromaDirection(
                request.SubsamplingX, request.SubsamplingY, direction.Direction);
        int damping = parameters.Damping + coefficientShift - 1;
        FilterPlaneBlock(
            request, sources, 1, row, column,
            primary, secondary, damping, selectedDirection);
        FilterPlaneBlock(
            request, sources, 2, row, column,
            primary, secondary, damping, selectedDirection);
    }

    static void FilterPlaneBlock(
        Av1CdefRequest request,
        int[][] sources,
        int plane,
        int row,
        int column,
        int primary,
        int secondary,
        int damping,
        int direction)
    {
        int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
        int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
        int x0 = column * Av1SyntaxConstants.MiSize >> subX;
        int y0 = row * Av1SyntaxConstants.MiSize >> subY;
        int width = 8 >> subX;
        int height = 8 >> subY;
        int planeWidth = GetPlaneWidth(request, plane);
        int planeHeight = GetPlaneHeight(request, plane);
        if (x0 + width > planeWidth)
        {
            width = planeWidth - x0;
        }
        if (y0 + height > planeHeight)
        {
            height = planeHeight - y0;
        }
        if (width <= 0 || height <= 0)
        {
            return;
        }

        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int sourceX = x0 + x;
                int sourceY = y0 + y;
                int center = sources[plane][
                    sourceY * request.Planes[plane].Width + sourceX];
                int minimum = center;
                int maximum = center;
                int sum = 0;
                int distance = 0;
                while (distance < 2)
                {
                    int sign = -1;
                    while (sign <= 1)
                    {
                        int sample = GetDirectionalSample(
                            request, sources, plane, row, column,
                            sourceX, sourceY, direction, distance, sign,
                            subX, subY);
                        if (sample >= 0)
                        {
                            int tapSet = (primary >> (request.BitDepth - 8)) & 1;
                            sum += primaryTaps[tapSet * 2 + distance]
                                * Constrain(sample - center, primary, damping);
                            if (sample < minimum)
                            {
                                minimum = sample;
                            }
                            if (sample > maximum)
                            {
                                maximum = sample;
                            }
                        }

                        int directionOffset = -2;
                        while (directionOffset <= 2)
                        {
                            int secondaryDirection =
                                (direction + directionOffset) & 7;
                            sample = GetDirectionalSample(
                                request, sources, plane, row, column,
                                sourceX, sourceY, secondaryDirection,
                                distance, sign, subX, subY);
                            if (sample >= 0)
                            {
                                int tapSet =
                                    (primary >> (request.BitDepth - 8)) & 1;
                                sum += secondaryTaps[tapSet * 2 + distance]
                                    * Constrain(
                                        sample - center, secondary, damping);
                                if (sample < minimum)
                                {
                                    minimum = sample;
                                }
                                if (sample > maximum)
                                {
                                    maximum = sample;
                                }
                            }
                            directionOffset += 4;
                        }
                        sign += 2;
                    }
                    distance++;
                }
                int filtered = center + ((8 + sum - (sum < 0 ? 1 : 0)) >> 4);
                WriteSample(
                    request.Planes[plane], sourceX, sourceY,
                    Clip(minimum, maximum, filtered));
                x++;
            }
            y++;
        }
    }

    static int GetDirectionalSample(
        Av1CdefRequest request,
        int[][] sources,
        int plane,
        int blockRow,
        int blockColumn,
        int x,
        int y,
        int direction,
        int distance,
        int sign,
        int subX,
        int subY)
    {
        int directionOffset = direction * 4 + distance * 2;
        int sampleY = y + sign * directions[directionOffset];
        int sampleX = x + sign * directions[directionOffset + 1];
        if (sampleX < 0 || sampleY < 0
            || sampleX >= GetPlaneWidth(request, plane)
            || sampleY >= GetPlaneHeight(request, plane))
        {
            return -1;
        }
        if (request.RegionIds != null
            && request.RegionIds.Length == request.MiRows * request.MiColumns)
        {
            int candidateRow = sampleY << subY >> 2;
            int candidateColumn = sampleX << subX >> 2;
            int currentRow = blockRow < request.MiRows
                ? blockRow
                : request.MiRows - 1;
            int currentColumn = blockColumn < request.MiColumns
                ? blockColumn
                : request.MiColumns - 1;
            if (candidateRow < 0 || candidateRow >= request.MiRows
                || candidateColumn < 0 || candidateColumn >= request.MiColumns
                || request.RegionIds[candidateRow * request.MiColumns
                    + candidateColumn]
                    != request.RegionIds[currentRow * request.MiColumns
                        + currentColumn])
            {
                return -1;
            }
        }
        return sources[plane][sampleY * request.Planes[plane].Width + sampleX];
    }

    static Av1CdefDirectionResult FindDirection(
        int[] source,
        int stride,
        int x0,
        int y0,
        int bitDepth)
    {
        int[] partial = new int[8 * 15];
        long[] costs = new long[8];
        int y = 0;
        while (y < 8)
        {
            int x = 0;
            while (x < 8)
            {
                int value = (source[(y0 + y) * stride + x0 + x]
                    >> (bitDepth - 8)) - 128;
                partial[y + x] += value;
                partial[15 + y + x / 2] += value;
                partial[30 + y] += value;
                partial[45 + 3 + y - x / 2] += value;
                partial[60 + 7 + y - x] += value;
                partial[75 + 3 - y / 2 + x] += value;
                partial[90 + x] += value;
                partial[105 + y / 2 + x] += value;
                x++;
            }
            y++;
        }

        int index = 0;
        while (index < 8)
        {
            costs[2] += (long)partial[30 + index] * partial[30 + index];
            costs[6] += (long)partial[90 + index] * partial[90 + index];
            index++;
        }
        costs[2] *= divisionTable[8];
        costs[6] *= divisionTable[8];
        index = 0;
        while (index < 7)
        {
            costs[0] += ((long)partial[index] * partial[index]
                + (long)partial[14 - index] * partial[14 - index])
                * divisionTable[index + 1];
            costs[4] += ((long)partial[60 + index] * partial[60 + index]
                + (long)partial[60 + 14 - index]
                    * partial[60 + 14 - index])
                * divisionTable[index + 1];
            index++;
        }
        costs[0] += (long)partial[7] * partial[7] * divisionTable[8];
        costs[4] += (long)partial[67] * partial[67] * divisionTable[8];

        int direction = 1;
        while (direction < 8)
        {
            int line = 0;
            while (line < 5)
            {
                int value = partial[direction * 15 + 3 + line];
                costs[direction] += (long)value * value;
                line++;
            }
            costs[direction] *= divisionTable[8];
            line = 0;
            while (line < 3)
            {
                int first = partial[direction * 15 + line];
                int second = partial[direction * 15 + 10 - line];
                costs[direction] += ((long)first * first + (long)second * second)
                    * divisionTable[2 * line + 2];
                line++;
            }
            direction += 2;
        }

        long bestCost = 0;
        int bestDirection = 0;
        direction = 0;
        while (direction < 8)
        {
            if (costs[direction] > bestCost)
            {
                bestCost = costs[direction];
                bestDirection = direction;
            }
            direction++;
        }
        long variance = (bestCost - costs[(bestDirection + 4) & 7]) >> 10;
        if (variance < 0)
        {
            variance = 0;
        }
        if (variance > int.MaxValue)
        {
            variance = int.MaxValue;
        }
        return new Av1CdefDirectionResult(
            AvifResultCode.Success, bestDirection, (int)variance);
    }

    static bool AllSkipped(Av1CdefRequest request, int row, int column)
    {
        int rowEnd = row + 2;
        int columnEnd = column + 2;
        if (rowEnd > request.MiRows)
        {
            rowEnd = request.MiRows;
        }
        if (columnEnd > request.MiColumns)
        {
            columnEnd = request.MiColumns;
        }
        int currentRow = row;
        while (currentRow < rowEnd)
        {
            int currentColumn = column;
            while (currentColumn < columnEnd)
            {
                if (!request.Skips[currentRow * request.MiColumns + currentColumn])
                {
                    return false;
                }
                currentColumn++;
            }
            currentRow++;
        }
        return true;
    }

    static int GetChromaDirection(
        bool subsamplingX,
        bool subsamplingY,
        int direction)
    {
        int table = (subsamplingX ? 2 : 0) + (subsamplingY ? 1 : 0);
        return chromaDirections[table * 8 + direction];
    }

    static int GetPlaneWidth(Av1CdefRequest request, int plane)
    {
        int width = (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size != null
            && (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth > 0
            ? request.Header.Size.FrameWidth
            : request.Planes[0].Width;
        int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
        return (width + (1 << subX) - 1) >> subX;
    }

    static int GetPlaneHeight(Av1CdefRequest request, int plane)
    {
        int height = (request.Header ?? throw new InvalidOperationException("Invalid internal state.")).Size != null
            && (request.Header.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameHeight > 0
            ? request.Header.Size.FrameHeight
            : request.Planes[0].Height;
        int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
        return (height + (1 << subY) - 1) >> subY;
    }

    static int[][] Snapshot(PlaneBuffer[] planes)
    {
        int[][] snapshots = new int[planes.Length][];
        int plane = 0;
        while (plane < planes.Length)
        {
            snapshots[plane] = SnapshotPlane(planes[plane]);
            plane++;
        }
        return snapshots;
    }

    static int[] SnapshotPlane(PlaneBuffer plane)
    {
        int[] samples = new int[plane.Width * plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                samples[y * plane.Width + x] = ReadSample(plane, x, y);
                x++;
            }
            y++;
        }
        return samples;
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

    static int FloorLog2(int value)
    {
        int result = 0;
        while (value > 1)
        {
            value >>= 1;
            result++;
        }
        return result;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        return value < minimum ? minimum : (value > maximum ? maximum : value);
    }

    static bool ValidPrimaryStrength(int strength)
    {
        return strength >= 0 && strength <= 15;
    }

    static bool ValidSecondaryStrength(int strength)
    {
        return strength == 0 || strength == 1
            || strength == 2 || strength == 4;
    }

    static AvifResultCode Validate(Av1CdefRequest request)
    {
        if (request == null || request.Header == null || request.Header.Cdef == null
            || request.Planes == null || request.Skips == null
            || request.StrengthIndexes == null
            || request.Planes.Length != 1 && request.Planes.Length != 3
            || request.MiRows <= 0 || request.MiColumns <= 0
            || request.Skips.Length != request.MiRows * request.MiColumns
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12)
        {
            return AvifResultCode.InvalidArgument;
        }
        int blockRows = (request.MiRows + 15) >> 4;
        int blockColumns = (request.MiColumns + 15) >> 4;
        if (request.StrengthIndexes.Length != blockRows * blockColumns
            || request.RegionIds != null && request.RegionIds.Length != 0
                && request.RegionIds.Length != request.MiRows * request.MiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1CdefParameters parameters = request.Header.Cdef;
        if (parameters.Bits < 0 || parameters.Bits > 3
            || parameters.Damping < 3 || parameters.Damping > 6)
        {
            return AvifResultCode.InvalidArgument;
        }
        int strengthCount = 1 << parameters.Bits;
        if (parameters.YPrimaryStrengths == null
            || parameters.YSecondaryStrengths == null
            || parameters.UvPrimaryStrengths == null
            || parameters.UvSecondaryStrengths == null
            || parameters.YPrimaryStrengths.Length != strengthCount
            || parameters.YSecondaryStrengths.Length != strengthCount
            || parameters.UvPrimaryStrengths.Length != strengthCount
            || parameters.UvSecondaryStrengths.Length != strengthCount)
        {
            return AvifResultCode.InvalidArgument;
        }
        int strength = 0;
        while (strength < strengthCount)
        {
            if (!ValidPrimaryStrength(parameters.YPrimaryStrengths[strength])
                || !ValidPrimaryStrength(parameters.UvPrimaryStrengths[strength])
                || !ValidSecondaryStrength(parameters.YSecondaryStrengths[strength])
                || !ValidSecondaryStrength(parameters.UvSecondaryStrengths[strength]))
            {
                return AvifResultCode.InvalidArgument;
            }
            strength++;
        }
        int index = 0;
        while (index < request.StrengthIndexes.Length)
        {
            if (request.StrengthIndexes[index] < -1
                || request.StrengthIndexes[index] >= strengthCount)
            {
                return AvifResultCode.InvalidArgument;
            }
            index++;
        }
        int frameWidth = request.Header.Size != null
            && request.Header.Size.FrameWidth > 0
            ? request.Header.Size.FrameWidth
            : request.Planes[0].Width;
        int frameHeight = request.Header.Size != null
            && request.Header.Size.FrameHeight > 0
            ? request.Header.Size.FrameHeight
            : request.Planes[0].Height;
        if (request.MiColumns != ((frameWidth + 3) >> 2)
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
            int width = (frameWidth + (1 << subX) - 1) >> subX;
            int height = (frameHeight + (1 << subY) - 1) >> subY;
            if (request.Planes[plane].Width < width
                || request.Planes[plane].Height < height
                || request.Planes[plane].BytesPerSample
                    != (request.BitDepth > 8 ? 2 : 1))
            {
                return AvifResultCode.InvalidArgument;
            }
            plane++;
        }
        return AvifResultCode.Success;
    }
}
