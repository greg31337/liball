using System.Diagnostics;
using System.Text.RegularExpressions;

namespace avif.Tests;

[NonParallelizable]
[Category("ReferenceCodec")]
public class ContainerModelDifferentialTests
{
    [Test]
    public void StillItemAndPropertiesMatchLibavifLibheifAndBoxDump()
    {
        RequireTool("avifdec", "--version");
        RequireTool("heif-info", "--version");
        string path = GetVectorPath(
            "fox.profile0.8bpc.yuv420.odd-width.odd-height.avif");
        AvifDocument document = ParseCompatible(File.ReadAllBytes(path));
        AvifImageItem? item = document.FindItem(1);
        string libavif = RunCapture(
            "avifdec",
            new string[] { "--info", path });
        string libheif = RunCapture(
            "heif-info",
            new string[] { path });
        string boxes = RunCapture(
            "heif-info",
            new string[] { "--dump-boxes", path });

        Assert.Multiple(() =>
        {
            Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
            Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetMajorBrand(), Is.EqualTo("avif"));
            Assert.That(document.GetItemCount(), Is.EqualTo(1));
            Assert.That(item, Is.Not.Null);
            Assert.That((item ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary(), Is.True);
            Assert.That(item.GetItemType(), Is.EqualTo("av01"));
            Assert.That(item.GetPixelWidth(), Is.EqualTo(1203));
            Assert.That(item.GetPixelHeight(), Is.EqualTo(799));
            Assert.That(item.GetPixelBitDepth(), Is.EqualTo(8));
            AssertPropertyTypes(
                item,
                new string[] { "pasp", "ispe", "pixi", "av1C", "colr" });

            Assert.That(libavif, Does.Contain("Resolution     : 1203x799"));
            Assert.That(libavif, Does.Contain("Bit Depth      : 8"));
            Assert.That(libavif, Does.Contain("Format         : YUV420"));
            Assert.That(libavif, Does.Contain("Color Primaries: 1"));
            Assert.That(libavif, Does.Contain("Transfer Char. : 13"));
            Assert.That(libavif, Does.Contain("Matrix Coeffs. : 6"));

            Assert.That(libheif, Does.Contain("image: 1203x799 (id=1), primary"));
            Assert.That(libheif, Does.Contain("colorspace: YCbCr, 4:2:0"));
            Assert.That(libheif, Does.Contain("bit depth: 8"));
            Assert.That(boxes, Does.Contain("item_ID: 1"));
            Assert.That(boxes, Does.Contain("item_type: av01"));
            Assert.That(boxes, Does.Contain("image width: 1203"));
            Assert.That(boxes, Does.Contain("image height: 799"));
            Assert.That(boxes, Does.Contain("bits_per_channel: 8,8,8"));
            Assert.That(boxes, Does.Contain("seq_profile: 0"));
            Assert.That(boxes, Does.Contain("seq_level_idx_0: 5"));
            Assert.That(boxes, Does.Contain("colour_primaries: 1"));
            Assert.That(boxes, Does.Contain("transfer_characteristics: 13"));
            Assert.That(boxes, Does.Contain("matrix_coefficients: 6"));
        });
    }

