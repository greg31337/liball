using System.IO.Compression;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxZipWriterTests
{
    [Test]
    public void WriteEntries_WritesXmlBinaryAndDirectoryEntries()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        List<global::docx.DocxZipWriteEntry> entries = CreateUnorderedEntries(writer);
        MemoryStream stream = new MemoryStream();

        writer.WriteEntries(stream, entries);

        stream.Position = 0;
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        Assert.That(reader.ReadXmlEntry(stream, "word/document.xml"), Is.EqualTo("<w:document />"));

        stream.Position = 0;

        Assert.That(reader.ReadBinaryEntry(stream, "word/media/image1.bin"), Is.EqualTo(new byte[] { 3, 1, 4, 1, 5 }));

        stream.Position = 0;

        List<global::docx.DocxZipEntry> readEntries = reader.ReadEntries(stream);

        Assert.That(readEntries.Any(entry => entry.FullName == "word/"), Is.True);
        Assert.That(readEntries.Single(entry => entry.FullName == "word/").IsDirectory, Is.True);
    }

    [Test]
    public void WriteEntries_PreservesInputOrderByDefault()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        List<global::docx.DocxZipWriteEntry> entries = CreateUnorderedEntries(writer);
        MemoryStream stream = new MemoryStream();

        writer.WriteEntries(stream, entries);

        stream.Position = 0;
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();
        List<global::docx.DocxZipEntry> readEntries = reader.ReadEntries(stream);

        Assert.That(readEntries.Select(entry => entry.FullName), Is.EqualTo(new string[]
        {
            "word/document.xml",
            "[Content_Types].xml",
            "word/",
            "word/media/image1.bin"
        }));
    }

    [Test]
    public void WriteEntries_DeterministicModeSortsEntriesAndUsesFixedTimestamps()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        List<global::docx.DocxZipWriteEntry> entries = CreateUnorderedEntries(writer);
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        MemoryStream stream = new MemoryStream();

        options.Deterministic = true;

        writer.WriteEntriesWithOptions(stream, entries, options);

        stream.Position = 0;

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
        {
            Assert.That(archive.Entries.Select(entry => entry.FullName), Is.EqualTo(new string[]
            {
                "[Content_Types].xml",
                "word/",
                "word/document.xml",
                "word/media/image1.bin"
            }));

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                Assert.That(entry.LastWriteTime.DateTime, Is.EqualTo(writer.GetDeterministicTimestamp().DateTime));
            }
        }
    }

    [Test]
    public void WriteEntries_DeterministicModeProducesRepeatableBytes()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();
        MemoryStream first = new MemoryStream();
        MemoryStream second = new MemoryStream();

        options.Deterministic = true;

        writer.WriteEntriesWithOptions(first, CreateUnorderedEntries(writer), options);
        writer.WriteEntriesWithOptions(second, CreateUnorderedEntries(writer), options);

        Assert.That(first.ToArray(), Is.EqualTo(second.ToArray()));
    }

    [Test]
    public void CreateBinaryEntry_CopiesInputBytes()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        byte[] bytes = new byte[] { 9, 8, 7 };

        global::docx.DocxZipWriteEntry entry = writer.CreateBinaryEntry("word/media/image1.bin", bytes);

        bytes[0] = 0;

        Assert.That(entry.Bytes, Is.EqualTo(new byte[] { 9, 8, 7 }));
    }

    [Test]
    public void ZipWriter_RejectsNullArguments()
    {
        global::docx.DocxZipWriter writer = new global::docx.DocxZipWriter();
        List<global::docx.DocxZipWriteEntry> entries = CreateUnorderedEntries(writer);
        global::docx.DocxSaveOptions options = new global::docx.DocxSaveOptions();

        Assert.Throws<ArgumentNullException>(() => writer.WriteEntriesWithOptions(null!, entries, options));
        Assert.Throws<ArgumentNullException>(() => writer.WriteEntriesWithOptions(new MemoryStream(), null!, options));
        Assert.Throws<ArgumentNullException>(() => writer.WriteEntriesWithOptions(new MemoryStream(), entries, null!));
        Assert.Throws<ArgumentNullException>(() => writer.WriteEntriesToFile(null!, entries, options));
        Assert.Throws<ArgumentNullException>(() => writer.CreateXmlEntry("word/document.xml", null!));
        Assert.Throws<ArgumentNullException>(() => writer.CreateBinaryEntry("word/media/image1.bin", null!));
        Assert.Throws<ArgumentNullException>(() => writer.CreateDirectoryEntry(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxZipWriteEntry(null!, new byte[0], false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxZipWriteEntry("word/document.xml", null!, false));
    }

    static List<global::docx.DocxZipWriteEntry> CreateUnorderedEntries(global::docx.DocxZipWriter writer)
    {
        return new List<global::docx.DocxZipWriteEntry>
        {
            writer.CreateXmlEntry("word/document.xml", "<w:document />"),
            writer.CreateXmlEntry("[Content_Types].xml", "<Types />"),
            writer.CreateDirectoryEntry("word"),
            writer.CreateBinaryEntry("word/media/image1.bin", new byte[] { 3, 1, 4, 1, 5 })
        };
    }
}
