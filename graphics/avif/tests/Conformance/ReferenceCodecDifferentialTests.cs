using System.Diagnostics;

namespace avif.Tests;

[NonParallelizable]
[Category("ReferenceCodec")]
public class ReferenceCodecDifferentialTests
{
    const int Width = 16;
    const int Height = 16;

    [OneTimeSetUp]
    public void RequireReferenceCodecs()
    {
        if (!CanRun("aomenc", "--help")
            || !CanRun("aomdec", "--help")
            || !CanRun("dav1d", "--version"))
        {
            Assert.Ignore(
                "aomenc, aomdec, and dav1d are required for reference-codec differential tests.");
        }
    }

    [TestCase(0, 8, true, true, true, "--i420")]
    [TestCase(0, 10, true, true, true, "--i420")]
    [TestCase(2, 12, true, true, true, "--i420")]
    [TestCase(0, 8, false, true, true, "--i420")]
    [TestCase(0, 10, false, true, true, "--i420")]
    [TestCase(2, 12, false, true, true, "--i420")]
    [TestCase(2, 8, false, true, false, "--i422")]
    [TestCase(2, 10, false, true, false, "--i422")]
    [TestCase(2, 12, false, true, false, "--i422")]
    [TestCase(1, 8, false, false, false, "--i444")]
    [TestCase(1, 10, false, false, false, "--i444")]
    [TestCase(2, 12, false, false, false, "--i444")]
    public void ManagedFormatValidationMatchesLibaomAndDav1d(
        int profile,
        int bitDepth,
        bool monochrome,
        bool subsamplingX,
        bool subsamplingY,
        string inputFormat)
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "avif-t062-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string inputPath = Path.Combine(directory, "input.yuv");
            string obuPath = Path.Combine(directory, "encoded.obu");
            string aomPath = Path.Combine(directory, "aom.yuv");
            string davPath = Path.Combine(directory, "dav1d.yuv");
            byte[] source = CreateRawFrames(
                Width,
                Height,
                bitDepth,
                subsamplingX,
                subsamplingY,
                1);
            File.WriteAllBytes(inputPath, source);

            List<string> encodeArguments = CreateEncodeArguments(
                inputPath,
                obuPath,
                Width,
                Height,
                profile,
                bitDepth,
                monochrome,
                inputFormat);
            Run("aomenc", encodeArguments, 30000);
            Run(
                "aomdec",
                new List<string>
                {
                    "--codec=av1",
                    "--rawvideo",
                    "--output-bit-depth=" + bitDepth,
                    "--output=" + aomPath,
                    obuPath
                },
                30000);
            Run(
                "dav1d",
                new List<string>
                {
                    "--quiet",
                    "--demuxer", "section5",
                    "--muxer", "yuv",
                    "--input", obuPath,
                    "--output", davPath
                },
                30000);

            ParsedAv1SequenceHeader? sequence = ParseSequenceHeader(obuPath);
            Av1PixelFormatDescription? format =
                Av1PixelConformance.Describe(sequence);
            PlaneBuffer[] sourcePlanes = ReadPlanes(
                source, sequence, Width, Height, false);
            PlaneBuffer[] aomPlanes = ReadPlanes(
                File.ReadAllBytes(aomPath), sequence, Width, Height, true);
            PlaneBuffer[] davPlanes = ReadPlanes(
                File.ReadAllBytes(davPath), sequence, Width, Height, true);
            Av1PixelComparisonResult sourceComparison =
                Av1PixelConformance.Compare(
                    sequence,
                    Width,
                    Height,
                    sourcePlanes,
                    aomPlanes);
            Av1PixelComparisonResult decoderComparison =
                Av1PixelConformance.Compare(
                    sequence,
                    Width,
                    Height,
                    aomPlanes,
                    davPlanes);

            Assert.Multiple(() =>
            {
                Assert.That(format, Is.Not.Null);
                Assert.That((format ?? throw new InvalidOperationException("Invalid internal state.")).Profile, Is.EqualTo(profile));
                Assert.That(format.BitDepth, Is.EqualTo(bitDepth));
                Assert.That(format.Monochrome, Is.EqualTo(monochrome));
                Assert.That(format.SubsamplingX, Is.EqualTo(subsamplingX));
                Assert.That(format.SubsamplingY, Is.EqualTo(subsamplingY));
                Assert.That(
                    Av1SemanticValidator.ValidateSequence(sequence).Code,
                    Is.EqualTo(AvifResultCode.Success));
                AssertComparison(sourceComparison, "lossless source/libaom");
                AssertComparison(decoderComparison, "libaom/dav1d");
            });

            ClosePlanes(sourcePlanes);
            ClosePlanes(aomPlanes);
            ClosePlanes(davPlanes);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static List<string> CreateEncodeArguments(
        string inputPath,
        string outputPath,
        int width,
        int height,
        int profile,
        int bitDepth,
        bool monochrome,
        string inputFormat)
    {
        int encoderProfile = profile;
        if (bitDepth == 12 && inputFormat == "--i422")
        {
            encoderProfile = 0;
        }
        else if (bitDepth == 12 && inputFormat == "--i444")
        {
            encoderProfile = 1;
        }
        List<string> arguments = new List<string>
        {
            "--quiet",
            "--obu",
            "--passes=1",
            "--allintra",
            "--lossless=1",
            "--cpu-used=6",
            "--width=" + width,
            "--height=" + height,
            "--fps=1/1",
            "--profile=" + encoderProfile,
            "--bit-depth=" + bitDepth,
            "--input-bit-depth=" + bitDepth,
            inputFormat,
            "--output=" + outputPath
        };
        if (monochrome)
        {
            arguments.Add("--monochrome");
        }
        if (bitDepth > 8)
        {
            arguments.Add("--use-16bit-internal");
        }
        arguments.Add(inputPath);
        return arguments;
    }