    [Test]
    public void GridItemReferencesMatchLibheifBoxDump()
    {
        RequireTool("ffmpeg", "-version");
        RequireTool("heif-enc", "--version");
        RequireTool("heif-info", "--version");
        string directory = CreateDirectory();
        try
        {
            string pngPath = Path.Combine(directory, "grid.png");
            string avifPath = Path.Combine(directory, "grid.avif");
            CreatePng(pngPath, 16, 16);
            RunCapture(
                "heif-enc",
                new string[]
                {
                    "--avif",
                    "--quality", "80",
                    "--cut-tiles", "8",
                    "--output", avifPath,
                    pngPath
                });

            AvifDocument document = ParseCompatible(File.ReadAllBytes(avifPath));
            AvifImageItem? grid = FindPrimary(document);
            string boxes = RunCapture(
                "heif-info",
                new string[] { "--dump-boxes", avifPath });
            Match dumpReference = Regex.Match(
                boxes,
                "reference with type 'dimg' from ID: ([0-9]+) to IDs: ([0-9 ]+)");
            Assert.That(dumpReference.Success, Is.True, boxes);
            uint dumpFrom = uint.Parse(dumpReference.Groups[1].Value);
            uint[] dumpTargets = ParseIds(dumpReference.Groups[2].Value);
            AvifItemReference? reference = (grid ?? throw new InvalidOperationException("Invalid internal state.")).GetOutgoingReference(0);

            Assert.Multiple(() =>
            {
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That(document.GetItemCount(), Is.EqualTo(5));
                Assert.That(grid.GetItemId(), Is.EqualTo(dumpFrom));
                Assert.That(grid.GetItemType(), Is.EqualTo("grid"));
                Assert.That(grid.IsPrimary(), Is.True);
                Assert.That((reference ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(), Is.EqualTo("dimg"));
                Assert.That(reference.GetKind(),
                    Is.EqualTo(AvifItemReferenceKind.DerivedImage));
                Assert.That(reference.GetFromItemId(), Is.EqualTo(dumpFrom));
                Assert.That(reference.GetTargetCount(), Is.EqualTo(dumpTargets.Length));
                AssertReferenceTargets(reference, dumpTargets);
                Assert.That(boxes, Does.Contain("rows: 2"));
                Assert.That(boxes, Does.Contain("columns: 2"));
                Assert.That(boxes, Does.Contain("output width: 16"));
                Assert.That(boxes, Does.Contain("output height: 16"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Test]
    public void SequenceTimingMatchesLibavifLibheifFfprobeAndBoxDump()
    {
        RequireTool("avifdec", "--version");
        RequireTool("ffmpeg", "-version");
        RequireTool("ffprobe", "-version");
        RequireTool("heif-enc", "--version");
        RequireTool("heif-info", "--version");
        string directory = CreateDirectory();
        try
        {
            string framePattern = Path.Combine(directory, "frame-%02d.png");
            string firstFrame = Path.Combine(directory, "frame-01.png");
            string avifPath = Path.Combine(directory, "sequence.avif");
            RunCapture(
                "ffmpeg",
                new string[]
                {
                    "-hide_banner",
                    "-loglevel", "error",
                    "-f", "lavfi",
                    "-i", "testsrc2=s=16x16:r=2:d=1",
                    "-frames:v", "2",
                    "-threads", "1",
                    framePattern
                });
            RunCapture(
                "heif-enc",
                new string[]
                {
                    "--avif",
                    "--quality", "80",
                    "--sequence",
                    "--max-frames", "2",
                    "--fps", "2",
                    "--output", avifPath,
                    firstFrame
                });

            AvifDocument document = ParseCompatible(File.ReadAllBytes(avifPath));
            AvifMovie? movie = document.GetMovie();
            AvifTrack? track = (movie ?? throw new InvalidOperationException("Invalid internal state.")).GetVisualTrack(0);
            string libavif = RunCapture(
                "avifdec",
                new string[] { "--info", avifPath });
            string libheif = RunCapture(
                "heif-info",
                new string[] { avifPath });
            string boxes = RunCapture(
                "heif-info",
                new string[] { "--dump-boxes", avifPath });
            string probe = RunCapture(
                "ffprobe",
                new string[]
                {
                    "-v", "error",
                    "-select_streams", "v:0",
                    "-show_entries",
                    "stream=width,height,avg_frame_rate,time_base,duration_ts,duration,nb_frames",
                    "-of", "default=noprint_wrappers=1",
                    avifPath
                });

            Assert.Multiple(() =>
            {
                Assert.That(document.GetCode(), Is.EqualTo(AvifResultCode.Success));
                Assert.That((document.GetFileInfo() ?? throw new InvalidOperationException("Invalid internal state.")).GetMajorBrand(), Is.EqualTo("avis"));
                Assert.That(movie, Is.Not.Null);
                Assert.That(movie.GetTimescale(), Is.EqualTo(90000));
                Assert.That(movie.GetDuration(), Is.EqualTo(90000));
                Assert.That(movie.GetDurationSeconds(), Is.EqualTo(1));
                Assert.That(movie.GetVisualTrackCount(), Is.EqualTo(1));
                Assert.That((track ?? throw new InvalidOperationException("Invalid internal state.")).GetTrackId(), Is.EqualTo(1));
                Assert.That(track.GetWidth(), Is.EqualTo(16));
                Assert.That(track.GetHeight(), Is.EqualTo(16));
                Assert.That(track.GetMediaTimescale(), Is.EqualTo(90000));
                Assert.That(track.GetMediaDuration(), Is.EqualTo(90000));
                Assert.That(track.GetSampleCount(), Is.EqualTo(2));
                Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.Zero);
                Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(45000));
                Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDecodeTime(), Is.EqualTo(45000));
                Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).GetDuration(), Is.EqualTo(45000));
                Assert.That((track.GetSample(0) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.True);
                Assert.That((track.GetSample(1) ?? throw new InvalidOperationException("Invalid internal state.")).IsSync(), Is.False);

                Assert.That(libavif, Does.Contain(
                    "90000 timescales per second, 1.00 seconds (90000 timescales), 2 frames"));
                Assert.That(libavif, Does.Contain(
                    "duration 0.50 (45000 timescales)"));
                Assert.That(libheif, Does.Contain("sequence time scale: 90000 Hz"));
                Assert.That(libheif, Does.Contain("sequence duration: 1 seconds"));
                Assert.That(libheif, Does.Contain("resolution: 16x16"));
                Assert.That(boxes, Does.Contain("timescale: 90000"));
                Assert.That(boxes, Does.Contain("[0] : cnt=2, delta=45000"));
                Assert.That(boxes, Does.Contain("sample count: 2"));
                Assert.That(boxes, Does.Contain("[0] : 1"));
                Assert.That(probe, Does.Contain("width=16"));
                Assert.That(probe, Does.Contain("height=16"));
                Assert.That(probe, Does.Contain("avg_frame_rate=2/1"));
                Assert.That(probe, Does.Contain("time_base=1/90000"));
                Assert.That(probe, Does.Contain("duration_ts=90000"));
                Assert.That(probe, Does.Contain("duration=1.000000"));
                Assert.That(probe, Does.Contain("nb_frames=2"));
            });
        }
        finally
        {
            Directory.Delete(directory, true);
        }
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

    static AvifImageItem? FindPrimary(AvifDocument document)
    {
        int index = 0;
        while (index < document.GetItemCount())
        {
            AvifImageItem? item = document.GetItem(index);
            if ((item ?? throw new InvalidOperationException("Invalid internal state.")).IsPrimary())
            {
                return item;
            }
            index++;
        }
        return null;
    }

    static void AssertPropertyTypes(
        AvifImageItem item,
        string[] expectedTypes)
    {
        Assert.That(item.GetPropertyCount(), Is.EqualTo(expectedTypes.Length));
        int index = 0;
        while (index < expectedTypes.Length)
        {
            Assert.That(
                (item.GetProperty(index) ?? throw new InvalidOperationException("Invalid internal state.")).GetTypeName(),
                Is.EqualTo(expectedTypes[index]),
                "property index " + index);
            index++;
        }
    }

    static void AssertReferenceTargets(
        AvifItemReference reference,
        uint[] expectedTargets)
    {
        int index = 0;
        while (index < expectedTargets.Length)
        {
            Assert.That(
                reference.GetTargetItemId(index),
                Is.EqualTo(expectedTargets[index]),
                "reference target " + index);
            index++;
        }
    }

    static uint[] ParseIds(string value)
    {
        string[] fields = value.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries);
        uint[] result = new uint[fields.Length];
        int index = 0;
        while (index < fields.Length)
        {
            result[index] = uint.Parse(fields[index]);
            index++;
        }
        return result;
    }

    static void CreatePng(string path, int width, int height)
    {
        RunCapture(
            "ffmpeg",
            new string[]
            {
                "-hide_banner",
                "-loglevel", "error",
                "-f", "lavfi",
                "-i", "testsrc2=s=" + width + "x" + height + ":d=1",
                "-frames:v", "1",
                "-threads", "1",
                path
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

    static string RunCapture(string fileName, string[] arguments)
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
        return standardOutput + standardError;
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
            "avif-t120-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }
}
