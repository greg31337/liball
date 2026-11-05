namespace avif.Tests;

public class Av1SuperResolutionTests
{
    [Test]
    public void NormativeFilterHasUnityGainAtEveryPhase()
    {
        int phase = 0;
        while (phase < Av1SyntaxConstants.SuperresFilterShifts)
        {
            int sum = 0;
            int tap = 0;
            while (tap < Av1SyntaxConstants.SuperresFilterTaps)
            {
                sum += Av1SuperResolution.GetFilterCoefficient(phase, tap);
                tap++;
            }

            Assert.That(sum, Is.EqualTo(128), $"phase={phase}");
            phase++;
        }

        int[] middlePhase = new int[] { -2,7,-20,79,79,-20,7,-2 };
        int index = 0;
        while (index < middlePhase.Length)
        {
            Assert.That(Av1SuperResolution.GetFilterCoefficient(32, index),
                Is.EqualTo(middlePhase[index]));
            index++;
        }
    }

    [Test]
    public void EightBitRampMatchesNormativeVectorAndReplicatesVisibleEdges()
    {
        Av1SuperResolutionRequest request = CreateRequest(
            12, 1, 12, 8, false, false, false, 8, 2);
        int x = 0;
        while (x < 8)
        {
            WriteSample(request.Source[0], x, 0, x * 20);
            x++;
        }
        while (x < request.Source[0].Width)
        {
            WriteSample(request.Source[0], x, 0, 255);
            x++;
        }
        Fill(request.Destination[0], 77);

        AvifResultCode code = Av1SuperResolution.Upscale(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRow(request.Destination[0], 0, 12), Is.EqualTo(
                new int[] { 0,8,23,37,50,63,77,90,103,117,132,141 }));
            Assert.That(ReadSample(request.Destination[0], 12, 0), Is.EqualTo(77));
            Assert.That(ReadSample(request.Destination[0], 13, 0), Is.EqualTo(77));
        });
        Close(request);
    }

    [Test]
    public void RampMatchesNormativeOutputAtEverySupportedBitDepth()
    {
        int[] bitDepths = new int[] { 8,10,12 };
        int[] scales = new int[] { 1,4,16 };
        int[][] expected = new int[][]
        {
            new int[] { 0,8,23,37,50,63,77,90,103,117,132,141 },
            new int[] { 0,33,94,148,199,253,308,361,412,466,528,565 },
            new int[] { 0,130,375,593,795,1010,1230,1445,1648,1865,2110,2260 }
        };
        int depthIndex = 0;
        while (depthIndex < bitDepths.Length)
        {
            Av1SuperResolutionRequest request = CreateRequest(
                12, 1, 12, bitDepths[depthIndex], false, false, false, 0, 0);
            int x = 0;
            while (x < request.Source[0].Width)
            {
                WriteSample(request.Source[0], x, 0,
                    x * 20 * scales[depthIndex]);
                x++;
            }

            AvifResultCode code = Av1SuperResolution.Upscale(request);

            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadRow(request.Destination[0], 0, 12),
                Is.EqualTo(expected[depthIndex]), $"bitDepth={bitDepths[depthIndex]}");
            Close(request);
            depthIndex++;
        }
    }

    [Test]
    public void EveryDenominatorPreservesConstantColorAcrossChromaLayouts()
    {
        bool[][] layouts = new bool[][]
        {
            new bool[] { false,false },
            new bool[] { true,false },
            new bool[] { true,true }
        };
        int[] bitDepths = new int[] { 8,10,12 };
        int denominator = 9;
        while (denominator <= 16)
        {
            int depthIndex = 0;
            while (depthIndex < bitDepths.Length)
            {
                int layoutIndex = 0;
                while (layoutIndex < layouts.Length)
                {
                    Av1SuperResolutionRequest request = CreateRequest(
                        31, 5, denominator, bitDepths[depthIndex], true,
                        layouts[layoutIndex][0], layouts[layoutIndex][1], 0, 0);
                    int maximum = (1 << bitDepths[depthIndex]) - 1;
                    int plane = 0;
                    while (plane < request.Source.Length)
                    {
                        Fill(request.Source[plane], maximum - plane * 17);
                        plane++;
                    }

                    AvifResultCode code = Av1SuperResolution.Upscale(request);

                    Assert.That(code, Is.EqualTo(AvifResultCode.Success));
                    plane = 0;
                    while (plane < request.Destination.Length)
                    {
                        Assert.That(ReadPlane(request.Destination[plane]),
                            Is.All.EqualTo(maximum - plane * 17),
                            $"denominator={denominator}, bitDepth={bitDepths[depthIndex]}, layout={layoutIndex}, plane={plane}");
                        plane++;
                    }
                    Close(request);
                    layoutIndex++;
                }
                depthIndex++;
            }
            denominator++;
        }
    }

    [Test]
    public void OddDimensionsUseCeilingChromaPlaneSizes()
    {
        Av1SuperResolutionRequest request = CreateRequest(
            15, 7, 12, 10, true, true, true, 0, 0);
        Fill(request.Source[0], 101);
        Fill(request.Source[1], 202);
        Fill(request.Source[2], 303);

        AvifResultCode code = Av1SuperResolution.Upscale(request);

        Assert.Multiple(() =>
        {
            Assert.That(code, Is.EqualTo(AvifResultCode.Success));
            Assert.That(request.Source[1].Width, Is.EqualTo(5));
            Assert.That(request.Source[1].Height, Is.EqualTo(4));
            Assert.That(request.Destination[1].Width, Is.EqualTo(8));
            Assert.That(request.Destination[1].Height, Is.EqualTo(4));
            Assert.That(ReadPlane(request.Destination[0]), Is.All.EqualTo(101));
            Assert.That(ReadPlane(request.Destination[1]), Is.All.EqualTo(202));
            Assert.That(ReadPlane(request.Destination[2]), Is.All.EqualTo(303));
        });
        Close(request);
    }

    [Test]
    public void DisabledSuperResolutionCopiesEveryPlane()
    {
        Av1SuperResolutionRequest request = CreateRequest(
            9, 3, 8, 12, true, true, false, 0, 0);
        int plane = 0;
        while (plane < request.Source.Length)
        {
            FillPattern(request.Source[plane], plane * 100);
            plane++;
        }

        AvifResultCode code = Av1SuperResolution.Upscale(request);

        Assert.That(code, Is.EqualTo(AvifResultCode.Success));
        plane = 0;
        while (plane < request.Source.Length)
        {
            Assert.That(ReadPlane(request.Destination[plane]),
                Is.EqualTo(ReadPlane(request.Source[plane])));
            plane++;
        }
        Close(request);
    }

    [Test]
    public void InvalidGeometryLeavesEveryDestinationUnchanged()
    {
        Av1SuperResolutionRequest request = CreateRequest(
            12, 2, 12, 8, true, true, true, 0, 0);
        int plane = 0;
        while (plane < request.Destination.Length)
        {
            Fill(request.Source[plane], 25 + plane);
            Fill(request.Destination[plane], 199);
            plane++;
        }
        (request.Size ?? throw new InvalidOperationException("Invalid internal state.")).FrameWidth++;

        AvifResultCode code = Av1SuperResolution.Upscale(request);

        Assert.That(code, Is.EqualTo(AvifResultCode.InvalidArgument));
        plane = 0;
        while (plane < request.Destination.Length)
        {
            Assert.That(ReadPlane(request.Destination[plane]), Is.All.EqualTo(199));
            plane++;
        }
        Close(request);
    }

    static Av1SuperResolutionRequest CreateRequest(
        int upscaledWidth,
        int height,
        int denominator,
        int bitDepth,
        bool color,
        bool subsamplingX,
        bool subsamplingY,
        int sourcePadding,
        int destinationPadding)
    {
        int frameWidth = (upscaledWidth * Av1SyntaxConstants.SuperresNumerator
            + denominator / 2) / denominator;
        Av1FrameSize size = new Av1FrameSize();
        size.UpscaledWidth = upscaledWidth;
        size.FrameWidth = frameWidth;
        size.FrameHeight = height;
        size.RenderWidth = upscaledWidth;
        size.RenderHeight = height;
        size.UsesSuperres = denominator != Av1SyntaxConstants.SuperresNumerator;
        size.SuperresDenominator = denominator;
        size.MiColumns = 2 * ((frameWidth + 7) >> 3);
        size.MiRows = 2 * ((height + 7) >> 3);

        Av1SuperResolutionRequest request = new Av1SuperResolutionRequest();
        request.Size = size;
        request.BitDepth = bitDepth;
        request.SubsamplingX = subsamplingX;
        request.SubsamplingY = subsamplingY;
        int planeCount = color ? 3 : 1;
        request.Source = new PlaneBuffer[planeCount];
        request.Destination = new PlaneBuffer[planeCount];
        int bytesPerSample = bitDepth > 8 ? 2 : 1;
        int plane = 0;
        while (plane < planeCount)
        {
            int subX = plane == 0 || !subsamplingX ? 0 : 1;
            int subY = plane == 0 || !subsamplingY ? 0 : 1;
            int sourceWidth = Round2(frameWidth, subX);
            int destinationWidth = Round2(upscaledWidth, subX);
            int planeHeight = Round2(height, subY);
            request.Source[plane] = new PlaneBuffer(
                sourceWidth + sourcePadding, planeHeight, bytesPerSample,
                1, 1 << 20, true);
            request.Destination[plane] = new PlaneBuffer(
                destinationWidth + destinationPadding, planeHeight, bytesPerSample,
                1, 1 << 20, true);
            plane++;
        }
        return request;
    }

    static int Round2(int value, int shift)
    {
        return (value + (1 << shift) - 1) >> shift;
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

    static void FillPattern(PlaneBuffer plane, int offset)
    {
        int y = 0;
        while (y < plane.Height)
        {
            int x = 0;
            while (x < plane.Width)
            {
                WriteSample(plane, x, y, offset + y * plane.Width + x);
                x++;
            }
            y++;
        }
    }

    static int[] ReadPlane(PlaneBuffer plane)
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

    static int[] ReadRow(PlaneBuffer plane, int y, int width)
    {
        int[] values = new int[width];
        int x = 0;
        while (x < width)
        {
            values[x] = ReadSample(plane, x, y);
            x++;
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

    static void Close(Av1SuperResolutionRequest request)
    {
        int plane = 0;
        while (plane < request.Source.Length)
        {
            request.Source[plane].Close();
            request.Destination[plane].Close();
            plane++;
        }
    }
}
