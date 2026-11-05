namespace avif.Tests;

public class Av1FilmGrainTests
{
    [Test]
    public void RandomGeneratorAndScalingLookupMatchNormativeVectors()
    {
        Assert.That(
            Av1FilmGrain.GetRandomNumbers(0x1234, 8, 8),
            Is.EqualTo(new int[] { 137,68,34,145,200,228,242,249 }));

        int[] lookup = Av1FilmGrain.CreateScalingLookup(new int[][]
        {
            new int[] { 16,32 },
            new int[] { 80,160 },
            new int[] { 240,40 }
        });

        Assert.Multiple(() =>
        {
            Assert.That(lookup[0], Is.EqualTo(32));
            Assert.That(lookup[16], Is.EqualTo(32));
            Assert.That(lookup[48], Is.EqualTo(96));
            Assert.That(lookup[80], Is.EqualTo(160));
            Assert.That(lookup[160], Is.EqualTo(100));
            Assert.That(lookup[240], Is.EqualTo(40));
            Assert.That(lookup[255], Is.EqualTo(40));
        });
    }

    [TestCase(8, 13343646140869292864UL)]
    [TestCase(10, 10644484809977408678UL)]
    [TestCase(12, 825345216947282068UL)]
    public void MonochromeSynthesisIsDeterministicAtEveryBitDepth(
        int bitDepth,
        ulong expectedHash)
    {
        Av1FilmGrainRequest first = CreateRequest(
            35, 35, bitDepth, false, false, false);
        Av1FilmGrainRequest second = CreateRequest(
            35, 35, bitDepth, false, false, false);
        FillSource(first, 17);
        CopySource(first, second);

        AvifResultCode firstCode = Av1FilmGrain.Synthesize(first);
        AvifResultCode secondCode = Av1FilmGrain.Synthesize(second);

        Assert.Multiple(() =>
        {
            Assert.That(firstCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondCode, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadVisible(first.DestinationPlanes[0]),
                Is.EqualTo(ReadVisible(second.DestinationPlanes[0])));
            Assert.That(ReadVisible(first.DestinationPlanes[0]),
                Is.Not.EqualTo(ReadVisible(first.SourcePlanes[0])));
            Assert.That(Hash(first.DestinationPlanes), Is.EqualTo(expectedHash));
        });
        Close(first);
        Close(second);
    }

