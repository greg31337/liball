using System;

namespace avif;

internal class Av1InverseTransformRequest
{
    internal Av1TransformSize TransformSize;
    internal Av1TransformType TransformType;
    internal int BitDepth;
    internal int[] Coefficients;
    internal int EndOfBlock;
    internal bool Lossless;
    internal PlaneBuffer? Destination;
    internal int X;
    internal int Y;

    internal Av1InverseTransformRequest()
    {
        TransformSize = Av1TransformSize.Transform4x4;
        TransformType = Av1TransformType.DctDct;
        BitDepth = 8;
        Coefficients = System.Array.Empty<int>();
        EndOfBlock = -1;
    }
}

internal class Av1InverseTransformResult
{
    internal AvifResultCode Code;
    internal int[] Residual;
    internal int Width;
    internal int Height;

    internal Av1InverseTransformResult(
        AvifResultCode code,
        int width,
        int height)
    {
        Code = code;
        Width = width;
        Height = height;
        Residual = code == AvifResultCode.Success
            ? new int[width * height]
            : System.Array.Empty<int>();
    }
}

internal class Av1TransformPartition
{
    internal int X;
    internal int Y;
    internal Av1TransformSize TransformSize;

    internal Av1TransformPartition(int x, int y, Av1TransformSize transformSize)
    {
        X = x;
        Y = y;
        TransformSize = transformSize;
    }
}

internal class Av1TransformSplitResult
{
    internal AvifResultCode Code;
    internal Av1TransformPartition[] Partitions;

    internal Av1TransformSplitResult(
        AvifResultCode code,
        Av1TransformPartition[] partitions)
    {
        Code = code;
        Partitions = partitions;
    }
}

// AV1 specification sections 7.12 and 7.13.
internal static class Av1InverseTransformer
{
    static readonly int[] rowShifts = new int[]
    {
        0,1,2,2,2,0,0,1,1,1,1,1,1,1,1,2,2,2,2
    };

    static readonly int[] cosineLookup = CreateCosineLookup();

    internal static Av1InverseTransformResult Transform(
        Av1InverseTransformRequest request)
    {
        int width = request == null
            ? 0
            : Av1ConversionTables.GetTransformWidth(request.TransformSize);
        int height = request == null
            ? 0
            : Av1ConversionTables.GetTransformHeight(request.TransformSize);
        AvifResultCode validation = ValidateTransform(request, width, height);
        Av1InverseTransformResult result = new Av1InverseTransformResult(
            validation, width, height);
        if (validation != AvifResultCode.Success)
        {
            return result;
        }

        int[] coefficients = ExpandCoefficients(request, width, height);
        int rowClampRange = (request ?? throw new InvalidOperationException("Invalid internal state.")).BitDepth + 8;
        int columnClampRange = request.BitDepth + 6;
        if (columnClampRange < 16)
        {
            columnClampRange = 16;
        }
        int rowShift = request.Lossless
            ? 0
            : rowShifts[(int)request.TransformSize];
        int columnShift = request.Lossless ? 0 : 4;
        int widthLog2 = Log2(width);
        int heightLog2 = Log2(height);

        int row = 0;
        while (row < height)
        {
            int[] values = new int[width];
            int column = 0;
            while (column < width)
            {
                values[column] = coefficients[row * width + column];
                if (widthLog2 - heightLog2 == 1
                    || heightLog2 - widthLog2 == 1)
                {
                    values[column] = Round2((long)values[column] * 2896, 12);
                }
                column++;
            }

            if (request.Lossless)
            {
                InverseWalshHadamard(values, 2);
            }
            else
            {
                TransformOneDimension(
                    values,
                    Av1ConversionTables.GetHorizontalTransformType(
                        request.TransformType),
                    widthLog2,
                    rowClampRange);
            }

            column = 0;
            while (column < width)
            {
                int value = Round2(values[column], rowShift);
                result.Residual[row * width + column] = ClampSigned(
                    value, columnClampRange);
                column++;
            }
            row++;
        }

        int columnIndex = 0;
        while (columnIndex < width)
        {
            int[] values = new int[height];
            row = 0;
            while (row < height)
            {
                values[row] = result.Residual[row * width + columnIndex];
                row++;
            }

            if (request.Lossless)
            {
                InverseWalshHadamard(values, 0);
            }
            else
            {
                TransformOneDimension(
                    values,
                    Av1ConversionTables.GetVerticalTransformType(
                        request.TransformType),
                    heightLog2,
                    columnClampRange);
            }

            row = 0;
            while (row < height)
            {
                result.Residual[row * width + columnIndex] =
                    Round2(values[row], columnShift);
                row++;
            }
            columnIndex++;
        }

        return result;
    }

