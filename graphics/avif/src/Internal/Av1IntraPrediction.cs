using System;

namespace avif;

internal enum Av1FilterIntraMode
{
    FilterDc = 0,
    FilterVertical = 1,
    FilterHorizontal = 2,
    FilterDiagonal = 3,
    FilterPaeth = 4,
    None = 5
}

internal class Av1IntraPredictionRequest
{
    internal PlaneBuffer? Destination;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal Av1PredictionMode Mode;
    internal int AngleDelta;
    internal bool HaveAbove;
    internal bool HaveLeft;
    internal bool HaveAboveRight;
    internal bool HaveBelowLeft;
    internal bool UseFilterIntra;
    internal Av1FilterIntraMode FilterIntraMode;
    internal bool UsePalette;
    internal int[]? PaletteColors;
    internal byte[]? PaletteMap;
    internal int PaletteStride;
    internal bool UseCfl;
    internal int[]? CflLuma;
    internal int CflLumaWidth;
    internal int CflLumaHeight;
    internal int CflLumaStride;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int CflAlphaQ3;
    internal bool UseIntraBlockCopy;
    internal int IntraBlockCopySourceX;
    internal int IntraBlockCopySourceY;

    internal Av1IntraPredictionRequest()
    {
        Mode = Av1PredictionMode.Dc;
        FilterIntraMode = Av1FilterIntraMode.None;
    }
}

// AV1 specification sections 7.11.2, 7.11.4, and 7.11.5.
internal static class Av1IntraPredictor
{
    static readonly int[] modeAngles = new int[]
    {
        0, 90, 180, 45, 135, 113, 157, 203, 67
    };

    static readonly int[] directionalDerivatives = new int[]
    {
        0, 0, 0, 1023, 0, 0, 547, 0, 0, 372,
        0, 0, 0, 0, 273, 0, 0, 215, 0, 0,
        178, 0, 0, 151, 0, 0, 132, 0, 0, 116,
        0, 0, 102, 0, 0, 0, 90, 0, 0, 80,
        0, 0, 71, 0, 0, 64, 0, 0, 57, 0,
        0, 51, 0, 0, 45, 0, 0, 0, 40, 0,
        0, 35, 0, 0, 31, 0, 0, 27, 0, 0,
        23, 0, 0, 19, 0, 0, 15, 0, 0, 0,
        0, 11, 0, 0, 7, 0, 0, 3, 0, 0
    };

    static readonly int[] smoothWeights = new int[]
    {
        255, 149, 85, 64,
        255, 197, 146, 105, 73, 50, 37, 32,
        255, 225, 196, 170, 145, 123, 102, 84,
        68, 54, 43, 33, 26, 20, 17, 16,
        255, 240, 225, 210, 196, 182, 169, 157,
        145, 133, 122, 111, 101, 92, 83, 74,
        66, 59, 52, 45, 39, 34, 29, 25,
        21, 17, 14, 12, 10, 9, 8, 8,
        255, 248, 240, 233, 225, 218, 210, 203,
        196, 189, 182, 176, 169, 163, 156, 150,
        144, 138, 133, 127, 121, 116, 111, 106,
        101, 96, 91, 86, 82, 77, 73, 69,
        65, 61, 57, 54, 50, 47, 44, 41,
        38, 35, 32, 29, 27, 25, 22, 20,
        18, 16, 15, 13, 12, 10, 9, 8,
        7, 6, 6, 5, 5, 4, 4, 4
    };

    static readonly int[] filterIntraTaps = new int[]
    {
        -6,10,0,0,0,12,0,0, -5,2,10,0,0,9,0,0,
        -3,1,1,10,0,7,0,0, -3,1,1,2,10,5,0,0,
        -4,6,0,0,0,2,12,0, -3,2,6,0,0,2,9,0,
        -3,2,2,6,0,2,7,0, -3,1,2,2,6,3,5,0,

        -10,16,0,0,0,10,0,0, -6,0,16,0,0,6,0,0,
        -4,0,0,16,0,4,0,0, -2,0,0,0,16,2,0,0,
        -10,16,0,0,0,0,10,0, -6,0,16,0,0,0,6,0,
        -4,0,0,16,0,0,4,0, -2,0,0,0,16,0,2,0,

        -8,8,0,0,0,16,0,0, -8,0,8,0,0,16,0,0,
        -8,0,0,8,0,16,0,0, -8,0,0,0,8,16,0,0,
        -4,4,0,0,0,0,16,0, -4,0,4,0,0,0,16,0,
        -4,0,0,4,0,0,16,0, -4,0,0,0,4,0,16,0,

        -2,8,0,0,0,10,0,0, -1,3,8,0,0,6,0,0,
        -1,2,3,8,0,4,0,0, 0,1,2,3,8,2,0,0,
        -1,4,0,0,0,3,10,0, -1,3,4,0,0,4,6,0,
        -1,2,3,4,0,4,4,0, -1,2,2,3,4,3,3,0,

        -12,14,0,0,0,14,0,0, -10,0,14,0,0,12,0,0,
        -9,0,0,14,0,11,0,0, -8,0,0,0,14,10,0,0,
        -10,12,0,0,0,0,14,0, -9,1,12,0,0,0,12,0,
        -8,0,0,12,0,1,11,0, -7,0,0,1,12,1,9,0
    };

