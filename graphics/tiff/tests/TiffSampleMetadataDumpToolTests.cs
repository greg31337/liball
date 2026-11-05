using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSampleMetadataDumpToolTests
{
    [Test]
    [NonParallelizable]
    public void MetadataDumpTool_PrintsHeaderImagesAndTags()
    {
        string root = FindRepositoryRoot();
        string path = CreateTempPath();
        try
        {
            File.WriteAllBytes(path, CreateClassicFileBytes());

            ProcessResult result = RunTool(root, path);

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("TIFF metadata dump"));
            Assert.That(result.StandardOutput, Does.Contain("FileKind: Classic"));
            Assert.That(result.StandardOutput, Does.Contain("Images: 1"));
            Assert.That(result.StandardOutput, Does.Contain("Image 0"));
            Assert.That(result.StandardOutput, Does.Contain("Width: 2"));
            Assert.That(result.StandardOutput, Does.Contain("ImageWidth"));
            Assert.That(result.StandardOutput, Does.Contain("ImageDescription"));
            Assert.That(result.StandardOutput, Does.Contain("metadata sample"));
            Assert.That(result.StandardError, Is.EqualTo(""));
        }
        finally
        {
            DeleteIfExists(path);
        }
    }

    [Test]
    [NonParallelizable]
    public void MetadataDumpTool_ReportsMissingFile()
    {
        string root = FindRepositoryRoot();
        string missingPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");

        ProcessResult result = RunTool(root, missingPath);

        Assert.That(result.ExitCode, Is.EqualTo(1));
        Assert.That(result.StandardError, Does.Contain("TIFF file does not exist"));
    }

    static ProcessResult RunTool(string root, string path)
    {
        string projectPath = Path.Combine(root, "samples", "TiffMetadataDump", "TiffMetadataDump.csproj");
        ProcessStartInfo startInfo = new ProcessStartInfo("dotnet");
        startInfo.WorkingDirectory = root;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--no-restore");
        startInfo.ArgumentList.Add("--");
        startInfo.ArgumentList.Add(path);

        Process process = Process.Start(startInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(120000);
        if (!exited)
        {
            process.Kill(true);
            Assert.Fail("Metadata dump sample timed out.");
        }

        return new ProcessResult(process.ExitCode, outputTask.Result, errorTask.Result);
    }

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 1, 2, 3, 4 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateStripTags()
    {
        return new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2),
            TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "metadata sample")
        };
    }

    static string CreateTempPath()
    {
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
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

public class ProcessResult
{
    public int ExitCode;
    public string StandardOutput;
    public string StandardError;

    public ProcessResult(int exitCode, string standardOutput, string standardError)
    {
        ExitCode = exitCode;
        StandardOutput = standardOutput;
        StandardError = standardError;
    }
}