    internal static AvifResultCode Reconstruct(
        Av1InverseTransformRequest request)
    {
        Av1InverseTransformResult transformed = Transform(request);
        if (transformed.Code != AvifResultCode.Success)
        {
            return transformed.Code;
        }
        if (request.Destination == null)
        {
            return AvifResultCode.InvalidArgument;
        }
        AvifResultCode destinationState = request.Destination.GetStateCode();
        if (destinationState != AvifResultCode.Success)
        {
            return destinationState;
        }
        if (request.Destination.BytesPerSample != (request.BitDepth > 8 ? 2 : 1)
            || request.X < 0 || request.Y < 0
            || request.X > request.Destination.Width
            || request.Y > request.Destination.Height
            || transformed.Width > request.Destination.Width - request.X
            || transformed.Height > request.Destination.Height - request.Y)
        {
            return AvifResultCode.InvalidArgument;
        }

        int maximum = (1 << request.BitDepth) - 1;
        bool flipLeftRight = HasHorizontalFlip(request.TransformType);
        bool flipUpDown = HasVerticalFlip(request.TransformType);
        int row = 0;
        while (row < transformed.Height)
        {
            int destinationRow = request.Y + (flipUpDown
                ? transformed.Height - row - 1
                : row);
            Span<byte> samples = request.Destination.GetRowSpan(destinationRow);
            int column = 0;
            while (column < transformed.Width)
            {
                int destinationColumn = request.X + (flipLeftRight
                    ? transformed.Width - column - 1
                    : column);
                int offset = destinationColumn * request.Destination.BytesPerSample;
                int prediction = request.Destination.BytesPerSample == 1
                    ? samples[offset]
                    : samples[offset] | samples[offset + 1] << 8;
                long reconstructed = (long)prediction
                    + transformed.Residual[row * transformed.Width + column];
                if (reconstructed < 0)
                {
                    reconstructed = 0;
                }
                else if (reconstructed > maximum)
                {
                    reconstructed = maximum;
                }
                samples[offset] = IntegerConversion.ToByte(reconstructed);
                if (request.Destination.BytesPerSample == 2)
                {
                    samples[offset + 1] = IntegerConversion.ToByte(reconstructed >> 8);
                }
                column++;
            }
            row++;
        }

        return AvifResultCode.Success;
    }

    internal static Av1TransformSplitResult Split(
        Av1TransformSize transformSize,
        int depth)
    {
        return Split(transformSize, 0, 0, depth);
    }

    internal static Av1TransformSplitResult Split(
        Av1TransformSize transformSize,
        int x,
        int y,
        int depth)
    {
        int width = Av1ConversionTables.GetTransformWidth(transformSize);
        int height = Av1ConversionTables.GetTransformHeight(transformSize);
        if (width == 0 || height == 0 || x < 0 || y < 0 || depth < 0)
        {
            return new Av1TransformSplitResult(
                AvifResultCode.InvalidArgument,
                System.Array.Empty<Av1TransformPartition>());
        }

        System.Collections.Generic.List<Av1TransformPartition> partitions =
            new System.Collections.Generic.List<Av1TransformPartition>();
        partitions.Add(new Av1TransformPartition(x, y, transformSize));
        int level = 0;
        while (level < depth)
        {
            System.Collections.Generic.List<Av1TransformPartition> children =
                new System.Collections.Generic.List<Av1TransformPartition>();
            int index = 0;
            while (index < partitions.Count)
            {
                Av1TransformPartition parent = partitions[index];
                Av1TransformSize childSize =
                    Av1ConversionTables.GetSplitTransformSize(parent.TransformSize);
                if (childSize == parent.TransformSize
                    || childSize == Av1TransformSize.Invalid)
                {
                    children.Add(parent);
                }
                else
                {
                    int parentWidth = Av1ConversionTables.GetTransformWidth(
                        parent.TransformSize);
                    int parentHeight = Av1ConversionTables.GetTransformHeight(
                        parent.TransformSize);
                    int childWidth = Av1ConversionTables.GetTransformWidth(childSize);
                    int childHeight = Av1ConversionTables.GetTransformHeight(childSize);
                    int childY = 0;
                    while (childY < parentHeight)
                    {
                        int childX = 0;
                        while (childX < parentWidth)
                        {
                            children.Add(new Av1TransformPartition(
                                parent.X + childX,
                                parent.Y + childY,
                                childSize));
                            childX += childWidth;
                        }
                        childY += childHeight;
                    }
                }
                index++;
            }
            partitions = children;
            level++;
        }

        return new Av1TransformSplitResult(
            AvifResultCode.Success, partitions.ToArray());
    }

