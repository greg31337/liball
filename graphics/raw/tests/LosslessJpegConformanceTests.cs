using raw;

namespace raw.Tests;

public class LosslessJpegConformanceTests
{
    [Test]
    public void ItuCategorySixteenLiteralHasNoMagnitudeBitsAndUsesModuloArithmetic()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        counts[1] = 1;
        byte[] jpeg = Join(
            new byte[] { 0xff, 0xd8 },
            Segment(0xc4, Join(new byte[] { 0 }, counts, new byte[] { 0, 16 })),
            Segment(0xc3, new byte[] { 16, 0, 1, 0, 1, 1, 0x2a, 0x11, 0 }),
            Segment(0xda, new byte[] { 1, 0x2a, 0, 1, 0, 0 }),
            new byte[] { 0xbf },
            new byte[] { 0xff, 0xd9 });

        LosslessJpegImage decoded = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.Precision, Is.EqualTo(16));
            Assert.That(decoded.GetPlane(0x2a).Samples, Is.EqualTo(new ushort[] { 0 }));
            Assert.That(decoded.GetPlane(0x2a).Predictor, Is.EqualTo(1));
        });
    }

    [Test]
    public void ItuPredictorsFiveAndSixUseArithmeticRightShiftForNegativeOddTerms()
    {
        byte[] predictorFive = SinglePlaneFixture(8, 2, 2, 5, 0, new int[] { -118, -5, -2, 0 });
        byte[] predictorSix = SinglePlaneFixture(8, 2, 2, 6, 0, new int[] { -118, -2, -5, 0 });

        LosslessJpegImage five = LosslessJpegCodec.Decode(predictorFive, new RawLimits(), CancellationToken.None);
        LosslessJpegImage six = LosslessJpegCodec.Decode(predictorSix, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(five.Planes.Single().Samples, Is.EqualTo(new ushort[] { 10, 5, 8, 5 }));
            Assert.That(six.Planes.Single().Samples, Is.EqualTo(new ushort[] { 10, 8, 5, 5 }));
        });
    }

    [Test]
    public void ItuBoundaryPredictionIgnoresInteriorSelectionAtTopAndLeftEdges()
    {
        ushort[] expectedTopRow = new ushort[] { 100, 110, 120 };
        ushort[] expectedLeftColumn = new ushort[] { 100, 90, 95 };

        for (int predictor = 1; predictor <= 7; predictor++)
        {
            LosslessJpegImage topRow = LosslessJpegCodec.Decode(SinglePlaneFixture(8, 3, 1, predictor, 0, new int[] { -28, 10, 10 }), new RawLimits(), CancellationToken.None);
            LosslessJpegImage leftColumn = LosslessJpegCodec.Decode(SinglePlaneFixture(8, 1, 3, predictor, 0, new int[] { -28, -10, 5 }), new RawLimits(), CancellationToken.None);
            Assert.Multiple(() =>
            {
                Assert.That(topRow.Planes.Single().Samples, Is.EqualTo(expectedTopRow), $"top row, predictor {predictor}");
                Assert.That(leftColumn.Planes.Single().Samples, Is.EqualTo(expectedLeftColumn), $"left column, predictor {predictor}");
            });
        }
    }

    [Test]
    public void Cr2StyleFourteenBitComponentsUseIndependentDcTablesAndArbitraryIds()
    {
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        byte[] tables = Segment(0xc4, Join(new byte[] { 0 }, counts, symbols, new byte[] { 1 }, counts, symbols));
        byte[] frame = Segment(0xc3, new byte[]
        {
            14, 0, 2, 0, 2, 2,
            0x11, 0x11, 0,
            0x22, 0x11, 0
        });
        byte[] scan = Segment(0xda, new byte[] { 2, 0x11, 0x00, 0x22, 0x10, 1, 0, 0 });
        int[] differences = new int[] { -7192, -6192, 1, -1, 3, 1, 3, 3 };
        byte[] jpeg = Join(new byte[] { 0xff, 0xd8 }, tables, frame, scan, EncodeDifferences(differences), new byte[] { 0xff, 0xd9 });

        JpegDocument document = JpegParser.Parse(jpeg, new RawLimits(), CancellationToken.None);
        LosslessJpegImage decoded = LosslessJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.HuffmanTables.Select(table => table.Identifier), Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(document.Scans.Single().Components.Select(component => component.DcTable), Is.EqualTo(new int[] { 0, 1 }));
            Assert.That(decoded.GetPlane(0x11).Samples, Is.EqualTo(new ushort[] { 1000, 1001, 1003, 1006 }));
            Assert.That(decoded.GetPlane(0x22).Samples, Is.EqualTo(new ushort[] { 2000, 1999, 2001, 2004 }));
        });
    }

    [Test]
    public void TiffStyleSeparateScansRetainComponentSamplingGeometryAndOrder()
    {
        LosslessJpegImage source = Image(5, 3, 12,
            Plane(0x31, 5, 3, 2, 1, Enumerable.Range(0, 15).Select(value => (ushort)(value * 8)).ToArray()),
            Plane(0x7f, 3, 3, 1, 1, Enumerable.Range(0, 9).Select(value => (ushort)(2000 + value * 8)).ToArray()));
        LosslessJpegEncodeParameters parameters = new LosslessJpegEncodeParameters(1, 3, 0, false, true);

        byte[] encoded = LosslessJpegEncoder.Encode(source, parameters, new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        LosslessJpegImage decoded = LosslessJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.Scans.Count, Is.EqualTo(2));
            Assert.That(document.Frames.Single().Components.Select(component => component.Identifier), Is.EqualTo(new byte[] { 0x31, 0x7f }));
            Assert.That(decoded.Planes.Select(plane => plane.Identifier), Is.EqualTo(new byte[] { 0x31, 0x7f }));
            Assert.That(decoded.GetPlane(0x31).Samples, Is.EqualTo(source.GetPlane(0x31).Samples));
            Assert.That(decoded.GetPlane(0x7f).Samples, Is.EqualTo(source.GetPlane(0x7f).Samples));
            Assert.That(decoded.GetPlane(0x31).Width, Is.EqualTo(5));
            Assert.That(decoded.GetPlane(0x7f).Width, Is.EqualTo(3));
        });
    }

    [Test]
    public void RestartIntervalsResetFirstSamplePredictionForEveryComponent()
    {
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        byte[] jpeg = Join(
            new byte[] { 0xff, 0xd8 },
            Segment(0xc4, Join(new byte[] { 0 }, counts, symbols)),
            Segment(0xc3, new byte[] { 8, 0, 1, 0, 3, 2, 1, 0x11, 0, 2, 0x11, 0 }),
            Segment(0xdd, new byte[] { 0, 1 }),
            Segment(0xda, new byte[] { 2, 1, 0, 2, 0, 1, 0, 0 }),
            EncodeDifferences(new int[] { -128, -118 }), new byte[] { 0xff, 0xd0 },
            EncodeDifferences(new int[] { -127, -117 }), new byte[] { 0xff, 0xd1 },
            EncodeDifferences(new int[] { -126, -116 }),
            new byte[] { 0xff, 0xd9 });

        LosslessJpegImage decoded = LosslessJpegCodec.Decode(jpeg, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(decoded.GetPlane(1).Samples, Is.EqualTo(new ushort[] { 0, 1, 2 }));
            Assert.That(decoded.GetPlane(2).Samples, Is.EqualTo(new ushort[] { 10, 11, 12 }));
        });
    }

    [Test]
    public void StandardDerivedValidStreamMutationsAreRejected()
    {
        byte[] valid = SinglePlaneFixture(8, 2, 1, 1, 0, new int[] { -128, 1 });
        byte[] badSofLength = valid.ToArray();
        int sof = FindMarker(badSofLength, 0xc3);
        badSofLength[sof + 3]--;
        byte[] missingTable = valid.ToArray();
        int sos = FindMarker(missingTable, 0xda);
        missingTable[sos + 6] = 0x10;
        byte[] missingEoi = valid[..^2];
        byte[] restartWithoutDri = InsertBeforeEoi(valid, new byte[] { 0xff, 0xd0 });
        byte[] illegalCategory = IllegalCategoryFixture();

        RawFormatException? length = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(badSofLength, new RawLimits(), CancellationToken.None));
        RawFormatException? table = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(missingTable, new RawLimits(), CancellationToken.None));
        RawFormatException? end = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(missingEoi, new RawLimits(), CancellationToken.None));
        RawFormatException? restart = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(restartWithoutDri, new RawLimits(), CancellationToken.None));
        RawFormatException? category = Assert.Throws<RawFormatException>(() => LosslessJpegCodec.Decode(illegalCategory, new RawLimits(), CancellationToken.None));

        Assert.Multiple(() =>
        {
            Assert.That((length ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.AnyOf(RawDiagnosticCode.InvalidLength, RawDiagnosticCode.InvalidCodecData));
            Assert.That((table ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((end ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.TruncatedInput));
            Assert.That((restart ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
            Assert.That((category ?? throw new System.InvalidOperationException("A required value is unavailable.")).Code, Is.EqualTo(RawDiagnosticCode.InvalidCodecData));
        });
    }

    [Test]
    public void LibJpegTurboDecodesGeneratedEightBitLosslessStreamWhenAvailable()
    {
        string? executable = FindDjpeg();
        if (executable == null)
        {
            Assert.Ignore("djpeg is not installed on this test host.");
        }
        ushort[] samples = new ushort[] { 0, 1, 127, 128, 200, 255 };
        LosslessJpegImage source = Image(3, 2, 8, Plane(1, 3, 2, 1, 1, samples));
        byte[] encoded = LosslessJpegEncoder.Encode(source, new LosslessJpegEncodeParameters(1, 0, 0, true, true), new RawLimits(), CancellationToken.None);
        string directory = Path.Combine(Path.GetTempPath(), "raw-lossless-jpeg-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string input = Path.Combine(directory, "input.jpg");
        string output = Path.Combine(directory, "output.pgm");

        try
        {
            File.WriteAllBytes(input, encoded);
            System.Diagnostics.ProcessStartInfo start = new System.Diagnostics.ProcessStartInfo();
            start.FileName = executable;
            start.ArgumentList.Add("-pnm");
            start.ArgumentList.Add("-outfile");
            start.ArgumentList.Add(output);
            start.ArgumentList.Add(input);
            start.RedirectStandardError = true;
            start.UseShellExecute = false;
            using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(start);
            string error = (process ?? throw new System.InvalidOperationException("A required value is unavailable.")).StandardError.ReadToEnd();
            process.WaitForExit();
            Assert.That(process.ExitCode, Is.Zero, error);
            byte[] pixels = ReadPgmPixels(File.ReadAllBytes(output), 3, 2, 255);
            Assert.That(pixels, Is.EqualTo(samples.Select(value => (byte)value)));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static byte[] IllegalCategoryFixture()
    {
        byte[] counts = new byte[16];
        counts[0] = 1;
        return Join(
            new byte[] { 0xff, 0xd8 },
            Segment(0xc4, Join(new byte[] { 0 }, counts, new byte[] { 17 })),
            Segment(0xc3, new byte[] { 8, 0, 1, 0, 1, 1, 1, 0x11, 0 }),
            Segment(0xda, new byte[] { 1, 1, 0, 1, 0, 0 }),
            new byte[] { 0x7f },
            new byte[] { 0xff, 0xd9 });
    }

    private static byte[] SinglePlaneFixture(int precision, int width, int height, int predictor, int pointTransform, int[] differences)
    {
        byte[] counts = new byte[16];
        counts[4] = 17;
        byte[] symbols = Enumerable.Range(0, 17).Select(value => (byte)value).ToArray();
        return Join(
            new byte[] { 0xff, 0xd8 },
            Segment(0xc4, Join(new byte[] { 0 }, counts, symbols)),
            Segment(0xc3, new byte[] { (byte)precision, (byte)(height >> 8), (byte)height, (byte)(width >> 8), (byte)width, 1, 1, 0x11, 0 }),
            Segment(0xda, new byte[] { 1, 1, 0, (byte)predictor, 0, (byte)pointTransform }),
            EncodeDifferences(differences),
            new byte[] { 0xff, 0xd9 });
    }

    private static byte[] EncodeDifferences(int[] differences)
    {
        List<int> bits = new List<int>();
        foreach (int difference in differences)
        {
            int category = LosslessJpegEncoder.GetDifferenceCategory(difference);
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
        List<byte> bytes = new List<byte>();
        for (int offset = 0; offset < bits.Count; offset += 8)
        {
            byte value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (byte)((value << 1) | bits[offset + bit]);
            }
            bytes.Add(value);
            if (value == 0xff)
            {
                bytes.Add(0);
            }
        }
        return bytes.ToArray();
    }

    private static void AppendBits(List<int> bits, int value, int count)
    {
        for (int bit = count - 1; bit >= 0; bit--)
        {
            bits.Add((value >> bit) & 1);
        }
    }

    private static int FindMarker(byte[] source, byte marker)
    {
        for (int index = 0; index + 1 < source.Length; index++)
        {
            if (source[index] == 0xff && source[index + 1] == marker)
            {
                return index;
            }
        }
        throw new AssertionException("Marker not found.");
    }

    private static byte[] InsertBeforeEoi(byte[] source, byte[] addition)
    {
        return Join(source[..^2], addition, source[^2..]);
    }

    private static string? FindDjpeg()
    {
        string[] candidates = new string[] { "/opt/homebrew/bin/djpeg", "/usr/local/bin/djpeg", "/usr/bin/djpeg" };
        foreach (string candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }
        return null;
    }

    private static byte[] ReadPgmPixels(byte[] source, int width, int height, int maximum)
    {
        int offset = 0;
        string magic = ReadPgmToken(source, ref offset);
        int parsedWidth = int.Parse(ReadPgmToken(source, ref offset));
        int parsedHeight = int.Parse(ReadPgmToken(source, ref offset));
        int parsedMaximum = int.Parse(ReadPgmToken(source, ref offset));
        while (offset < source.Length && char.IsWhiteSpace((char)source[offset]))
        {
            offset++;
        }
        Assert.Multiple(() =>
        {
            Assert.That(magic, Is.EqualTo("P5"));
            Assert.That(parsedWidth, Is.EqualTo(width));
            Assert.That(parsedHeight, Is.EqualTo(height));
            Assert.That(parsedMaximum, Is.EqualTo(maximum));
        });
        return source.AsSpan(offset, width * height).ToArray();
    }

    private static string ReadPgmToken(byte[] source, ref int offset)
    {
        while (offset < source.Length && char.IsWhiteSpace((char)source[offset]))
        {
            offset++;
        }
        int start = offset;
        while (offset < source.Length && !char.IsWhiteSpace((char)source[offset]))
        {
            offset++;
        }
        return System.Text.Encoding.ASCII.GetString(source, start, offset - start);
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
