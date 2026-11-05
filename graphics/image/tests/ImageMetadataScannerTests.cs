using System;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class ImageMetadataScannerTests
{
    [Test]
    public void ScanOptionsAndResults_HaveInitializedDefaults()
    {
        ImageMetadataScanOptions options = new ImageMetadataScanOptions();
        ImageMetadataScanFileResult file = new ImageMetadataScanFileResult();
        ImageMetadataScanResult result = new ImageMetadataScanResult();

        Assert.That(options.Recursive, Is.False);
        Assert.That(options.Extensions, Is.Empty);
        Assert.That(options.Formats, Is.Empty);
        Assert.That(options.ReaderOptions, Is.Not.Null);
        Assert.That(file.Path, Is.Empty);
        Assert.That(file.IsSuccess, Is.False);
        Assert.That(file.Document, Is.Not.Null);
        Assert.That(file.Diagnostics, Is.Empty);
        Assert.That(result.Files, Is.Empty);
        Assert.That(result.Diagnostics, Is.Empty);
    }

    [Test]
    public void ScanDirectory_FiltersExtensionsAndSupportsRecursiveStableOrdering()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            string child = Directory.CreateDirectory(Path.Combine(root, "sub")).FullName;
            string a = WriteQoi(root, "a.qoi");
            string z = WriteQoi(root, "z.QOI");
            string nested = WriteQoi(child, "m.qoi");
            WriteQoi(root, "ignored.bin");
            ImageMetadataScanOptions options = new ImageMetadataScanOptions();
            options.Extensions = new string[] { "qoi" };
            IImageMetadataScanner scanner = new ImageMetadataScanner();

            ImageMetadataScanResult shallow = scanner.ScanDirectory(root, options);

            Assert.That(shallow.Diagnostics, Is.Empty);
            Assert.That(shallow.Files.Select(item => item.Path), Is.EqualTo(new[] { a, z }));
            Assert.That(shallow.Files.Select(item => item.Document.Format.Kind), Is.All.EqualTo(ImageFormatKind.Qoi));
            Assert.That(shallow.Files.Select(item => item.IsSuccess), Is.All.True);

            options.Recursive = true;
            ImageMetadataScanResult recursive = scanner.ScanDirectory(root, options);
            string[] expected = new string[] { a, z, nested };
            Array.Sort(expected, StringComparer.Ordinal);

            Assert.That(recursive.Files.Select(item => item.Path), Is.EqualTo(expected));
            Assert.That(recursive.Files.Select(item => Path.IsPathFullyQualified(item.Path)), Is.All.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_FiltersUsingDetectedMagicInsteadOfFilenameExtension()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            string qoi = WriteQoi(root, "image.bin");
            File.WriteAllBytes(Path.Combine(root, "unknown.qoi.txt"), new byte[] { 1, 2, 3, 4 });
            ImageMetadataScanOptions options = new ImageMetadataScanOptions();
            options.Formats = new ImageFormatKind[] { ImageFormatKind.Qoi };

            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, options);

            Assert.That(result.Files, Has.Count.EqualTo(1));
            Assert.That(result.Files[0].Path, Is.EqualTo(qoi));
            Assert.That(result.Files[0].Document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_PassesReaderOptionsAndCopiesPerFileDiagnostics()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            WriteQoi(root, "limited.qoi");
            ImageMetadataScanOptions options = new ImageMetadataScanOptions();
            options.ReaderOptions.MaxFileBytes = 8;

            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, options);

            Assert.That(result.Files, Has.Count.EqualTo(1));
            Assert.That(result.Files[0].IsSuccess, Is.True);
            Assert.That(result.Files[0].Diagnostics.Any(item => item.Code == "reader.file.limit"), Is.True);
            Assert.That(result.Files[0].Diagnostics.Any(item => item.Code == "qoi.header.truncated"), Is.True);
            Assert.That(result.Files[0].Diagnostics, Is.EqualTo(result.Files[0].Document.Diagnostics));
            Assert.That(result.Diagnostics, Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_UnfilteredScanReturnsUnknownFileWithItsDiagnostics()
    {
        string root = CreateTemporaryDirectory();
        try
        {
            string path = Path.Combine(root, "unknown.data");
            File.WriteAllBytes(path, new byte[] { 1, 2, 3, 4 });

            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, null);

            Assert.That(result.Files, Has.Count.EqualTo(1));
            Assert.That(result.Files[0].Path, Is.EqualTo(path));
            Assert.That(result.Files[0].IsSuccess, Is.True);
            Assert.That(result.Files[0].Document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
            Assert.That(result.Files[0].Diagnostics.Any(item => item.Code == "format.unknown"), Is.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_InvalidDirectoryReturnsBatchDiagnostic()
    {
        string missing = Path.Combine(Path.GetTempPath(), "image-scanner-missing-" + Guid.NewGuid().ToString("N"));
        IImageMetadataScanner scanner = new ImageMetadataScanner();

        ImageMetadataScanResult empty = scanner.ScanDirectory(null, null);
        ImageMetadataScanResult notFound = scanner.ScanDirectory(missing, new ImageMetadataScanOptions());

        Assert.That(empty.Files, Is.Empty);
        Assert.That(empty.Diagnostics.Single().Code, Is.EqualTo("scanner.directory.empty"));
        Assert.That(notFound.Files, Is.Empty);
        Assert.That(notFound.Diagnostics.Single().Code, Is.EqualTo("scanner.directory.not-found"));
    }

    [Test]
    public void ReadFile_DisappearedPathReturnsPerFileFailureDiagnostic()
    {
        string missing = Path.Combine(Path.GetTempPath(), "image-scanner-file-missing-" + Guid.NewGuid().ToString("N") + ".qoi");

        ImageMetadataScanFileResult result = ImageMetadataScannerCore.ReadFile(missing, new ImageMetadataReaderOptions());

        Assert.That(result.Path, Is.EqualTo(missing));
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.Document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
        Assert.That(result.Diagnostics.Single().Code, Is.EqualTo("scanner.file.unavailable"));
    }

    public static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "image-scanner-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return Path.GetFullPath(path);
    }

    public static string WriteQoi(string directory, string name)
    {
        string path = Path.Combine(directory, name);
        File.WriteAllBytes(path, QoiMetadataParserTests.CreateQoi(2, 2, 3, 0, new byte[] { 254, 1, 2, 3 }));
        return Path.GetFullPath(path);
    }
}
