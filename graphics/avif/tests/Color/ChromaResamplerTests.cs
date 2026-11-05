namespace avif.Tests;

public class ChromaResamplerTests
{
    [Test]
    public void NearestUpsamplingUsesVerticalSitingAndOddEdgeReplication()
    {
        AvifPlane source = AvifPlane.Create(3, 2, 8, AvifPlaneKind.U);
        AvifPlane destination = AvifPlane.Create(5, 3, 8, AvifPlaneKind.U);
        WriteSamples(source, new int[] { 10, 20, 30, 40, 50, 60 });

        AvifResultCode result = Resample(
            source,
            destination,
            5,
            3,
            AvifChromaFormat.Yuv420,
            AvifChromaSamplePosition.Vertical,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Nearest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination), Is.EqualTo(new int[]
            {
                10, 20, 20, 30, 30,
                10, 20, 20, 30, 30,
                40, 50, 50, 60, 60
            }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void BilinearUpsamplingDistinguishesVerticalAndColocatedSiting()
    {
        AvifPlane source = AvifPlane.Create(3, 2, 8, AvifPlaneKind.U);
        AvifPlane vertical = AvifPlane.Create(5, 3, 8, AvifPlaneKind.U);
        AvifPlane colocated = AvifPlane.Create(5, 3, 8, AvifPlaneKind.U);
        WriteSamples(source, new int[] { 10, 20, 30, 40, 50, 60 });

        AvifResultCode verticalResult = Resample(
            source,
            vertical,
            5,
            3,
            AvifChromaFormat.Yuv420,
            AvifChromaSamplePosition.Vertical,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Bilinear);
        AvifResultCode colocatedResult = Resample(
            source,
            colocated,
            5,
            3,
            AvifChromaFormat.Yuv420,
            AvifChromaSamplePosition.Colocated,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Bilinear);

        Assert.Multiple(() =>
        {
            Assert.That(verticalResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(colocatedResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(vertical), Is.EqualTo(new int[]
            {
                10, 15, 20, 25, 30,
                18, 23, 28, 33, 38,
                33, 38, 43, 48, 53
            }));
            Assert.That(ReadSamples(colocated), Is.EqualTo(new int[]
            {
                10, 15, 20, 25, 30,
                25, 30, 35, 40, 45,
                40, 45, 50, 55, 60
            }));
        });

        source.Close();
        vertical.Close();
        colocated.Close();
    }

    [Test]
    public void FourTwoTwoUsesHorizontallyColocatedGridWhenPositionIsUnknown()
    {
        AvifPlane source = AvifPlane.Create(3, 1, 8, AvifPlaneKind.U);
        AvifPlane destination = AvifPlane.Create(5, 1, 8, AvifPlaneKind.U);
        WriteSamples(source, new int[] { 10, 20, 30 });

        AvifResultCode result = Resample(
            source,
            destination,
            5,
            1,
            AvifChromaFormat.Yuv422,
            AvifChromaSamplePosition.Unknown,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Bilinear);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination),
                Is.EqualTo(new int[] { 10, 15, 20, 25, 30 }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void BicubicUpsamplingUsesCatmullRomReconstructionAndClampsRinging()
    {
        AvifPlane source = AvifPlane.Create(4, 1, 8, AvifPlaneKind.V);
        AvifPlane destination = AvifPlane.Create(7, 1, 8, AvifPlaneKind.V);
        WriteSamples(source, new int[] { 0, 100, 0, 0 });

        AvifResultCode result = Resample(
            source,
            destination,
            7,
            1,
            AvifChromaFormat.Yuv422,
            AvifChromaSamplePosition.Vertical,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Bicubic);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination),
                Is.EqualTo(new int[] { 0, 56, 100, 56, 0, 0, 0 }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void LanczosUpsamplingUsesThreeLobeReconstruction()
    {
        AvifPlane source = AvifPlane.Create(4, 1, 8, AvifPlaneKind.V);
        AvifPlane destination = AvifPlane.Create(7, 1, 8, AvifPlaneKind.V);
        WriteSamples(source, new int[] { 0, 100, 0, 0 });

        AvifResultCode result = Resample(
            source,
            destination,
            7,
            1,
            AvifChromaFormat.Yuv422,
            AvifChromaSamplePosition.Vertical,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Lanczos3);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination),
                Is.EqualTo(new int[] { 0, 61, 100, 61, 0, 0, 0 }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void EveryFilteredModePreservesConstantHighDepthPlanesAtOddBorders()
    {
        AvifResamplingFilter[] filters = new AvifResamplingFilter[]
        {
            AvifResamplingFilter.Bilinear,
            AvifResamplingFilter.Bicubic,
            AvifResamplingFilter.Lanczos3
        };
        foreach (AvifResamplingFilter filter in filters)
        {
            AvifPlane source = AvifPlane.Create(3, 2, 10, AvifPlaneKind.U);
            AvifPlane destination = AvifPlane.Create(5, 3, 10, AvifPlaneKind.U);
            WriteSamples(source, Enumerable.Repeat(777, 6).ToArray());

            AvifResultCode result = Resample(
                source,
                destination,
                5,
                3,
                AvifChromaFormat.Yuv420,
                AvifChromaSamplePosition.Unknown,
                AvifChromaFormat.Yuv444,
                AvifChromaSamplePosition.Unknown,
                filter);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.EqualTo(AvifResultCode.Success));
                Assert.That(ReadSamples(destination), Is.All.EqualTo(777));
            });

            source.Close();
            destination.Close();
        }
    }

    [Test]
    public void NearestDownsamplingUsesDestinationSitingAndClampsOddBottomEdge()
    {
        AvifPlane source = AvifPlane.Create(5, 3, 8, AvifPlaneKind.U);
        AvifPlane destination = AvifPlane.Create(3, 2, 8, AvifPlaneKind.U);
        WriteSamples(source, new int[]
        {
            0, 1, 2, 3, 4,
            10, 11, 12, 13, 14,
            20, 21, 22, 23, 24
        });

        AvifResultCode result = Resample(
            source,
            destination,
            5,
            3,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifChromaFormat.Yuv420,
            AvifChromaSamplePosition.Vertical,
            AvifResamplingFilter.Nearest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination),
                Is.EqualTo(new int[] { 10, 12, 14, 20, 22, 24 }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void BilinearDownsamplingWidensItsKernelToAvoidAliasing()
    {
        AvifPlane source = AvifPlane.Create(5, 1, 8, AvifPlaneKind.U);
        AvifPlane destination = AvifPlane.Create(3, 1, 8, AvifPlaneKind.U);
        WriteSamples(source, new int[] { 0, 100, 0, 100, 0 });

        AvifResultCode result = Resample(
            source,
            destination,
            5,
            1,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifChromaFormat.Yuv422,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Bilinear);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination),
                Is.EqualTo(new int[] { 25, 50, 25 }));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void ImageResamplingConvertsBothChromaPlanesWithoutTouchingLuma()
    {
        AvifImage source = CreateImage(
            5,
            3,
            AvifChromaFormat.Yuv420,
            AvifChromaSamplePosition.Vertical);
        AvifImage destination = CreateImage(
            5,
            3,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown);
        WriteSamples(
            source.GetPlane(AvifPlaneKind.U),
            new int[] { 10, 20, 30, 40, 50, 60 });
        WriteSamples(
            source.GetPlane(AvifPlaneKind.V),
            new int[] { 60, 50, 40, 30, 20, 10 });
        WriteSamples(
            destination.GetPlane(AvifPlaneKind.Y),
            Enumerable.Repeat(99, 15).ToArray());

        AvifResultCode result = ChromaResampler.ResampleImageChroma(
            source, destination, AvifResamplingFilter.Bilinear);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination.GetPlane(AvifPlaneKind.U)),
                Is.EqualTo(new int[]
                {
                    10, 15, 20, 25, 30,
                    18, 23, 28, 33, 38,
                    33, 38, 43, 48, 53
                }));
            Assert.That(ReadSamples(destination.GetPlane(AvifPlaneKind.V)),
                Is.EqualTo(new int[]
                {
                    60, 55, 50, 45, 40,
                    53, 48, 43, 38, 33,
                    38, 33, 28, 23, 18
                }));
            Assert.That(ReadSamples(destination.GetPlane(AvifPlaneKind.Y)),
                Is.All.EqualTo(99));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void IdentityResamplingSnapshotsOverlappingPlaneViews()
    {
        AvifPlane owner = AvifPlane.Create(5, 1, 8, AvifPlaneKind.U);
        WriteSamples(owner, new int[] { 1, 2, 3, 4, 5 });
        AvifPlane source = owner.CreateView(0, 0, 4, 1);
        AvifPlane destination = owner.CreateView(1, 0, 4, 1);

        AvifResultCode result = Resample(
            source,
            destination,
            4,
            1,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown,
            AvifResamplingFilter.Nearest);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(owner),
                Is.EqualTo(new int[] { 1, 1, 2, 3, 4 }));
        });

        source.Close();
        destination.Close();
        owner.Close();
    }

