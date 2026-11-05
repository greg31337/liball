using System.Collections.Generic;
using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxZipWriterTests
{
    [Test]
    public void Write_OrdersEntriesDeterministically()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        entries.Add(CreateEntry("ppt/slides/slide1.xml", "slide"));
        entries.Add(CreateEntry(PptxPartNames.ContentTypesEntry, "types"));
        entries.Add(CreateEntry("ppt/presentation.xml", "presentation"));
        entries.Add(CreateEntry("_rels/.rels", "rels"));

        byte[] bytes = WriteEntries(entries);
        string[] names = ReadEntryNames(bytes);

        Assert.That(
            names,
            Is.EqualTo(new string[]
            {
                PptxPartNames.ContentTypesEntry,
                "_rels/.rels",
                "ppt/presentation.xml",
                "ppt/slides/slide1.xml"
            }));
    }

    [Test]
    public void Write_ProducesStableBytesForSameInput()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        entries.Add(CreateEntry("ppt/slides/slide1.xml", "slide"));
        entries.Add(CreateEntry(PptxPartNames.ContentTypesEntry, "types"));
        entries.Add(CreateEntry("_rels/.rels", "rels"));

        byte[] first = WriteEntries(entries);
        byte[] second = WriteEntries(entries);

        Assert.That(first, Is.EqualTo(second));
    }

    [Test]
    public void Write_RoundTripsThroughReader()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        entries.Add(CreateEntry(PptxPartNames.ContentTypesEntry, "<Types/>"));
        entries.Add(CreateEntry("ppt/presentation.xml", "<p:presentation/>"));

        byte[] bytes = WriteEntries(entries);

        using MemoryStream stream = new MemoryStream(bytes);
        PptxZipReadResult readResult = new PptxZipReader().Read(stream, PptxZipReadOptions.CreateDefault());

        Assert.That(readResult.Succeeded(), Is.True);
        Assert.That(readResult.Count(), Is.EqualTo(2));
        Assert.That(Encoding.UTF8.GetString(readResult.Entries[1].Bytes), Is.EqualTo("<p:presentation/>"));
    }

    [Test]
    public void Write_RejectsUnsafeAndDuplicateEntryNamesBeforeWriting()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        entries.Add(CreateEntry("ppt/slides/slide1.xml", "first"));
        entries.Add(CreateEntry("ppt/slides/slide1.xml", "second"));
        entries.Add(new PptxZipEntry("../evil.xml", "", Encoding.UTF8.GetBytes("evil"), 4, 0));

        using MemoryStream stream = new MemoryStream();
        PptxResult result = new PptxZipWriter().Write(stream, entries, PptxZipWriteOptions.CreateDefault());

        Assert.That(result.Failed(), Is.True);
        Assert.That(stream.Length, Is.EqualTo(0));
        Assert.That(result.FormatDiagnostics(), Does.Contain("ZIP013"));
        Assert.That(result.FormatDiagnostics(), Does.Contain("URI010"));
    }

    [Test]
    public void Write_CanDeriveEntryNameFromPartName()
    {
        List<PptxZipEntry> entries = new List<PptxZipEntry>();
        entries.Add(new PptxZipEntry("", "/ppt/presentation.xml", Encoding.UTF8.GetBytes("presentation"), 12, 0));

        byte[] bytes = WriteEntries(entries);
        string[] names = ReadEntryNames(bytes);

        Assert.That(names, Is.EqualTo(new string[] { "ppt/presentation.xml" }));
    }

    static PptxZipEntry CreateEntry(string name, string text)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return new PptxZipEntry(name, PptxZipReader.GetPartNameForEntry(name), bytes, bytes.Length, 0);
    }

    static byte[] WriteEntries(List<PptxZipEntry> entries)
    {
        using MemoryStream stream = new MemoryStream();
        PptxResult result = new PptxZipWriter().Write(stream, entries, PptxZipWriteOptions.CreateDefault());
        Assert.That(result.Succeeded(), Is.True, result.FormatDiagnostics());
        return stream.ToArray();
    }

    static string[] ReadEntryNames(byte[] bytes)
    {
        using MemoryStream stream = new MemoryStream(bytes);
        using ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read);
        string[] names = new string[archive.Entries.Count];

        for (int index = 0; index < archive.Entries.Count; index++)
        {
            names[index] = archive.Entries[index].FullName;
        }

        return names;
    }
}
