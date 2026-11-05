using System;

namespace avif;

internal enum Av1CompoundPredictionType
{
    Average = 0,
    DistanceWeighted = 1,
    Wedge = 2,
    DifferenceWeighted = 3
}

internal enum Av1InterIntraMode
{
    Dc = 0,
    Vertical = 1,
    Horizontal = 2,
    Smooth = 3
}

internal class Av1CompoundPredictionRequest
{
    internal PlaneBuffer? Destination;
    internal PlaneBuffer? First;
    internal PlaneBuffer? Second;
    internal int DestinationX;
    internal int DestinationY;
    internal int FirstX;
    internal int FirstY;
    internal int SecondX;
    internal int SecondY;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal Av1CompoundPredictionType Type;
    internal int FirstDistance;
    internal int SecondDistance;
    internal int WedgeIndex;
    internal bool WedgeSign;
    internal bool DifferenceMaskInverted;

    internal Av1CompoundPredictionRequest()
    {
        Type = Av1CompoundPredictionType.Average;
    }
}

internal class Av1InterIntraPredictionRequest
{
    internal PlaneBuffer? Destination;
    internal PlaneBuffer? Inter;
    internal PlaneBuffer? Intra;
    internal int DestinationX;
    internal int DestinationY;
    internal int InterX;
    internal int InterY;
    internal int IntraX;
    internal int IntraY;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal Av1InterIntraMode Mode;
    internal bool UseWedge;
    internal int WedgeIndex;
    internal bool WedgeSign;

    internal Av1InterIntraPredictionRequest()
    {
        Mode = Av1InterIntraMode.Dc;
    }
}

// AV1 specification sections 7.11.3.7 through 7.11.3.12.
internal static class Av1CompoundPredictor
{
    static readonly int[] quantDistanceWeights = new int[]
    {
        2,3, 2,5, 2,7
    };

    static readonly int[] quantDistanceLookup = new int[]
    {
        9,7, 11,5, 12,4, 13,3
    };

    static readonly int[] wedgeMasterObliqueOdd = new int[]
    {
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,1,2,6,18,
        37,53,60,63,64,64,64,64,64,64,64,64,64,64,64,64,
        64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64
    };

    static readonly int[] wedgeMasterObliqueEven = new int[]
    {
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,1,4,11,27,
        46,58,62,63,64,64,64,64,64,64,64,64,64,64,64,64,
        64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64
    };

    static readonly int[] wedgeMasterVertical = new int[]
    {
        0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,
        0,0,0,0,0,0,0,0,0,0,0,0,0,2,7,21,
        43,57,62,64,64,64,64,64,64,64,64,64,64,64,64,64,
        64,64,64,64,64,64,64,64,64,64,64,64,64,64,64,64
    };

    static readonly int[] wedgeCodebookHigher = new int[]
    {
        2,4,4, 3,4,4, 4,4,4, 5,4,4,
        0,4,2, 0,4,4, 0,4,6, 1,4,4,
        2,4,2, 2,4,6, 5,4,2, 5,4,6,
        3,2,4, 3,6,4, 4,2,4, 4,6,4
    };

    static readonly int[] wedgeCodebookWider = new int[]
    {
        2,4,4, 3,4,4, 4,4,4, 5,4,4,
        1,2,4, 1,4,4, 1,6,4, 0,4,4,
        2,4,2, 2,4,6, 5,4,2, 5,4,6,
        3,2,4, 3,6,4, 4,2,4, 4,6,4
    };

    static readonly int[] wedgeCodebookEqual = new int[]
    {
        2,4,4, 3,4,4, 4,4,4, 5,4,4,
        0,4,2, 0,4,6, 1,2,4, 1,6,4,
        2,4,2, 2,4,6, 5,4,2, 5,4,6,
        3,2,4, 3,6,4, 4,2,4, 4,6,4
    };

    static readonly int[] interIntraWeights = new int[]
    {
        60,58,56,54,52,50,48,47,45,44,42,41,39,38,37,35,34,33,32,
        31,30,29,28,27,26,25,24,23,22,22,21,20,19,19,18,18,17,16,
        16,15,15,14,14,13,13,12,12,12,11,11,10,10,10,9,9,9,8,
        8,8,8,7,7,7,7,6,6,6,6,6,5,5,5,5,5,4,4,
        4,4,4,4,4,4,3,3,3,3,3,3,3,3,3,2,2,2,2,
        2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,
        1,1,1,1,1,1,1,1,1,1,1,1,1,1
    };