    static int[] ExpandCoefficients(
        Av1InverseTransformRequest? request,
        int width,
        int height)
    {
        int[] expanded = new int[width * height];
        Av1TransformSize adjusted =
            Av1ConversionTables.GetAdjustedTransformSize((request ?? throw new InvalidOperationException("Invalid internal state.")).TransformSize);
        int coefficientWidth = Av1ConversionTables.GetTransformWidth(adjusted);
        int coefficientHeight = Av1ConversionTables.GetTransformHeight(adjusted);
        int endOfBlock = request.EndOfBlock < 0
            ? request.Coefficients.Length
            : request.EndOfBlock;
        int[] scan = Av1ScanTables.GetScan(
            request.TransformSize, request.TransformType);
        int scanIndex = 0;
        while (scanIndex < endOfBlock)
        {
            int position = scan[scanIndex];
            int row = position % coefficientHeight;
            int column = position / coefficientHeight;
            if (column < coefficientWidth)
            {
                expanded[row * width + column] = request.Coefficients[position];
            }
            scanIndex++;
        }
        return expanded;
    }

    static void TransformOneDimension(
        int[] values,
        Av1Transform1DType type,
        int log2Length,
        int clampRange)
    {
        if (type == Av1Transform1DType.Dct)
        {
            InverseDct(values, log2Length, clampRange);
        }
        else if (type == Av1Transform1DType.Adst
            || type == Av1Transform1DType.FlipAdst)
        {
            InverseAdst(values, log2Length, clampRange);
        }
        else
        {
            InverseIdentity(values, clampRange);
        }
    }

