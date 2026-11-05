using System;

namespace avif;

internal class Av1InterPredictionRequest
{
    internal PlaneBuffer? Destination;
    internal PlaneBuffer? Reference;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal Av1MotionVector MotionVector;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int ScaleXNumerator;
    internal int ScaleXDenominator;
    internal int ScaleYNumerator;
    internal int ScaleYDenominator;
    internal Av1InterpolationFilter HorizontalFilter;
    internal Av1InterpolationFilter VerticalFilter;
    internal Av1MotionMode MotionMode;
    internal Av1WarpModelType GlobalMotionType;
    internal int[] WarpParameters;
    internal int[] AbovePrediction;
    internal int AboveStride;
    internal int AboveOverlap;
    internal int[] LeftPrediction;
    internal int LeftStride;
    internal int LeftOverlap;

    internal Av1InterPredictionRequest()
    {
        MotionVector = new Av1MotionVector(0, 0);
        ScaleXNumerator = 1;
        ScaleXDenominator = 1;
        ScaleYNumerator = 1;
        ScaleYDenominator = 1;
        HorizontalFilter = Av1InterpolationFilter.EightTap;
        VerticalFilter = Av1InterpolationFilter.EightTap;
        MotionMode = Av1MotionMode.SimpleTranslation;
        GlobalMotionType = Av1WarpModelType.Identity;
        WarpParameters = System.Array.Empty<int>();
        AbovePrediction = System.Array.Empty<int>();
        LeftPrediction = System.Array.Empty<int>();
    }
}

// AV1 specification sections 7.11.3, 7.11.3.3, 7.11.3.5, and 7.11.3.6.
internal static class Av1InterPredictor
{
    static readonly int[] bilinearFilters = new int[]
    {
        0,0,0,128,0,0,0,0, 0,0,0,120,8,0,0,0,
        0,0,0,112,16,0,0,0, 0,0,0,104,24,0,0,0,
        0,0,0,96,32,0,0,0, 0,0,0,88,40,0,0,0,
        0,0,0,80,48,0,0,0, 0,0,0,72,56,0,0,0,
        0,0,0,64,64,0,0,0, 0,0,0,56,72,0,0,0,
        0,0,0,48,80,0,0,0, 0,0,0,40,88,0,0,0,
        0,0,0,32,96,0,0,0, 0,0,0,24,104,0,0,0,
        0,0,0,16,112,0,0,0, 0,0,0,8,120,0,0,0
    };

    static readonly int[] regularFilters = new int[]
    {
        0,0,0,128,0,0,0,0, 0,2,-6,126,8,-2,0,0,
        0,2,-10,122,18,-4,0,0, 0,2,-12,116,28,-8,2,0,
        0,2,-14,110,38,-10,2,0, 0,2,-14,102,48,-12,2,0,
        0,2,-16,94,58,-12,2,0, 0,2,-14,84,66,-12,2,0,
        0,2,-14,76,76,-14,2,0, 0,2,-12,66,84,-14,2,0,
        0,2,-12,58,94,-16,2,0, 0,2,-12,48,102,-14,2,0,
        0,2,-10,38,110,-14,2,0, 0,2,-8,28,116,-12,2,0,
        0,0,-4,18,122,-10,2,0, 0,0,-2,8,126,-6,2,0
    };

    static readonly int[] smoothFilters = new int[]
    {
        0,0,0,128,0,0,0,0, 0,2,28,62,34,2,0,0,
        0,0,26,62,36,4,0,0, 0,0,22,62,40,4,0,0,
        0,0,20,60,42,6,0,0, 0,0,18,58,44,8,0,0,
        0,0,16,56,46,10,0,0, 0,-2,16,54,48,12,0,0,
        0,-2,14,52,52,14,-2,0, 0,0,12,48,54,16,-2,0,
        0,0,10,46,56,16,0,0, 0,0,8,44,58,18,0,0,
        0,0,6,42,60,20,0,0, 0,0,4,40,62,22,0,0,
        0,0,4,36,62,26,0,0, 0,0,2,34,62,28,2,0
    };

    static readonly int[] sharpFilters = new int[]
    {
        0,0,0,128,0,0,0,0, -2,2,-6,126,8,-2,2,0,
        -2,6,-12,124,16,-6,4,-2, -2,8,-18,120,26,-10,6,-2,
        -4,10,-22,116,38,-14,6,-2, -4,10,-22,108,48,-18,8,-2,
        -4,10,-24,100,60,-20,8,-2, -4,10,-24,90,70,-22,10,-2,
        -4,12,-24,80,80,-24,12,-4, -2,10,-22,70,90,-24,10,-4,
        -2,8,-20,60,100,-24,10,-4, -2,8,-18,48,108,-22,10,-4,
        -2,6,-14,38,116,-22,10,-4, -2,6,-10,26,120,-18,8,-2,
        -2,4,-6,16,124,-12,6,-2, 0,2,-2,8,126,-6,2,-2
    };

