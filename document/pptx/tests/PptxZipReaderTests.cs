using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxZipReaderTests
{
    [Test]
    public void Read_EnumeratesSafeEntriesAndReadsBytes()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                PptxPartNames.ContentTypesEntry,
                "ppt/presentation.xml"
            },
            new string[]
            {
                "<Types/>",
                "<p:presentation/>"
            },
            CompressionLevel.NoCompression);

        PptxZipReadResult result = ReadBytes(packageBytes, PptxZipReadOptions.CreateDefault());

        Assert.That(result.Succeeded(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(2));
        Assert.That(result.Entries[0].Name, Is.EqualTo(PptxPartNames.ContentTypesEntry));
        Assert.That(result.Entries[0].PartName, Is.EqualTo(""));
        Assert.That(result.Entries[1].Name, Is.EqualTo("ppt/presentation.xml"));
        Assert.That(result.Entries[1].PartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(Encoding.UTF8.GetString(result.Entries[1].Bytes), Is.EqualTo("<p:presentation/>"));
    }

    [Test]
    public void Read_RejectsTraversalAbsoluteDirectoryAndDuplicateEntries()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "../evil.xml",
                "/ppt/presentation.xml",
                "ppt/",
                "ppt/slides/slide1.xml",
                "ppt/slides/slide1.xml"
            },
            new string[]
            {
                "evil",
                "absolute",
                "",
                "first",
                "second"
            },
            CompressionLevel.NoCompression);

        PptxZipReadResult result = ReadBytes(packageBytes, PptxZipReadOptions.CreateDefault());

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("URI010"));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP011"));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP012"));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP013"));
    }

    [Test]
    public void Read_EnforcesEntryCountLimit()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "ppt/presentation.xml",
                "ppt/slides/slide1.xml"
            },
            new string[]
            {
                "presentation",
                "slide"
            },
            CompressionLevel.NoCompression);

        PptxZipReadOptions options = PptxZipReadOptions.CreateDefault();
        options.MaxEntryCount = 1;

        PptxZipReadResult result = ReadBytes(packageBytes, options);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(1));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP001"));
    }

    [Test]
    public void Read_EnforcesEntryAndTotalInflatedSizeLimits()
    {
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "ppt/presentation.xml",
                "ppt/slides/slide1.xml"
            },
            new string[]
            {
                "12345",
                "67890"
            },
            CompressionLevel.NoCompression);

        PptxZipReadOptions entryOptions = PptxZipReadOptions.CreateDefault();
        entryOptions.MaxEntryBytes = 4;

        PptxZipReadResult entryResult = ReadBytes(packageBytes, entryOptions);

        Assert.That(entryResult.Failed(), Is.True);
        Assert.That(entryResult.Count(), Is.EqualTo(0));
        Assert.That(entryResult.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP020"));

        PptxZipReadOptions totalOptions = PptxZipReadOptions.CreateDefault();
        totalOptions.MaxTotalBytes = 6;

        PptxZipReadResult totalResult = ReadBytes(packageBytes, totalOptions);

        Assert.That(totalResult.Failed(), Is.True);
        Assert.That(totalResult.Count(), Is.EqualTo(1));
        Assert.That(totalResult.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP021"));
    }

    [Test]
    public void Read_EnforcesCompressionRatioLimit()
    {
        string repeated = new string('A', 4096);
        byte[] packageBytes = CreateZip(
            new string[]
            {
                "ppt/presentation.xml"
            },
            new string[]
            {
                repeated
            },
            CompressionLevel.SmallestSize);

        PptxZipReadOptions options = PptxZipReadOptions.CreateDefault();
        options.MaxCompressionRatio = 1.1;

        PptxZipReadResult result = ReadBytes(packageBytes, options);

        Assert.That(result.Failed(), Is.True);
        Assert.That(result.Count(), Is.EqualTo(0));
        Assert.That(result.Diagnostics.FormatDiagnostics(), Does.Contain("ZIP022"));
    }

    [Test]
    public void ValidateEntrySize_RejectsTotalThatWouldOverflow()
    {
        byte[] packageBytes = CreateZip(
            new string[] { "ppt/presentation.xml" },
            new string[] { "x" },
            CompressionLevel.NoCompression);
        using MemoryStream stream = new MemoryStream(packageBytes);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
        PptxZipReadOptions options = PptxZipReadOptions.CreateDefault();
        options.MaxTotalBytes = long.MaxValue;
        PptxResult diagnostics = new PptxResult();

        bool valid = PptxZipReader.ValidateEntrySize(archive.Entries[0], options, long.MaxValue, diagnostics);

        Assert.That(valid, Is.False);
        Assert.That(diagnostics.FormatDiagnostics(), Does.Contain("ZIP021"));
    }

    static PptxZipReadResult ReadBytes(byte[] packageBytes, PptxZipReadOptions options)
    {
        using MemoryStream stream = new MemoryStream(packageBytes);
        return new PptxZipReader().Read(stream, options);
    }

    static byte[] CreateZip(string[] names, string[] contents, CompressionLevel compressionLevel)
    {
        using MemoryStream stream = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            for (int index = 0; index < names.Length; index++)
            {
                ZipArchiveEntry entry = archive.CreateEntry(names[index], compressionLevel);
                using Stream entryStream = entry.Open();
                byte[] bytes = Encoding.UTF8.GetBytes(contents[index]);
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return stream.ToArray();
    }
}
