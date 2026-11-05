using raw;

namespace raw.Tests;

public class LosslessJpegEncoderTests
{
    [Test]
    public void FixedTableSingleSampleMatchesExactDeterministicStream()
    {
        LosslessJpegImage image = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 128 }));
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 0, 0, true, false);
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        byte[] expected = Join(
            new byte[] { 0xff, 0xd8 },
            Segment(0xc4, Join(new byte[] { 0 }, counts, symbols)),
            Segment(0xc3, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 }),
            Segment(0xda, new byte[] { 1, 1, 0, 1, 0, 0 }),
            new byte[] { 0x07 },
            new byte[] { 0xff, 0xd9 });

        byte[] encoded = LosslessJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);

        Assert.That(encoded, Is.EqualTo(expected));
        Assert.That(LosslessJpegCodec.Decode(encoded, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(new ushort[] { 128 }));
    }

    [Test]
    public void EveryPredictorRoundTripsAndEncodingIsDeterministic()
    {
        ushort[] samples = new ushort[] { 10, 12, 15, 11, 14, 18, 13, 17, 22 };
        LosslessJpegImage image = Image(3, 3, 8, Plane(1, 3, 3, 1, 1, samples));

        for (int predictor = 1; predictor <= 7; predictor++)
        {
            LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(predictor, 0, 0, true, false);
            byte[] first = LosslessJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
            byte[] second = LosslessJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
            LosslessJpegImage decoded = LosslessJpegCodec.Decode(first, new RawLimits(), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(second, Is.EqualTo(first), $"predictor {predictor}");
                Assert.That(decoded.Planes.Single().Samples, Is.EqualTo(samples), $"predictor {predictor}");
                Assert.That(decoded.Planes.Single().Predictor, Is.EqualTo(predictor));
            });
        }
    }

    [Test]
    public void OptimizedTableIsCanonicalSmallerAndDeterministic()
    {
        ushort[] samples = Enumerable.Repeat((ushort)128, 256).ToArray();
        LosslessJpegImage image = Image(16, 16, 8, Plane(1, 16, 16, 1, 1, samples));
        LosslessJpegEncodeParameters fixedParameters = new LosslessJpegEncodeParameters(1, 0, 0, true, false);
        LosslessJpegEncodeParameters optimizedParameters = new LosslessJpegEncodeParameters(1, 0, 0, true, true);

        byte[] fixedStream = LosslessJpegEncoder.Encode(image, fixedParameters, new RawLimits(), CancellationToken.None);
        byte[] optimized = LosslessJpegEncoder.Encode(image, optimizedParameters, new RawLimits(), CancellationToken.None);
        byte[] repeated = LosslessJpegEncoder.Encode(image, optimizedParameters, new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(optimized, new RawLimits(), CancellationToken.None);
        JpegHuffmanTableDefinition table = document.HuffmanTables.Single();

        Assert.Multiple(() =>
        {
            Assert.That(repeated, Is.EqualTo(optimized));
            Assert.That(optimized.Length, Is.LessThan(fixedStream.Length));
            Assert.That(table.Symbols, Is.EqualTo(new byte[] { 0 }));
            Assert.That((table.CodeLengthCounts ?? throw new System.InvalidOperationException("A required value is unavailable."))[0], Is.EqualTo(1));
            Assert.That((table.Table ?? throw new System.InvalidOperationException("A required value is unavailable.")).IsComplete(), Is.False);
            Assert.That(LosslessJpegCodec.Decode(optimized, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(samples));
        });
    }

    [Test]
    public void OptimizedLengthLimitingRetainsEveryObservedCategory()
    {
        long[] frequencies = new long[17];
        frequencies[0] = 1;
        frequencies[1] = 1;
        for (int index = 2; index < frequencies.Length; index++)
        {
            frequencies[index] = frequencies[index - 1] + frequencies[index - 2];
        }

        LosslessJpegHuffmanDefinition first = LosslessJpegEncoder.BuildOptimizedHuffmanTable(frequencies);
        LosslessJpegHuffmanDefinition second = LosslessJpegEncoder.BuildOptimizedHuffmanTable(frequencies);

        Assert.Multiple(() =>
        {
            Assert.That(((first.Symbols ?? throw new System.InvalidOperationException("A required value is unavailable.")).OrderBy(value => value) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.EqualTo(Enumerable.Range(0, 17).Select(value => (byte)value)));
            Assert.That((first.Table ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetMaxCodeLength(), Is.LessThanOrEqualTo(16));
            Assert.That(first.Table.IsComplete(), Is.False);
            Assert.That(second.CodeLengthCounts, Is.EqualTo(first.CodeLengthCounts));
            Assert.That(second.Symbols, Is.EqualTo(first.Symbols));
        });
        for (byte category = 0; category <= 16; category++)
        {
            Assert.That((first.Table ?? throw new System.InvalidOperationException("A required value is unavailable.")).GetEncodeCodeLength(category), Is.GreaterThan(0));
        }
    }

    [Test]
    public void PrecisionPointTransformAndCategorySixteenRoundTrip()
    {
        LosslessJpegImage twoBit = Image(2, 2, 2, Plane(1, 2, 2, 1, 1, new ushort[] { 0, 3, 2, 1 }));
        LosslessJpegImage transformed = Image(2, 2, 12, Plane(1, 2, 2, 1, 1, new ushort[] { 0, 8, 2040, 4088 }));
        LosslessJpegImage sixteenBit = Image(2, 1, 16, Plane(1, 2, 1, 1, 1, new ushort[] { 0, 65535 }));

        byte[] encodedTwo = LosslessJpegEncoder.Encode(twoBit, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        byte[] encodedTransform = LosslessJpegEncoder.Encode(transformed, new LosslessJpegEncodeParameters(1, 3, 0, true, true), new RawLimits(), CancellationToken.None);
        byte[] encodedSixteen = LosslessJpegEncoder.Encode(sixteenBit, new LosslessJpegEncodeParameters(1, 0, 0, true, false), new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(LosslessJpegCodec.Decode(encodedTwo, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(twoBit.Planes.Single().Samples));
            Assert.That(LosslessJpegCodec.Decode(encodedTransform, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(transformed.Planes.Single().Samples));
            Assert.That(LosslessJpegCodec.Decode(encodedSixteen, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(sixteenBit.Planes.Single().Samples));
        });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void MultiComponentSamplingRoundTripsInterleavedAndSeparateScans(bool interleaved)
    {
        LosslessJpegPlane full = Plane(1, 3, 2, 2, 1, new ushort[] { 10, 11, 12, 13, 14, 15 });
        LosslessJpegPlane subsampled = Plane(2, 2, 2, 1, 1, new ushort[] { 100, 90, 110, 105 });
        LosslessJpegImage image = Image(3, 2, 8, full, subsampled);
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(4, 0, 0, interleaved, true);

        byte[] encoded = LosslessJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        LosslessJpegImage decoded = LosslessJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.Scans.Count, Is.EqualTo(interleaved ? 1 : 2));
            Assert.That(decoded.GetPlane(1).Samples, Is.EqualTo(full.Samples));
            Assert.That(decoded.GetPlane(2).Samples, Is.EqualTo(subsampled.Samples));
            Assert.That(decoded.GetPlane(1).Width, Is.EqualTo(3));
            Assert.That(decoded.GetPlane(2).Width, Is.EqualTo(2));
        });
    }

    [Test]
    public void RestartMarkersResetPredictionAndCycleModuloEight()
    {
        ushort[] samples = Enumerable.Range(0, 20).Select(value => (ushort)(value * 17)).ToArray();
        LosslessJpegImage image = Image(20, 1, 12, Plane(1, 20, 1, 1, 1, samples));
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 0, 2, true, true);

        byte[] encoded = LosslessJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        LosslessJpegImage decoded = LosslessJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.RestartIntervals.Single().Interval, Is.EqualTo(2));
            Assert.That(document.Scans.Single().RestartMarkers.Select(marker => marker.Code), Is.EqualTo(new byte[] { 0xd0, 0xd1, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd0 }));
            Assert.That(decoded.Planes.Single().Samples, Is.EqualTo(samples));
        });
    }

    [Test]
    public void EntropyBytesEqualToMarkerPrefixAreStuffed()
    {
        Random random = new Random(24024);
        ushort[] samples = new ushort[4096];
        for (int index = 0; index < samples.Length; index++)
        {
            samples[index] = (ushort)random.Next(0, 4096);
        }
        LosslessJpegImage image = Image(4096, 1, 12, Plane(1, 4096, 1, 1, 1, samples));

        byte[] encoded = LosslessJpegEncoder.Encode(image, new LosslessJpegEncodeParameters(1, 0, 0, true, false), new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        JpegEntropySegment segment = document.Scans.Single().EntropySegments.Single();
        byte[] rawEntropy = document.Source.Slice(segment.Offset, segment.EncodedLength).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ContainsStuffedByte(rawEntropy), Is.True);
            Assert.That(LosslessJpegCodec.Decode(document, new RawLimits(), CancellationToken.None).Planes.Single().Samples, Is.EqualTo(samples));
        });
    }

    [Test]
    public void CallerParametersGeometryAndSamplesAreValidated()
    {
        LosslessJpegImage valid = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 1 }));
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 0, 0, true, false);
        LosslessJpegImage badPrecision = Image(1, 1, 1, Plane(1, 1, 1, 1, 1, new ushort[] { 1 }));
        LosslessJpegImage badGeometry = Image(2, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 1 }));
        LosslessJpegImage duplicate = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 1 }), Plane(1, 1, 1, 1, 1, new ushort[] { 1 }));
        LosslessJpegImage badSampling = Image(1, 1, 8, Plane(1, 1, 1, 0, 1, new ushort[] { 1 }));
        LosslessJpegImage badLength = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, Array.Empty<ushort>()));
        LosslessJpegImage badSample = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 256 }));
        LosslessJpegImage lostBits = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 3 }));
        LosslessJpegImage fiveComponents = Image(1, 1, 8,
            Plane(1, 1, 1, 1, 1, new ushort[] { 1 }), Plane(2, 1, 1, 1, 1, new ushort[] { 1 }),
            Plane(3, 1, 1, 1, 1, new ushort[] { 1 }), Plane(4, 1, 1, 1, 1, new ushort[] { 1 }),
            Plane(5, 1, 1, 1, 1, new ushort[] { 1 }));

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(null, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(valid, null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(valid, new LosslessJpegEncodeParameters(0, 0, 0, true, false), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(valid, new LosslessJpegEncodeParameters(1, 8, 0, true, false), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(valid, new LosslessJpegEncodeParameters(1, 0, 65536, true, false), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(badPrecision, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(badGeometry, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(duplicate, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(badSampling, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(badLength, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(badSample, parameters, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(lostBits, new LosslessJpegEncodeParameters(1, 1, 0, true, false), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => LosslessJpegEncoder.Encode(fiveComponents, parameters, new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void OutputDecodedAllocationAndCancellationLimitsAreEnforced()
    {
        LosslessJpegImage small = Image(1, 1, 8, Plane(1, 1, 1, 1, 1, new ushort[] { 1 }));
        LosslessJpegImage larger = Image(10, 10, 8, Plane(1, 10, 10, 1, 1, Enumerable.Repeat((ushort)128, 100).ToArray()));
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 0, 0, true, false);
        RawLimits outputAllocation = new RawLimits();
        outputAllocation.MaxDecodedBytes = 1000;
        outputAllocation.MaxAllocationBytes = 50;
        RawLimits outputFile = new RawLimits();
        outputFile.MaxFileSizeBytes = 50;
        RawLimits decoded = new RawLimits();
        decoded.MaxDecodedBytes = 399;
        decoded.MaxAllocationBytes = 399;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        RawLimitException? allocation = Assert.Throws<RawLimitException>(() => LosslessJpegEncoder.Encode(small, parameters, outputAllocation, CancellationToken.None));
        RawLimitException? file = Assert.Throws<RawLimitException>(() => LosslessJpegEncoder.Encode(small, parameters, outputFile, CancellationToken.None));
        RawLimitException? working = Assert.Throws<RawLimitException>(() => LosslessJpegEncoder.Encode(larger, parameters, decoded, CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((allocation ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((file ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.That((working ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.ResourceLimitExceeded));
            Assert.Throws<OperationCanceledException>(() => LosslessJpegEncoder.Encode(small, parameters, new RawLimits(), cancellation.Token));
        });
    }

    private static bool ContainsStuffedByte(byte[] bytes)
    {
        for (int index = 0; index + 1 < bytes.Length; index++)
        {
            if (bytes[index] == 0xff && bytes[index + 1] == 0x00)
            {
                return true;
            }
        }
        return false;
    }

    private static LosslessJpegImage Image(int width, int height, int precision, params LosslessJpegPlane[] planes)
    {
        LosslessJpegImage image = new LosslessJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = precision;
        image.Planes.AddRange(planes);
        return image;
    }

    private static LosslessJpegPlane Plane(byte identifier, int width, int height, int horizontalSampling, int verticalSampling, ushort[] samples)
    {
        LosslessJpegPlane plane = new LosslessJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = horizontalSampling;
        plane.VerticalSampling = verticalSampling;
        plane.Samples = samples;
        return plane;
    }

    private static byte[] Segment(byte marker, byte[] payload)
    {
        int length = payload.Length + 2;
        return Join(new byte[] { 0xff, marker, (byte)(length >> 8), (byte)length }, payload);
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