    static void InverseDct(int[] values, int log2Length, int clampRange)
    {
        int length = 1 << log2Length;
        int[] copy = (int[])values.Clone();
        int index = 0;
        while (index < length)
        {
            values[index] = copy[BitReverse(log2Length, index)];
            index++;
        }

        int i = 0;
        int j = 0;
        if (log2Length == 6)
        {
            while (i < 16)
            {
                Butterfly(values, 32 + i, 63 - i,
                    63 - 4 * BitReverse(4, i), false, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 8)
            {
                Butterfly(values, 16 + i, 31 - i,
                    6 + (BitReverse(3, 7 - i) << 3), false, clampRange);
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 16)
            {
                Hadamard(values, 32 + i * 2, 33 + i * 2,
                    (i & 1) != 0, clampRange);
                i++;
            }
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 4)
            {
                Butterfly(values, 8 + i, 15 - i,
                    12 + (BitReverse(2, 3 - i) << 4), false, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 8)
            {
                Hadamard(values, 16 + 2 * i, 17 + 2 * i,
                    (i & 1) != 0, clampRange);
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 4)
            {
                j = 0;
                while (j < 2)
                {
                    Butterfly(values, 62 - i * 4 - j, 33 + i * 4 + j,
                        60 - 16 * BitReverse(2, i) + 64 * j,
                        true, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length >= 3)
        {
            i = 0;
            while (i < 2)
            {
                Butterfly(values, 4 + i, 7 - i, 56 - 32 * i,
                    false, clampRange);
                i++;
            }
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 4)
            {
                Hadamard(values, 8 + 2 * i, 9 + 2 * i,
                    (i & 1) != 0, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 2)
            {
                j = 0;
                while (j < 2)
                {
                    Butterfly(values, 30 - 4 * i - j, 17 + 4 * i + j,
                        24 + (j << 6) + ((1 - i) << 5), true, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 8)
            {
                j = 0;
                while (j < 2)
                {
                    Hadamard(values, 32 + i * 4 + j, 35 + i * 4 - j,
                        (i & 1) != 0, clampRange);
                    j++;
                }
                i++;
            }
        }

        i = 0;
        while (i < 2)
        {
            Butterfly(values, 2 * i, 2 * i + 1, 32 + 16 * i,
                1 - i != 0, clampRange);
            i++;
        }
        if (log2Length >= 3)
        {
            i = 0;
            while (i < 2)
            {
                Hadamard(values, 4 + 2 * i, 5 + 2 * i,
                    i != 0, clampRange);
                i++;
            }
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 2)
            {
                Butterfly(values, 14 - i, 9 + i, 48 + 64 * i,
                    true, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 4)
            {
                j = 0;
                while (j < 2)
                {
                    Hadamard(values, 16 + 4 * i + j, 19 + 4 * i - j,
                        (i & 1) != 0, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 2)
            {
                j = 0;
                while (j < 4)
                {
                    Butterfly(values, 61 - i * 8 - j, 34 + i * 8 + j,
                        56 - i * 32 + (j >> 1) * 64, true, clampRange);
                    j++;
                }
                i++;
            }
        }

        i = 0;
        while (i < 2)
        {
            Hadamard(values, i, 3 - i, false, clampRange);
            i++;
        }
        if (log2Length >= 3)
        {
            Butterfly(values, 6, 5, 32, true, clampRange);
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 2)
            {
                j = 0;
                while (j < 2)
                {
                    Hadamard(values, 8 + 4 * i + j, 11 + 4 * i - j,
                        i != 0, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 4)
            {
                Butterfly(values, 29 - i, 18 + i,
                    48 + (i >> 1) * 64, true, clampRange);
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 4)
            {
                j = 0;
                while (j < 4)
                {
                    Hadamard(values, 32 + 8 * i + j, 39 + 8 * i - j,
                        (i & 1) != 0, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length >= 3)
        {
            i = 0;
            while (i < 4)
            {
                Hadamard(values, i, 7 - i, false, clampRange);
                i++;
            }
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 2)
            {
                Butterfly(values, 13 - i, 10 + i, 32, true, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 2)
            {
                j = 0;
                while (j < 4)
                {
                    Hadamard(values, 16 + i * 8 + j, 23 + i * 8 - j,
                        i != 0, clampRange);
                    j++;
                }
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 8)
            {
                Butterfly(values, 59 - i, 36 + i,
                    i < 4 ? 48 : 112, true, clampRange);
                i++;
            }
        }
        if (log2Length >= 4)
        {
            i = 0;
            while (i < 8)
            {
                Hadamard(values, i, 15 - i, false, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 4)
            {
                Butterfly(values, 27 - i, 20 + i, 32, true, clampRange);
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 8)
            {
                Hadamard(values, 32 + i, 47 - i, false, clampRange);
                Hadamard(values, 48 + i, 63 - i, true, clampRange);
                i++;
            }
        }
        if (log2Length >= 5)
        {
            i = 0;
            while (i < 16)
            {
                Hadamard(values, i, 31 - i, false, clampRange);
                i++;
            }
        }
        if (log2Length == 6)
        {
            i = 0;
            while (i < 8)
            {
                Butterfly(values, 55 - i, 40 + i, 32, true, clampRange);
                i++;
            }
            i = 0;
            while (i < 32)
            {
                Hadamard(values, i, 63 - i, false, clampRange);
                i++;
            }
        }
    }

    static void InverseAdst(int[] values, int log2Length, int clampRange)
    {
        if (log2Length == 2)
        {
            InverseAdst4(values, clampRange);
            return;
        }

        int length = 1 << log2Length;
        int[] copy = (int[])values.Clone();
        int index = 0;
        while (index < length)
        {
            int source = (index & 1) != 0 ? index - 1 : length - index - 1;
            values[index] = copy[source];
            index++;
        }

        int i = 0;
        int j = 0;
        int firstCount = log2Length == 3 ? 4 : 8;
        while (i < firstCount)
        {
            Butterfly(values, 2 * i, 2 * i + 1,
                (log2Length == 3 ? 60 - 16 * i : 62 - 8 * i),
                true, clampRange);
            i++;
        }
        i = 0;
        while (i < length / 2)
        {
            Hadamard(values, i, length / 2 + i, false, clampRange);
            i++;
        }
        if (log2Length == 4)
        {
            i = 0;
            while (i < 2)
            {
                Butterfly(values, 8 + 2 * i, 9 + 2 * i,
                    56 - 32 * i, true, clampRange);
                Butterfly(values, 13 + 2 * i, 12 + 2 * i,
                    8 + 32 * i, true, clampRange);
                i++;
            }
            j = 0;
            while (j < 2)
            {
                i = 0;
                while (i < 4)
                {
                    Hadamard(values, 8 * j + i, 4 + 8 * j + i,
                        false, clampRange);
                    i++;
                }
                j++;
            }
        }
        j = 0;
        int groupCount = log2Length == 3 ? 1 : 2;
        while (j < groupCount)
        {
            i = 0;
            while (i < 2)
            {
                Butterfly(values, 4 * j + 3 * i, 4 * j + 5 + i,
                    48 - 32 * i, true, clampRange);
                i++;
            }
            j++;
        }
        j = 0;
        int pairGroupCount = log2Length == 3 ? 2 : 4;
        while (j < pairGroupCount)
        {
            i = 0;
            while (i < 2)
            {
                Hadamard(values, 4 * j + i, 2 + 4 * j + i,
                    false, clampRange);
                i++;
            }
            j++;
        }
        i = 0;
        while (i < length / 4)
        {
            Butterfly(values, 2 + 4 * i, 3 + 4 * i, 32,
                true, clampRange);
            i++;
        }

        copy = (int[])values.Clone();
        index = 0;
        while (index < length)
        {
            int a = (index >> 3) & 1;
            int b = (index >> 2 & 1) ^ (index >> 3 & 1);
            int c = (index >> 1 & 1) ^ (index >> 2 & 1);
            int d = (index & 1) ^ (index >> 1 & 1);
            int target = ((d << 3) | (c << 2) | (b << 1) | a)
                >> (4 - log2Length);
            values[index] = (index & 1) != 0 ? -copy[target] : copy[target];
            index++;
        }
    }

    static void InverseAdst4(int[] values, int clampRange)
    {
        long[] s = new long[7];
        long[] x = new long[4];
        s[0] = 1321L * values[0];
        s[1] = 2482L * values[0];
        s[2] = 3344L * values[1];
        s[3] = 3803L * values[2];
        s[4] = 1321L * values[2];
        s[5] = 2482L * values[3];
        s[6] = 3803L * values[3];
        long difference = values[0] - values[2];
        long combined = difference + values[3];
        s[0] += s[3];
        s[1] -= s[4];
        s[3] = s[2];
        s[2] = 3344L * combined;
        s[0] += s[5];
        s[1] -= s[6];
        x[0] = s[0] + s[3];
        x[1] = s[1] + s[3];
        x[2] = s[2];
        x[3] = s[0] + s[1] - s[3];
        int index = 0;
        while (index < 4)
        {
            values[index] = ClampSigned(Round2(x[index], 12), clampRange);
            index++;
        }
    }

    static void InverseIdentity(int[] values, int clampRange)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values.Length == 4)
            {
                values[index] = ClampSigned(
                    Round2((long)values[index] * 5793, 12), clampRange);
            }
            else if (values.Length == 8)
            {
                values[index] = ClampSigned((long)values[index] * 2, clampRange);
            }
            else if (values.Length == 16)
            {
                values[index] = ClampSigned(
                    Round2((long)values[index] * 11586, 12), clampRange);
            }
            else
            {
                values[index] = ClampSigned((long)values[index] * 4, clampRange);
            }
            index++;
        }
    }

    static void InverseWalshHadamard(int[] values, int shift)
    {
        int a = values[0] >> shift;
        int c = values[1] >> shift;
        int d = values[2] >> shift;
        int b = values[3] >> shift;
        a += c;
        d -= b;
        int e = (a - d) >> 1;
        b = e - b;
        c = e - c;
        a -= b;
        d += c;
        values[0] = a;
        values[1] = b;
        values[2] = c;
        values[3] = d;
    }

    static void Butterfly(
        int[] values,
        int first,
        int second,
        int angle,
        bool flip,
        int clampRange)
    {
        long firstValue = values[first];
        long secondValue = values[second];
        int cosine = Cos128(angle);
        int sine = Cos128(64 - angle);
        int firstResult = ClampSigned(Round2(
            firstValue * cosine - secondValue * sine, 12), clampRange);
        int secondResult = ClampSigned(Round2(
            firstValue * sine + secondValue * cosine, 12), clampRange);
        values[first] = flip ? secondResult : firstResult;
        values[second] = flip ? firstResult : secondResult;
    }

    static void Hadamard(
        int[] values,
        int first,
        int second,
        bool flip,
        int clampRange)
    {
        int firstIndex = flip ? second : first;
        int secondIndex = flip ? first : second;
        int x = values[firstIndex];
        int y = values[secondIndex];
        values[firstIndex] = ClampSigned((long)x + y, clampRange);
        values[secondIndex] = ClampSigned((long)x - y, clampRange);
    }

    static int Cos128(int angle)
    {
        int normalized = angle & 255;
        int lookup = normalized;
        int sign = 1;
        if (normalized > 64 && normalized <= 128)
        {
            lookup = 128 - normalized;
            sign = -1;
        }
        else if (normalized > 128 && normalized <= 192)
        {
            lookup = normalized - 128;
            sign = -1;
        }
        else if (normalized > 192)
        {
            lookup = 256 - normalized;
        }
        return sign * cosineLookup[lookup];
    }

    static int[] CreateCosineLookup()
    {
        int[] values = new int[65];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = (int)System.Math.Round(
                4096.0 * System.Math.Cos(System.Math.PI * index / 128.0));
            index++;
        }
        return values;
    }

    static int BitReverse(int bits, int value)
    {
        int reversed = 0;
        int index = 0;
        while (index < bits)
        {
            reversed |= (value >> index & 1) << (bits - index - 1);
            index++;
        }
        return reversed;
    }

    static int Log2(int value)
    {
        int log = 0;
        while (value > 1)
        {
            value >>= 1;
            log++;
        }
        return log;
    }

    static int Round2(long value, int shift)
    {
        if (shift == 0)
        {
            return ClampInt(value);
        }
        return ClampInt((value + (1L << shift - 1)) >> shift);
    }

    static int ClampSigned(long value, int bits)
    {
        long minimum = -(1L << bits - 1);
        long maximum = (1L << bits - 1) - 1;
        if (value < minimum)
        {
            return (int)minimum;
        }
        if (value > maximum)
        {
            return (int)maximum;
        }
        return (int)value;
    }

    static int ClampInt(long value)
    {
        if (value < int.MinValue)
        {
            return int.MinValue;
        }
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }
        return (int)value;
    }

    static bool HasVerticalFlip(Av1TransformType type)
    {
        return type == Av1TransformType.FlipAdstDct
            || type == Av1TransformType.FlipAdstFlipAdst
            || type == Av1TransformType.FlipAdstAdst
            || type == Av1TransformType.VerticalFlipAdst;
    }

    static bool HasHorizontalFlip(Av1TransformType type)
    {
        return type == Av1TransformType.DctFlipAdst
            || type == Av1TransformType.FlipAdstFlipAdst
            || type == Av1TransformType.AdstFlipAdst
            || type == Av1TransformType.HorizontalFlipAdst;
    }

    static AvifResultCode ValidateTransform(
        Av1InverseTransformRequest? request,
        int width,
        int height)
    {
        if (request == null || width == 0 || height == 0
            || request.Coefficients == null
            || request.Coefficients.Length
                != Av1ConversionTables.GetTransformCoefficientCount(
                    request.TransformSize)
            || request.EndOfBlock < -1
            || request.EndOfBlock > request.Coefficients.Length
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.TransformType < Av1TransformType.DctDct
            || request.TransformType > Av1TransformType.HorizontalFlipAdst)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.Lossless
            && (request.TransformSize != Av1TransformSize.Transform4x4
                || request.TransformType != Av1TransformType.DctDct))
        {
            return AvifResultCode.InvalidArgument;
        }
        Av1Transform1DType horizontal =
            Av1ConversionTables.GetHorizontalTransformType(request.TransformType);
        Av1Transform1DType vertical =
            Av1ConversionTables.GetVerticalTransformType(request.TransformType);
        if (!SupportsLength(horizontal, width) || !SupportsLength(vertical, height))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool SupportsLength(Av1Transform1DType type, int length)
    {
        if (type == Av1Transform1DType.Dct)
        {
            return length >= 4 && length <= 64;
        }
        if (type == Av1Transform1DType.Identity)
        {
            return length >= 4 && length <= 32;
        }
        return length >= 4 && length <= 16;
    }
}
