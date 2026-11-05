using raw;

namespace raw.Tests;

public class LosslessJpegTests
{
    [Test]
    public void PredictorsOneThroughSevenReconstructIndependentDifferenceVectors()
    {
        ushort[] expected = new ushort[] { 10, 12, 15, 11, 14, 18, 13, 17, 22 };
        int[][] differences = new int[][]
        {
            new int[] { -118, 2, 3, 1, 3, 4, 2, 4, 5 },
            new int[] { -118, 2, 3, 1, 2, 3, 2, 3, 4 },
            new int[] { -118, 2, 3, 1, 4, 6, 2, 6, 8 },
            new int[] { -118, 2, 3, 1, 1, 1, 2, 1, 1 },
            new int[] { -118, 2, 3, 1, 2, 3, 2, 3, 3 },
            new int[] { -118, 2, 3, 1, 2, 2, 2, 2, 3 },
            new int[] { -118, 2, 3, 1, 3, 4, 2, 4, 5 }
        };

        for (int predictor = 1; predictor <= 7; predictor++)
        {
            byte[] jpeg = BuildSingleComponent(8, 3, 3, predictor, 0, differences[predictor - 1]);
            LosslessJpegImage image = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(image.Width, Is.EqualTo(3), $"predictor {predictor}");
                Assert.That(image.Height, Is.EqualTo(3), $"predictor {predictor}");
                Assert.That(image.Precision, Is.EqualTo(8), $"predictor {predictor}");
                Assert.That(image.Planes.Single().Predictor, Is.EqualTo(predictor));
                Assert.That(image.Planes.Single().Samples, Is.EqualTo(expected), $"predictor {predictor}");
            });
        }
    }

    [Test]
    public void PrecisionsTwoThroughSixteenAndPointTransformAreDecoded()
    {
        LosslessJpegImage twoBit = LosslessJpegCodec.Decode(BuildSingleComponent(2, 2, 2, 1, 0, new int[] { -2, 3, 2, -1 }), new RawLimits(), CancellationToken.None);
        LosslessJpegImage eightBit = LosslessJpegCodec.Decode(BuildSingleComponent(8, 2, 1, 1, 0, new int[] { -128, 255 }), new RawLimits(), CancellationToken.None);
        LosslessJpegImage transformed = LosslessJpegCodec.Decode(BuildSingleComponent(12, 2, 2, 1, 3, new int[] { -256, 1, 255, 256 }), new RawLimits(), CancellationToken.None);
        LosslessJpegImage sixteenBit = LosslessJpegCodec.Decode(BuildSingleComponent(16, 2, 1, 1, 0, new int[] { 32768, -1 }), new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(twoBit.Planes.Single().Samples, Is.EqualTo(new ushort[] { 0, 3, 2, 1 }));
            Assert.That(eightBit.Planes.Single().Samples, Is.EqualTo(new ushort[] { 0, 255 }));
            Assert.That(transformed.Planes.Single().Samples, Is.EqualTo(new ushort[] { 0, 8, 2040, 4088 }));
            Assert.That(transformed.Planes.Single().PointTransform, Is.EqualTo(3));
            Assert.That(sixteenBit.Planes.Single().Samples, Is.EqualTo(new ushort[] { 0, 65535 }));
        });
    }

    [Test]
    public void InterleavedMultiComponentScanUsesIndependentPredictionState()
    {
        byte[] frame = Frame(0xc3, 8, 2, 2, new byte[] { 1, 2 }, new byte[] { 0x11, 0x11 }, new byte[] { 0, 0 });
        byte[] scan = Scan(new byte[] { 1, 2 }, new byte[] { 0, 0 }, 4, 0, 0);
        int[] differences = new int[] { -118, -28, 2, -10, 1, 10, 1, 5 };
        byte[] jpeg = Join(Soi(), HuffmanMarker(), frame, scan, EncodeDifferences(differences), Eoi());

        LosslessJpegImage image = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(image.Planes.Count, Is.EqualTo(2));
            Assert.That(image.GetPlane(1).Samples, Is.EqualTo(new ushort[] { 10, 12, 11, 14 }));
            Assert.That(image.GetPlane(2).Samples, Is.EqualTo(new ushort[] { 100, 90, 110, 105 }));
            Assert.That(image.GetPlane(1).ReducedSamples, Is.Null);
            Assert.That(image.GetPlane(2).ReducedSamples, Is.Null);
        });
    }

    [Test]
    public void SeparateComponentScansCoverOneFrame()
    {
        byte[] frame = Frame(0xc3, 8, 2, 2, new byte[] { 1, 2 }, new byte[] { 0x11, 0x11 }, new byte[] { 0, 0 });
        byte[] firstScan = Scan(new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0);
        byte[] secondScan = Scan(new byte[] { 2 }, new byte[] { 0 }, 1, 0, 0);
        byte[] jpeg = Join(
            Soi(), HuffmanMarker(), frame,
            firstScan, EncodeDifferences(new int[] { -118, 2, 1, 3 }),
            secondScan, EncodeDifferences(new int[] { -28, -10, 10, -5 }),
            Eoi());

        LosslessJpegImage image = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(image.GetPlane(1).Samples, Is.EqualTo(new ushort[] { 10, 12, 11, 14 }));
            Assert.That(image.GetPlane(2).Samples, Is.EqualTo(new ushort[] { 100, 90, 110, 105 }));
        });
    }

    [Test]
    public void InterleavedSamplingFactorsProduceComponentSizedPlanesAndDiscardPadding()
    {
        byte[] frame = Frame(0xc3, 8, 3, 1, new byte[] { 1, 2 }, new byte[] { 0x21, 0x11 }, new byte[] { 0, 0 });
        byte[] scan = Scan(new byte[] { 1, 2 }, new byte[] { 0, 0 }, 1, 0, 0);
        int[] differences = new int[] { -118, 1, -108, 1, 1, 1 };
        byte[] jpeg = Join(Soi(), HuffmanMarker(), frame, scan, EncodeDifferences(differences), Eoi());

        LosslessJpegImage image = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(image.GetPlane(1).Width, Is.EqualTo(3));
            Assert.That(image.GetPlane(1).Height, Is.EqualTo(1));
            Assert.That(image.GetPlane(1).Samples, Is.EqualTo(new ushort[] { 10, 11, 12 }));
            Assert.That(image.GetPlane(2).Width, Is.EqualTo(2));
            Assert.That(image.GetPlane(2).Height, Is.EqualTo(1));
            Assert.That(image.GetPlane(2).Samples, Is.EqualTo(new ushort[] { 20, 21 }));
        });
    }

    [Test]
    public void RestartIntervalsResetPredictionAndMatchMcuBoundaries()
    {
        byte[] frame = Frame(0xc3, 8, 5, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 });
        byte[] dri = Segment(0xdd, new byte[] { 0, 2 });
        byte[] scan = Scan(new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0);
        byte[] entropy = Join(
            EncodeDifferences(new int[] { -28, 1 }), new byte[] { 0xff, 0xd0 },
            EncodeDifferences(new int[] { -26, -52 }), new byte[] { 0xff, 0xd1 },
            EncodeDifferences(new int[] { -77 }));
        byte[] jpeg = Join(Soi(), HuffmanMarker(), frame, dri, scan, entropy, Eoi());

        LosslessJpegImage image = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.That(image.Planes.Single().Samples, Is.EqualTo(new ushort[] { 100, 101, 102, 50, 51 }));
    }

    [Test]
    public void RestartCountAndSegmentMcuCountsAreValidated()
    {
        byte[] frame = Frame(0xc3, 8, 5, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 });
        byte[] dri = Segment(0xdd, new byte[] { 0, 2 });
        byte[] scan = Scan(new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0);
        byte[] missingMarkers = Join(Soi(), HuffmanMarker(), frame, dri, scan, EncodeDifferences(new int[] { -28, 1, 1, 1, 1 }), Eoi());
        byte[] earlyMarkerEntropy = Join(
            EncodeDifferences(new int[] { -28 }), new byte[] { 0xff, 0xd0 },
            EncodeDifferences(new int[] { -26, 1 }), new byte[] { 0xff, 0xd1 },
            EncodeDifferences(new int[] { -77, 1 }));
        byte[] earlyMarker = Join(Soi(), HuffmanMarker(), frame, dri, scan, earlyMarkerEntropy, Eoi());

        RawFormatException? count = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(missingMarkers, new RawLimits(), CancellationToken.None));
        RawFormatException? boundary = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(earlyMarker, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((count ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((boundary ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void UnsupportedFrameAndInvalidLosslessScanParametersAreRejected()
    {
        byte[] entropy = EncodeDifferences(new int[] { -127 });
        RawUnsupportedException? process = Assert.Throws<RawUnsupportedException>(() => LosslessJpegCodec.Decode(BuildCustom(0xc0, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0, entropy), new RawLimits(), CancellationToken.None));
        RawFormatException? predictor = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 0, 0, 0, entropy));
        RawFormatException? spectralEnd = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 1, 0, entropy));
        RawFormatException? approximationHigh = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0x10, entropy));
        RawFormatException? pointTransform = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 8, entropy));
        RawFormatException? acTable = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 1 }, 1, 0, 0, entropy));
        RawFormatException? quantization = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 1 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0, entropy));
        RawFormatException? undefinedTable = DecodeFailure(BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0x10 }, 1, 0, 0, entropy));

        Assert.Multiple(() =>
        {
            Assert.That((process ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
            Assert.That((predictor ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((spectralEnd ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((approximationHigh ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((pointTransform ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((acTable ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((quantization ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((undefinedTable ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void ComponentCoverageAndDeferredHeightAreRejected()
    {
        byte[] frame = Frame(0xc3, 8, 1, 1, new byte[] { 1, 2 }, new byte[] { 0x11, 0x11 }, new byte[] { 0, 0 });
        byte[] scanOne = Scan(new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0);
        byte[] missing = Join(Soi(), HuffmanMarker(), frame, scanOne, EncodeDifferences(new int[] { -127 }), Eoi());
        byte[] duplicate = Join(Soi(), HuffmanMarker(), frame, scanOne, EncodeDifferences(new int[] { -127 }), scanOne, EncodeDifferences(new int[] { -127 }), Eoi());
        byte[] deferred = BuildCustom(0xc3, 8, 1, 0, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0, EncodeDifferences(new int[] { -127 }));

        RawFormatException? absent = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(missing, new RawLimits(), CancellationToken.None));
        RawFormatException? repeated = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(duplicate, new RawLimits(), CancellationToken.None));
        RawUnsupportedException? height = Assert.Throws<RawUnsupportedException>(() => LosslessJpegCodec.Decode(deferred, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((absent ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((repeated ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((height ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.UnsupportedCodec));
        });
    }

    [Test]
    public void CorruptDifferencesCategoriesEntropyAndPaddingAreRejected()
    {
        byte[] overflow = BuildSingleComponent(8, 1, 1, 1, 0, new int[] { 128 });
        byte[] excess = BuildSingleComponent(8, 1, 1, 1, 0, new int[] { 0, 0 });
        byte[] badPaddingEntropy = EncodeDifferences(new int[] { 0 });
        badPaddingEntropy[badPaddingEntropy.Length - 1] &= 0xfe;
        byte[] badPadding = BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 0, badPaddingEntropy);
        byte[] category = BuildCustom(0xc3, 8, 1, 1, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, 1, 0, 7, EncodeDifferences(new int[] { 2 }));

        RawFormatException? sample = DecodeFailure(overflow);
        RawFormatException? extra = DecodeFailure(excess);
        RawFormatException? padding = DecodeFailure(badPadding);
        RawFormatException? categoryFailure = DecodeFailure(category);

        Assert.Multiple(() =>
        {
            Assert.That((sample ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((extra ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((padding ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((categoryFailure ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void DecodedAllocationFileAndCancellationLimitsAreEnforced()
    {
        int[] differences = new int[100];
        byte[] jpeg = BuildSingleComponent(8, 10, 10, 1, 0, differences);
        RawLimits decodedLimits = new RawLimits();
        decodedLimits.MaxDecodedBytes = 599;
        decodedLimits.MaxAllocationBytes = 599;
        RawLimits allocationLimits = new RawLimits();
        allocationLimits.MaxDecodedBytes = 1000;
        allocationLimits.MaxAllocationBytes = 399;
        RawLimits fileLimits = new RawLimits();
        fileLimits.MaxFileSizeBytes = jpeg.Length - 1;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        RawLimitException? decoded = Assert.Throws<RawLimitException>(() => LosslessJpegCodec.Decode(jpeg, decodedLimits, CancellationToken.None));
        RawLimitException? allocation = Assert.Throws<RawLimitException>(() => LosslessJpegCodec.Decode(jpeg, allocationLimits, CancellationToken.None));
        RawLimitException? file = Assert.Throws<RawLimitException>(() => LosslessJpegCodec.Decode(jpeg, fileLimits, CancellationToken.None));
        RawFormatException? missing = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode((JpegDocument?)null, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((decoded ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((allocation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((file ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((missing ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidArgument));
            Assert.Throws<OperationCanceledException>(() => LosslessJpegCodec.Decode(jpeg, new RawLimits(), cancellation.Token));
        });
    }

    private static RawFormatException? DecodeFailure(byte[] jpeg)
    {
        return Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None));
    }

    private static byte[] BuildSingleComponent(int precision, int width, int height, int predictor, int pointTransform, int[] differences)
    {
        return BuildCustom(0xc3, precision, width, height, new byte[] { 1 }, new byte[] { 0x11 }, new byte[] { 0 }, new byte[] { 1 }, new byte[] { 0 }, predictor, 0, pointTransform, EncodeDifferences(differences));
    }

    private static byte[] BuildCustom(byte frameMarker, int precision, int width, int height, byte[] frameIdentifiers, byte[] sampling, byte[] quantization, byte[] scanIdentifiers, byte[] tableSelectors, int predictor, int spectralEnd, int approximation, byte[] entropy)
    {
        return Join(
            Soi(),
            HuffmanMarker(),
            Frame(frameMarker, precision, width, height, frameIdentifiers, sampling, quantization),
            Scan(scanIdentifiers, tableSelectors, predictor, spectralEnd, approximation),
            entropy,
            Eoi());
    }

    private static byte[] Frame(byte marker, int precision, int width, int height, byte[] identifiers, byte[] sampling, byte[] quantization)
    {
        List<byte> payload = new List<byte>();
        payload.Add((byte)precision);
        payload.Add((byte)(height >> 8));
        payload.Add((byte)height);
        payload.Add((byte)(width >> 8));
        payload.Add((byte)width);
        payload.Add((byte)identifiers.Length);
        for (int index = 0; index < identifiers.Length; index++)
        {
            payload.Add(identifiers[index]);
            payload.Add(sampling[index]);
            payload.Add(quantization[index]);
        }
        return Segment(marker, payload.ToArray());
    }

    private static byte[] Scan(byte[] identifiers, byte[] tableSelectors, int predictor, int spectralEnd, int approximation)
    {
        List<byte> payload = new List<byte>();
        payload.Add((byte)identifiers.Length);
        for (int index = 0; index < identifiers.Length; index++)
        {
            payload.Add(identifiers[index]);
            payload.Add(tableSelectors[index]);
        }
        payload.Add((byte)predictor);
        payload.Add((byte)spectralEnd);
        payload.Add((byte)approximation);
        return Segment(0xda, payload.ToArray());
    }

    private static byte[] HuffmanMarker()
    {
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        return Segment(0xc4, Join(new byte[] { 0 }, counts, symbols));
    }

    private static byte[] EncodeDifferences(int[] differences)
    {
        List<int> bits = new List<int>();
        foreach (int difference in differences)
        {
            int magnitude = difference < 0 ? -difference : difference;
            int category = 0;
            if (difference == 32768)
            {
                category = 16;
            }
            while (magnitude != 0 && category != 16)
            {
                category++;
                magnitude >>= 1;
            }
            AppendBits(bits, category, 5);
            if (category != 0 && category != 16)
            {
                int amplitude = difference;
                if (difference < 0)
                {
                    amplitude += (1 << category) - 1;
                }
                AppendBits(bits, amplitude, category);
            }
        }
        while (bits.Count % 8 != 0)
        {
            bits.Add(1);
        }

        List<byte> encoded = new List<byte>();
        for (int offset = 0; offset < bits.Count; offset += 8)
        {
            byte value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (byte)((value << 1) | bits[offset + bit]);
            }
            encoded.Add(value);
            if (value == 0xff)
            {
                encoded.Add(0);
            }
        }
        return encoded.ToArray();
    }

    private static void AppendBits(List<int> bits, int value, int count)
    {
        for (int bit = count - 1; bit >= 0; bit--)
        {
            bits.Add((value >> bit) & 1);
        }
    }

    private static byte[] Segment(byte marker, byte[] payload)
    {
        int length = payload.Length + 2;
        return Join(new byte[] { 0xff, marker, (byte)(length >> 8), (byte)length }, payload);
    }

    private static byte[] Soi()
    {
        return new byte[] { 0xff, 0xd8 };
    }

    private static byte[] Eoi()
    {
        return new byte[] { 0xff, 0xd9 };
    }

    private static byte[] Join(params byte[][] arrays)
    {
        int length = arrays.Sum(array => array.Length);
        byte[] result = new byte[length];
        int offset = 0;
        foreach (byte[] array in arrays)
        {
            array.CopyTo(result, offset);
            offset += array.Length;
        }
        return result;
    }
}
