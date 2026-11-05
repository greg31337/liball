using System;
using System.IO;
using System.Linq;
using image;
using NUnit.Framework;

namespace image.Tests;

[TestFixture]
public class PublicScannerCompletionTests
{
    [Test]
    public void ScanDirectory_EmptyDirectoryReturnsAnEmptySuccessfulBatch()
    {
        string root = ImageMetadataScannerTests.CreateTemporaryDirectory();
        try
        {
            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, new ImageMetadataScanOptions());

            Assert.That(result.Files, Is.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_MixedSupportedUnsupportedAndUnreadableFilesRemainDeterministicallyOrdered()
    {
        string root = ImageMetadataScannerTests.CreateTemporaryDirectory();
        FileStream? locked = null;
        try
        {
            string child = Directory.CreateDirectory(Path.Combine(root, "child")).FullName;
            string zQoi = ImageMetadataScannerTests.WriteQoi(root, "z.qoi");
            string aUnknown = Path.GetFullPath(Path.Combine(root, "a.unknown"));
            File.WriteAllBytes(aUnknown, new byte[] { 1, 2, 3, 4 });
            string nestedQoi = ImageMetadataScannerTests.WriteQoi(child, "nested.qoi");
            string lockedQoi = ImageMetadataScannerTests.WriteQoi(root, "m-locked.qoi");
            locked = new FileStream(lockedQoi, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            ImageMetadataScanOptions options = new ImageMetadataScanOptions();
            options.Recursive = true;

            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, options);
            string[] expected = new string[] { zQoi, aUnknown, nestedQoi, lockedQoi };
            Array.Sort(expected, StringComparer.Ordinal);

            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Files.Select(item => item.Path), Is.EqualTo(expected));
            Assert.That(result.Files.Single(item => item.Path == zQoi).Document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
            Assert.That(result.Files.Single(item => item.Path == nestedQoi).Document.Format.Kind, Is.EqualTo(ImageFormatKind.Qoi));
            Assert.That(result.Files.Single(item => item.Path == aUnknown).Document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
            Assert.That(result.Files.Single(item => item.Path == aUnknown).IsSuccess, Is.True);
            ImageMetadataScanFileResult unavailable = result.Files.Single(item => item.Path == lockedQoi);
            Assert.That(unavailable.IsSuccess, Is.False);
            Assert.That(unavailable.Document.Format.Kind, Is.EqualTo(ImageFormatKind.Unknown));
            Assert.That(unavailable.Diagnostics.Single().Code, Is.EqualTo("scanner.file.read-failed"));
        }
        finally
        {
            if (locked != null)
            {
                locked.Dispose();
            }
            Directory.Delete(root, true);
        }
    }

    [Test]
    public void ScanDirectory_RecursiveTraversalDoesNotFollowDirectorySymlinkCycles()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Ignore("Directory symlink creation is not consistently available on Windows test hosts.");
        }

        string root = ImageMetadataScannerTests.CreateTemporaryDirectory();
        try
        {
            string child = Directory.CreateDirectory(Path.Combine(root, "child")).FullName;
            string rootFile = ImageMetadataScannerTests.WriteQoi(root, "root.qoi");
            string childFile = ImageMetadataScannerTests.WriteQoi(child, "child.qoi");
            Directory.CreateSymbolicLink(Path.Combine(child, "back-to-root"), root);
            ImageMetadataScanOptions options = new ImageMetadataScanOptions();
            options.Recursive = true;
            options.Extensions = new string[] { ".qoi" };

            ImageMetadataScanResult result = new ImageMetadataScanner().ScanDirectory(root, options);
            string[] expected = new string[] { rootFile, childFile };
            Array.Sort(expected, StringComparer.Ordinal);

            Assert.That(result.Diagnostics, Is.Empty);
            Assert.That(result.Files.Select(item => item.Path), Is.EqualTo(expected));
            Assert.That(result.Files.Select(item => item.Path).Distinct().Count(), Is.EqualTo(2));
            Assert.That(result.Files.Select(item => item.IsSuccess), Is.All.True);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
