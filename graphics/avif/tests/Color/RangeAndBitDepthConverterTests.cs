namespace avif.Tests;

public class RangeAndBitDepthConverterTests
{
    [Test]
    public void EveryRangeAndDepthCombinationMapsExactEndpoints()
    {
        int[] depths = new int[] { 8, 10, 12, 16 };
        AvifRange[] ranges = new AvifRange[]
        {
            AvifRange.Full,
            AvifRange.Limited
        };

        foreach (int sourceDepth in depths)
        {
            foreach (int destinationDepth in depths)
            {
                foreach (AvifRange sourceRange in ranges)
                {
                    foreach (AvifRange destinationRange in ranges)
                    {
                        foreach (bool chroma in new bool[] { false, true })
                        {
                            int sourceMinimum = RangeConverter.GetMinimum(
                                sourceDepth, sourceRange);
                            int sourceMaximum = RangeConverter.GetMaximum(
                                sourceDepth, sourceRange, chroma);
                            int destinationMinimum = RangeConverter.GetMinimum(
                                destinationDepth, destinationRange);
                            int destinationMaximum = RangeConverter.GetMaximum(
                                destinationDepth, destinationRange, chroma);

                            Assert.Multiple(() =>
                            {
                                Assert.That(
                                    RangeConverter.ConvertSample(
                                        sourceMinimum,
                                        sourceDepth,
                                        sourceRange,
                                        destinationDepth,
                                        destinationRange,
                                        chroma),
                                    Is.EqualTo(destinationMinimum));
                                Assert.That(
                                    RangeConverter.ConvertSample(
                                        sourceMaximum,
                                        sourceDepth,
                                        sourceRange,
                                        destinationDepth,
                                        destinationRange,
                                        chroma),
                                    Is.EqualTo(destinationMaximum));
                                Assert.That(
                                    RangeConverter.ConvertSample(
                                        sourceMinimum - 1,
                                        sourceDepth,
                                        sourceRange,
                                        destinationDepth,
                                        destinationRange,
                                        chroma),
                                    Is.EqualTo(destinationMinimum));
                                Assert.That(
                                    RangeConverter.ConvertSample(
                                        sourceMaximum + 1,
                                        sourceDepth,
                                        sourceRange,
                                        destinationDepth,
                                        destinationRange,
                                        chroma),
                                    Is.EqualTo(destinationMaximum));
                            });
                        }
                    }
                }
            }
        }
    }

    [Test]
    public void ImageConversionUsesDistinctLumaChromaRangesAndFullRangeAlpha()
    {
        AvifImage source = CreateImage(
            3, 3, 10, AvifRange.Limited, AvifAlphaMode.Straight);
        AvifImage destination = CreateImage(
            3, 3, 8, AvifRange.Full, AvifAlphaMode.Straight);
        FillAlternating(
            source.GetPlane(AvifPlaneKind.Y), 64, 940);
        FillAlternating(
            source.GetPlane(AvifPlaneKind.U), 64, 960);
        FillAlternating(
            source.GetPlane(AvifPlaneKind.V), 64, 960);
        FillAlternating(
            source.GetPlane(AvifPlaneKind.Alpha), 0, 1023);

        AvifResultCode result = BitDepthConverter.ConvertImage(
            source, destination, AvifDitherMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            AssertAlternatingEndpoints(
                destination.GetPlane(AvifPlaneKind.Y), 0, 255);
            AssertAlternatingEndpoints(
                destination.GetPlane(AvifPlaneKind.U), 0, 255);
            AssertAlternatingEndpoints(
                destination.GetPlane(AvifPlaneKind.V), 0, 255);
            AssertAlternatingEndpoints(
                destination.GetPlane(AvifPlaneKind.Alpha), 0, 255);
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void NearestConversionRoundsPredictablyAndPreservesPadding()
    {
        AvifPlane source = AvifPlane.Create(
            3, 2, 16, AvifPlaneKind.Y, 8, 32);
        AvifPlane destination = AvifPlane.Create(
            3, 2, 8, AvifPlaneKind.Y, 8, 16);
        ((destination.GetInternalBuffer() ?? throw new InvalidOperationException("Invalid internal state.")).Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Data.AsSpan().Fill(0xcc);
        int[] samples = new int[]
        {
            0, 32768, 65535,
            65535, 32768, 0
        };
        WriteSamples(source, samples);

        AvifResultCode result = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            destination,
            AvifRange.Full,
            AvifDitherMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(destination), Is.EqualTo(
                new int[] { 0, 128, 255, 255, 128, 0 }));
            byte[] storage = ((destination.GetInternalBuffer() ?? throw new InvalidOperationException("Invalid internal state.")).Buffer ?? throw new InvalidOperationException("Invalid internal state.")).Data;
            Assert.That(storage.AsSpan(3, 5).ToArray(),
                Is.All.EqualTo((byte)0xcc));
            Assert.That(storage.AsSpan(11, 5).ToArray(),
                Is.All.EqualTo((byte)0xcc));
        });

        source.Close();
        destination.Close();
    }