    static readonly int[] regularFourTapFilters = new int[]
    {
        0,0,0,128,0,0,0,0, 0,0,-4,126,8,-2,0,0,
        0,0,-8,122,18,-4,0,0, 0,0,-10,116,28,-6,0,0,
        0,0,-12,110,38,-8,0,0, 0,0,-12,102,48,-10,0,0,
        0,0,-14,94,58,-10,0,0, 0,0,-12,84,66,-10,0,0,
        0,0,-12,76,76,-12,0,0, 0,0,-10,66,84,-12,0,0,
        0,0,-10,58,94,-14,0,0, 0,0,-10,48,102,-12,0,0,
        0,0,-8,38,110,-12,0,0, 0,0,-6,28,116,-10,0,0,
        0,0,-4,18,122,-8,0,0, 0,0,-2,8,126,-4,0,0
    };

    static readonly int[] smoothFourTapFilters = new int[]
    {
        0,0,0,128,0,0,0,0, 0,0,30,62,34,2,0,0,
        0,0,26,62,36,4,0,0, 0,0,22,62,40,4,0,0,
        0,0,20,60,42,6,0,0, 0,0,18,58,44,8,0,0,
        0,0,16,56,46,10,0,0, 0,0,14,54,48,12,0,0,
        0,0,12,52,52,12,0,0, 0,0,12,48,54,14,0,0,
        0,0,10,46,56,16,0,0, 0,0,8,44,58,18,0,0,
        0,0,6,42,60,20,0,0, 0,0,4,40,62,22,0,0,
        0,0,4,36,62,26,0,0, 0,0,2,34,62,30,0,0
    };

    static readonly int[] obmcMask1 = new int[] { 64 };
    static readonly int[] obmcMask2 = new int[] { 45,64 };
    static readonly int[] obmcMask4 = new int[] { 39,50,59,64 };
    static readonly int[] obmcMask8 = new int[] { 36,42,48,53,57,61,64,64 };
    static readonly int[] obmcMask16 = new int[]
    {
        34,37,40,43,46,49,52,54,56,58,60,61,64,64,64,64
    };
    static readonly int[] obmcMask32 = new int[]
    {
        33,35,36,38,40,41,43,44,45,47,48,50,51,52,53,55,
        56,57,58,59,60,60,61,62,64,64,64,64,64,64,64,64
    };
    static readonly int[] obmcMask64 = new int[]
    {
        33,34,35,35,36,37,38,39,40,40,41,42,43,44,44,44,
        45,46,47,47,48,49,50,51,51,51,52,52,53,54,55,56,
        56,56,57,57,58,58,59,60,60,60,60,60,61,62,62,62,
        62,62,63,63,63,63,64,64,64,64,64,64,64,64,64,64
    };

