using System;

namespace avif;

internal class Av1FilmGrainRequest
{
    internal Av1FilmGrainParameters? Parameters;
    internal PlaneBuffer[] SourcePlanes;
    internal PlaneBuffer[] DestinationPlanes;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int MatrixCoefficients;
    internal long MaxTemporaryBytes;

    internal Av1FilmGrainRequest()
    {
        SourcePlanes = System.Array.Empty<PlaneBuffer>();
        DestinationPlanes = System.Array.Empty<PlaneBuffer>();
        BitDepth = 8;
        MaxTemporaryBytes = 1L << 30;
    }
}

internal class Av1FilmGrainRandom
{
    internal int State;

    internal Av1FilmGrainRandom(int seed)
    {
        State = seed & 65535;
    }

    internal int Get(int bits)
    {
        int value = State;
        int bit = (value ^ value >> 1 ^ value >> 3 ^ value >> 12) & 1;
        value = value >> 1 | bit << 15;
        State = value;
        return value >> (16 - bits) & ((1 << bits) - 1);
    }
}

// AV1 specification sections 7.18.3.1 through 7.18.3.5.
internal static class Av1FilmGrain
{
    const int LumaGrainWidth = 82;
    const int LumaGrainHeight = 73;
    static readonly int[] gaussianSequence = LoadGaussianSequence();

    internal static AvifResultCode Synthesize(Av1FilmGrainRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return validation;
        }

