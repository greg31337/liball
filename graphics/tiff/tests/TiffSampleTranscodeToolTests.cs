using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace tiff.Tests;

[TestFixture]
public class TiffSampleTranscodeToolTests
{
    [Test]
    [NonParallelizable]
    public void TranscodeTool_CopiesStrippedImageMetadataAndRawDataToBigTiff()
    {
        string root = FindRepositoryRoot();
        string inputPath = CreateTempPath(".tif");
        string outputPath = CreateTempPath(".tif");
        try
        {
            File.WriteAllBytes(inputPath, CreateClassicFileBytes());

            ProcessResult result = RunTool(root, inputPath, outputPath, "--bigtiff");
            TiffFile outputFile = TiffFile.OpenRead(outputPath);
            TiffImage outputImage = outputFile.GetImage(0);
            TiffMetadataCopyResult metadata = TiffCopy.CopyMetadata(outputImage);

            Assert.That(result.ExitCode, Is.EqualTo(0), result.StandardError);
            Assert.That(result.StandardOutput, Does.Contain("Transcoded TIFF"));
            Assert.That(result.StandardOutput, Does.Contain("OutputKind: BigTiff"));
            Assert.That(outputFile.Success, Is.True);
            Assert.That(outputFile.GetHeader().FileKind, Is.EqualTo(TiffFileKind.BigTiff));
            Assert.That(outputFile.GetImageCount(), Is.EqualTo(1));
            Assert.That(outputImage.GetInfo().Width, Is.EqualTo(2));
            Assert.That(outputImage.GetInfo().Length, Is.EqualTo(2));
            Assert.That(outputImage.ReadRawStrip(0).Bytes, Is.EqualTo(new byte[] { 5, 6, 7, 8 }));
            Assert.That(FindValue(metadata.Values, TiffTagId.ImageDescription)!.Text, Is.EqualTo("transcode source"));
            Assert.That(FindValue(metadata.Values, TiffTagId.Software)!.Text, Is.EqualTo("sample-test"));
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
    public void TranscodeTool_ReportsMissingInput()
    {
        string root = FindRepositoryRoot();
        string inputPath = CreateTempPath(".tif");
        string outputPath = CreateTempPath(".tif");
        DeleteIfExists(inputPath);
        try
        {
            ProcessResult result = RunTool(root, inputPath, outputPath, "--classic");

            Assert.That(result.ExitCode, Is.EqualTo(1));
            Assert.That(result.StandardError, Does.Contain("TIFF file does not exist"));
            Assert.That(File.Exists(outputPath), Is.False);
        }
        finally
        {
            DeleteIfExists(inputPath);
            DeleteIfExists(outputPath);
        }
    }

    static ProcessResult RunTool(string root, string inputPath, string outputPath, string option)
    {
        string projectPath = Path.Combine(root, "samples", "TiffTranscode", "TiffTranscode.csproj");
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
        startInfo.ArgumentList.Add(outputPath);
        startInfo.ArgumentList.Add(option);

        Process process = Process.Start(startInfo)!;
        Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
        Task<string> errorTask = process.StandardError.ReadToEndAsync();
        bool exited = process.WaitForExit(120000);
        if (!exited)
        {
            process.Kill(true);
            Assert.Fail("Transcode sample timed out.");
        }

        return new ProcessResult(process.ExitCode, outputTask.Result, errorTask.Result);
    }

    static TiffTagValue? FindValue(TiffTagValue[] values, int tagId)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values[index].TagId == tagId)
            {
                return values[index];
            }

            index = index + 1;
        }

        return null;
    }

    static byte[] CreateClassicFileBytes()
    {
        TiffWriter writer = TiffWriter.Create();
        int imageIndex = writer.AddImage(TiffImageDefinition.Create(CreateStripTags()));
        writer.WriteRawStrip(imageIndex, 0, new byte[] { 5, 6, 7, 8 });
        writer.Flush();
        return writer.GetFileBytes();
    }

    static TiffTagValue[] CreateStripTags()
    {
        TiffTagValue[] tags = new TiffTagValue[]
        {
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageWidth, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.ImageLength, TiffDataType.Long, 2),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffTagValue.CreateUnsignedScalar(TiffTagId.RowsPerStrip, TiffDataType.Long, 2)
        };
        tags = TiffImageDefinition.SetTagValue(tags, TiffTagValue.CreateAscii(TiffTagId.ImageDescription, "transcode source"));
        tags = TiffImageDefinition.SetTagValue(tags, TiffTagValue.CreateAscii(TiffTagId.Software, "sample-test"));
        return tags;
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
