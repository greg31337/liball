using System.IO.Compression;
using NUnit.Framework;
using xlsx;

namespace xlsx.Tests;

[TestFixture]
public class ZipPackageWriterTests
{
    [Test]
    public void WriteBytes_SortsEntriesDeterministicallyAndCanBeReadBack()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.Worksheet1, "<sheet />"));
        entries.Add(CreateEntry(XlsxPackagePaths.ContentTypes, "<Types />"));
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, "<workbook />"));
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.CompressionLevel = CompressionLevel.NoCompression;
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult writeResult = writer.WriteBytes(entries, options);
        XlsxZipPackageReadResult readResult = new XlsxZipPackageReader().ReadBytes(writeResult.Bytes, new XlsxReaderOptions());

        Assert.That(writeResult.IsValid, Is.True);
        Assert.That(writeResult.EntryCount, Is.EqualTo(3));
        Assert.That(writeResult.TotalUncompressedBytes, Is.EqualTo(30));
        Assert.That(readResult.IsValid, Is.True);
        Assert.That(readResult.Entries[0].Path, Is.EqualTo(XlsxPackagePaths.ContentTypes));
        Assert.That(readResult.Entries[1].Path, Is.EqualTo(XlsxPackagePaths.Workbook));
        Assert.That(readResult.Entries[2].Path, Is.EqualTo(XlsxPackagePaths.Worksheet1));
        Assert.That(readResult.GetEntry(XlsxPackagePaths.Workbook)!.GetText(), Is.EqualTo("<workbook />"));
    }

    [Test]
    public void WriteBytes_ProducesIdenticalBytesForIdenticalEntriesInDifferentOrders()
    {
        List<XlsxPackageEntry> firstOrder = new List<XlsxPackageEntry>();
        firstOrder.Add(CreateEntry(XlsxPackagePaths.Worksheet1, "<sheet />"));
        firstOrder.Add(CreateEntry(XlsxPackagePaths.Workbook, "<workbook />"));
        List<XlsxPackageEntry> secondOrder = new List<XlsxPackageEntry>();
        secondOrder.Add(CreateEntry(XlsxPackagePaths.Workbook, "<workbook />"));
        secondOrder.Add(CreateEntry(XlsxPackagePaths.Worksheet1, "<sheet />"));
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.CompressionLevel = CompressionLevel.NoCompression;
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult first = writer.WriteBytes(firstOrder, options);
        XlsxZipPackageWriteResult second = writer.WriteBytes(secondOrder, options);

        Assert.That(first.IsValid, Is.True);
        Assert.That(second.IsValid, Is.True);
        Assert.That(first.Bytes, Is.EqualTo(second.Bytes));
    }

    [Test]
    public void WriteBytes_SetsStableZipTimestampsWhenDeterministic()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry(XlsxPackagePaths.Workbook, "<workbook />"));
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.CompressionLevel = CompressionLevel.NoCompression;
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult result = writer.WriteBytes(entries, options);

        Assert.That(result.IsValid, Is.True);
        using (MemoryStream stream = new MemoryStream(result.Bytes))
        {
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.That(archive.Entries[0].LastWriteTime.Year, Is.EqualTo(1980));
                Assert.That(archive.Entries[0].LastWriteTime.Month, Is.EqualTo(1));
                Assert.That(archive.Entries[0].LastWriteTime.Day, Is.EqualTo(1));
            }
        }
    }

    [Test]
    public void WriteBytes_UsesDefaultOptionsWhenOptionsAreNull()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry("xl/workbook.xml", "<workbook />"));
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult result = writer.WriteBytes(entries, null);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.EntryCount, Is.EqualTo(1));
        Assert.That(result.Bytes.Length, Is.GreaterThan(0));
    }

    [Test]
    public void WriteStream_RejectsInvalidEntriesBeforeWriting()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry("/xl/../workbook.xml", "<workbook />"));
        MemoryStream stream = new MemoryStream();
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult result = writer.WriteStream(stream, entries, new XlsxWriterOptions());

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Bytes.Length, Is.EqualTo(0));
        Assert.That(stream.Length, Is.EqualTo(0));
        Assert.That(result.Validation.Diagnostics[0].Code, Is.EqualTo("path.parentSegment"));
        stream.Dispose();
    }

    [Test]
    public void WriteStream_RejectsDuplicateEntriesAndConfiguredLimits()
    {
        List<XlsxPackageEntry> entries = new List<XlsxPackageEntry>();
        entries.Add(CreateEntry("/xl/workbook.xml", "123"));
        entries.Add(CreateEntry("xl/./workbook.xml", "456"));
        entries.Add(CreateEntry("/xl/worksheets/sheet1.xml", "789"));
        XlsxWriterOptions options = new XlsxWriterOptions();
        options.MaxZipEntries = 1;
        options.MaxUncompressedBytes = 5;
        MemoryStream stream = new MemoryStream();
        XlsxZipPackageWriter writer = new XlsxZipPackageWriter();

        XlsxZipPackageWriteResult result = writer.WriteStream(stream, entries, options);

        Assert.That(result.IsValid, Is.False);
        Assert.That(stream.Length, Is.EqualTo(0));
        Assert.That(HasDiagnostic(result, "writer.duplicateEntry"), Is.True);
        Assert.That(HasDiagnostic(result, "writer.maxZipEntriesExceeded"), Is.True);
        Assert.That(HasDiagnostic(result, "writer.maxUncompressedBytesExceeded"), Is.True);
        stream.Dispose();
    }

    static XlsxPackageEntry CreateEntry(string path, string text)
    {
        XlsxPackageEntry entry = new XlsxPackageEntry();
        entry.Path = path;
        entry.SetText(text);
        return entry;
    }

    static bool HasDiagnostic(XlsxZipPackageWriteResult result, string code)
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
}
