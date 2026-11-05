using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSampleTileExtractToolTests
{
    [Test]
    [NonParallelizable]
    public void TileExtractTool_WritesSelectedRawTileBytes()
    {
        string root = FindRepositoryRoot();
        string inputPath = CreateTempPath(".tif");
        string outputPath = CreateTempPath(".bin");
        try
        {
            File.WriteAllBytes(inputPath, CreateTiledFileBytes());

            ProcessResult result = RunTool(root, inputPath, "0", "2", outputPath);

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);
            Assert.That(File.ReadAllBytes(outputPath), Is.EqualTo(new byte[] { 12 }));
            Assert.That(result.StandardOutput, Does.Contain("Extracted raw tile"));
            Assert.That(result.StandardOutput, Does.Contain("ImageIndex: 0"));
            Assert.That(result.StandardOutput, Does.Contain("TileIndex: 2"));
            Assert.That(result.StandardOutput, Does.Contain("Bytes: 1"));
            Assert.That(result.StandardError, Is.EqualTo(""));
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
        }
    }

    [Test]
    [NonParallelizable]
    public void TileExtractTool_ReportsInvalidTileIndex()
    {
        string root = FindRepositoryRoot();
        string inputPath = CreateTempPath(".tif");
        string outputPath = CreateTempPath(".bin");
        try
        {
            File.WriteAllBytes(inputPath, CreateTiledFileBytes());

            ProcessResult result = RunTool(root, inputPath, "0", "9", outputPath);

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(File.Exists(outputPath), Is.False);
            Assert.That(result.StandardError, Does.Contain("Failed to read tile"));
            Assert.That(result.StandardError, Does.Contain("InvalidOffset"));
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
        }
    }

    static ProcessResult RunTool(string root, string inputPath, string imageIndex, string tileIndex, string outputPath)
    {
        string projectPath = Path.Combine(root, "samples", "TiffTileExtract", "TiffTileExtract.csproj");
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.WorkingDirectory = root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(inputPath);
        startInfo.ArgumentList.Add(imageIndex);
        startInfo.ArgumentList.Add(tileIndex);
        startInfo.ArgumentList.Add(outputPath);

        Process process = Process.Start(startInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(120000);
        if (!exited)
        {
            process.Kill(true);
            Assert.Fail("Tile extract sample timed out.");
        }

        return new ProcessResult(process.ExitCode, outputTask.Result, errorTask.Result);
    }

    static byte[] CreateTiledFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateTileTags()));
        writer.WriteRawTile(imageIndex, 0, new byte[] { 10 });
        writer.WriteRawTile(imageIndex, 1, new byte[] { 11 });
        writer.WriteRawTile(imageIndex, 2, new byte[] { 12 });
        writer.WriteRawTile(imageIndex, 3, new byte[] { 13 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateTileTags()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileWidth, TiffDataType.Long, 1),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.TileLength, TiffDataType.Long, 1)
        };
    }

    static string CreateTempPath(string extension)
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + extension);
        DeleteIfExists(path);
        return path;
    }

    static string FindRepositoryRoot()
    {
        string directory = TestContext.CurrentContext.TestDirectory;

        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "tiff.slnx")) && Directory.Exists(Path.Combine(directory, "samples")))
            {
                return directory;
            }

            DirectoryInfo? parent = Directory.GetParent(directory);
            if (parent == null)
            {
                break;
            }

            directory = parent.FullName;
        }

        return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
    }

    static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
