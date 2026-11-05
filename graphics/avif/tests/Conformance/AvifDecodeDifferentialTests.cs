using System.Diagnostics;
using System.Text;

namespace avif.Tests;

[NonParallelizable]
[Category("ReferenceCodec")]
public class AvifDecodeDifferentialTests
{
    const int MaximumRenderedChannelError = 32;
    const double MaximumRenderedMeanError = 12;

    [TestCase("fox.profile0.8bpc.yuv420.monochrome.avif", 1204, 800, 1)]
    [TestCase("fox.profile0.8bpc.yuv420.odd-width.odd-height.avif", 1203, 799, 3)]
    public void ExtractedAvifPlanesMatchBetweenLibaomAndDav1d(
        string fileName,
        int width,
        int height,
        int planeCount)
    {
        RequireTool("aomdec", "--help");
        RequireTool("dav1d", "--version");
        string directory = CreateDirectory();
        try
        {
            string avifPath = GetVectorPath(fileName);
            byte[] source = File.ReadAllBytes(avifPath);
            byte[] payload = ExtractPrimaryPayload(source);
            string obuPath = Path.Combine(directory, "primary.obu");
            string dav1dObuPath = Path.Combine(directory, "primary-dav1d.obu");
            string aomPath = Path.Combine(directory, "aom.yuv");
            string dav1dPath = Path.Combine(directory, "dav1d.yuv");
            File.WriteAllBytes(obuPath, payload);
            File.WriteAllBytes(dav1dObuPath, AddTemporalDelimiter(payload));
            DecodeWithAom(obuPath, aomPath);
            DecodeWithDav1d(dav1dObuPath, dav1dPath);

            byte[] aomPlanes = File.ReadAllBytes(aomPath);
            byte[] dav1dPlanes = File.ReadAllBytes(dav1dPath);
            ParsedAv1SequenceHeader? sequence = ParseSequenceHeader(payload);

            Assert.That((sequence ?? throw new InvalidOperationException("Invalid internal state.")).GetMaximumWidth(), Is.EqualTo((uint)width));
            Assert.That(sequence.GetMaximumHeight(), Is.EqualTo((uint)height));
            Assert.That(sequence.GetPlaneCount(), Is.EqualTo(planeCount));
            AssertExactBytes(aomPlanes, dav1dPlanes, "libaom/dav1d planes");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [TestCase("fox.profile0.8bpc.yuv420.monochrome.avif")]
    [TestCase("fox.profile0.8bpc.yuv420.odd-width.odd-height.avif")]
    public void ExtractedAvifPlanesMatchBetweenLibaomAndLibavif(
        string fileName)
    {
        RequireTool("avifdec", "--version");
        string directory = CreateDirectory();
        try
        {
            string avifPath = GetVectorPath(fileName);
            string y4mPath = Path.Combine(directory, "libavif.y4m");
            string obuPath = Path.Combine(directory, "primary.obu");
            string aomPath = Path.Combine(directory, "aom.yuv");
            Run(
                "avifdec",
                new string[]
                {
                    "--upsampling", "bilinear",
                    "--ignore-icc",
                    avifPath,
                    y4mPath
                });

            byte[] source = File.ReadAllBytes(avifPath);
            byte[] payload = ExtractPrimaryPayload(source);
            File.WriteAllBytes(obuPath, payload);
            DecodeWithAom(obuPath, aomPath);
            byte[] referencePlanes = File.ReadAllBytes(aomPath);
            AssertExactBytes(
                referencePlanes,
                ReadY4mPayload(y4mPath),
                "libaom/libavif planes");
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void ManagedRenderedPixelsTrackLibavifBilinearOutput()
    {
        RequireTool("avifdec", "--version");
        RequireTool("ffmpeg", "-version");
        string directory = CreateDirectory();
        AvifImage? managed = null;
        AvifPixelBuffer? rendered = null;
        try
        {
            string avifPath = GetVectorPath(
                "fox.profile0.8bpc.yuv420.odd-width.odd-height.avif");
            string pngPath = Path.Combine(directory, "libavif.png");
            string rgbaPath = Path.Combine(directory, "libavif.rgba");
            string obuPath = Path.Combine(directory, "primary.obu");
            string aomPath = Path.Combine(directory, "aom.yuv");
            Run(
                "avifdec",
                new string[]
                {
                    "--upsampling", "bilinear",
                    "--ignore-icc",
                    avifPath,
                    pngPath
                });
            Run(
                "ffmpeg",
                new string[]
                {
                    "-hide_banner",
                    "-loglevel", "error",
                    "-i", pngPath,
                    "-frames:v", "1",
                    "-f", "rawvideo",
                    "-pix_fmt", "rgba",
                    rgbaPath
                });

            byte[] source = File.ReadAllBytes(avifPath);
            byte[] payload = ExtractPrimaryPayload(source);
            File.WriteAllBytes(obuPath, payload);
            DecodeWithAom(obuPath, aomPath);
            byte[] referencePlanes = File.ReadAllBytes(aomPath);

            managed = CreateImageFromPlanes(
                referencePlanes,
                ParseSequenceHeader(payload));
            Assert.That(managed.GetCode(), Is.EqualTo(AvifResultCode.Success));

            AvifColorConversionOptions conversionOptions =
                new AvifColorConversionOptions();
            conversionOptions.ResamplingFilter = AvifResamplingFilter.Bilinear;
            conversionOptions.UseEmbeddedIccProfile = false;
            AvifColorConverter converter = AvifColorConverter.Create(
                conversionOptions);
            rendered = converter.Convert(managed, AvifPixelFormat.Rgba);
            Assert.That(rendered.GetCode(), Is.EqualTo(AvifResultCode.Success));
            AssertRenderedPixels(
                FlattenPixels(rendered),
                File.ReadAllBytes(rgbaPath),
                MaximumRenderedChannelError,
                MaximumRenderedMeanError);
        }
        finally
        {
            if (rendered != null)
            {
                rendered.Close();
            }
            if (managed != null)
            {
                managed.Close();
            }
            Directory.Delete(directory, true);
        }
    }

    static byte[] ExtractPrimaryPayload(byte[] source)
    {
        AvifReader reader = AvifReader.Create(source);
        Assert.That(reader.Parse(), Is.EqualTo(AvifResultCode.Success));
        uint itemId = ((reader.GetDocument() ?? throw new InvalidOperationException("Invalid internal state.")).GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetPrimaryItemId();
        byte[] payload = reader.GetEncodedItem(itemId);
        reader.Close();
        Assert.That(payload, Is.Not.Empty);
        return payload;
    }

    static byte[] AddTemporalDelimiter(byte[] payload)
    {
        byte[] framed = new byte[payload.Length + 2];
        framed[0] = 18;
        framed[1] = 0;
        payload.CopyTo(framed, 2);
        return framed;
    }

    static ParsedAv1SequenceHeader? ParseSequenceHeader(byte[] payload)
    {
        Av1SequenceHeaderParseResult result =
            Av1SequenceHeaderParser.ParseObuStream(payload, false, true);
        Assert.That(result.Code, Is.EqualTo(AvifResultCode.Success));
        Assert.That(result.Header, Is.Not.Null);
        return result.Header;
    }

    static AvifImage CreateImageFromPlanes(
        byte[] data,
        ParsedAv1SequenceHeader? sequence)
    {
        AvifChromaFormat chroma = GetChromaFormat(sequence);
        AvifRange range = (sequence ?? throw new InvalidOperationException("Invalid internal state.")).IsFullRange()
            ? AvifRange.Full : AvifRange.Limited;
        AvifImage image = AvifImage.Create(
            (int)sequence.GetMaximumWidth(),
            (int)sequence.GetMaximumHeight(),
            sequence.GetBitDepth(),
            chroma,
            range,
            (AvifChromaSamplePosition)sequence.GetChromaSamplePosition(),
            AvifAlphaMode.None,
            1,
            new AvifSecurityLimits());
        Assert.That(image.GetCode(), Is.EqualTo(AvifResultCode.Success));
        AvifColorInfo color = AvifColorInfo.Create(
            (AvifColorPrimaries)sequence.GetColorPrimaries(),
            (AvifTransferCharacteristics)sequence.GetTransferCharacteristics(),
            (AvifMatrixCoefficients)sequence.GetMatrixCoefficients(),
            range);
        Assert.That(image.SetColorInfo(color), Is.EqualTo(AvifResultCode.Success));

        int input = 0;
        int kind = (int)AvifPlaneKind.Y;
        while (kind <= (int)AvifPlaneKind.V)
        {
            AvifPlane? plane = image.GetPlane((AvifPlaneKind)kind);
            if (plane != null)
            {
                int row = 0;
                while (row < plane.GetHeight())
                {
                    int rowBytes = plane.GetRowByteCount();
                    Assert.That(input + rowBytes, Is.LessThanOrEqualTo(data.Length));
                    data.AsSpan(input, rowBytes).CopyTo(plane.GetRowSpan(row));
                    input += rowBytes;
                    row++;
                }
            }
            kind++;
        }
        Assert.That(input, Is.EqualTo(data.Length));
        return image;
    }

    static AvifChromaFormat GetChromaFormat(ParsedAv1SequenceHeader? sequence)
    {
        if ((sequence ?? throw new InvalidOperationException("Invalid internal state.")).IsMonochrome())
        {
            return AvifChromaFormat.Monochrome;
        }
        if (sequence.HasChromaSubsamplingX()
            && sequence.HasChromaSubsamplingY())
        {
            return AvifChromaFormat.Yuv420;
        }
        return sequence.HasChromaSubsamplingX()
            ? AvifChromaFormat.Yuv422 : AvifChromaFormat.Yuv444;
    }

    static byte[] FlattenPixels(AvifPixelBuffer pixels)
    {
        using MemoryStream output = new MemoryStream();
        int row = 0;
        while (row < pixels.GetHeight())
        {
            output.Write(pixels.GetRowSpan(row));
            row++;
        }
        return output.ToArray();
    }

    static byte[] ReadY4mPayload(string path)
    {
        byte[] data = File.ReadAllBytes(path);
        int headerEnd = FindLineEnd(data, 0);
        Assert.That(headerEnd, Is.GreaterThan(0));
        string header = Encoding.ASCII.GetString(data, 0, headerEnd);
        Assert.That(header, Does.StartWith("YUV4MPEG2 "));
        int frameStart = headerEnd + 1;
        int frameEnd = FindLineEnd(data, frameStart);
        Assert.That(frameEnd, Is.GreaterThan(frameStart));
        string frame = Encoding.ASCII.GetString(
            data,
            frameStart,
            frameEnd - frameStart);
        Assert.That(frame, Does.StartWith("FRAME"));
        return data.AsSpan(frameEnd + 1).ToArray();
    }

    static int FindLineEnd(byte[] data, int start)
    {
        int index = start;
        while (index < data.Length)
        {
            if (data[index] == 10)
            {
                return index;
            }
            index++;
        }
        return -1;
    }

    static void AssertExactBytes(
        byte[] expected,
        byte[] actual,
        string label)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), label + " length");
        int index = 0;
        while (index < expected.Length && expected[index] == actual[index])
        {
            index++;
        }
        Assert.That(
            index,
            Is.EqualTo(expected.Length),
            label + " mismatch at byte " + index
                + (index < expected.Length
                    ? ": expected " + expected[index] + ", actual " + actual[index]
                    : string.Empty));
    }

    static void AssertRenderedPixels(
        byte[] expected,
        byte[] actual,
        int maximumError,
        double maximumMeanError)
    {
        Assert.That(actual.Length, Is.EqualTo(expected.Length), "rendered length");
        int largestError = 0;
        int largestIndex = -1;
        long totalError = 0;
        int index = 0;
        while (index < expected.Length)
        {
            int error = System.Math.Abs(expected[index] - actual[index]);
            totalError += error;
            if (error > largestError)
            {
                largestError = error;
                largestIndex = index;
            }
            index++;
        }
        double meanError = totalError / (double)expected.Length;
        Assert.Multiple(() =>
        {
            Assert.That(
                largestError,
                Is.LessThanOrEqualTo(maximumError),
                "rendered mismatch at byte " + largestIndex
                    + ": managed "
                    + (largestIndex < 0 ? 0 : expected[largestIndex])
                    + ", libavif "
                    + (largestIndex < 0 ? 0 : actual[largestIndex]));
            Assert.That(
                meanError,
                Is.LessThanOrEqualTo(maximumMeanError),
                "rendered mean absolute error");
        });
    }

    static void DecodeWithAom(string input, string output)
    {
        Run(
            "aomdec",
            new string[]
            {
                "--codec=av1",
                "--rawvideo",
                "--output-bit-depth=8",
                "--output=" + output,
                input
            });
    }

    static void DecodeWithDav1d(string input, string output)
    {
        Run(
            "dav1d",
            new string[]
            {
                "--quiet",
                "--demuxer", "section5",
                "--muxer", "yuv",
                "--input", input,
                "--output", output
            });
    }

    static void RequireTool(string fileName, string versionArgument)
    {
        if (!CanRun(fileName, versionArgument))
        {
            Assert.Ignore(fileName + " is required for this differential test.");
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

    static void Run(string fileName, string[] arguments)
    {
        ProcessStartInfo info = new ProcessStartInfo();
        info.FileName = fileName;
        info.UseShellExecute = false;
        info.RedirectStandardOutput = true;
        info.RedirectStandardError = true;
        int index = 0;
        while (index < arguments.Length)
        {
            info.ArgumentList.Add(arguments[index]);
            index++;
        }

        using Process? process = Process.Start(info);
        string standardOutput = (process ?? throw new InvalidOperationException("Invalid internal state.")).StandardOutput.ReadToEnd();
        string standardError = process.StandardError.ReadToEnd();
        if (!process.WaitForExit(60000))
        {
            process.Kill(true);
            Assert.Fail(fileName + " timed out after 60000 ms.");
        }
        Assert.That(
            process.ExitCode,
            Is.Zero,
            fileName + " failed.\n" + standardOutput + standardError);
    }

    static string GetVectorPath(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null
            && !File.Exists(Path.Combine(directory.FullName, "todo.md")))
        {
            directory = directory.Parent;
        }
        Assert.That(directory, Is.Not.Null, "Could not locate the repository root.");
        string path = Path.Combine(
            (directory ?? throw new InvalidOperationException("Invalid internal state.")).FullName,
            "tests",
            "Vectors",
            "AOMedia-Link-U",
            fileName);
        Assert.That(File.Exists(path), Is.True, "Missing imported vector: " + path);
        return path;
    }

    static string CreateDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "avif-t119-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
