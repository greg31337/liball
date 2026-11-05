using System.Buffers.Binary;
using System.Diagnostics;

namespace avif.Tests;

[NonParallelizable]
[Category("ReferenceCodec")]
public class EncoderInteroperabilityTests
{
    [OneTimeSetUp]
    public void RequireCurrentAvifTools()
    {
        if (!CanRun("ffmpeg", "-version")
            || !CanRun("heif-enc", "--version")
            || !CanRun("heif-convert", "--version")
            || !CanRun("aomdec", "--help"))
        {
            Assert.Ignore(
                "ffmpeg, heif-enc, heif-convert, and aomdec are required for encoder interoperability tests.");
        }
    }

    [Test]
    public void CanonicallyRewrittenStillDecodesInLibheifAndLibaom()
    {
        string directory = CreateDirectory();
        try
        {
            string input = CreatePng(directory, "still.png", 16, 16, "red");
            string generated = Path.Combine(directory, "generated-still.avif");
            EncodeAvif(generated, new string[] { input });
            AvifRemuxResult rewritten = Rewrite(File.ReadAllBytes(generated));
            string output = Path.Combine(directory, "decoded-still.png");
            File.WriteAllBytes(
                Path.Combine(directory, "rewritten-still.avif"),
                rewritten.Data);
            DecodeAvif(
                Path.Combine(directory, "rewritten-still.avif"),
                output,
                false,
                false);
            AvifDocument document = ParseCompatible(rewritten.Data);
            int decodedItems = DecodeCodedItemsWithAom(
                rewritten.Data,
                directory,
                "still");

            Assert.Multiple(() =>
            {
                Assert.That(rewritten.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(rewritten.PreservedSourceBytes, Is.False);
                Assert.That((rewritten.OutputValidation ?? throw new InvalidOperationException("Invalid internal state.")).Code,
                    Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetItemCount(), Is.EqualTo(1));
                Assert.That((document.GetItem(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsCodedImage(), Is.True);
                AssertPng(output, 16, 16);
                Assert.That(decodedItems, Is.EqualTo(1));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GeneratedSpatialLayerOutputsDecodeAtEveryPlannedResolution()
    {
        string directory = CreateDirectory();
        try
        {
            Av1EncoderTileLayerPlanRequest request =
                new Av1EncoderTileLayerPlanRequest();
            request.Width = 16;
            request.Height = 16;
            request.Progressive = true;
            request.SpatialLayers = new Av1EncoderSpatialLayer[]
            {
                new Av1EncoderSpatialLayer(8, 8),
                new Av1EncoderSpatialLayer(16, 16)
            };
            Av1EncoderTileLayerPlan plan =
                Av1EncoderTileLayerPlanner.Plan(request);
            string low = CreatePng(directory, "layer-01.png", 8, 8, "green");
            string high = CreatePng(
                directory,
                "layer-02.png",
                16,
                16,
                "yellow");
            string generated = Path.Combine(directory, "generated-layers.avif");
            EncodeAvif(generated, new string[] { low, high });
            AvifRemuxResult rewritten = Rewrite(File.ReadAllBytes(generated));
            string rewrittenPath = Path.Combine(
                directory,
                "rewritten-layers.avif");
            File.WriteAllBytes(rewrittenPath, rewritten.Data);
            string output = Path.Combine(directory, "decoded-layer.png");
            DecodeAvif(rewrittenPath, output, false, false);
            AvifDocument document = ParseCompatible(rewritten.Data);
            int decodedItems = DecodeCodedItemsWithAom(
                rewritten.Data,
                directory,
                "layer");

            Assert.Multiple(() =>
            {
                Assert.That(plan.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(plan.OperatingPoints, Has.Length.EqualTo(2));
                Assert.That(plan.LayerFrames, Has.Length.EqualTo(2));
                Assert.That(rewritten.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetItemCount(), Is.EqualTo(2));
                AssertPng(Path.Combine(directory, "decoded-layer-1.png"), 8, 8);
                AssertPng(Path.Combine(directory, "decoded-layer-2.png"), 16, 16);
                Assert.That(decodedItems, Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void CanonicallyRewrittenGridAndEveryTileDecodeIndependently()
    {
        string directory = CreateDirectory();
        try
        {
            string input = CreatePng(directory, "grid.png", 16, 16, "blue");
            string generated = Path.Combine(directory, "generated-grid.avif");
            Run(
                "heif-enc",
                new List<string>
                {
                    "--avif",
                    "--quality", "80",
                    "--cut-tiles", "8",
                    "--output", generated,
                    input
                },
                30000);
            AvifRemuxResult rewritten = Rewrite(File.ReadAllBytes(generated));
            string rewrittenPath = Path.Combine(directory, "rewritten-grid.avif");
            File.WriteAllBytes(rewrittenPath, rewritten.Data);
            string output = Path.Combine(directory, "decoded-grid.png");
            DecodeAvif(rewrittenPath, output, false, false);
            DecodeAvif(
                rewrittenPath,
                Path.Combine(directory, "decoded-tile.png"),
                false,
                true);
            AvifDocument document = ParseCompatible(rewritten.Data);
            AvifImageItem? primary = FindPrimary(document);
            int decodedItems = DecodeCodedItemsWithAom(
                rewritten.Data,
                directory,
                "grid");

            Assert.Multiple(() =>
            {
                Assert.That(rewritten.Code, Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(primary, Is.Not.Null);
                Assert.That((primary ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType(), Is.EqualTo("grid"));
                Assert.That(primary.IsDerivedImage(), Is.True);
                Assert.That(document.GetItemCount(), Is.EqualTo(5));
                AssertPng(output, 16, 16);
                AssertPng(Path.Combine(directory, "decoded-tile-0-0.png"), 8, 8);
                AssertPng(Path.Combine(directory, "decoded-tile-0-1.png"), 8, 8);
                AssertPng(Path.Combine(directory, "decoded-tile-1-0.png"), 8, 8);
                AssertPng(Path.Combine(directory, "decoded-tile-1-1.png"), 8, 8);
                Assert.That(decodedItems, Is.EqualTo(4));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void GeneratedTimedSequenceDecodesInLibheifAndLibaom()
    {
        string directory = CreateDirectory();
        try
        {
            CreatePng(directory, "frame-01.png", 16, 16, "magenta");
            CreatePng(directory, "frame-02.png", 16, 16, "cyan");
            string generated = Path.Combine(directory, "generated-sequence.avif");
            Run(
                "heif-enc",
                new List<string>
                {
                    "--avif",
                    "--quality", "80",
                    "--sequence",
                    "--max-frames", "2",
                    "--fps", "1",
                    "--output", generated,
                    Path.Combine(directory, "frame-01.png")
                },
                30000);
            byte[] source = File.ReadAllBytes(generated);
            AvifDocument document = ParseCompatible(source);
            AvifMovie? movie = document.GetMovie();
            AvifTrack? track = movie == null ? null : movie.GetVisualTrack(0);
            string output = Path.Combine(directory, "decoded-sequence.png");
            DecodeAvif(generated, output, true, false);
            int decodedFrames = DecodeTrackWithAom(
                source,
                track,
                directory,
                "sequence");

            Assert.Multiple(() =>
            {
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(movie, Is.Not.Null);
                Assert.That((movie ?? throw new InvalidOperationException("Invalid internal state.")).GetVisualTrackCount(), Is.EqualTo(1));
                Assert.That(track, Is.Not.Null);
                Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetSampleCount(), Is.EqualTo(2));
                AssertPng(Path.Combine(directory, "decoded-sequence-1.png"), 16, 16);
                AssertPng(Path.Combine(directory, "decoded-sequence-2.png"), 16, 16);
                Assert.That(decodedFrames, Is.EqualTo(2));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    static AvifRemuxResult Rewrite(byte[] source)
    {
        AvifRewriteOptions options = new AvifRewriteOptions();
        options.HeaderFormat = AvifHeaderFormat.Full;
        options.PreserveUnknownBoxes = false;
        options.Canonicalize = true;
        options.Limits.MaxSourceBytes = 16L << 20;
        options.Limits.MaxTemporaryBytes = 16L << 20;
        options.Limits.MaxDecodedBytes = 16L << 20;
        AvifRemuxResult result = AvifRemuxer.Rewrite(source, options);
        Assert.That(
            result.Code,
            Is.EqualTo(AvifResultCode.Success),
            "input=" + (result.InputValidation == null
                ? "null"
                : result.InputValidation.Code.ToString()
                    + " " + FirstDiagnostic(result.InputValidation))
                + " output=" + (result.OutputValidation == null
                    ? "null"
                    : result.OutputValidation.Code.ToString()
                        + " " + FirstDiagnostic(result.OutputValidation)));
        return result;
    }

    static AvifDocument ParseCompatible(byte[] source)
    {
        AvifValidationOptions options = new AvifValidationOptions();
        options.Mode = AvifValidationMode.Compatible;
        options.ValidateEncodedPayload = false;
        options.Limits.MaxSourceBytes = 16L << 20;
        options.Limits.MaxTemporaryBytes = 16L << 20;
        AvifDocument document = AvifDocument.Parse(source, options);
        Assert.That(
            document.GetCode(),
            Is.EqualTo(AvifResultCode.Success),
            document.DumpFindings());
        return document;
    }

    static string FirstDiagnostic(AvifContainerValidationResult validation)
    {
        return validation.Diagnostics.Diagnostics.Count == 0
            ? "no diagnostic"
            : (validation.Diagnostics.Get(0) ?? throw new InvalidOperationException("Invalid internal state.")).Message;
    }

    static AvifImageItem? FindPrimary(AvifDocument document)
    {
        int index = 0;
        while (index < document.GetItemCount())
        {
            if ((document.GetItem(index) ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary())
            {
                return document.GetItem(index);
            }
            index++;
        }
        return null;
    }

    static int DecodeCodedItemsWithAom(
        byte[] source,
        string directory,
        string prefix)
    {
        AvifSecurityLimits limits = new AvifSecurityLimits();
        limits.MaxSourceBytes = 16L << 20;
        limits.MaxTemporaryBytes = 16L << 20;
        MetaParseResult parsed = MetaBoxParser.Parse(source, limits);
        Assert.That(parsed.Code, Is.EqualTo(AvifResultCode.Success));
        int decoded = 0;
        int index = 0;
        while (index < (parsed.Meta ?? throw new InvalidOperationException("Invalid internal state.")).GetItemCount())
        {
            ParsedItem? item = parsed.Meta.GetItem(index);
            if ((item ?? throw new InvalidOperationException("Invalid internal state.")).GetItemType() == "av01")
            {
                EncodedItemExtractionResult extraction =
                    parsed.Meta.ExtractEncodedItem(source, item.GetItemId());
                Assert.That(extraction.Code, Is.EqualTo(AvifResultCode.Success));
                string obu = Path.Combine(
                    directory,
                    prefix + "-" + decoded + ".obu");
                string raw = Path.Combine(
                    directory,
                    prefix + "-" + decoded + ".yuv");
                File.WriteAllBytes(obu, (extraction.Data ?? throw new InvalidOperationException("Invalid internal state.")).GetSpan().ToArray());
                extraction.Data.Close();
                DecodeObu(obu, raw);
                Assert.That(new FileInfo(raw).Length, Is.GreaterThan(0));
                decoded++;
            }
            index++;
        }
        return decoded;
    }

    static int DecodeTrackWithAom(
        byte[] source,
        AvifTrack? track,
        string directory,
        string prefix)
    {
        Assert.That(track, Is.Not.Null);
        List<byte> payload = new List<byte>();
        int index = 0;
        while (index < (track ?? throw new InvalidOperationException("Invalid internal state.")).GetSampleCount())
        {
            AvifSample? sample = track.GetSample(index);
            Assert.That((sample ?? throw new InvalidOperationException("Invalid internal state.")).GetOffset(), Is.LessThanOrEqualTo((ulong)source.Length));
            Assert.That(sample.GetSize(),
                Is.LessThanOrEqualTo((ulong)source.Length - sample.GetOffset()));
            int offset = checked((int)sample.GetOffset());
            int size = checked((int)sample.GetSize());
            payload.AddRange(source.AsSpan(offset, size).ToArray());
            index++;
        }
        string obu = Path.Combine(directory, prefix + ".obu");
        string raw = Path.Combine(directory, prefix + ".yuv");
        File.WriteAllBytes(obu, payload.ToArray());
        DecodeObu(obu, raw);
        long frameBytes = track.GetWidth() * track.GetHeight() * 3L / 2;
        Assert.That(frameBytes, Is.GreaterThan(0));
        return checked((int)(new FileInfo(raw).Length / frameBytes));
    }

    static void DecodeObu(string input, string output)
    {
        Run(
            "aomdec",
            new List<string>
            {
                "--codec=av1",
                "--rawvideo",
                "--output=" + output,
                input
            },
            30000);
    }

    static string CreatePng(
        string directory,
        string name,
        int width,
        int height,
        string color)
    {
        string path = Path.Combine(directory, name);
        Run(
            "ffmpeg",
            new List<string>
            {
                "-hide_banner",
                "-loglevel", "error",
                "-f", "lavfi",
                "-i", "color=c=" + color + ":s=" + width + "x" + height + ":d=1",
                "-frames:v", "1",
                "-threads", "1",
                path
            },
            30000);
        return path;
    }

    static void EncodeAvif(string output, string[] inputs)
    {
        List<string> arguments = new List<string>
        {
            "--avif",
            "--quality", "80",
            "--output", output
        };
        arguments.AddRange(inputs);
        Run("heif-enc", arguments, 30000);
    }

    static void DecodeAvif(
        string input,
        string output,
        bool sequence,
        bool tiles)
    {
        List<string> arguments = new List<string> { "--quiet" };
        if (sequence)
        {
            arguments.Add("--sequence");
        }
        if (tiles)
        {
            arguments.Add("--tiles");
        }
        arguments.Add(input);
        arguments.Add(output);
        Run("heif-convert", arguments, 30000);
    }

    static void AssertPng(string path, int width, int height)
    {
        Assert.That(File.Exists(path), Is.True, path);
        byte[] data = File.ReadAllBytes(path);
        Assert.That(data.Length, Is.GreaterThanOrEqualTo(24), path);
        Assert.That(data.AsSpan(0, 8).ToArray(), Is.EqualTo(new byte[]
        {
            137, 80, 78, 71, 13, 10, 26, 10
        }), path);
        Assert.That(BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(16, 4)),
            Is.EqualTo(width), path);
        Assert.That(BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(20, 4)),
            Is.EqualTo(height), path);
    }

    static string CreateDirectory()
    {
        string directory = Path.Combine(
            Path.GetTempPath(),
            "avif-t100-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
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
        int index = 0;
        while (index < arguments.Count)
        {
            info.ArgumentList.Add(arguments[index]);
            index++;
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