    internal static AvifResultCode Predict(Av1CompoundPredictionRequest request)
    {
        AvifResultCode validation = ValidateCompound(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[] mask = System.Array.Empty<int>();
        int[] weights = System.Array.Empty<int>();
        if (request.Type == Av1CompoundPredictionType.DistanceWeighted)
        {
            weights = DeriveDistanceWeights(
                request.FirstDistance, request.SecondDistance);
        }
        else if (request.Type == Av1CompoundPredictionType.Wedge)
        {
            mask = GenerateWedgeMask(
                request.Width, request.Height, request.WedgeIndex, request.WedgeSign);
        }
        else if (request.Type == Av1CompoundPredictionType.DifferenceWeighted)
        {
            mask = GenerateDifferenceMask(
                request.First,
                request.FirstX,
                request.FirstY,
                request.Second,
                request.SecondX,
                request.SecondY,
                request.Width,
                request.Height,
                request.BitDepth,
                request.DifferenceMaskInverted);
        }

        int[] prediction = new int[request.Width * request.Height];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int first = ReadSample(
                    request.First, request.FirstX + column, request.FirstY + row);
                int second = ReadSample(
                    request.Second, request.SecondX + column, request.SecondY + row);
                int value;
                if (request.Type == Av1CompoundPredictionType.Average)
                {
                    value = (first + second + 1) >> 1;
                }
                else if (request.Type == Av1CompoundPredictionType.DistanceWeighted)
                {
                    value = (weights[0] * first + weights[1] * second + 8) >> 4;
                }
                else
                {
                    int alpha = mask[row * request.Width + column];
                    value = (alpha * first + (64 - alpha) * second + 32) >> 6;
                }
                prediction[row * request.Width + column] = Clip(
                    0, (1 << request.BitDepth) - 1, value);
                column++;
            }
            row++;
        }