        int[][] source = Snapshot(request);
        int[][] output = ClonePlanes(source);
        if ((request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).ApplyGrain)
        {
            int[][] grain = GenerateGrain(request);
            int[][] scaling = CreateScalingLookups(request.Parameters, output.Length);
            int[][] noise = GenerateNoise(request, grain);
            AddNoise(request, source, output, noise, scaling);
        }
        WritePlanes(request, output);
        return AvifResultCode.Success;
    }

    internal static int[] GetRandomNumbers(
        int seed,
        int bits,
        int count)
    {
        if (bits < 1 || bits > 16 || count < 0)
        {
            return System.Array.Empty<int>();
        }
        int[] values = new int[count];
        Av1FilmGrainRandom random = new Av1FilmGrainRandom(seed);
        int index = 0;
        while (index < count)
        {
            values[index] = random.Get(bits);
            index++;
        }
        return values;
    }

    internal static int[] CreateScalingLookup(int[][] points)
    {
        int[] lookup = new int[256];
        if (points == null || points.Length == 0)
        {
            return lookup;
        }

        int x = 0;
        while (x < points[0][0])
        {
            lookup[x] = points[0][1];
            x++;
        }

        int point = 0;
        while (point < points.Length - 1)
        {
            int deltaX = points[point + 1][0] - points[point][0];
            int deltaY = points[point + 1][1] - points[point][1];
            int delta = deltaY * ((65536 + (deltaX >> 1)) / deltaX);
            x = 0;
            while (x < deltaX)
            {
                lookup[points[point][0] + x] =
                    points[point][1] + ((x * delta + 32768) >> 16);
                x++;
            }
            point++;
        }

        x = points[points.Length - 1][0];
        while (x < 256)
        {
            lookup[x] = points[points.Length - 1][1];
            x++;
        }
        return lookup;
    }

    static int[][] CreateScalingLookups(
        Av1FilmGrainParameters parameters,
        int planeCount)
    {
        int[][] lookups = new int[planeCount][];
        lookups[0] = CreateScalingLookup(parameters.YPoints);
        int plane = 1;
        while (plane < planeCount)
        {
            int[][] points = parameters.ChromaScalingFromLuma
                ? parameters.YPoints
                : (plane == 1 ? parameters.CbPoints : parameters.CrPoints);
            lookups[plane] = CreateScalingLookup(points);
            plane++;
        }
        return lookups;
    }

    static int[][] GenerateGrain(Av1FilmGrainRequest request)
    {
        Av1FilmGrainParameters? parameters = request.Parameters;
        int[][] grain = new int[request.SourcePlanes.Length][];
        grain[0] = new int[LumaGrainWidth * LumaGrainHeight];
        FillWhiteNoise(
            grain[0],
            LumaGrainWidth,
            LumaGrainHeight,
            (parameters ?? throw new InvalidOperationException("Invalid internal state.")).GrainSeed,
            parameters.YPoints.Length > 0,
            request.BitDepth,
            parameters.GrainScaleShift);
        FilterLumaGrain(grain[0], parameters, request.BitDepth);

        if (grain.Length == 1)
        {
            return grain;
        }

        int chromaWidth = request.SubsamplingX ? 44 : 82;
        int chromaHeight = request.SubsamplingY ? 38 : 73;
        grain[1] = new int[chromaWidth * chromaHeight];
        grain[2] = new int[chromaWidth * chromaHeight];
        FillWhiteNoise(
            grain[1],
            chromaWidth,
            chromaHeight,
            parameters.GrainSeed ^ 0xb524,
            parameters.CbPoints.Length > 0
                || parameters.ChromaScalingFromLuma,
            request.BitDepth,
            parameters.GrainScaleShift);
        FillWhiteNoise(
            grain[2],
            chromaWidth,
            chromaHeight,
            parameters.GrainSeed ^ 0x49d8,
            parameters.CrPoints.Length > 0
                || parameters.ChromaScalingFromLuma,
            request.BitDepth,
            parameters.GrainScaleShift);
        FilterChromaGrain(
            grain, chromaWidth, chromaHeight, request);
        return grain;
    }

    static void FillWhiteNoise(
        int[] grain,
        int width,
        int height,
        int seed,
        bool enabled,
        int bitDepth,
        int grainScaleShift)
    {
        Av1FilmGrainRandom random = new Av1FilmGrainRandom(seed);
        int shift = 12 - bitDepth + grainScaleShift;
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int gaussian = enabled
                    ? gaussianSequence[random.Get(11)]
                    : 0;
                grain[y * width + x] = Round2(gaussian, shift);
                x++;
            }
            y++;
        }
    }

    static void FilterLumaGrain(
        int[] grain,
        Av1FilmGrainParameters parameters,
        int bitDepth)
    {
        if (parameters.YPoints.Length == 0)
        {
            return;
        }
        int lag = parameters.ArCoefficientLag;
        int minimum = -(128 << (bitDepth - 8));
        int maximum = (128 << (bitDepth - 8)) - 1;
        int y = 3;
        while (y < LumaGrainHeight)
        {
            int x = 3;
            while (x < LumaGrainWidth - 3)
            {
                int sum = 0;
                int position = 0;
                int deltaRow = -lag;
                while (deltaRow <= 0)
                {
                    int deltaColumn = -lag;
                    while (deltaColumn <= lag)
                    {
                        if (deltaRow == 0 && deltaColumn == 0)
                        {
                            break;
                        }
                        sum += grain[(y + deltaRow) * LumaGrainWidth
                            + x + deltaColumn]
                            * parameters.ArCoefficientsY[position];
                        position++;
                        deltaColumn++;
                    }
                    deltaRow++;
                }
                int index = y * LumaGrainWidth + x;
                grain[index] = Clip(
                    minimum,
                    maximum,
                    grain[index] + Round2(sum, parameters.ArCoefficientShift));
                x++;
            }
            y++;
        }
    }

    static void FilterChromaGrain(
        int[][] grain,
        int width,
        int height,
        Av1FilmGrainRequest request)
    {
        Av1FilmGrainParameters? parameters = request.Parameters;
        int lag = (parameters ?? throw new InvalidOperationException("Invalid internal state.")).ArCoefficientLag;
        int minimum = -(128 << (request.BitDepth - 8));
        int maximum = (128 << (request.BitDepth - 8)) - 1;
        int subX = request.SubsamplingX ? 1 : 0;
        int subY = request.SubsamplingY ? 1 : 0;
        bool cbEnabled = parameters.CbPoints.Length > 0
            || parameters.ChromaScalingFromLuma;
        bool crEnabled = parameters.CrPoints.Length > 0
            || parameters.ChromaScalingFromLuma;
        int y = 3;
        while (y < height)
        {
            int x = 3;
            while (x < width - 3)
            {
                int cbSum = 0;
                int crSum = 0;
                int position = 0;
                int deltaRow = -lag;
                while (deltaRow <= 0)
                {
                    int deltaColumn = -lag;
                    while (deltaColumn <= lag)
                    {
                        if (deltaRow == 0 && deltaColumn == 0)
                        {
                            if (parameters.YPoints.Length > 0)
                            {
                                int lumaX = ((x - 3) << subX) + 3;
                                int lumaY = ((y - 3) << subY) + 3;
                                int luma = 0;
                                int i = 0;
                                while (i <= subY)
                                {
                                    int j = 0;
                                    while (j <= subX)
                                    {
                                        luma += grain[0][
                                            (lumaY + i) * LumaGrainWidth
                                            + lumaX + j];
                                        j++;
                                    }
                                    i++;
                                }
                                luma = Round2(luma, subX + subY);
                                if (cbEnabled)
                                {
                                    cbSum += luma
                                        * parameters.ArCoefficientsCb[position];
                                }
                                if (crEnabled)
                                {
                                    crSum += luma
                                        * parameters.ArCoefficientsCr[position];
                                }
                            }
                            break;
                        }
                        int index = (y + deltaRow) * width
                            + x + deltaColumn;
                        if (cbEnabled)
                        {
                            cbSum += grain[1][index]
                                * parameters.ArCoefficientsCb[position];
                        }
                        if (crEnabled)
                        {
                            crSum += grain[2][index]
                                * parameters.ArCoefficientsCr[position];
                        }
                        position++;
                        deltaColumn++;
                    }
                    deltaRow++;
                }

                int grainIndex = y * width + x;
                if (cbEnabled)
                {
                    grain[1][grainIndex] = Clip(
                        minimum,
                        maximum,
                        grain[1][grainIndex]
                            + Round2(cbSum, parameters.ArCoefficientShift));
                }
                if (crEnabled)
                {
                    grain[2][grainIndex] = Clip(
                        minimum,
                        maximum,
                        grain[2][grainIndex]
                            + Round2(crSum, parameters.ArCoefficientShift));
                }
                x++;
            }
            y++;
        }
    }

    static int[][] GenerateNoise(
        Av1FilmGrainRequest request,
        int[][] grain)
    {
        int planeCount = request.SourcePlanes.Length;
        int[][] noise = new int[planeCount][];
        int plane = 0;
        while (plane < planeCount)
        {
            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX);
            int height = RoundUp(request.Height, subY);
            noise[plane] = new int[width * height];
            plane++;
        }

        int stripeCount = (request.Height + 31) / 32;
        int[][]? previous = null;
        int stripe = 0;
        while (stripe < stripeCount)
        {
            int[][] current = CreateStripePlanes(request);
            FillStripe(request, grain, stripe, current);
            CopyStripeToNoise(request, stripe, previous, current, noise);
            previous = current;
            stripe++;
        }
        return noise;
    }

    static int[][] CreateStripePlanes(Av1FilmGrainRequest request)
    {
        int[][] stripe = new int[request.SourcePlanes.Length][];
        int plane = 0;
        while (plane < stripe.Length)
        {
            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX) + (34 >> subX);
            int height = 34 >> subY;
            stripe[plane] = new int[width * height];
            plane++;
        }
        return stripe;
    }

    static void FillStripe(
        Av1FilmGrainRequest request,
        int[][] grain,
        int stripeNumber,
        int[][] stripe)
    {
        int seed = (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).GrainSeed;
        seed ^= ((stripeNumber * 37 + 178) & 255) << 8;
        seed ^= (stripeNumber * 173 + 105) & 255;
        Av1FilmGrainRandom random = new Av1FilmGrainRandom(seed);
        int halfWidth = (request.Width + 1) / 2;
        int x = 0;
        while (x < halfWidth)
        {
            int randomValue = random.Get(8);
            int offsetX = randomValue >> 4;
            int offsetY = randomValue & 15;
            int plane = 0;
            while (plane < stripe.Length)
            {
                FillStripeBlock(
                    request,
                    grain,
                    stripe,
                    plane,
                    x,
                    offsetX,
                    offsetY);
                plane++;
            }
            x += 16;
        }
    }

    static void FillStripeBlock(
        Av1FilmGrainRequest request,
        int[][] grain,
        int[][] stripe,
        int plane,
        int blockX,
        int offsetX,
        int offsetY)
    {
        int subX = PlaneSubX(request, plane);
        int subY = PlaneSubY(request, plane);
        int stripeWidth = RoundUp(request.Width, subX) + (34 >> subX);
        int grainWidth = plane == 0
            ? LumaGrainWidth
            : (request.SubsamplingX ? 44 : 82);
        int planeOffsetX = subX != 0 ? 6 + offsetX : 9 + offsetX * 2;
        int planeOffsetY = subY != 0 ? 6 + offsetY : 9 + offsetY * 2;
        int blockWidth = 34 >> subX;
        int blockHeight = 34 >> subY;
        int i = 0;
        while (i < blockHeight)
        {
            int j = 0;
            while (j < blockWidth)
            {
                int value = grain[plane][
                    (planeOffsetY + i) * grainWidth + planeOffsetX + j];
                int destinationX;
                if (subX == 0)
                {
                    destinationX = blockX * 2 + j;
                    if (j < 2 && (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).Overlap && blockX > 0)
                    {
                        int old = stripe[plane][i * stripeWidth + destinationX];
                        value = j == 0
                            ? old * 27 + value * 17
                            : old * 17 + value * 27;
                        value = ClipGrain(
                            request.BitDepth, Round2(value, 5));
                    }
                }
                else
                {
                    destinationX = blockX + j;
                    if (j == 0 && (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).Overlap && blockX > 0)
                    {
                        int old = stripe[plane][i * stripeWidth + destinationX];
                        value = ClipGrain(
                            request.BitDepth,
                            Round2(old * 23 + value * 22, 5));
                    }
                }
                stripe[plane][i * stripeWidth + destinationX] = value;
                j++;
            }
            i++;
        }
    }

    static void CopyStripeToNoise(
        Av1FilmGrainRequest request,
        int stripeNumber,
        int[][]? previous,
        int[][] current,
        int[][] noise)
    {
        int plane = 0;
        while (plane < noise.Length)
        {
            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX);
            int height = RoundUp(request.Height, subY);
            int stripeWidth = width + (34 >> subX);
            int rowCount = 32 >> subY;
            int firstY = stripeNumber << (5 - subY);
            int i = 0;
            while (i < rowCount && firstY + i < height)
            {
                int x = 0;
                while (x < width)
                {
                    int value = current[plane][i * stripeWidth + x];
                    if (previous != null && (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).Overlap)
                    {
                        if (subY == 0 && i < 2)
                        {
                            int old = previous[plane][
                                (i + 32) * stripeWidth + x];
                            value = i == 0
                                ? old * 27 + value * 17
                                : old * 17 + value * 27;
                            value = ClipGrain(
                                request.BitDepth, Round2(value, 5));
                        }
                        else if (subY != 0 && i == 0)
                        {
                            int old = previous[plane][16 * stripeWidth + x];
                            value = ClipGrain(
                                request.BitDepth,
                                Round2(old * 23 + value * 22, 5));
                        }
                    }
                    noise[plane][(firstY + i) * width + x] = value;
                    x++;
                }
                i++;
            }
            plane++;
        }
    }

    static void AddNoise(
        Av1FilmGrainRequest request,
        int[][] source,
        int[][] output,
        int[][] noise,
        int[][] scaling)
    {
        Av1FilmGrainParameters? parameters = request.Parameters;
        int depthShift = request.BitDepth - 8;
        int minimum = (parameters ?? throw new InvalidOperationException("Invalid internal state.")).ClipToRestrictedRange
            ? 16 << depthShift
            : 0;
        int maximumLuma = parameters.ClipToRestrictedRange
            ? 235 << depthShift
            : (256 << depthShift) - 1;
        int maximumChroma = parameters.ClipToRestrictedRange
            ? (request.MatrixCoefficients == 0
                ? maximumLuma
                : 240 << depthShift)
            : maximumLuma;

        if (output.Length > 1)
        {
            AddChromaNoise(
                request,
                source,
                output,
                noise,
                scaling,
                1,
                minimum,
                maximumChroma);
            AddChromaNoise(
                request,
                source,
                output,
                noise,
                scaling,
                2,
                minimum,
                maximumChroma);
        }

        if (parameters.YPoints.Length == 0)
        {
            return;
        }
        int y = 0;
        while (y < request.Height)
        {
            int x = 0;
            while (x < request.Width)
            {
                int index = y * request.Width + x;
                int original = source[0][index];
                int scale = ScaleLookup(
                    scaling[0], original, request.BitDepth);
                int delta = Round2(
                    scale * noise[0][index], parameters.ScalingShift);
                output[0][index] = Clip(
                    minimum, maximumLuma, original + delta);
                x++;
            }
            y++;
        }
    }

    static void AddChromaNoise(
        Av1FilmGrainRequest request,
        int[][] source,
        int[][] output,
        int[][] noise,
        int[][] scaling,
        int plane,
        int minimum,
        int maximum)
    {
        Av1FilmGrainParameters? parameters = request.Parameters;
        int[][] points = plane == 1
            ? (parameters ?? throw new InvalidOperationException("Invalid internal state.")).CbPoints
            : (parameters ?? throw new InvalidOperationException("Invalid internal state.")).CrPoints;
        if (points.Length == 0 && !parameters.ChromaScalingFromLuma)
        {
            return;
        }

        int subX = request.SubsamplingX ? 1 : 0;
        int subY = request.SubsamplingY ? 1 : 0;
        int width = RoundUp(request.Width, subX);
        int height = RoundUp(request.Height, subY);
        int multiplier = plane == 1
            ? parameters.CbMultiplier
            : parameters.CrMultiplier;
        int lumaMultiplier = plane == 1
            ? parameters.CbLumaMultiplier
            : parameters.CrLumaMultiplier;
        int offset = plane == 1
            ? parameters.CbOffset
            : parameters.CrOffset;
        int depthShift = request.BitDepth - 8;
        int maximumSample = (1 << request.BitDepth) - 1;
        int y = 0;
        while (y < height)
        {
            int x = 0;
            while (x < width)
            {
                int lumaX = x << subX;
                int lumaY = y << subY;
                int nextLumaX = System.Math.Min(lumaX + 1, request.Width - 1);
                int averageLuma = subX != 0
                    ? Round2(
                        source[0][lumaY * request.Width + lumaX]
                        + source[0][lumaY * request.Width + nextLumaX],
                        1)
                    : source[0][lumaY * request.Width + lumaX];
                int index = y * width + x;
                int original = source[plane][index];
                int merged;
                if (parameters.ChromaScalingFromLuma)
                {
                    merged = averageLuma;
                }
                else
                {
                    int combined = averageLuma * (lumaMultiplier - 128)
                        + original * (multiplier - 128);
                    merged = Clip(
                        0,
                        maximumSample,
                        (combined >> 6)
                            + ((offset - 256) << depthShift));
                }
                int scale = ScaleLookup(
                    scaling[plane], merged, request.BitDepth);
                int delta = Round2(
                    scale * noise[plane][index],
                    parameters.ScalingShift);
                output[plane][index] = Clip(
                    minimum, maximum, original + delta);
                x++;
            }
            y++;
        }
    }

    static int ScaleLookup(int[] lookup, int index, int bitDepth)
    {
        int shift = bitDepth - 8;
        int x = index >> shift;
        int remainder = index - (x << shift);
        if (bitDepth == 8 || x == 255)
        {
            return lookup[x];
        }
        return lookup[x] + Round2(
            (lookup[x + 1] - lookup[x]) * remainder, shift);
    }

    static int[][] Snapshot(Av1FilmGrainRequest request)
    {
        int[][] samples = new int[request.SourcePlanes.Length][];
        int plane = 0;
        while (plane < samples.Length)
        {
            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX);
            int height = RoundUp(request.Height, subY);
            samples[plane] = new int[width * height];
            int y = 0;
            while (y < height)
            {
                int x = 0;
                while (x < width)
                {
                    samples[plane][y * width + x] =
                        ReadSample(request.SourcePlanes[plane], x, y);
                    x++;
                }
                y++;
            }
            plane++;
        }
        return samples;
    }

    static int[][] ClonePlanes(int[][] source)
    {
        int[][] clone = new int[source.Length][];
        int plane = 0;
        while (plane < source.Length)
        {
            clone[plane] = (int[])source[plane].Clone();
            plane++;
        }
        return clone;
    }

    static void WritePlanes(
        Av1FilmGrainRequest request,
        int[][] output)
    {
        int plane = 0;
        while (plane < output.Length)
        {
            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX);
            int height = RoundUp(request.Height, subY);
            int y = 0;
            while (y < height)
            {
                int x = 0;
                while (x < width)
                {
                    WriteSample(
                        request.DestinationPlanes[plane],
                        x,
                        y,
                        output[plane][y * width + x]);
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

    static int PlaneSubX(Av1FilmGrainRequest request, int plane)
    {
        return plane > 0 && request.SubsamplingX ? 1 : 0;
    }

    static int PlaneSubY(Av1FilmGrainRequest request, int plane)
    {
        return plane > 0 && request.SubsamplingY ? 1 : 0;
    }

    static int RoundUp(int value, int shift)
    {
        return (value + (1 << shift) - 1) >> shift;
    }

    static int Round2(int value, int shift)
    {
        if (shift <= 0)
        {
            return value;
        }
        return (value + (1 << (shift - 1))) >> shift;
    }

    static int ClipGrain(int bitDepth, int value)
    {
        int center = 128 << (bitDepth - 8);
        return Clip(-center, center - 1, value);
    }

    static int Clip(int minimum, int maximum, int value)
    {
        return value < minimum ? minimum : (value > maximum ? maximum : value);
    }

    static AvifResultCode Validate(Av1FilmGrainRequest request)
    {
        if (request == null || request.Parameters == null
            || request.SourcePlanes == null
            || request.DestinationPlanes == null
            || request.SourcePlanes.Length != 1
                && request.SourcePlanes.Length != 3
            || request.DestinationPlanes.Length
                != request.SourcePlanes.Length
            || request.Width <= 0 || request.Height <= 0
            || request.Width > System.Array.MaxLength - 64
            || request.Height > System.Array.MaxLength - 64
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.MaxTemporaryBytes <= 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.SourcePlanes.Length == 1
            && (request.SubsamplingX || request.SubsamplingY))
        {
            return AvifResultCode.InvalidArgument;
        }
        int bytesPerSample = request.BitDepth > 8 ? 2 : 1;
        long temporaryBytes = 0;
        int plane = 0;
        while (plane < request.SourcePlanes.Length)
        {
            PlaneBuffer source = request.SourcePlanes[plane];
            PlaneBuffer destination = request.DestinationPlanes[plane];
            if (source == null || destination == null)
            {
                return AvifResultCode.InvalidArgument;
            }
            AvifResultCode state = source.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }
            state = destination.GetStateCode();
            if (state != AvifResultCode.Success)
            {
                return state;
            }

            int subX = PlaneSubX(request, plane);
            int subY = PlaneSubY(request, plane);
            int width = RoundUp(request.Width, subX);
            int height = RoundUp(request.Height, subY);
            if (source.Width < width || source.Height < height
                || destination.Width < width || destination.Height < height
                || source.BytesPerSample != bytesPerSample
                || destination.BytesPerSample != bytesPerSample)
            {
                return AvifResultCode.InvalidArgument;
            }

            long samples = (long)width * height;
            if (samples > System.Array.MaxLength)
            {
                return AvifResultCode.LimitExceeded;
            }
            temporaryBytes += samples * 12;
            temporaryBytes += (long)(width + (34 >> subX))
                * (34 >> subY) * 8;
            plane++;
        }
        temporaryBytes += (long)LumaGrainWidth * LumaGrainHeight * 4;
        if (request.SourcePlanes.Length > 1)
        {
            int chromaWidth = request.SubsamplingX ? 44 : 82;
            int chromaHeight = request.SubsamplingY ? 38 : 73;
            temporaryBytes += (long)chromaWidth * chromaHeight * 8;
        }
        if (temporaryBytes > request.MaxTemporaryBytes)
        {
            return AvifResultCode.LimitExceeded;
        }

        if (!request.Parameters.ApplyGrain)
        {
            return AvifResultCode.Success;
        }
        return ValidateParameters(request);
    }

    static AvifResultCode ValidateParameters(Av1FilmGrainRequest request)
    {
        Av1FilmGrainParameters? parameters = request.Parameters;
        if ((parameters ?? throw new InvalidOperationException("Invalid internal state.")).YPoints == null || parameters.CbPoints == null
            || parameters.CrPoints == null
            || parameters.ArCoefficientsY == null
            || parameters.ArCoefficientsCb == null
            || parameters.ArCoefficientsCr == null
            || parameters.GrainSeed < 0 || parameters.GrainSeed > 65535
            || parameters.YPoints.Length > 14
            || parameters.CbPoints.Length > 10
            || parameters.CrPoints.Length > 10
            || parameters.ScalingShift < 8 || parameters.ScalingShift > 11
            || parameters.ArCoefficientLag < 0
            || parameters.ArCoefficientLag > 3
            || parameters.ArCoefficientShift < 6
            || parameters.ArCoefficientShift > 9
            || parameters.GrainScaleShift < 0
            || parameters.GrainScaleShift > 3
            || !ValidatePoints(parameters.YPoints)
            || !ValidatePoints(parameters.CbPoints)
            || !ValidatePoints(parameters.CrPoints))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (parameters.ChromaScalingFromLuma
            && (parameters.CbPoints.Length != 0
                || parameters.CrPoints.Length != 0)
            || request.SubsamplingX && request.SubsamplingY
                && parameters.YPoints.Length == 0
                && (parameters.CbPoints.Length != 0
                    || parameters.CrPoints.Length != 0))
        {
            return AvifResultCode.InvalidArgument;
        }
        if (request.SourcePlanes.Length == 1
            && (parameters.CbPoints.Length != 0
                || parameters.CrPoints.Length != 0
                || parameters.ChromaScalingFromLuma))
        {
            return AvifResultCode.InvalidArgument;
        }

        int lumaCount = 2 * parameters.ArCoefficientLag
            * (parameters.ArCoefficientLag + 1);
        int chromaCount = lumaCount
            + (parameters.YPoints.Length > 0 ? 1 : 0);
        if (parameters.YPoints.Length > 0
            && !ValidateCoefficients(
                parameters.ArCoefficientsY, lumaCount)
            || parameters.YPoints.Length == 0
                && parameters.ArCoefficientsY.Length != 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        bool cbEnabled = parameters.CbPoints.Length > 0
            || parameters.ChromaScalingFromLuma;
        bool crEnabled = parameters.CrPoints.Length > 0
            || parameters.ChromaScalingFromLuma;
        if (cbEnabled
            && !ValidateCoefficients(
                parameters.ArCoefficientsCb, chromaCount)
            || !cbEnabled && parameters.ArCoefficientsCb.Length != 0
            || crEnabled
                && !ValidateCoefficients(
                    parameters.ArCoefficientsCr, chromaCount)
            || !crEnabled && parameters.ArCoefficientsCr.Length != 0)
        {
            return AvifResultCode.InvalidArgument;
        }
        if (parameters.CbPoints.Length > 0
            && !ValidateChromaTransform(
                parameters.CbMultiplier,
                parameters.CbLumaMultiplier,
                parameters.CbOffset)
            || parameters.CrPoints.Length > 0
                && !ValidateChromaTransform(
                    parameters.CrMultiplier,
                    parameters.CrLumaMultiplier,
                    parameters.CrOffset))
        {
            return AvifResultCode.InvalidArgument;
        }
        return AvifResultCode.Success;
    }

    static bool ValidatePoints(int[][] points)
    {
        int previous = -1;
        int index = 0;
        while (index < points.Length)
        {
            if (points[index] == null || points[index].Length != 2
                || points[index][0] <= previous || points[index][0] > 255
                || points[index][1] < 0 || points[index][1] > 255)
            {
                return false;
            }
            previous = points[index][0];
            index++;
        }
        return true;
    }

    static bool ValidateCoefficients(int[] coefficients, int count)
    {
        if (coefficients.Length != count)
        {
            return false;
        }
        int index = 0;
        while (index < coefficients.Length)
        {
            if (coefficients[index] < -128 || coefficients[index] > 127)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    static bool ValidateChromaTransform(
        int multiplier,
        int lumaMultiplier,
        int offset)
    {
        return multiplier >= 0 && multiplier <= 255
            && lumaMultiplier >= 0 && lumaMultiplier <= 255
            && offset >= 0 && offset <= 511;
    }

    static int[] LoadGaussianSequence()
    {
        byte[] bytes = System.Convert.FromBase64String(
            "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");
        int[] values = new int[2048];
        int index = 0;
        while (index < values.Length)
        {
            ushort encoded = (ushort)(bytes[index * 2]
                | bytes[index * 2 + 1] << 8);
            values[index] = IntegerConversion.ToInt16(encoded);
            index++;
        }
        return values;
    }
}