    internal static AvifResultCode Predict(Av1InterPredictionRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[] prediction = new int[request.Width * request.Height];
        int[] position = new int[2];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                GetSourcePosition(request, column, row, position);
                int sourceX = position[0];
                int sourceY = position[1];
                prediction[row * request.Width + column] = Interpolate(
                    request, sourceX, sourceY);
                column++;
            }
            row++;
        }

        ApplyObmc(request, prediction);
        WritePrediction(request, prediction);
        return AvifResultCode.Success;
    }

    static void GetSourcePosition(
        Av1InterPredictionRequest request,
        int column,
        int row,
        int[] position)
    {
        int sourceX;
        int sourceY;
        int destinationX = request.X + column;
        int destinationY = request.Y + row;
        bool warped = request.MotionMode == Av1MotionMode.LocalWarp
            || request.GlobalMotionType != Av1WarpModelType.Identity;
        if (warped)
        {
            int[] parameters = request.WarpParameters;
            long lumaX = (long)destinationX * (request.SubsamplingX ? 2 : 1);
            long lumaY = (long)destinationY * (request.SubsamplingY ? 2 : 1);
            long x = parameters[2] * lumaX
                + parameters[3] * lumaY + parameters[0];
            long y = parameters[4] * lumaX
                + parameters[5] * lumaY + parameters[1];
            sourceX = ClipLong(int.MinValue, int.MaxValue,
                RoundShift(x, 12 + (request.SubsamplingX ? 1 : 0)));
            sourceY = ClipLong(int.MinValue, int.MaxValue,
                RoundShift(y, 12 + (request.SubsamplingY ? 1 : 0)));
        }
        else
        {
            int horizontalDivisor = request.SubsamplingX ? 2 : 1;
            int verticalDivisor = request.SubsamplingY ? 2 : 1;
            sourceX = DivideRound(
                (long)destinationX * request.ScaleXNumerator * 16,
                request.ScaleXDenominator)
                + DivideRound((long)request.MotionVector.Column * 2,
                    horizontalDivisor);
            sourceY = DivideRound(
                (long)destinationY * request.ScaleYNumerator * 16,
                request.ScaleYDenominator)
                + DivideRound((long)request.MotionVector.Row * 2,
                    verticalDivisor);
        }

        position[0] = sourceX;
        position[1] = sourceY;
    }

    static int Interpolate(Av1InterPredictionRequest request, int sourceX, int sourceY)
    {
        int baseX = FloorDivide(sourceX, 16);
        int baseY = FloorDivide(sourceY, 16);
        int phaseX = sourceX - baseX * 16;
        int phaseY = sourceY - baseY * 16;
        int[] horizontalKernel = SelectFilter(
            request.HorizontalFilter, request.Width);
        int[] verticalKernel = SelectFilter(
            request.VerticalFilter, request.Height);
        long verticalSum = 0;
        int verticalTap = 0;
        while (verticalTap < 8)
        {
            long horizontalSum = 0;
            int horizontalTap = 0;
            while (horizontalTap < 8)
            {
                int sample = ReadReference(
                    request.Reference,
                    baseX + horizontalTap - 3,
                    baseY + verticalTap - 3);
                horizontalSum += (long)sample
                    * horizontalKernel[phaseX * 8 + horizontalTap];
                horizontalTap++;
            }

            long intermediate = RoundShift(horizontalSum, 3);
            verticalSum += intermediate
                * verticalKernel[phaseY * 8 + verticalTap];
            verticalTap++;
        }

        return Clip(0, (1 << request.BitDepth) - 1,
            (int)RoundShift(verticalSum, 11));
    }

    static void ApplyObmc(Av1InterPredictionRequest request, int[] prediction)
    {
        if (request.MotionMode != Av1MotionMode.Obmc)
        {
            return;
        }

        int[] aboveMask = GetObmcMask(request.AboveOverlap);
        int row = 0;
        while (row < request.AboveOverlap)
        {
            int column = 0;
            while (column < request.Width)
            {
                int offset = row * request.Width + column;
                int neighbor = request.AbovePrediction[row * request.AboveStride + column];
                prediction[offset] = (aboveMask[row] * prediction[offset]
                    + (64 - aboveMask[row]) * neighbor + 32) >> 6;
                column++;
            }
            row++;
        }

        int[] leftMask = GetObmcMask(request.LeftOverlap);
        row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.LeftOverlap)
            {
                int offset = row * request.Width + column;
                int neighbor = request.LeftPrediction[row * request.LeftStride + column];
                prediction[offset] = (leftMask[column] * prediction[offset]
                    + (64 - leftMask[column]) * neighbor + 32) >> 6;
                column++;
            }
            row++;
        }
    }

    static void WritePrediction(Av1InterPredictionRequest request, int[] prediction)
    {
        int row = 0;
        while (row < request.Height)
        {
            Span<byte> bytes = (request.Destination ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(request.Y + row);
            int column = 0;
            while (column < request.Width)
            {
                int value = prediction[row * request.Width + column];
                int offset = (request.X + column) * request.Destination.BytesPerSample;
                bytes[offset] = IntegerConversion.ToByte(value);
                if (request.Destination.BytesPerSample == 2)
                {
                    bytes[offset + 1] = IntegerConversion.ToByte(value >> 8);
                }
                column++;
            }
            row++;
        }
    }

    static int ReadReference(PlaneBuffer? reference, int column, int row)
    {
        int clippedColumn = Clip(0, (reference ?? throw new InvalidOperationException("Invalid internal state.")).Width - 1, column);
        int clippedRow = Clip(0, reference.Height - 1, row);
        Span<byte> bytes = reference.GetRowSpan(clippedRow);
        int offset = clippedColumn * reference.BytesPerSample;
        return reference.BytesPerSample == 1
            ? bytes[offset]
            : bytes[offset] | bytes[offset + 1] << 8;
    }

    static int[] SelectFilter(Av1InterpolationFilter filter, int blockSize)
    {
        if (filter == Av1InterpolationFilter.Bilinear)
        {
            return bilinearFilters;
        }
        if (blockSize <= 4)
        {
            return filter == Av1InterpolationFilter.EightTapSmooth
                ? smoothFourTapFilters
                : regularFourTapFilters;
        }
        if (filter == Av1InterpolationFilter.EightTapSmooth)
        {
            return smoothFilters;
        }
        return filter == Av1InterpolationFilter.EightTapSharp
            ? sharpFilters
            : regularFilters;
    }

    static int[] GetObmcMask(int length)
    {
        if (length == 1)
        {
            return obmcMask1;
        }
        if (length == 2)
        {
            return obmcMask2;
        }
        if (length == 4)
        {
            return obmcMask4;
        }
        if (length == 8)
        {
            return obmcMask8;
        }
        if (length == 16)
        {
            return obmcMask16;
        }
        if (length == 32)
        {
            return obmcMask32;
        }
        return length == 64 ? obmcMask64 : System.Array.Empty<int>();
    }

    static AvifResultCode Validate(Av1InterPredictionRequest request)
    {
        if (request == null || request.Destination == null || request.Reference == null
            || request.MotionVector == null
            || request.Destination.Code != AvifResultCode.Success
            || request.Reference.Code != AvifResultCode.Success
            || request.Destination.Closed || request.Reference.Closed
            || request.Width <= 0 || request.Height <= 0
            || request.Width > 128 || request.Height > 128
            || request.X < 0 || request.Y < 0
            || request.X > request.Destination.Width - request.Width
            || request.Y > request.Destination.Height - request.Height
            || request.BitDepth != 8 && request.BitDepth != 10 && request.BitDepth != 12
            || request.Destination.BytesPerSample != (request.BitDepth > 8 ? 2 : 1)
            || request.Reference.BytesPerSample != request.Destination.BytesPerSample
            || request.ScaleXNumerator <= 0 || request.ScaleXDenominator <= 0
            || request.ScaleYNumerator <= 0 || request.ScaleYDenominator <= 0
            || request.ScaleXNumerator > 1048576
            || request.ScaleXDenominator > 1048576
            || request.ScaleYNumerator > 1048576
            || request.ScaleYDenominator > 1048576
            || !ValidFilter(request.HorizontalFilter)
            || !ValidFilter(request.VerticalFilter)
            || request.MotionMode < Av1MotionMode.SimpleTranslation
            || request.MotionMode > Av1MotionMode.LocalWarp
            || request.GlobalMotionType < Av1WarpModelType.Identity
            || request.GlobalMotionType > Av1WarpModelType.Affine)
        {
            return AvifResultCode.InvalidArgument;
        }

        bool warped = request.MotionMode == Av1MotionMode.LocalWarp
            || request.GlobalMotionType != Av1WarpModelType.Identity;
        if (warped && (request.WarpParameters == null
            || request.WarpParameters.Length < 6
            || !ValidWarpParameters(request.WarpParameters)))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.MotionMode == Av1MotionMode.Obmc
            && (!ValidObmc(request.AbovePrediction, request.AboveStride,
                    request.AboveOverlap, request.Width, request.Height, true)
                || !ValidObmc(request.LeftPrediction, request.LeftStride,
                    request.LeftOverlap, request.Width, request.Height, false)))
        {
            return AvifResultCode.InvalidArgument;
        }

        return AvifResultCode.Success;
    }

    static bool ValidFilter(Av1InterpolationFilter filter)
    {
        return filter >= Av1InterpolationFilter.EightTap
            && filter <= Av1InterpolationFilter.Bilinear;
    }

    static bool ValidObmc(
        int[] prediction,
        int stride,
        int overlap,
        int width,
        int height,
        bool above)
    {
        if (overlap == 0)
        {
            return prediction != null;
        }
        if (prediction == null || GetObmcMask(overlap).Length == 0
            || overlap > (above ? height : width))
        {
            return false;
        }

        int rowCount = above ? overlap : height;
        int columnCount = above ? width : overlap;
        return stride >= columnCount
            && prediction.Length >= (long)(rowCount - 1) * stride + columnCount;
    }

    static bool ValidWarpParameters(int[] parameters)
    {
        int index = 0;
        while (index < 6)
        {
            if (parameters[index] < -1073741824
                || parameters[index] > 1073741824)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static int FloorDivide(int value, int divisor)
    {
        if (value >= 0)
        {
            return value / divisor;
        }
        return -((-value + divisor - 1) / divisor);
    }

    static int DivideRound(long numerator, int denominator)
    {
        if (numerator < 0)
        {
            return ClipLong(int.MinValue, int.MaxValue,
                -((-numerator + (denominator >> 1)) / denominator));
        }
        return ClipLong(int.MinValue, int.MaxValue,
            (numerator + (denominator >> 1)) / denominator);
    }

    static long RoundShift(long value, int bits)
    {
        if (value < 0)
        {
            return -((-value + (1L << (bits - 1))) >> bits);
        }
        return (value + (1L << (bits - 1))) >> bits;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : value;
    }

    static int ClipLong(int minimum, int maximum, long value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : (int)value;
    }
}
