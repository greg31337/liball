using System.Diagnostics;
using System.Text;
using webp;

namespace webp.Tests;

[TestFixture]
public class WebpReferenceInteroperabilityTests
{
    [TestCase("reference-lossless.webp", "reference-lossless.pam", WebpBitstreamKind.Lossless, 4, 3)]
    public void DecodeExternallyGeneratedStillMatchesLibwebpReferencePixels(
        string webpFixture,
        string pamFixture,
        WebpBitstreamKind expectedKind,
        int width,
        int height)
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = ReadFixture(webpFixture);
        byte[] expectedPixels = ReadPamPixels(ReadFixture(pamFixture), width, height);

        WebpDecodeResult result = codec.Decode(data, new WebpDecodeOptions());

        Assert.That(result.IsValid, Is.True, JoinDiagnostics(result.Diagnostics));
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(width));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(height));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(1));
        Assert.That(result.Image.Frames[0].BitstreamKind, Is.EqualTo(expectedKind));
        Assert.That(result.Image.Frames[0].Pixels, Is.Not.Null);
        Assert.That(result.Image.Frames[0].Pixels!.Rgba, Is.EqualTo(expectedPixels));
    }

    [Test]
    public void DecodeExternalAnimationMatchesTrustedTimelineAndCompositedSnapshots()
    {
        WebpCodec codec = new WebpCodec();
        byte[] data = ReadFixture("reference-animation.webp");
        WebpDecodeOptions options = new WebpDecodeOptions();
        options.AnimationDecodeMode = WebpAnimationDecodeMode.CompositedFrames;

        WebpDecodeResult result = codec.Decode(data, options);

        Assert.That(result.IsValid, Is.True, JoinDiagnostics(result.Diagnostics));
        Assert.That(result.Image.CanvasWidth, Is.EqualTo(3));
        Assert.That(result.Image.CanvasHeight, Is.EqualTo(1));
        Assert.That(result.Image.LoopCount, Is.EqualTo(3));
        Assert.That(result.Image.BackgroundColorBgra, Is.EqualTo(0U));
        Assert.That(result.Image.Frames, Has.Count.EqualTo(3));
        AssertReferenceFrame(result.Image.Frames[0], 40, "reference-animation-01.pam");
        AssertReferenceFrame(result.Image.Frames[1], 70, "reference-animation-02.pam");
        AssertReferenceFrame(result.Image.Frames[2], 110, "reference-animation-03.pam");
    }

    [TestCase(WebpEncodeMode.Automatic)]
    [TestCase(WebpEncodeMode.Lossy)]
    [TestCase(WebpEncodeMode.Lossless)]
    [TestCase(WebpEncodeMode.NearLossless)]
    public void EveryEncoderModeIsAcceptedByIndependentLibwebpDecoder(WebpEncodeMode mode)
    {
        WebpCodec codec = new WebpCodec();
        WebpPixelBuffer source = NewReferencePattern();
        WebpEncodeOptions options = new WebpEncodeOptions();
        options.Mode = mode;
        options.Quality = 83;
        options.NearLosslessQuality = 65;
        options.ExactTransparentRgb = true;

        byte[] encoded = codec.Encode(source, options);
        byte[] referencePam = DecodeWithDwebp(encoded);
        byte[] referencePixels = ReadPamPixels(referencePam, source.Width, source.Height);

        Assert.That(referencePixels, Has.Length.EqualTo(source.Width * source.Height * 4));
        Assert.That(ReadPamDepth(referencePam), Is.EqualTo(4));
    }

    [Test]
    public void EveryMuxMutationCategoryIsAcceptedByIndependentLibwebpMuxer()
    {
        WebpCodec codec = new WebpCodec();
        byte[] still = ReadFixture("reference-lossless.webp");
        byte[] animation = ReadFixture("reference-animation.webp");
        WebpMetadata metadata = new WebpMetadata();
        metadata.IccProfile = new byte[] { 1, 2, 3 };
        metadata.Exif = new byte[] { 4, 5, 6, 7 };
        metadata.Xmp = new byte[] { 8, 9 };
        List<WebpUnknownChunk> unknown = new List<WebpUnknownChunk>();
        unknown.Add(new WebpUnknownChunk("t026", new byte[] { 10, 11, 12 }));
        List<WebpFrame> frames = codec.ExtractFrames(animation);
        WebpFrame insertedFrame = CopyFrame(frames[2]);
        insertedFrame.DurationMilliseconds = 25;
        WebpFrame replacementFrame = CopyFrame(frames[0]);
        replacementFrame.DurationMilliseconds = 35;
        List<byte[]> results = new List<byte[]>();

        results.Add(codec.ReplaceMetadata(still, metadata));
        results.Add(codec.SetIccProfile(still, metadata.IccProfile));
        results.Add(codec.SetExif(still, metadata.Exif));
        results.Add(codec.SetXmp(still, metadata.Xmp));
        results.Add(codec.ReplaceUnknownChunks(still, unknown, WebpUnknownChunkPlacement.AfterRecognizedChunks));
        results.Add(codec.InsertAnimationFrame(animation, 1, insertedFrame));
        results.Add(codec.ReplaceAnimationFrame(animation, 1, replacementFrame));
        results.Add(codec.RemoveAnimationFrame(animation, 1));
        results.Add(codec.NormalizeContainer(animation).Data);
        results.Add(codec.SetAnimationLoopCount(animation, 17));
        results.Add(codec.SetAnimationBackgroundColor(animation, 0x80402010U));
        results.Add(codec.SetAnimationLoopAndBackground(animation, 19, 0xFF112233U));

        int index = 0;
        while (index < results.Count)
        {
            AssertAcceptedByWebpmux(results[index], index);
            index = index + 1;
        }
    }

    static void AssertReferenceFrame(WebpFrame frame, int duration, string pamFixture)
    {
        Assert.That(frame.X, Is.EqualTo(0));
        Assert.That(frame.Y, Is.EqualTo(0));
        Assert.That(frame.Width, Is.EqualTo(3));
        Assert.That(frame.Height, Is.EqualTo(1));
        Assert.That(frame.DurationMilliseconds, Is.EqualTo(duration));
        Assert.That(frame.Pixels, Is.Not.Null);
        Assert.That(frame.Pixels!.Rgba, Is.EqualTo(ReadPamPixels(ReadFixture(pamFixture), 3, 1)));
    }

    static WebpPixelBuffer NewReferencePattern()
    {
        WebpPixelBuffer buffer = WebpPixelBufferHelper.AllocateRgba(7, 5);
        int y = 0;
        while (y < buffer.Height)
        {
            int x = 0;
            while (x < buffer.Width)
            {
                int offset = (y * buffer.Stride) + (x * 4);
                buffer.Rgba[offset] = (byte)((x * 31) + (y * 7));
                buffer.Rgba[offset + 1] = (byte)((x * 13) + (y * 29));
                buffer.Rgba[offset + 2] = (byte)((x * 47) + (y * 3));
                buffer.Rgba[offset + 3] = 255;
                x = x + 1;
            }

            y = y + 1;
        }

        return buffer;
    }

    static WebpFrame CopyFrame(WebpFrame source)
    {
        WebpFrame copy = new WebpFrame();
        copy.X = source.X;
        copy.Y = source.Y;
        copy.Width = source.Width;
        copy.Height = source.Height;
        copy.DurationMilliseconds = source.DurationMilliseconds;
        copy.BlendMode = source.BlendMode;
        copy.DisposeMode = source.DisposeMode;
        copy.BitstreamKind = source.BitstreamKind;
        copy.EncodedBitstream = source.EncodedBitstream;
        copy.AlphaData = source.AlphaData;
        return copy;
    }

    static byte[] DecodeWithDwebp(byte[] data)
    {
        string directory = NewTemporaryDirectory();
        string input = Path.Combine(directory, "input.webp");
        string output = Path.Combine(directory, "output.pam");
        try
        {
            File.WriteAllBytes(input, data);
            RunTool("dwebp", new string[] { input, "-pam", "-o", output });
            return File.ReadAllBytes(output);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void AssertAcceptedByWebpmux(byte[] data, int resultIndex)
    {
        string directory = NewTemporaryDirectory();
        string input = Path.Combine(directory, "mux-result-" + resultIndex + ".webp");
        try
        {
            File.WriteAllBytes(input, data);
            RunTool("webpmux", new string[] { "-info", input });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static void RunTool(string toolName, string[] arguments)
    {
        string executable = ResolveTool(toolName);
        ProcessStartInfo startInfo = new ProcessStartInfo(executable);
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        int index = 0;
        while (index < arguments.Length)
        {
            startInfo.ArgumentList.Add(arguments[index]);
            index = index + 1;
        }

        using Process process = new Process();
        process.StartInfo = startInfo;
        Assert.That(process.Start(), Is.True);
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        Assert.That(process.WaitForExit(10000), Is.True, toolName + " timed out");
        Assert.That(process.ExitCode, Is.EqualTo(0), output + error);
    }

    static string ResolveTool(string toolName)
    {
        string? pathValue = Environment.GetEnvironmentVariable("PATH");
        if (pathValue != null)
        {
            string[] paths = pathValue.Split(Path.PathSeparator);
            int index = 0;
            while (index < paths.Length)
            {
                string candidate = Path.Combine(paths[index], toolName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                index = index + 1;
            }
        }

        throw new AssertionException(toolName + " is required for reference interoperability tests");
    }

    static string NewTemporaryDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), "webp-interop-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    static byte[] ReadFixture(string name)
    {
        string path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Fixtures", "Interop", name);
        return File.ReadAllBytes(path);
    }

    static byte[] ReadPamPixels(byte[] pam, int expectedWidth, int expectedHeight)
    {
        byte[] marker = Encoding.ASCII.GetBytes("ENDHDR\n");
        int markerOffset = FindSequence(pam, marker);
        Assert.That(markerOffset, Is.GreaterThanOrEqualTo(0));
        string header = Encoding.ASCII.GetString(pam, 0, markerOffset + marker.Length);
        Assert.That(header, Does.Contain("WIDTH " + expectedWidth + "\n"));
        Assert.That(header, Does.Contain("HEIGHT " + expectedHeight + "\n"));
        Assert.That(header, Does.Contain("DEPTH 4\n"));
        int pixelOffset = markerOffset + marker.Length;
        int pixelLength = expectedWidth * expectedHeight * 4;
        Assert.That(pam.Length - pixelOffset, Is.EqualTo(pixelLength));
        byte[] pixels = new byte[pixelLength];
        Array.Copy(pam, pixelOffset, pixels, 0, pixelLength);
        return pixels;
    }

    static int ReadPamDepth(byte[] pam)
    {
        byte[] marker = Encoding.ASCII.GetBytes("ENDHDR\n");
        int markerOffset = FindSequence(pam, marker);
        string header = Encoding.ASCII.GetString(pam, 0, markerOffset + marker.Length);
        if (header.Contains("DEPTH 4\n", StringComparison.Ordinal))
        {
            return 4;
        }

        if (header.Contains("DEPTH 3\n", StringComparison.Ordinal))
        {
            return 3;
        }

        return 0;
    }

    static int FindSequence(byte[] data, byte[] sequence)
    {
        int offset = 0;
        while (offset <= data.Length - sequence.Length)
        {
            int index = 0;
            while (index < sequence.Length && data[offset + index] == sequence[index])
            {
                index = index + 1;
            }

            if (index == sequence.Length)
            {
                return offset;
            }

            offset = offset + 1;
        }

        return -1;
    }

    static string JoinDiagnostics(List<WebpDiagnostic> diagnostics)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (index > 0)
            {
                builder.Append("; ");
            }

            builder.Append(diagnostics[index].Message);
            index = index + 1;
        }

        return builder.ToString();
    }
}