    internal static AvifResultCode Predict(Av1IntraPredictionRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        if (request.UseIntraBlockCopy)
        {
            return CopyIntraBlock(request);
        }

        if (request.UsePalette)
        {
            return PredictPalette(request);
        }

        int[] above = new int[request.Width + request.Height + 1];
        int[] left = new int[request.Width + request.Height + 1];
        int topLeft = PrepareEdges(request, above, left);
        int[] prediction = new int[request.Width * request.Height];

        if (request.UseCfl)
        {
            PredictDc(request, above, left, prediction);
            ApplyCfl(request, prediction);
        }
        else if (request.UseFilterIntra)
        {
            PredictFilterIntra(request, above, left, topLeft, prediction);
        }
        else if (request.Mode >= Av1PredictionMode.Vertical
            && request.Mode <= Av1PredictionMode.Directional67)
        {
            PredictDirectional(request, above, left, topLeft, prediction);
        }
        else if (request.Mode == Av1PredictionMode.Dc)
        {
            PredictDc(request, above, left, prediction);
        }
        else if (request.Mode == Av1PredictionMode.Smooth
            || request.Mode == Av1PredictionMode.SmoothVertical
            || request.Mode == Av1PredictionMode.SmoothHorizontal)
        {
            PredictSmooth(request, above, left, prediction);
        }
        else
        {
            PredictPaeth(request, above, left, topLeft, prediction);
        }

        WritePrediction(request, prediction);
        return AvifResultCode.Success;
    }

    static AvifResultCode Validate(Av1IntraPredictionRequest request)
    {
        if (request == null || request.Destination == null
            || request.Destination.GetStateCode() != AvifResultCode.Success
            || request.Width < 4 || request.Height < 4
            || request.Width > 64 || request.Height > 64
            || !IsPowerOfTwo(request.Width) || !IsPowerOfTwo(request.Height)
            || request.Width > request.Height * 4
            || request.Height > request.Width * 4
            || request.X < 0 || request.Y < 0
            || request.X > request.Destination.Width - request.Width
            || request.Y > request.Destination.Height - request.Height
            || request.BitDepth != 8 && request.BitDepth != 10 && request.BitDepth != 12
            || request.Destination.BytesPerSample != (request.BitDepth > 8 ? 2 : 1))
        {
            return AvifResultCode.InvalidArgument;
        }

        int mode = (int)request.Mode;
        if (mode < (int)Av1PredictionMode.Dc || mode > (int)Av1PredictionMode.Paeth
            || request.AngleDelta < -Av1SyntaxConstants.MaxAngleDelta
            || request.AngleDelta > Av1SyntaxConstants.MaxAngleDelta)
        {
            return AvifResultCode.InvalidArgument;
        }

        int operationCount = BooleanValue(request.UseFilterIntra)
            + BooleanValue(request.UsePalette)
            + BooleanValue(request.UseCfl)
            + BooleanValue(request.UseIntraBlockCopy);
        if (operationCount > 1)
        {
            return AvifResultCode.InvalidArgument;
        }

        if (request.UseFilterIntra
            && (request.Mode != Av1PredictionMode.Dc
                || request.FilterIntraMode < Av1FilterIntraMode.FilterDc
                || request.FilterIntraMode > Av1FilterIntraMode.FilterPaeth
                || request.Width > 32 || request.Height > 32))
        {
            return AvifResultCode.InvalidArgument;
        }

        if (request.UsePalette
            && (request.PaletteColors == null || request.PaletteMap == null
                || request.PaletteColors.Length < Av1SyntaxConstants.MinPaletteSize
                || request.PaletteColors.Length > Av1SyntaxConstants.MaxPaletteSize
                || request.PaletteStride < request.Width
                || request.PaletteMap.Length
                    < (long)request.PaletteStride * request.Height))
        {
            return AvifResultCode.InvalidArgument;
        }

        int lumaWidth = request.Width << BooleanValue(request.SubsamplingX);
        int lumaHeight = request.Height << BooleanValue(request.SubsamplingY);
        if (request.UseCfl
            && (request.CflLuma == null || request.CflLumaStride < lumaWidth
                || request.CflLumaWidth < lumaWidth
                || request.CflLumaHeight < lumaHeight
                || request.CflLuma.Length
                    < (long)request.CflLumaStride * request.CflLumaHeight
                || request.CflAlphaQ3 < -16 || request.CflAlphaQ3 > 16))
        {
            return AvifResultCode.InvalidArgument;
        }

        if (request.UseIntraBlockCopy
            && (request.IntraBlockCopySourceX < 0
                || request.IntraBlockCopySourceY < 0
                || request.IntraBlockCopySourceX
                    > request.Destination.Width - request.Width
                || request.IntraBlockCopySourceY
                    > request.Destination.Height - request.Height))
        {
            return AvifResultCode.InvalidArgument;
        }

        return AvifResultCode.Success;
    }