    static byte[] CreateRawFrames(
        int width,
        int height,
        int bitDepth,
        bool subsamplingX,
        bool subsamplingY,
        int frameCount)
    {
        int bytesPerSample = bitDepth > 8 ? 2 : 1;
        int chromaWidth = (width + (subsamplingX ? 1 : 0))
            >> (subsamplingX ? 1 : 0);
        int chromaHeight = (height + (subsamplingY ? 1 : 0))
            >> (subsamplingY ? 1 : 0);
        int frameSamples = width * height
            + 2 * chromaWidth * chromaHeight;
        byte[] data = new byte[frameSamples * bytesPerSample * frameCount];
        int output = 0;
        for (int frame = 0; frame < frameCount; frame++)
        {
            for (int plane = 0; plane < 3; plane++)
            {
                int planeWidth = plane == 0 ? width : chromaWidth;
                int planeHeight = plane == 0 ? height : chromaHeight;
                for (int y = 0; y < planeHeight; y++)
                {
                    for (int x = 0; x < planeWidth; x++)
                    {
                        int maximum = (1 << bitDepth) - 1;
                        int value = (frame * 193 + plane * 269
                            + y * 37 + x * 23 + x * y * 3) & maximum;
                        data[output++] = (byte)value;
                        if (bytesPerSample == 2)
                        {
                            data[output++] = (byte)(value >> 8);
                        }
                    }
                }
            }
        }
        return data;
    }

    static ParsedAv1SequenceHeader? ParseSequenceHeader(string path)
    {
        byte[] source = File.ReadAllBytes(path);
        Av1ObuParseResult parsed = Av1ObuParser.ParseLowOverhead(
            source, new AvifSecurityLimits(), 0);
        Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
        Av1Obu? sequenceObu = null;
        for (int index = 0; index < parsed.Obus.Count; index++)
        {
            if (parsed.Obus[index].Header.Type == Av1ObuType.SequenceHeader)
            {
                sequenceObu = parsed.Obus[index];
                break;
            }
        }
        Assert.That(sequenceObu, Is.Not.Null);
        Av1SequenceHeaderParseResult sequence =
            Av1SequenceHeaderParser.ParsePayload((sequenceObu ?? throw new InvalidOperationException("Invalid internal state.")).GetPayload());
        Assert.That(sequence.Code, Is.EqualTo(AvifResultCode.Success));
        return sequence.Header;
    }

    static PlaneBuffer[] ReadPlanes(
        byte[] data,
        ParsedAv1SequenceHeader? sequence,
        int width,
        int height,
        bool requireExactLength)
    {
        Av1PixelFormatDescription? format =
            Av1PixelConformance.Describe(sequence);
        Assert.That(format, Is.Not.Null);
        PlaneBuffer[] planes = new PlaneBuffer[(format ?? throw new InvalidOperationException("Invalid internal state.")).PlaneCount];
        int input = 0;
        for (int plane = 0; plane < planes.Length; plane++)
        {
            int shiftX = plane == 0 || !format.SubsamplingX ? 0 : 1;
            int shiftY = plane == 0 || !format.SubsamplingY ? 0 : 1;
            int planeWidth = (width + shiftX) >> shiftX;
            int planeHeight = (height + shiftY) >> shiftY;
            planes[plane] = new PlaneBuffer(
                planeWidth,
                planeHeight,
                format.BytesPerSample,
                1,
                1 << 20,
                true);
            for (int y = 0; y < planeHeight; y++)
            {
                int rowBytes = planeWidth * format.BytesPerSample;
                Assert.That(input + rowBytes, Is.LessThanOrEqualTo(data.Length));
                data.AsSpan(input, rowBytes).CopyTo(planes[plane].GetRowSpan(y));
                input += rowBytes;
            }
        }
        if (requireExactLength)
        {
            Assert.That(input, Is.EqualTo(data.Length));
        }
        return planes;
    }

    static void AssertComparison(
        Av1PixelComparisonResult comparison,
        string label)
    {
        Assert.That(
            comparison.Code,
            Is.EqualTo(AvifResultCode.Success),
            label + " mismatch at plane " + comparison.Plane
                + ", x " + comparison.X + ", y " + comparison.Y
                + ": expected " + comparison.Expected
                + ", actual " + comparison.Actual);
        Assert.That(comparison.Matches, Is.True, label);
    }

    static void ClosePlanes(PlaneBuffer[] planes)
    {
        for (int plane = 0; plane < planes.Length; plane++)
        {
            planes[plane].Close();
        }
    }

    static bool CanRun(string fileName, string argument)
    {
        try
        {
            ProcessStartInfo info = new ProcessStartInfo();
            info.FileName = fileName;
            info.UseShellExecute = false;
            info.RedirectStandardOutput = true;
            info.RedirectStandardError = true;
            info.ArgumentList.Add(argument);
            using Process? process = Process.Start(info);
            (process ?? throw new InvalidOperationException("Invalid internal state.")).WaitForExit(10000);
            return process.HasExited;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    static void Run(
        string fileName,
        List<string> arguments,
        int timeoutMilliseconds)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = fileName;
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        for (int index = 0; index < arguments.Count; index++)
        {
            info.ArgumentList.Add(arguments[index]);
        }

        using Process? process = Process.Start(info);
        string standardOutput = (process ?? throw new InvalidOperationException("Invalid internal state.")).StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            process.Kill(true);
            Assert.Fail(fileName + " timed out after "
                + timeoutMilliseconds + " ms.");
        }
        Assert.That(
            process.ExitCode,
            Is.Zero,
            fileName + " failed.\n" + standardOutput + standardError);
    }
}