    [Test]
    public void OrderedDitherIsDeterministicAndLeavesExactEndpointsUntouched()
    {
        AvifPlane source = AvifPlane.Create(
            4, 4, 16, AvifPlaneKind.Y);
        AvifPlane first = AvifPlane.Create(
            4, 4, 8, AvifPlaneKind.Y);
        AvifPlane second = AvifPlane.Create(
            4, 4, 8, AvifPlaneKind.Y);
        WriteSamples(source, new int[]
        {
            0, 32768, 32768, 65535,
            32768, 32768, 32768, 32768,
            32768, 32768, 32768, 32768,
            65535, 32768, 32768, 0
        });

        AvifResultCode firstResult = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            first,
            AvifRange.Full,
            AvifDitherMode.Ordered);
        AvifResultCode secondResult = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            second,
            AvifRange.Full,
            AvifDitherMode.Ordered);
        int[] firstSamples = ReadSamples(first);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(firstSamples, Is.EqualTo(ReadSamples(second)));
            Assert.That(firstSamples[0], Is.Zero);
            Assert.That(firstSamples[3], Is.EqualTo(255));
            Assert.That(firstSamples[12], Is.EqualTo(255));
            Assert.That(firstSamples[15], Is.Zero);
            Assert.That(firstSamples, Has.Some.EqualTo(127));
            Assert.That(firstSamples, Has.Some.EqualTo(128));
        });

        source.Close();
        first.Close();
        second.Close();
    }

    [Test]
    public void ErrorDiffusionIsDeterministicAndMaintainsTheMean()
    {
        AvifPlane source = AvifPlane.Create(
            16, 4, 16, AvifPlaneKind.Y);
        AvifPlane first = AvifPlane.Create(
            16, 4, 8, AvifPlaneKind.Y);
        AvifPlane second = AvifPlane.Create(
            16, 4, 8, AvifPlaneKind.Y);
        WriteSamples(source, Enumerable.Repeat(32768, 64).ToArray());

        AvifResultCode firstResult = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            first,
            AvifRange.Full,
            AvifDitherMode.ErrorDiffusion);
        AvifResultCode secondResult = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            second,
            AvifRange.Full,
            AvifDitherMode.ErrorDiffusion);
        int[] values = ReadSamples(first);

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(secondResult, Is.EqualTo(AvifResultCode.Success));
            Assert.That(values, Is.EqualTo(ReadSamples(second)));
            Assert.That(values, Has.Some.EqualTo(127));
            Assert.That(values, Has.Some.EqualTo(128));
            Assert.That(values.Sum(), Is.InRange(8159, 8161));
        });

        source.Close();
        first.Close();
        second.Close();
    }

    [Test]
    public void EveryDitherModePreservesEndpointsAndOnlyChangesReducedDepth()
    {
        AvifDitherMode[] modes = new AvifDitherMode[]
        {
            AvifDitherMode.None,
            AvifDitherMode.Ordered,
            AvifDitherMode.ErrorDiffusion
        };
        foreach (AvifDitherMode mode in modes)
        {
            AvifPlane highDepth = AvifPlane.Create(
                4, 1, 16, AvifPlaneKind.Y);
            AvifPlane reduced = AvifPlane.Create(
                4, 1, 8, AvifPlaneKind.Y);
            AvifPlane sameDepth = AvifPlane.Create(
                4, 1, 8, AvifPlaneKind.Y);
            AvifPlane limited = AvifPlane.Create(
                4, 1, 8, AvifPlaneKind.Y);
            WriteSamples(highDepth, new int[] { 0, 32768, 32768, 65535 });
            WriteSamples(sameDepth, new int[] { 0, 64, 128, 255 });

            AvifResultCode reducedResult = BitDepthConverter.ConvertPlane(
                highDepth,
                AvifRange.Full,
                reduced,
                AvifRange.Full,
                mode);
            AvifResultCode rangeResult = BitDepthConverter.ConvertPlane(
                sameDepth,
                AvifRange.Full,
                limited,
                AvifRange.Limited,
                mode);
            int[] reducedValues = ReadSamples(reduced);

            Assert.Multiple(() =>
            {
                Assert.That(reducedResult, Is.EqualTo(AvifResultCode.Success));
                Assert.That(rangeResult, Is.EqualTo(AvifResultCode.Success));
                Assert.That(reducedValues[0], Is.Zero);
                Assert.That(reducedValues[3], Is.EqualTo(255));
                Assert.That(ReadSamples(limited), Is.EqualTo(
                    new int[] { 16, 71, 126, 235 }));
            });

            highDepth.Close();
            reduced.Close();
            sameDepth.Close();
            limited.Close();
        }
    }

    [Test]
    public void ConversionSnapshotsOverlappingViewsBeforeWriting()
    {
        AvifPlane owner = AvifPlane.Create(
            6, 1, 8, AvifPlaneKind.Y);
        WriteSamples(owner, new int[] { 0, 32, 64, 128, 192, 255 });
        AvifPlane source = owner.CreateView(0, 0, 5, 1);
        AvifPlane destination = owner.CreateView(1, 0, 5, 1);

        AvifResultCode result = BitDepthConverter.ConvertPlane(
            source,
            AvifRange.Full,
            destination,
            AvifRange.Limited,
            AvifDitherMode.None);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(AvifResultCode.Success));
            Assert.That(ReadSamples(owner), Is.EqualTo(
                new int[] { 0, 16, 43, 71, 126, 181 }));
        });

        source.Close();
        destination.Close();
        owner.Close();
    }

    [Test]
    public void PlaneConversionRejectsInvalidLayoutsModesAndAlphaRanges()
    {
        AvifPlane source = AvifPlane.Create(
            2, 2, 10, AvifPlaneKind.Y);
        AvifPlane wrongSize = AvifPlane.Create(
            3, 2, 8, AvifPlaneKind.Y);
        AvifPlane alpha = AvifPlane.Create(
            2, 2, 8, AvifPlaneKind.Alpha);
        AvifPlane alphaDestination = AvifPlane.Create(
            2, 2, 8, AvifPlaneKind.Alpha);
        AvifPlane closed = AvifPlane.Create(
            2, 2, 8, AvifPlaneKind.Y);
        closed.Close();

        Assert.Multiple(() =>
        {
            Assert.That(
                BitDepthConverter.ConvertPlane(
                    source,
                    AvifRange.Full,
                    wrongSize,
                    AvifRange.Full,
                    AvifDitherMode.None),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(
                BitDepthConverter.ConvertPlane(
                    source,
                    AvifRange.Full,
                    closed,
                    AvifRange.Full,
                    AvifDitherMode.None),
                Is.EqualTo(AvifResultCode.Closed));
            Assert.That(
                BitDepthConverter.ConvertPlane(
                    source,
                    AvifRange.Full,
                    wrongSize,
                    AvifRange.Full,
                    (AvifDitherMode)99),
                Is.EqualTo(AvifResultCode.InvalidArgument));
            Assert.That(
                BitDepthConverter.ConvertPlane(
                    alpha,
                    AvifRange.Limited,
                    alphaDestination,
                    AvifRange.Full,
                    AvifDitherMode.None),
                Is.EqualTo(AvifResultCode.InvalidArgument));
        });

        source.Close();
        wrongSize.Close();
        alpha.Close();
        alphaDestination.Close();
    }

    static AvifImage CreateImage(
        int width,
        int height,
        int bitDepth,
        AvifRange range,
        AvifAlphaMode alphaMode)
    {
        return AvifImage.Create(
            width,
            height,
            bitDepth,
            AvifChromaFormat.Yuv420,
            range,
            AvifChromaSamplePosition.Vertical,
            alphaMode,
            1,
            new AvifSecurityLimits());
    }

    static void FillAlternating(
        AvifPlane? plane,
        int minimum,
        int maximum)
    {
        int[] values = new int[(plane ?? throw new InvalidOperationException("Invalid internal state.")).GetWidth() * plane.GetHeight()];
        for (int index = 0; index < values.Length; index++)
        {
            values[index] = (index & 1) == 0 ? minimum : maximum;
        }
        WriteSamples(plane, values);
    }

    static void AssertAlternatingEndpoints(
        AvifPlane? plane,
        int minimum,
        int maximum)
    {
        int[] values = ReadSamples(plane);
        for (int index = 0; index < values.Length; index++)
        {
            Assert.That(
                values[index],
                Is.EqualTo((index & 1) == 0 ? minimum : maximum));
        }
    }

    static void WriteSamples(AvifPlane plane, int[] values)
    {
        int index = 0;
        for (int y = 0; y < plane.GetHeight(); y++)
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