    [TestCase(8, false, false, 15068693989168307801UL)]
    [TestCase(10, true, false, 13025961192049286601UL)]
    [TestCase(12, true, true, 5688550243503021488UL)]
    public void ColorChromaFromLumaSupportsEveryLayoutAndBitDepth(
        int bitDepth,
        bool subsamplingX,
        bool subsamplingY,
        ulong expectedHash)
    {
        Av1FilmGrainRequest request = CreateRequest(
            37, 35, bitDepth, subsamplingX, subsamplingY, true);
        FillSource(request, 43);

        AvifResultCode code = Av1FilmGrain.Synthesize(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadVisible(request.DestinationPlanes[1]),
                Is.Not.EqualTo(ReadVisible(request.SourcePlanes[1])));
            Assert.That(ReadVisible(request.DestinationPlanes[2]),
                Is.Not.EqualTo(ReadVisible(request.SourcePlanes[2])));
            Assert.That(Hash(request.DestinationPlanes), Is.EqualTo(expectedHash));
        });
        Close(request);
    }

    [Test]
    public void OverlapChangesBlockSeamsWithoutChangingDeterminism()
    {
        Av1FilmGrainRequest withoutOverlap = CreateRequest(
            65, 65, 8, false, false, false);
        Av1FilmGrainRequest withOverlap = CreateRequest(
            65, 65, 8, false, false, false);
        (withoutOverlap.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).Overlap = false;
        (withOverlap.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).Overlap = true;
        FillSource(withoutOverlap, 5);
        CopySource(withoutOverlap, withOverlap);

        Assert.That(Av1FilmGrain.Synthesize(withoutOverlap),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(Av1FilmGrain.Synthesize(withOverlap),
            Is.EqualTo(AvifResultCode.Success));

        Assert.Multiple(() =>
        {
            Assert.That(ReadColumn(withoutOverlap.DestinationPlanes[0], 32),
                Is.Not.EqualTo(ReadColumn(withOverlap.DestinationPlanes[0], 32)));
            Assert.That(ReadRow(withoutOverlap.DestinationPlanes[0], 32),
                Is.Not.EqualTo(ReadRow(withOverlap.DestinationPlanes[0], 32)));
            Assert.That(Hash(withOverlap.DestinationPlanes),
                Is.EqualTo(13460426952741249888UL));
        });
        Close(withoutOverlap);
        Close(withOverlap);
    }

    [Test]
    public void IndependentChromaScalingWorksWithoutLumaGrain()
    {
        Av1FilmGrainRequest request = CreateRequest(
            33, 31, 8, false, false, true);
        (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).ChromaScalingFromLuma = false;
        request.Parameters.YPoints = System.Array.Empty<int[]>();
        request.Parameters.ArCoefficientsY = System.Array.Empty<int>();
        request.Parameters.CbPoints = new int[][]
        {
            new int[] { 0,96 },
            new int[] { 255,96 }
        };
        request.Parameters.CbMultiplier = 128;
        request.Parameters.CbLumaMultiplier = 128;
        request.Parameters.CbOffset = 320;
        request.Parameters.ArCoefficientsCb = new int[]
        {
            3,-4,7,2,-6,9,5,-8,4,1,-3,6
        };
        request.Parameters.ArCoefficientsCr = System.Array.Empty<int>();
        FillSource(request, 57);

        AvifResultCode code = Av1FilmGrain.Synthesize(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadVisible(request.DestinationPlanes[0]),
                Is.EqualTo(ReadVisible(request.SourcePlanes[0])));
            Assert.That(ReadVisible(request.DestinationPlanes[1]),
                Is.Not.EqualTo(ReadVisible(request.SourcePlanes[1])));
            Assert.That(ReadVisible(request.DestinationPlanes[2]),
                Is.EqualTo(ReadVisible(request.SourcePlanes[2])));
            Assert.That(Hash(request.DestinationPlanes),
                Is.EqualTo(17117464937857373980UL));
        });
        Close(request);
    }

    [Test]
    public void RestrictedRangeUsesIdentityChromaEndpoint()
    {
        Av1FilmGrainRequest identity = CreateRequest(
            32, 32, 8, false, false, true);
        Av1FilmGrainRequest nonIdentity = CreateRequest(
            32, 32, 8, false, false, true);
        (identity.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).ClipToRestrictedRange = true;
        (nonIdentity.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).ClipToRestrictedRange = true;
        identity.MatrixCoefficients = 0;
        nonIdentity.MatrixCoefficients = 1;
        FillConstant(identity, 255);
        CopySource(identity, nonIdentity);

        Assert.That(Av1FilmGrain.Synthesize(identity),
            Is.EqualTo(AvifResultCode.Success));
        Assert.That(Av1FilmGrain.Synthesize(nonIdentity),
            Is.EqualTo(AvifResultCode.Success));

        Assert.Multiple(() =>
        {
            Assert.That(Max(identity.DestinationPlanes[0]), Is.EqualTo(235));
            Assert.That(Max(identity.DestinationPlanes[1]), Is.EqualTo(235));
            Assert.That(Max(identity.DestinationPlanes[2]), Is.EqualTo(235));
            Assert.That(Max(nonIdentity.DestinationPlanes[1]), Is.EqualTo(240));
            Assert.That(Max(nonIdentity.DestinationPlanes[2]), Is.EqualTo(240));
            Assert.That(Min(identity.DestinationPlanes[0]),
                Is.GreaterThanOrEqualTo(16));
        });
        Close(identity);
        Close(nonIdentity);
    }

    [Test]
    public void DisabledGrainCopiesAliasedAndSeparatePlanesExactly()
    {
        Av1FilmGrainRequest request = CreateRequest(
            9, 7, 10, true, true, true);
        FillSource(request, 91);
        (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).ApplyGrain = false;
        request.DestinationPlanes[0].Close();
        request.DestinationPlanes[0] = request.SourcePlanes[0];

        AvifResultCode code = Av1FilmGrain.Synthesize(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadVisible(request.DestinationPlanes[0]),
                Is.EqualTo(ReadVisible(request.SourcePlanes[0])));
            Assert.That(ReadVisible(request.DestinationPlanes[1]),
                Is.EqualTo(ReadVisible(request.SourcePlanes[1])));
            Assert.That(ReadVisible(request.DestinationPlanes[2]),
                Is.EqualTo(ReadVisible(request.SourcePlanes[2])));
        });
        Close(request);
    }

    [Test]
    public void InvalidParametersLeaveDestinationUntouched()
    {
        Av1FilmGrainRequest request = CreateRequest(
            16, 16, 8, false, false, false);
        FillSource(request, 12);
        Fill(request.DestinationPlanes[0], 77);
        (request.Parameters ?? throw new InvalidOperationException("Invalid internal state.")).YPoints = new int[][]
        {
            new int[] { 20,40 },
            new int[] { 20,80 }
        };
        int[] before = ReadVisible(request.DestinationPlanes[0]);

        AvifResultCode code = Av1FilmGrain.Synthesize(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(ReadVisible(request.DestinationPlanes[0]),
                Is.EqualTo(before));
        });
        Close(request);
    }

    static Av1FilmGrainRequest CreateRequest(
        int width,
        int height,
        int bitDepth,
        bool subsamplingX,
        bool subsamplingY,
        bool color)
    {
        Av1FilmGrainRequest request = new Av1FilmGrainRequest();
        request.Width = width;
        request.Height = height;
        request.BitDepth = bitDepth;
        request.SubsamplingX = color && subsamplingX;
        request.SubsamplingY = color && subsamplingY;
        request.MatrixCoefficients = 1;
        request.Parameters = CreateParameters(color);
        int planeCount = color ? 3 : 1;
        request.SourcePlanes = new PlaneBuffer[planeCount];
        request.DestinationPlanes = new PlaneBuffer[planeCount];
        int plane = 0;
        while (plane < planeCount)
        {
            int planeWidth = plane == 0 || !subsamplingX
                ? width
                : (width + 1) >> 1;
            int planeHeight = plane == 0 || !subsamplingY
                ? height
                : (height + 1) >> 1;
            request.SourcePlanes[plane] = CreatePlane(
                planeWidth, planeHeight, bitDepth);
            request.DestinationPlanes[plane] = CreatePlane(
                planeWidth, planeHeight, bitDepth);
            plane++;
        }
        return request;
    }

    static Av1FilmGrainParameters CreateParameters(bool color)
    {
        Av1FilmGrainParameters parameters = new Av1FilmGrainParameters();
        parameters.ApplyGrain = true;
        parameters.GrainSeed = 0x7281;
        parameters.YPoints = new int[][]
        {
            new int[] { 0,48 },
            new int[] { 72,144 },
            new int[] { 180,96 },
            new int[] { 255,32 }
        };
        parameters.ScalingShift = 9;
        parameters.ArCoefficientLag = 2;
        parameters.ArCoefficientsY = new int[]
        {
            4,-7,12,5,-3,8,16,-9,6,2,-5,11
        };
        parameters.ArCoefficientShift = 7;
        parameters.GrainScaleShift = 1;
        parameters.Overlap = true;
        if (color)
        {
            parameters.ChromaScalingFromLuma = true;
            parameters.ArCoefficientsCb = new int[]
            {
                3,-4,7,2,-6,9,5,-8,4,1,-3,6,24
            };
            parameters.ArCoefficientsCr = new int[]
            {
                -2,5,-7,3,8,-4,6,1,-5,7,2,-6,-20
            };
        }
        return parameters;
    }

    static PlaneBuffer CreatePlane(int width, int height, int bitDepth)
    {
        return new PlaneBuffer(
            width, height, bitDepth > 8 ? 2 : 1, 1, 1 << 22, true);
    }

    static void FillSource(Av1FilmGrainRequest request, int offset)
    {
        int maximum = (1 << request.BitDepth) - 1;
        int scale = 1 << (request.BitDepth - 8);
        int plane = 0;
        while (plane < request.SourcePlanes.Length)
        {
            PlaneBuffer buffer = request.SourcePlanes[plane];
            int y = 0;
            while (y < buffer.Height)
            {
                int x = 0;
                while (x < buffer.Width)
                {
                    int value = (offset + plane * 29 + x * 7 + y * 11)
                        & 255;
                    WriteSample(buffer, x, y,
                        Math.Min(maximum, value * scale));
                    x++;
                }
                y++;
            }
            plane++;
        }
    }

    static void FillConstant(Av1FilmGrainRequest request, int value)
    {
        int plane = 0;
        while (plane < request.SourcePlanes.Length)
        {
            Fill(request.SourcePlanes[plane], value);
            plane++;
        }
    }

    static void CopySource(
        Av1FilmGrainRequest source,
        Av1FilmGrainRequest destination)
    {
        int plane = 0;
        while (plane < source.SourcePlanes.Length)
        {
            Assert.That(destination.SourcePlanes[plane].CopyFrom(
                source.SourcePlanes[plane]), Is.EqualTo(AvifResultCode.Success));
            plane++;
        }
    }

    static void Fill(PlaneBuffer plane, int value)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, value);
                x++;
            }
            y++;
        }
    }

    static int[] ReadVisible(PlaneBuffer plane)
    {
        int[] values = new int[plane.Width * plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                values[y * plane.Width + x] = ReadSample(plane, x, y);
                x++;
            }
            y++;
        }
        return values;
    }

    static int ReadSample(PlaneBuffer plane, int x, int y)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        return plane.BytesPerSample == 1
            ? row[offset]
            : row[offset] | row[offset + 1] << 8;
    }

    static int[] ReadColumn(PlaneBuffer plane, int x)
    {
        int[] values = new int[plane.Height];
        int y = 0;
        while (y < plane.Height)
        {
            values[y] = ReadSample(plane, x, y);
            y++;
        }
        return values;
    }

    static int[] ReadRow(PlaneBuffer plane, int y)
    {
        int[] values = new int[plane.Width];
        int x = 0;
        while (x < plane.Width)
        {
            values[x] = ReadSample(plane, x, y);
            x++;
        }
        return values;
    }

    static void WriteSample(PlaneBuffer plane, int x, int y, int value)
    {
        Span<byte> row = plane.GetRowSpan(y);
        int offset = x * plane.BytesPerSample;
        row[offset] = (byte)value;
        if (plane.BytesPerSample == 2)
        {
            row[offset + 1] = (byte)(value >> 8);
        }
    }

    static int Min(PlaneBuffer plane)
    {
        return ReadVisible(plane).Min();
    }

    static int Max(PlaneBuffer plane)
    {
        return ReadVisible(plane).Max();
    }

    static ulong Hash(PlaneBuffer[] planes)
    {
        ulong hash = 1469598103934665603UL;
        int plane = 0;
        while (plane < planes.Length)
        {
            int[] values = ReadVisible(planes[plane]);
            int index = 0;
            while (index < values.Length)
            {
                hash ^= (uint)values[index];
                hash *= 1099511628211UL;
                index++;
            }
            plane++;
        }
        return hash;
    }

    static void Close(Av1FilmGrainRequest request)
    {
        int plane = 0;
        while (plane < request.SourcePlanes.Length)
        {
            if (request.DestinationPlanes[plane]
                != request.SourcePlanes[plane])
            {
                request.DestinationPlanes[plane].Close();
            }
            request.SourcePlanes[plane].Close();
            plane++;
        }
    }
}
