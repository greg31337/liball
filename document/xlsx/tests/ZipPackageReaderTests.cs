using System.IO.Compression;
using System.Text;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ZipPackageReaderTests
{
    [Test]
    public void ReadBytes_LoadsEntriesAndTracksByteCounts()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "[Content_Types].xml",
                "xl/workbook.xml"
            },
            new string[]
            {
                "<Types />",
                "<workbook />"
            });
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult result = reader.ReadBytes(packageBytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.EntryCount, Is.EqualTo(2));
        Assert.That(result.Entries.Count, Is.EqualTo(2));
        Assert.That(result.TotalCompressedBytes, Is.GreaterThan(0));
        Assert.That(result.TotalUncompressedBytes, Is.EqualTo(21));
        Assert.That(result.GetEntry(XlsxPackagePaths.Workbook), Is.Not.Null);
        Assert.That(result.GetEntry(XlsxPackagePaths.Workbook)!.GetText(), Is.EqualTo("<workbook />"));
        Assert.That(result.GetEntry(XlsxPackagePaths.ContentTypes)!.ZipEntryName, Is.EqualTo("[Content_Types].xml"));
    }

    [Test]
    public void ReadBytes_UsesDefaultOptionsWhenOptionsAreNull()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "xl/workbook.xml"
            },
            new string[]
            {
                "<workbook />"
            });
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult? result = reader.ReadBytes(packageBytes, null!);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.GetEntry("xl/workbook.xml")!.Path, Is.EqualTo(XlsxPackagePaths.Workbook));
    }

    [Test]
    public void ReadBytes_DetectsDuplicateNormalizedPartNames()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "xl/workbook.xml",
                "/xl/workbook.xml"
            },
            new string[]
            {
                "<workbook />",
                "<duplicate />"
            });
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult result = reader.ReadBytes(packageBytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Validation.HasErrors(), Is.True);
        Assert.That(result.Validation.Diagnostics[0].Code, Is.EqualTo("zip.duplicateEntry"));
        Assert.That(result.Validation.Diagnostics[0].PartPath, Is.EqualTo(XlsxPackagePaths.Workbook));
    }

    [Test]
    public void ReadBytes_ReportsConfiguredEntryAndByteLimits()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "xl/workbook.xml",
                "xl/worksheets/sheet1.xml"
            },
            new string[]
            {
                "1234567890",
                "abcdefghij"
            });
        XlsxReaderOptions options = new XlsxReaderOptions();
        options.MaxZipEntries = 1;
        options.MaxCompressedBytes = 1;
        options.MaxUncompressedBytes = 12;
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult result = reader.ReadBytes(packageBytes, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.EntryCount, Is.EqualTo(2));
        Assert.That(result.TotalUncompressedBytes, Is.EqualTo(20));
        Assert.That(HasDiagnostic(result, "zip.maxEntries"), Is.True);
        Assert.That(HasDiagnostic(result, "zip.maxCompressedBytes"), Is.True);
        Assert.That(HasDiagnostic(result, "zip.maxUncompressedBytes"), Is.True);
    }

    [Test]
    public void ReadBytes_RejectsInvalidPackagePartNames()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "xl/../workbook.xml"
            },
            new string[]
            {
                "<workbook />"
            });
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult result = reader.ReadBytes(packageBytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Entries.Count, Is.EqualTo(0));
        Assert.That(result.Validation.Diagnostics[0].Code, Is.EqualTo("path.parentSegment"));
        Assert.That(result.Validation.Diagnostics[0].PartPath, Is.EqualTo("xl/../workbook.xml"));
    }

    [Test]
    public void ReadBytes_CountsDirectoryEntriesButDoesNotExposeThemAsParts()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "xl/",
                "xl/workbook.xml"
            },
            new string[]
            {
                "",
                "<workbook />"
            });
        XlsxZipPackageReader reader = new XlsxZipPackageReader();

        XlsxZipPackageReadResult result = reader.ReadBytes(packageBytes, new XlsxReaderOptions());

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.EntryCount, Is.EqualTo(2));
        Assert.That(result.Entries.Count, Is.EqualTo(1));
        Assert.That(result.GetEntry(XlsxPackagePaths.Workbook), Is.Not.Null);
    }

    static bool HasDiagnostic(XlsxZipPackageReadResult result, string code)
    {
        foreach (XlsxDiagnostic diagnostic in result.Validation.Diagnostics)
        {
            if (diagnostic.Code == code)
            {
                return true;
            }
        }

        return false;
    }

    static byte[] CreateZip(string[] names, string[] values)
    {
        MemoryStream stream = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            int index = 0;
            while (index < names.Length)
            {
                ZipArchiveEntry entry = archive.CreateEntry(names[index], CompressionLevel.NoCompression);
                if (!names[index].EndsWith("/"))
                {
                    using (Stream entryStream = entry.Open())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(values[index]);
                        entryStream.Write(bytes, 0, bytes.Length);
                    }
                }

                index = index + 1;
            }
        }

        byte[] result = stream.ToArray();
        stream.Dispose();
        return result;
    }
}