    static int PrepareEdges(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left)
    {
        int baseValue = 1 << (request.BitDepth - 1);
        int aboveDefault = baseValue - 1;
        int leftDefault = baseValue + 1;
        int aboveAvailable = request.HaveAbove && request.Y > 0
            ? request.Width
            : 0;
        if (request.HaveAboveRight && request.Y > 0)
        {
            aboveAvailable = Minimum(
                above.Length,
                (request.Destination ?? throw new InvalidOperationException("Invalid internal state.")).Width - request.X);
        }

        int leftAvailable = request.HaveLeft && request.X > 0
            ? request.Height
            : 0;
        if (request.HaveBelowLeft && request.X > 0)
        {
            leftAvailable = Minimum(
                left.Length,
                (request.Destination ?? throw new InvalidOperationException("Invalid internal state.")).Height - request.Y);
        }

        int index = 0;
        while (index < aboveAvailable)
        {
            above[index] = ReadSample(
                request.Destination,
                request.Y - 1,
                request.X + index);
            index++;
        }

        int aboveFill = aboveAvailable > 0 ? above[aboveAvailable - 1] : aboveDefault;
        while (index < above.Length)
        {
            above[index] = aboveFill;
            index++;
        }

        index = 0;
        while (index < leftAvailable)
        {
            left[index] = ReadSample(
                request.Destination,
                request.Y + index,
                request.X - 1);
            index++;
        }

        int leftFill = leftAvailable > 0 ? left[leftAvailable - 1] : leftDefault;
        while (index < left.Length)
        {
            left[index] = leftFill;
            index++;
        }

        if (aboveAvailable == 0 && leftAvailable > 0)
        {
            Fill(above, left[0]);
        }
        else if (leftAvailable == 0 && aboveAvailable > 0)
        {
            Fill(left, above[0]);
        }

        if (aboveAvailable > 0 && leftAvailable > 0)
        {
            return ReadSample(request.Destination, request.Y - 1, request.X - 1);
        }

        if (aboveAvailable > 0)
        {
            return above[0];
        }

        return leftAvailable > 0 ? left[0] : baseValue;
    }

    static void PredictDc(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int[] prediction)
    {
        bool haveAbove = request.HaveAbove && request.Y > 0;
        bool haveLeft = request.HaveLeft && request.X > 0;
        int sum = 0;
        int divisor = 0;
        int index = 0;
        if (haveAbove)
        {
            while (index < request.Width)
            {
                sum += above[index];
                index++;
            }

            divisor += request.Width;
        }

        index = 0;
        if (haveLeft)
        {
            while (index < request.Height)
            {
                sum += left[index];
                index++;
            }

            divisor += request.Height;
        }

        int dc = divisor == 0
            ? 1 << (request.BitDepth - 1)
            : (sum + (divisor >> 1)) / divisor;
        Fill(prediction, dc);
    }

