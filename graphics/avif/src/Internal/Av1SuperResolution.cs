using System;

namespace avif;

internal class Av1SuperResolutionRequest
{
    internal Av1FrameSize? Size;
    internal PlaneBuffer[] Source;
    internal PlaneBuffer[] Destination;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;

    internal Av1SuperResolutionRequest()
    {
        Source = System.Array.Empty<PlaneBuffer>();
        Destination = System.Array.Empty<PlaneBuffer>();
        BitDepth = 8;
    }
}

// AV1 specification section 7.16.
internal static class Av1SuperResolution
{
    static readonly int[] upscaleFilter = new int[]
    {
         0,0,0,128,0,0,0,0, 0,0,-1,128,2,-1,0,0,
         0,1,-3,127,4,-2,1,0, 0,1,-4,127,6,-3,1,0,
         0,2,-6,126,8,-3,1,0, 0,2,-7,125,11,-4,1,0,
        -1,2,-8,125,13,-5,2,0, -1,3,-9,124,15,-6,2,0,
        -1,3,-10,123,18,-6,2,-1, -1,3,-11,122,20,-7,3,-1,
        -1,4,-12,121,22,-8,3,-1, -1,4,-13,120,25,-9,3,-1,
        -1,4,-14,118,28,-9,3,-1, -1,4,-15,117,30,-10,4,-1,
        -1,5,-16,116,32,-11,4,-1, -1,5,-16,114,35,-12,4,-1,
        -1,5,-17,112,38,-12,4,-1, -1,5,-18,111,40,-13,5,-1,
        -1,5,-18,109,43,-14,5,-1, -1,6,-19,107,45,-14,5,-1,
        -1,6,-19,105,48,-15,5,-1, -1,6,-19,103,51,-16,5,-1,
        -1,6,-20,101,53,-16,6,-1, -1,6,-20,99,56,-17,6,-1,
        -1,6,-20,97,58,-17,6,-1, -1,6,-20,95,61,-18,6,-1,
        -2,7,-20,93,64,-18,6,-2, -2,7,-20,91,66,-19,6,-1,
        -2,7,-20,88,69,-19,6,-1, -2,7,-20,86,71,-19,6,-1,
        -2,7,-20,84,74,-20,7,-2, -2,7,-20,81,76,-20,7,-1,
        -2,7,-20,79,79,-20,7,-2, -1,7,-20,76,81,-20,7,-2,
        -2,7,-20,74,84,-20,7,-2, -1,6,-19,71,86,-20,7,-2,
        -1,6,-19,69,88,-20,7,-2, -1,6,-19,66,91,-20,7,-2,
        -2,6,-18,64,93,-20,7,-2, -1,6,-18,61,95,-20,6,-1,
        -1,6,-17,58,97,-20,6,-1, -1,6,-17,56,99,-20,6,-1,
        -1,6,-16,53,101,-20,6,-1, -1,5,-16,51,103,-19,6,-1,
        -1,5,-15,48,105,-19,6,-1, -1,5,-14,45,107,-19,6,-1,
        -1,5,-14,43,109,-18,5,-1, -1,5,-13,40,111,-18,5,-1,
        -1,4,-12,38,112,-17,5,-1, -1,4,-12,35,114,-16,5,-1,
        -1,4,-11,32,116,-16,5,-1, -1,4,-10,30,117,-15,4,-1,
        -1,3,-9,28,118,-14,4,-1, -1,3,-9,25,120,-13,4,-1,
        -1,3,-8,22,121,-12,4,-1, -1,3,-7,20,122,-11,3,-1,
        -1,2,-6,18,123,-10,3,-1, 0,2,-6,15,124,-9,3,-1,
         0,2,-5,13,125,-8,2,-1, 0,1,-4,11,125,-7,2,0,
         0,1,-3,8,126,-6,2,0, 0,1,-3,6,127,-4,1,0,
         0,1,-2,4,127,-3,1,0, 0,0,-1,2,128,-1,0,0
    };