    [Test]
    public void ResamplingRejectsInvalidPlanesDimensionsModesAndStates()
    {
        AvifPlane source = AvifPlane.Create(2, 2, 8, AvifPlaneKind.U);
        AvifPlane destination = AvifPlane.Create(3, 3, 8, AvifPlaneKind.U);
        AvifPlane yPlane = AvifPlane.Create(2, 2, 8, AvifPlaneKind.Y);
        AvifPlane closed = AvifPlane.Create(3, 3, 8, AvifPlaneKind.U);
        closed.Close();
        AvifImage monochrome = AvifImage.Create(
            3, 3, 8, AvifChromaFormat.Monochrome, false);
        AvifImage color = CreateImage(
            3,
            3,
            AvifChromaFormat.Yuv444,
            AvifChromaSamplePosition.Unknown);

        Assert.Multiple(() =>
        {
            Assert.That(
                Resample(
                    source,
                    destination,
                    3,
                    3,
                    AvifChromaFormat.Yuv420,
                    AvifChromaSamplePosition.Vertical,
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.Success));
            Assert.That(
                Resample(
                    yPlane,
                    destination,
                    3,
                    3,
                    AvifChromaFormat.Yuv420,
                    AvifChromaSamplePosition.Vertical,
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(
                Resample(
                    source,
                    destination,
                    4,
                    3,
                    AvifChromaFormat.Yuv420,
                    AvifChromaSamplePosition.Vertical,
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(
                Resample(
                    source,
                    destination,
                    3,
                    3,
                    AvifChromaFormat.Yuv420,
                    AvifChromaSamplePosition.Vertical,
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    (AvifResamplingFilter)99),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(
                Resample(
                    source,
                    closed,
                    3,
                    3,
                    AvifChromaFormat.Yuv420,
                    AvifChromaSamplePosition.Vertical,
                    AvifChromaFormat.Yuv444,
                    AvifChromaSamplePosition.Unknown,
                    AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.Closed));
            Assert.That(
                ChromaResampler.ResampleImageChroma(
                    monochrome, color, AvifResamplingFilter.Bilinear),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        source.Close();
        destination.Close();
        yPlane.Close();
        monochrome.Close();
        color.Close();
    }

    static AvifResultCode Resample(
        AvifPlane source,
        AvifPlane destination,
        int width,
        int height,
        AvifChromaFormat sourceFormat,
        AvifChromaSamplePosition sourcePosition,
        AvifChromaFormat destinationFormat,
        AvifChromaSamplePosition destinationPosition,
        AvifResamplingFilter filter)
    {
        return ChromaResampler.ResamplePlane(
            source,
            destination,
            width,
            height,
            sourceFormat,
            sourcePosition,
            destinationFormat,
            destinationPosition,
            filter);
    }

    static AvifImage CreateImage(
        int width,
        int height,
        AvifChromaFormat format,
        AvifChromaSamplePosition position)
    {
        return AvifImage.Create(
            width,
            height,
            8,
            format,
            AvifRange.Full,
            position,
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
    }

    static void WriteSamples(AvifPlane? plane, int[] values)
    {
        int index = 0;
        for (int y = 0; y < (plane ?? throw new InvalidOperationException("Invalid internal state.")).GetHeight(); y++)
        {
            Span<byte> row = plane.GetRowSpan(y);
            for (int x = 0; x < plane.GetWidth(); x++)
            {
                int value = values[index++];
                if (plane.GetBitDepth() == 8)
                {
                    row[x] = (byte)value;
                }
                else
                {
                    int offset = x * 2;
                    row[offset] = (byte)value;
                    row[offset + 1] = (byte)(value >> 8);
                }
            }
        }
    }

    static int[] ReadSamples(AvifPlane? plane)
    {
        int[] values = new int[(plane ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth() * plane.GetHeight()];
        int index = 0;
        for (int y = 0; y < plane.GetHeight(); y++)
        {
            Span<byte> row = plane.GetRowSpan(y);
            for (int x = 0; x < plane.GetWidth(); x++)
            {
                if (plane.GetBitDepth() == 8)
                {
                    values[index++] = row[x];
                }
                else
                {
                    int offset = x * 2;
                    values[index++] = row[offset] | row[offset + 1] << 8;
                }
            }
        }
        return values;
    }
}