    static void PredictPaeth(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int topLeft,
        int[] prediction)
    {
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int basis = above[column] + left[row] - topLeft;
                int leftDistance = Absolute(basis - left[row]);
                int aboveDistance = Absolute(basis - above[column]);
                int cornerDistance = Absolute(basis - topLeft);
                int value;
                if (leftDistance <= aboveDistance && leftDistance <= cornerDistance)
                {
                    value = left[row];
                }
                else if (aboveDistance <= cornerDistance)
                {
                    value = above[column];
                }
                else
                {
                    value = topLeft;
                }

                prediction[row * request.Width + column] = value;
                column++;
            }

            row++;
        }
    }

    static void PredictSmooth(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int[] prediction)
    {
        int widthOffset = GetSmoothWeightOffset(request.Width);
        int heightOffset = GetSmoothWeightOffset(request.Height);
        int topRight = above[request.Width - 1];
        int bottomLeft = left[request.Height - 1];
        int row = 0;
        while (row < request.Height)
        {
            int weightY = smoothWeights[heightOffset + row];
            int column = 0;
            while (column < request.Width)
            {
                int weightX = smoothWeights[widthOffset + column];
                int value;
                if (request.Mode == Av1PredictionMode.SmoothVertical)
                {
                    value = weightY * above[column]
                        + (256 - weightY) * bottomLeft;
                    value = (value + 128) >> 8;
                }
                else if (request.Mode == Av1PredictionMode.SmoothHorizontal)
                {
                    value = weightX * left[row]
                        + (256 - weightX) * topRight;
                    value = (value + 128) >> 8;
                }
                else
                {
                    value = weightY * above[column]
                        + (256 - weightY) * bottomLeft
                        + weightX * left[row]
                        + (256 - weightX) * topRight;
                    value = (value + 256) >> 9;
                }

                prediction[row * request.Width + column] = value;
                column++;
            }

            row++;
        }
    }

    static void PredictDirectional(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int topLeft,
        int[] prediction)
    {
        int angle = modeAngles[(int)request.Mode] + request.AngleDelta * 3;
        if (angle == 90)
        {
            int row = 0;
            while (row < request.Height)
            {
                System.Array.Copy(above, 0, prediction, row * request.Width, request.Width);
                row++;
            }

            return;
        }

        if (angle == 180)
        {
            int row = 0;
            while (row < request.Height)
            {
                int column = 0;
                while (column < request.Width)
                {
                    prediction[row * request.Width + column] = left[row];
                    column++;
                }

                row++;
            }

            return;
        }

        if (angle < 90)
        {
            PredictDirectionalZone1(request, above, prediction, angle);
        }
        else if (angle < 180)
        {
            PredictDirectionalZone2(request, above, left, topLeft, prediction, angle);
        }
        else
        {
            PredictDirectionalZone3(request, left, prediction, angle);
        }
    }

    static void PredictDirectionalZone1(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] prediction,
        int angle)
    {
        int derivative = directionalDerivatives[angle];
        int maximum = request.Width + request.Height - 1;
        int row = 0;
        while (row < request.Height)
        {
            int position = (row + 1) * derivative;
            int basis = position >> 6;
            int shift = (position & 63) >> 1;
            int column = 0;
            while (column < request.Width)
            {
                int sample = basis + column;
                if (sample < maximum)
                {
                    prediction[row * request.Width + column] =
                        (above[sample] * (32 - shift)
                            + above[sample + 1] * shift + 16) >> 5;
                }
                else
                {
                    prediction[row * request.Width + column] = above[maximum];
                }

                column++;
            }

            row++;
        }
    }

    static void PredictDirectionalZone2(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int topLeft,
        int[] prediction,
        int angle)
    {
        int derivativeX = directionalDerivatives[180 - angle];
        int derivativeY = directionalDerivatives[angle - 90];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int position = (column << 6) - (row + 1) * derivativeX;
                int basis = position >> 6;
                int shift = (position & 63) >> 1;
                int first;
                int second;
                if (basis >= -1)
                {
                    first = GetEdgeSample(above, topLeft, basis);
                    second = GetEdgeSample(above, topLeft, basis + 1);
                }
                else
                {
                    position = (row << 6) - (column + 1) * derivativeY;
                    basis = position >> 6;
                    shift = (position & 63) >> 1;
                    first = GetEdgeSample(left, topLeft, basis);
                    second = GetEdgeSample(left, topLeft, basis + 1);
                }

                prediction[row * request.Width + column] =
                    (first * (32 - shift) + second * shift + 16) >> 5;
                column++;
            }

            row++;
        }
    }

    static void PredictDirectionalZone3(
        Av1IntraPredictionRequest request,
        int[] left,
        int[] prediction,
        int angle)
    {
        int derivative = directionalDerivatives[270 - angle];
        int maximum = request.Width + request.Height - 1;
        int column = 0;
        while (column < request.Width)
        {
            int position = (column + 1) * derivative;
            int basis = position >> 6;
            int shift = (position & 63) >> 1;
            int row = 0;
            while (row < request.Height)
            {
                int sample = basis + row;
                if (sample < maximum)
                {
                    prediction[row * request.Width + column] =
                        (left[sample] * (32 - shift)
                            + left[sample + 1] * shift + 16) >> 5;
                }
                else
                {
                    prediction[row * request.Width + column] = left[maximum];
                }

                row++;
            }

            column++;
        }
    }

    static void PredictFilterIntra(
        Av1IntraPredictionRequest request,
        int[] above,
        int[] left,
        int topLeft,
        int[] prediction)
    {
        int stride = request.Width + 1;
        int[] buffer = new int[(request.Height + 1) * stride];
        buffer[0] = topLeft;
        int index = 0;
        while (index < request.Width)
        {
            buffer[index + 1] = above[index];
            index++;
        }

        index = 0;
        while (index < request.Height)
        {
            buffer[(index + 1) * stride] = left[index];
            index++;
        }

        int maximum = (1 << request.BitDepth) - 1;
        int row = 1;
        while (row < request.Height + 1)
        {
            int column = 1;
            while (column < request.Width + 1)
            {
                int[] neighbors = new int[7];
                neighbors[0] = buffer[(row - 1) * stride + column - 1];
                neighbors[1] = buffer[(row - 1) * stride + column];
                neighbors[2] = buffer[(row - 1) * stride + column + 1];
                neighbors[3] = buffer[(row - 1) * stride + column + 2];
                neighbors[4] = buffer[(row - 1) * stride + column + 3];
                neighbors[5] = buffer[row * stride + column - 1];
                neighbors[6] = buffer[(row + 1) * stride + column - 1];
                int output = 0;
                while (output < 8)
                {
                    int sum = 0;
                    int tap = 0;
                    while (tap < 7)
                    {
                        int tapIndex = (int)request.FilterIntraMode * 64
                            + output * 8 + tap;
                        sum += filterIntraTaps[tapIndex] * neighbors[tap];
                        tap++;
                    }

                    int outputRow = row + (output >> 2);
                    int outputColumn = column + (output & 3);
                    buffer[outputRow * stride + outputColumn] = Clip(
                        0,
                        maximum,
                        RoundSigned(sum, 4));
                    output++;
                }

                column += 4;
            }

            row += 2;
        }

        row = 0;
        while (row < request.Height)
        {
            System.Array.Copy(
                buffer,
                (row + 1) * stride + 1,
                prediction,
                row * request.Width,
                request.Width);
            row++;
        }
    }

    static void ApplyCfl(
        Av1IntraPredictionRequest request,
        int[] prediction)
    {
        int scaleX = request.SubsamplingX ? 2 : 1;
        int scaleY = request.SubsamplingY ? 2 : 1;
        int sampleCount = scaleX * scaleY;
        int[] lumaQ3 = new int[request.Width * request.Height];
        int sumQ3 = 0;
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int lumaSum = 0;
                int lumaRow = 0;
                while (lumaRow < scaleY)
                {
                    int lumaColumn = 0;
                    while (lumaColumn < scaleX)
                    {
                        lumaSum += (request.CflLuma ?? throw new InvalidOperationException("Invalid internal state."))[
                            (row * scaleY + lumaRow) * request.CflLumaStride
                                + column * scaleX + lumaColumn];
                        lumaColumn++;
                    }

                    lumaRow++;
                }

                int q3 = lumaSum * 8 / sampleCount;
                lumaQ3[row * request.Width + column] = q3;
                sumQ3 += q3;
                column++;
            }

            row++;
        }

        int count = request.Width * request.Height;
        int averageQ3 = (sumQ3 + (count >> 1)) / count;
        int maximum = (1 << request.BitDepth) - 1;
        int index = 0;
        while (index < prediction.Length)
        {
            int scaled = RoundSigned(
                request.CflAlphaQ3 * (lumaQ3[index] - averageQ3),
                6);
            prediction[index] = Clip(0, maximum, prediction[index] + scaled);
            index++;
        }
    }

    static AvifResultCode PredictPalette(Av1IntraPredictionRequest request)
    {
        int maximum = (1 << request.BitDepth) - 1;
        int color = 0;
        while (color < (request.PaletteColors ?? throw new InvalidOperationException("Invalid internal state.")).Length)
        {
            if (request.PaletteColors[color] < 0
                || request.PaletteColors[color] > maximum)
            {
                return AvifResultCode.InvalidAv1Bitstream;
            }

            color++;
        }

        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int colorIndex = (request.PaletteMap ?? throw new InvalidOperationException("Invalid internal state."))[row * request.PaletteStride + column];
                if (colorIndex >= request.PaletteColors.Length)
                {
                    return AvifResultCode.InvalidAv1Bitstream;
                }

                column++;
            }

            row++;
        }

        row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int colorIndex = (request.PaletteMap ?? throw new InvalidOperationException("Invalid internal state."))[row * request.PaletteStride + column];

                WriteSample(
                    request.Destination,
                    request.Y + row,
                    request.X + column,
                    request.PaletteColors[colorIndex]);
                column++;
            }

            row++;
        }

        return AvifResultCode.Success;
    }

    static AvifResultCode CopyIntraBlock(Av1IntraPredictionRequest request)
    {
        int sourceEndY = request.IntraBlockCopySourceY + request.Height;
        bool decodedEarlier = sourceEndY <= request.Y
            || (request.IntraBlockCopySourceY == request.Y
                && request.IntraBlockCopySourceX + request.Width <= request.X);
        if (!decodedEarlier || RectanglesOverlap(request))
        {
            return AvifResultCode.InvalidAv1Bitstream;
        }

        int[] samples = new int[request.Width * request.Height];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                samples[row * request.Width + column] = ReadSample(
                    request.Destination,
                    request.IntraBlockCopySourceY + row,
                    request.IntraBlockCopySourceX + column);
                column++;
            }

            row++;
        }

        WritePrediction(request, samples);
        return AvifResultCode.Success;
    }

    static bool RectanglesOverlap(Av1IntraPredictionRequest request)
    {
        return request.IntraBlockCopySourceX < request.X + request.Width
            && request.X < request.IntraBlockCopySourceX + request.Width
            && request.IntraBlockCopySourceY < request.Y + request.Height
            && request.Y < request.IntraBlockCopySourceY + request.Height;
    }

    static void WritePrediction(
        Av1IntraPredictionRequest request,
        int[] prediction)
    {
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                WriteSample(
                    request.Destination,
                    request.Y + row,
                    request.X + column,
                    prediction[row * request.Width + column]);
                column++;
            }

            row++;
        }
    }

    static int ReadSample(PlaneBuffer? plane, int row, int column)
    {
        Span<byte> bytes = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row);
        int index = column * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? bytes[index]
            : bytes[index] | bytes[index + 1] << 8;
    }

    static void WriteSample(PlaneBuffer? plane, int row, int column, int value)
    {
        Span<byte> bytes = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row);
        int index = column * plane.BytesPerSample;
        bytes[index] = IntegerConversion.ToByte(value);
        if (plane.BytesPerSample == 2)
        {
            bytes[index + 1] = IntegerConversion.ToByte(value >> 8);
        }
    }

    static int GetEdgeSample(int[] edge, int topLeft, int index)
    {
        if (index < 0)
        {
            return topLeft;
        }

        return edge[index < edge.Length ? index : edge.Length - 1];
    }

    static int GetSmoothWeightOffset(int size)
    {
        if (size == 4)
        {
            return 0;
        }

        if (size == 8)
        {
            return 4;
        }

        if (size == 16)
        {
            return 12;
        }

        return size == 32 ? 28 : 60;
    }

    static int RoundSigned(int value, int bits)
    {
        if (value < 0)
        {
            return -((-value + (1 << (bits - 1))) >> bits);
        }

        return (value + (1 << (bits - 1))) >> bits;
    }

    static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int Minimum(int first, int second)
    {
        return first < second ? first : second;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }

        return value > maximum ? maximum : value;
    }

    static int BooleanValue(bool value)
    {
        return value ? 1 : 0;
    }

    static void Fill(int[] values, int value)
    {
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index++;
        }
    }
}