        WritePrediction(
            request.Destination,
            request.DestinationX,
            request.DestinationY,
            request.Width,
            request.Height,
            prediction);
        return AvifResultCode.Success;
    }

    internal static AvifResultCode PredictInterIntra(
        Av1InterIntraPredictionRequest request)
    {
        AvifResultCode validation = ValidateInterIntra(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[] mask = request.UseWedge
            ? GenerateWedgeMask(
                request.Width, request.Height, request.WedgeIndex, request.WedgeSign)
            : GenerateInterIntraMask(request.Width, request.Height, request.Mode);
        int[] prediction = new int[request.Width * request.Height];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int inter = ReadSample(
                    request.Inter, request.InterX + column, request.InterY + row);
                int intra = ReadSample(
                    request.Intra, request.IntraX + column, request.IntraY + row);
                int alpha = mask[row * request.Width + column];
                int value = (alpha * intra + (64 - alpha) * inter + 32) >> 6;
                prediction[row * request.Width + column] = Clip(
                    0, (1 << request.BitDepth) - 1, value);
                column++;
            }
            row++;
        }

        WritePrediction(
            request.Destination,
            request.DestinationX,
            request.DestinationY,
            request.Width,
            request.Height,
            prediction);
        return AvifResultCode.Success;
    }

    internal static int[] DeriveDistanceWeights(int firstDistance, int secondDistance)
    {
        if (firstDistance < 0 || firstDistance > 31
            || secondDistance < 0 || secondDistance > 31)
        {
            return System.Array.Empty<int>();
        }

        bool firstIsCloser = firstDistance <= secondDistance;
        int order = firstIsCloser ? 1 : 0;
        int tableRow = 3;
        if (firstDistance != 0 && secondDistance != 0)
        {
            int index = 0;
            while (index < 3)
            {
                int firstCoefficient = quantDistanceWeights[index * 2 + order];
                int secondCoefficient = quantDistanceWeights[index * 2 + 1 - order];
                int firstProduct = firstDistance * firstCoefficient;
                int secondProduct = secondDistance * secondCoefficient;
                if (firstDistance > secondDistance && firstProduct < secondProduct
                    || firstDistance <= secondDistance && firstProduct > secondProduct)
                {
                    tableRow = index;
                    break;
                }
                index++;
            }
        }

        return new int[]
        {
            quantDistanceLookup[tableRow * 2 + order],
            quantDistanceLookup[tableRow * 2 + 1 - order]
        };
    }

    internal static int[] GenerateDifferenceMask(
        PlaneBuffer? first,
        int firstX,
        int firstY,
        PlaneBuffer? second,
        int secondX,
        int secondY,
        int width,
        int height,
        int bitDepth,
        bool inverted)
    {
        if (!ValidSource(first, firstX, firstY, width, height, bitDepth)
            || !ValidSource(second, secondX, secondY, width, height, bitDepth))
        {
            return System.Array.Empty<int>();
        }

        int[] mask = new int[width * height];
        int shift = bitDepth - 8;
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int difference = Absolute(
                    ReadSample(first, firstX + column, firstY + row)
                    - ReadSample(second, secondX + column, secondY + row));
                int alpha = Clip(0, 64, 38 + (difference >> shift) / 16);
                mask[row * width + column] = inverted ? 64 - alpha : alpha;
                column++;
            }
            row++;
        }
        return mask;
    }

    internal static int[] GenerateWedgeMask(
        int width,
        int height,
        int wedgeIndex,
        bool wedgeSign)
    {
        if (!ValidWedgeSize(width, height) || wedgeIndex < 0 || wedgeIndex >= 16)
        {
            return System.Array.Empty<int>();
        }

        int[] codebook = width == height
            ? wedgeCodebookEqual
            : (height > width ? wedgeCodebookHigher : wedgeCodebookWider);
        int direction = codebook[wedgeIndex * 3];
        int xOffset = codebook[wedgeIndex * 3 + 1];
        int yOffset = codebook[wedgeIndex * 3 + 2];
        bool negative = wedgeSign != GetWedgeSignFlip(width, height, wedgeIndex);
        int horizontalOffset = xOffset * width >> 3;
        int verticalOffset = yOffset * height >> 3;
        int[] mask = new int[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int masterRow = 32 - verticalOffset + row;
                int masterColumn = 32 - horizontalOffset + column;
                int alpha = ReadWedgeMaster(direction, masterRow, masterColumn);
                mask[row * width + column] = negative ? 64 - alpha : alpha;
                column++;
            }
            row++;
        }
        return mask;
    }

    internal static int[] GenerateInterIntraMask(
        int width,
        int height,
        Av1InterIntraMode mode)
    {
        if (!ValidBlockSize(width, height)
            || mode < Av1InterIntraMode.Dc || mode > Av1InterIntraMode.Smooth)
        {
            return System.Array.Empty<int>();
        }

        int largest = width > height ? width : height;
        int scale = 128 / largest;
        int[] mask = new int[width * height];
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                int alpha = 32;
                if (mode == Av1InterIntraMode.Vertical)
                {
                    alpha = interIntraWeights[row * scale];
                }
                else if (mode == Av1InterIntraMode.Horizontal)
                {
                    alpha = interIntraWeights[column * scale];
                }
                else if (mode == Av1InterIntraMode.Smooth)
                {
                    int distance = row < column ? row : column;
                    alpha = interIntraWeights[distance * scale];
                }
                mask[row * width + column] = alpha;
                column++;
            }
            row++;
        }
        return mask;
    }

    static int ReadWedgeMaster(int direction, int row, int column)
    {
        int clippedRow = Clip(0, 63, row);
        int clippedColumn = Clip(0, 63, column);
        if (direction == 0)
        {
            return wedgeMasterVertical[clippedRow];
        }
        if (direction == 1)
        {
            return wedgeMasterVertical[clippedColumn];
        }
        if (direction == 2)
        {
            return ReadOblique63(clippedColumn, clippedRow);
        }
        if (direction == 3)
        {
            return ReadOblique63(clippedRow, clippedColumn);
        }
        if (direction == 4)
        {
            return 64 - ReadOblique63(clippedRow, 63 - clippedColumn);
        }
        return 64 - ReadOblique63(clippedColumn, 63 - clippedRow);
    }

    static int ReadOblique63(int row, int column)
    {
        int shift = 16 - ((row + 1) >> 1);
        int sourceIndex = Clip(0, 63, column - shift);
        return (row & 1) == 0
            ? wedgeMasterObliqueEven[sourceIndex]
            : wedgeMasterObliqueOdd[sourceIndex];
    }

    static bool GetWedgeSignFlip(int width, int height, int wedgeIndex)
    {
        if (wedgeIndex == 10 || wedgeIndex == 14)
        {
            return false;
        }
        if (width == 8 && height == 32 && wedgeIndex == 8)
        {
            return false;
        }
        if (width == 32 && height == 8 && wedgeIndex == 12)
        {
            return false;
        }
        if (width != height && (wedgeIndex == 4 || wedgeIndex == 14))
        {
            return false;
        }
        return true;
    }

    static AvifResultCode ValidateCompound(Av1CompoundPredictionRequest request)
    {
        if (request == null
            || !ValidDestination(request.Destination, request.DestinationX,
                request.DestinationY, request.Width, request.Height, request.BitDepth)
            || !ValidSource(request.First, request.FirstX, request.FirstY,
                request.Width, request.Height, request.BitDepth)
            || !ValidSource(request.Second, request.SecondX, request.SecondY,
                request.Width, request.Height, request.BitDepth)
            || request.Type < Av1CompoundPredictionType.Average
            || request.Type > Av1CompoundPredictionType.DifferenceWeighted)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.Type == Av1CompoundPredictionType.DistanceWeighted
            && DeriveDistanceWeights(
                request.FirstDistance, request.SecondDistance).Length != 2)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.Type == Av1CompoundPredictionType.Wedge
            && GenerateWedgeMask(
                request.Width, request.Height, request.WedgeIndex,
                request.WedgeSign).Length == 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static AvifResultCode ValidateInterIntra(Av1InterIntraPredictionRequest request)
    {
        if (request == null
            || !ValidDestination(request.Destination, request.DestinationX,
                request.DestinationY, request.Width, request.Height, request.BitDepth)
            || !ValidSource(request.Inter, request.InterX, request.InterY,
                request.Width, request.Height, request.BitDepth)
            || !ValidSource(request.Intra, request.IntraX, request.IntraY,
                request.Width, request.Height, request.BitDepth)
            || request.Mode < Av1InterIntraMode.Dc
            || request.Mode > Av1InterIntraMode.Smooth)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.UseWedge && GenerateWedgeMask(
            request.Width, request.Height, request.WedgeIndex,
            request.WedgeSign).Length == 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool ValidDestination(
        PlaneBuffer? plane,
        int x,
        int y,
        int width,
        int height,
        int bitDepth)
    {
        return ValidPlane(plane, bitDepth)
            && ValidBlockSize(width, height)
            && x >= 0 && y >= 0
            && x <= (plane ?? throw new InvalidOperationException("Invalid internal state.")).Width - width && y <= plane.Height - height;
    }

    static bool ValidSource(
        PlaneBuffer? plane,
        int x,
        int y,
        int width,
        int height,
        int bitDepth)
    {
        return ValidDestination(plane, x, y, width, height, bitDepth);
    }

    static bool ValidPlane(PlaneBuffer? plane, int bitDepth)
    {
        return plane != null
            && plane.Code == AvifResultCode.Success
            && !plane.Closed
            && (bitDepth == 8 || bitDepth == 10 || bitDepth == 12)
            && plane.BytesPerSample == (bitDepth > 8 ? 2 : 1);
    }

    static bool ValidBlockSize(int width, int height)
    {
        if (!ValidDimension(width) || !ValidDimension(height))
        {
            return false;
        }
        int larger = width > height ? width : height;
        int smaller = width < height ? width : height;
        return larger <= smaller * 4;
    }

    static bool ValidDimension(int dimension)
    {
        return dimension == 4 || dimension == 8 || dimension == 16
            || dimension == 32 || dimension == 64 || dimension == 128;
    }

    static bool ValidWedgeSize(int width, int height)
    {
        return width == 8 && height == 8
            || width == 8 && height == 16
            || width == 16 && height == 8
            || width == 16 && height == 16
            || width == 16 && height == 32
            || width == 32 && height == 16
            || width == 32 && height == 32
            || width == 8 && height == 32
            || width == 32 && height == 8;
    }

    static int ReadSample(PlaneBuffer? plane, int column, int row)
    {
        Span<byte> bytes = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(row);
        int offset = column * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? bytes[offset]
            : bytes[offset] | bytes[offset + 1] << 8;
    }

    static void WritePrediction(
        PlaneBuffer? plane,
        int x,
        int y,
        int width,
        int height,
        int[] prediction)
    {
        int row = 0;
        while (row < height)
        {
            Span<byte> bytes = (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(y + row);
            int column = 0;
            while (column < width)
            {
                int value = prediction[row * width + column];
                int offset = (x + column) * plane.BytesPerSample;
                bytes[offset] = IntegerConversion.ToByte(value);
                if (plane.BytesPerSample == 2)
                {
                    bytes[offset + 1] = IntegerConversion.ToByte(value >> 8);
                }
                column++;
            }
            row++;
        }
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }

    static int Clip(int minimum, int maximum, int value)
    {
        if (value < minimum)
        {
            return minimum;
        }
        return value > maximum ? maximum : value;
    }
}