    internal static AvifResultCode Upscale(
        Av1SuperResolutionRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int plane = 0;
        while (plane < request.Source.Length)
        {
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int sourceWidth = Round2((request.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth, subX);
            int destinationWidth = Round2(request.Size.UpscaledWidth, subX);
            int height = Round2(request.Size.FrameHeight, subY);
            if (!request.Size.UsesSuperres)
            {
                CopyPlane(
                    request.Source[plane], request.Destination[plane],
                    sourceWidth, height);
            }
            else
            {
                UpscalePlane(
                    request, plane, sourceWidth, destinationWidth, height);
            }
            plane++;
        }
        return AvifResultCode.Success;
    }

    internal static int GetStep(int sourceWidth, int destinationWidth)
    {
        if (sourceWidth <= 0 || destinationWidth <= 0)
        {
            return 0;
        }
        return ((sourceWidth << Av1SyntaxConstants.SuperresScaleBits)
            + destinationWidth / 2) / destinationWidth;
    }

    internal static int GetInitialSubpixel(
        int sourceWidth,
        int destinationWidth,
        int step)
    {
        if (sourceWidth <= 0 || destinationWidth <= 0 || step <= 0)
        {
            return 0;
        }
        int error = destinationWidth * step
            - (sourceWidth << Av1SyntaxConstants.SuperresScaleBits);
        int initial = (-((destinationWidth - sourceWidth)
            << (Av1SyntaxConstants.SuperresScaleBits - 1))
            + destinationWidth / 2) / destinationWidth
            + (1 << (Av1SyntaxConstants.SuperresExtraBits - 1))
            - error / 2;
        return initial & Av1SyntaxConstants.SuperresScaleMask;
    }

    internal static int GetFilterCoefficient(int phase, int tap)
    {
        if (phase < 0 || phase >= Av1SyntaxConstants.SuperresFilterShifts
            || tap < 0 || tap >= Av1SyntaxConstants.SuperresFilterTaps)
        {
            return 0;
        }
        return upscaleFilter[phase * Av1SyntaxConstants.SuperresFilterTaps + tap];
    }

    static void UpscalePlane(
        Av1SuperResolutionRequest request,
        int plane,
        int sourceWidth,
        int destinationWidth,
        int height)
    {
        int step = GetStep(sourceWidth, destinationWidth);
        int initialSubpixel = GetInitialSubpixel(
            sourceWidth, destinationWidth, step);
        int maximumSourceX = sourceWidth - 1;
        int maximumSample = (1 << request.BitDepth) - 1;
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < destinationWidth)
            {
                int sourcePosition = -(1 << Av1SyntaxConstants.SuperresScaleBits)
                    + initialSubpixel + x * step;
                int sourcePixel = sourcePosition
                    >> Av1SyntaxConstants.SuperresScaleBits;
                int phase = (sourcePosition & Av1SyntaxConstants.SuperresScaleMask)
                    >> Av1SyntaxConstants.SuperresExtraBits;
                long sum = 0;
                int tap = 0;
                while (tap < Av1SyntaxConstants.SuperresFilterTaps)
                {
                    int sampleX = sourcePixel
                        + tap - Av1SyntaxConstants.SuperresFilterOffset;
                    sampleX = Clip(0, maximumSourceX, sampleX);
                    int sample = ReadSample(request.Source[plane], sampleX, y);
                    sum += (long)sample * GetFilterCoefficient(phase, tap);
                    tap++;
                }
                int value = Round2(sum, Av1SyntaxConstants.FilterBits);
                value = Clip(0, maximumSample, value);
                WriteSample(request.Destination[plane], x, y, value);
                x++;
            }
            y++;
        }
    }

    static void CopyPlane(
        PlaneBuffer source,
        PlaneBuffer destination,
        int width,
        int height)
    {
        if (source == destination)
        {
            return;
        }
        int row = 0;
        while (row < height)
        {
            int column = 0;
            while (column < width)
            {
                WriteSample(destination, column, row,
                    ReadSample(source, column, row));
                column++;
            }
            row++;
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
        if (shift <= 0)
        {
            return (int)value;
        }
        return (int)((value + (1L << shift - 1)) >> shift);
    }

    static int Clip(int minimum, int maximum, int value)
    {
        return value < minimum ? minimum : (value > maximum ? maximum : value);
    }

    static AvifResultCode Validate(Av1SuperResolutionRequest request)
    {
        if (request == null || request.Size == null
            || request.Source == null || request.Destination == null
            || request.Source.Length != 1 && request.Source.Length != 3
            || request.Destination.Length != request.Source.Length
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.Size.FrameWidth <= 0 || request.Size.UpscaledWidth <= 0
            || request.Size.FrameHeight <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.Size.UsesSuperres)
        {
            int expectedWidth = (request.Size.UpscaledWidth
                * Av1SyntaxConstants.SuperresNumerator
                + request.Size.SuperresDenominator / 2)
                / request.Size.SuperresDenominator;
            if (request.Size.SuperresDenominator
                    < Av1SyntaxConstants.SuperresMinimumDenominator
                || request.Size.SuperresDenominator > 16
                || request.Size.UpscaledWidth <= request.Size.FrameWidth
                || request.Size.FrameWidth != expectedWidth)
            {
                return AvifResultCode.InvalidArgument;
            }
        }
        else if (request.Size.SuperresDenominator
                != Av1SyntaxConstants.SuperresNumerator
            || request.Size.FrameWidth != request.Size.UpscaledWidth)
        {
            return AvifResultCode.InvalidArgument;
        }
        int expectedMiColumns = 2 * ((request.Size.FrameWidth + 7) >> 3);
        if (request.Size.MiColumns != expectedMiColumns)
        {
            return AvifResultCode.InvalidArgument;
        }

        int plane = 0;
        while (plane < request.Source.Length)
        {
            if (request.Source[plane] == null || request.Destination[plane] == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode sourceState = request.Source[plane].GetStateCode();
            AvifResultCode destinationState =
                request.Destination[plane].GetStateCode();
            if (sourceState != AvifResultCode.Success)
            {
                return sourceState;
            }
            if (destinationState != AvifResultCode.Success)
            {
                return destinationState;
            }
            int subX = plane == 0 || !request.SubsamplingX ? 0 : 1;
            int subY = plane == 0 || !request.SubsamplingY ? 0 : 1;
            int sourceWidth = Round2(request.Size.FrameWidth, subX);
            int destinationWidth = Round2(request.Size.UpscaledWidth, subX);
            int height = Round2(request.Size.FrameHeight, subY);
            int bytesPerSample = request.BitDepth > 8 ? 2 : 1;
            if (request.Source[plane].Width < sourceWidth
                || request.Source[plane].Height < height
                || request.Destination[plane].Width < destinationWidth
                || request.Destination[plane].Height < height
                || request.Source[plane].BytesPerSample != bytesPerSample
                || request.Destination[plane].BytesPerSample != bytesPerSample
                || request.Size.UsesSuperres
                    && request.Source[plane].Buffer
                        == request.Destination[plane].Buffer)
            {
                return AvifResultCode.InvalidArgument;
            }
            plane++;
        }
        return AvifResultCode.Success;
    }
}
