using System.Diagnostics;
using raw;

namespace raw.Tests;

public class BaselineJpegEncoderTests
{
    [Test]
    public void QualityScalingProducesDeterministicBaselineTables()
    {
        byte[] luminanceFifty = BaselineJpegEncoder.BuildQuantizationTable(50, false);
        byte[] chrominanceFifty = BaselineJpegEncoder.BuildQuantizationTable(50, true);
        byte[] maximumQuality = BaselineJpegEncoder.BuildQuantizationTable(100, false);
        byte[] minimumQuality = BaselineJpegEncoder.BuildQuantizationTable(1, false);
        BaselineJpegImage image = GrayImage(8, 8, Enumerable.Repeat((byte)128, 64).ToArray());
        byte[] encoded = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(50, 1, 1, 0), new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(luminanceFifty, Is.EqualTo(BaselineJpegEncoder.LuminanceQuantizationBase.Select(value => (byte)value)));
            Assert.That(chrominanceFifty, Is.EqualTo(BaselineJpegEncoder.ChrominanceQuantizationBase.Select(value => (byte)value)));
            Assert.That(maximumQuality, Is.All.EqualTo(1));
            Assert.That(minimumQuality, Is.All.EqualTo(255));
            Assert.That(document.QuantizationTables.Single().Values, Is.EqualTo(BaselineJpegCodec.ZigZag.Select(index => (ushort)luminanceFifty[index])));
        });
    }

    [Test]
    public void GrayscaleEncodingIsDeterministicAndRoundTripsSmoothSamples()
    {
        byte[] samples = Samples(17, 9, (x, y) => 20 + x * 5 + y * 3);
        BaselineJpegImage image = GrayImage(17, 9, samples);
        BaselineJpegEncodeParameters parameters = new BaselineJpegEncodeParameters(95, 1, 1, 0);

        byte[] first = BaselineJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
        byte[] second = BaselineJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
        BaselineJpegImage decoded = BaselineJpegCodec.Decode(first, new RawLimits(), CancellationToken.None);
        JpegDocument document = JpegParser.Parse(first, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.EqualTo(first));
            Assert.That(document.Frames.Single().ProcessMarker, Is.EqualTo(0xc0));
            Assert.That(document.Scans.Single().SpectralStart, Is.Zero);
            Assert.That(document.Scans.Single().SpectralEnd, Is.EqualTo(63));
            Assert.That(((decoded.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip(samples, Difference) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void SelectableYcbcrSamplingModesRoundTripNativePlanes()
    {
        foreach ((int horizontal, int vertical) in new (int, int)[] { (1, 1), (2, 1), (2, 2) })
        {
            BaselineJpegImage image = ColorImage(17, 15, horizontal, vertical);
            BaselineJpegEncodeParameters parameters = new BaselineJpegEncodeParameters(100, horizontal, vertical, 0);

            byte[] encoded = BaselineJpegEncoder.Encode(image, parameters, new RawLimits(), CancellationToken.None);
            JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
            BaselineJpegImage decoded = BaselineJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

            Assert.Multiple(() =>
            {
                Assert.That(document.QuantizationTables.Select(table => table.Identifier), Is.EqualTo(new int[] { 0, 1 }), $"sampling {horizontal}x{vertical}");
                Assert.That(document.Frames.Single().Components.Select(component => (component.HorizontalSampling, component.VerticalSampling)), Is.EqualTo(new (int, int)[] { (horizontal, vertical), (1, 1), (1, 1) }), $"sampling {horizontal}x{vertical}");
                Assert.That(document.Frames.Single().Components.Select(component => component.QuantizationTable), Is.EqualTo(new int[] { 0, 1, 1 }), $"sampling {horizontal}x{vertical}");
                Assert.That(decoded.Planes.Select(plane => (plane.Width, plane.Height)), Is.EqualTo(image.Planes.Select(plane => (plane.Width, plane.Height))), $"sampling {horizontal}x{vertical}");
                Assert.That((decoded.GetPlane(1).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip((image.GetPlane(1).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")), Difference), Is.All.LessThanOrEqualTo(2), $"Y {horizontal}x{vertical}");
                Assert.That((decoded.GetPlane(2).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip((image.GetPlane(2).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")), Difference), Is.All.LessThanOrEqualTo(2), $"Cb {horizontal}x{vertical}");
                Assert.That((decoded.GetPlane(3).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip((image.GetPlane(3).Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")), Difference), Is.All.LessThanOrEqualTo(2), $"Cr {horizontal}x{vertical}");
            });
        }
    }

    [Test]
    public void RestartIntervalsAreWrittenAtMcuBoundariesAndResetDcState()
    {
        byte[] samples = Samples(40, 8, (x, y) => 30 + x * 4 + y);
        BaselineJpegImage image = GrayImage(40, 8, samples);
        byte[] encoded = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(95, 1, 1, 2), new RawLimits(), CancellationToken.None);

        JpegDocument document = JpegParser.Parse(encoded, new RawLimits(), CancellationToken.None);
        BaselineJpegImage decoded = BaselineJpegCodec.Decode(document, new RawLimits(), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(document.RestartIntervals.Single().Interval, Is.EqualTo(2));
            Assert.That(document.Scans.Single().RestartMarkers.Select(marker => marker.Code), Is.EqualTo(new byte[] { 0xd0, 0xd1 }));
            Assert.That(document.Scans.Single().EntropySegments.Count, Is.EqualTo(3));
            Assert.That(((decoded.Planes.Single().Samples ?? throw new System.InvalidOperationException("A required value is unavailable.")).Zip(samples, Difference) ?? throw new System.InvalidOperationException("A required value is unavailable.")), Is.All.LessThanOrEqualTo(2));
        });
    }

    [Test]
    public void LibJpegTurboDecodesGeneratedBaselineStreamWhenAvailable()
    {
        string? executable = FindDjpeg();
        if (executable == null)
        {
            Assert.Ignore("djpeg is not installed.");
        }
        byte[] samples = Samples(8, 8, (x, y) => 15 + x * 12 + y * 8);
        BaselineJpegImage image = GrayImage(8, 8, samples);
        byte[] encoded = BaselineJpegEncoder.Encode(image, new BaselineJpegEncodeParameters(95, 1, 1, 0), new RawLimits(), CancellationToken.None);
        string directory = Path.Combine(Path.GetTempPath(), "raw-baseline-encoder-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string input = Path.Combine(directory, "input.jpg");
        string output = Path.Combine(directory, "output.pgm");
        try
        {
            File.WriteAllBytes(input, encoded);
            ProcessStartInfo start = new ProcessStartInfo(executable);
            start.ArgumentList.Add("-pnm");
            start.ArgumentList.Add("-outfile");
            start.ArgumentList.Add(output);
            start.ArgumentList.Add(input);
            start.RedirectStandardError = true;
            start.UseShellExecute = false;
            using Process? process = Process.Start(start);
            (process ?? throw new System.InvalidOperationException("A required value is unavailable.")).WaitForExit();

            Assert.Multiple(() =>
            {
                Assert.That(process.ExitCode, Is.Zero, process.StandardError.ReadToEnd());
                Assert.That(ReadPgmPixels(output).Zip(samples, Difference), Is.All.LessThanOrEqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void InvalidParametersAndPlaneDescriptorsAreRejected()
    {
        BaselineJpegImage valid = GrayImage(8, 8, new byte[64]);
        BaselineJpegImage badPrecision = GrayImage(8, 8, new byte[64]);
        badPrecision.Precision = 12;
        BaselineJpegImage twoPlanes = GrayImage(8, 8, new byte[64]);
        twoPlanes.Planes.Add(Plane(2, 8, 8, 1, 1, new byte[64]));
        BaselineJpegImage badGeometry = GrayImage(8, 8, new byte[64]);
        badGeometry.Planes[0].Width = 7;
        BaselineJpegImage badLength = GrayImage(8, 8, new byte[63]);

        Assert.Multiple(() =>
        {
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(null, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(valid, null, new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(valid, new BaselineJpegEncodeParameters(0, 1, 1, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(valid, new BaselineJpegEncodeParameters(90, 1, 2, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(valid, new BaselineJpegEncodeParameters(90, 2, 2, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(valid, new BaselineJpegEncodeParameters(90, 1, 1, 65536), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(badPrecision, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(twoPlanes, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(badGeometry, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None));
            Assert.Throws<RawFormatException>(() => BaselineJpegEncoder.Encode(badLength, new BaselineJpegEncodeParameters(90, 1, 1, 0), new RawLimits(), CancellationToken.None));
        });
    }

    [Test]
    public void EncodingHonorsDecodedOutputAndCancellationLimits()
    {
        BaselineJpegImage image = GrayImage(8, 8, new byte[64]);
        BaselineJpegEncodeParameters parameters = new BaselineJpegEncodeParameters(90, 1, 1, 0);
        RawLimits decoded = new RawLimits();
        decoded.MaxDecodedBytes = 63;
        RawLimits output = new RawLimits();
        output.MaxFileSizeBytes = 100;
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Multiple(() =>
        {
            Assert.Throws<RawLimitException>(() => BaselineJpegEncoder.Encode(image, parameters, decoded, CancellationToken.None));
            Assert.Throws<RawLimitException>(() => BaselineJpegEncoder.Encode(image, parameters, output, CancellationToken.None));
            Assert.Throws<OperationCanceledException>(() => BaselineJpegEncoder.Encode(image, parameters, new RawLimits(), cancellation.Token));
        });
    }

    private static int Difference(byte left, byte right)
    {
        return Math.Abs(left - right);
    }

    private static BaselineJpegImage GrayImage(int width, int height, byte[] samples)
    {
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.Planes.Add(Plane(1, width, height, 1, 1, samples));
        return image;
    }

    private static BaselineJpegImage ColorImage(int width, int height, int horizontal, int vertical)
    {
        int chromaWidth = (width + horizontal - 1) / horizontal;
        int chromaHeight = (height + vertical - 1) / vertical;
        BaselineJpegImage image = new BaselineJpegImage();
        image.Width = width;
        image.Height = height;
        image.Precision = 8;
        image.Planes.Add(Plane(1, width, height, horizontal, vertical, Samples(width, height, (x, y) => 40 + x * 3 + y * 2)));
        image.Planes.Add(Plane(2, chromaWidth, chromaHeight, 1, 1, Samples(chromaWidth, chromaHeight, (x, y) => 90 + x * 2 + y)));
        image.Planes.Add(Plane(3, chromaWidth, chromaHeight, 1, 1, Samples(chromaWidth, chromaHeight, (x, y) => 170 - x * 2 - y)));
        return image;
    }

    private static BaselineJpegPlane Plane(byte identifier, int width, int height, int horizontal, int vertical, byte[] samples)
    {
        BaselineJpegPlane plane = new BaselineJpegPlane();
        plane.Identifier = identifier;
        plane.Width = width;
        plane.Height = height;
        plane.HorizontalSampling = horizontal;
        plane.VerticalSampling = vertical;
        plane.Samples = samples;
        return plane;
    }

    private static byte[] Samples(int width, int height, Func<int, int, int> value)
    {
        byte[] samples = new byte[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                samples[y * width + x] = (byte)value(x, y);
            }
        }
        return samples;
    }

    private static string? FindDjpeg()
    {
        foreach (string path in new string[] { "/opt/homebrew/bin/djpeg", "/usr/local/bin/djpeg", "/usr/bin/djpeg" })
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    private static byte[] ReadPgmPixels(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        int offset = 0;
        Assert.That(ReadPgmToken(bytes, ref offset), Is.EqualTo("P5"));
        int width = int.Parse(ReadPgmToken(bytes, ref offset));
        int height = int.Parse(ReadPgmToken(bytes, ref offset));
        Assert.That(ReadPgmToken(bytes, ref offset), Is.EqualTo("255"));
        while (offset < bytes.Length && char.IsWhiteSpace((char)bytes[offset]))
        {
            offset++;
        }
        return bytes.Skip(offset).Take(width * height).ToArray();
    }

    private static string ReadPgmToken(byte[] bytes, ref int offset)
    {
        while (offset < bytes.Length && char.IsWhiteSpace((char)bytes[offset]))
        {
            offset++;
        }
        int start = offset;
        while (offset < bytes.Length && !char.IsWhiteSpace((char)bytes[offset]))
        {
            offset++;
        }
        return System.Text.Encoding.ASCII.GetString(bytes, start, offset - start);
    }
}
