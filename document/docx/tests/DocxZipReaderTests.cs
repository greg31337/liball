using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxZipReaderTests
{
    [Test]
    public void ReadEntries_ReturnsZipEntryMetadataInArchiveOrder()
    {
        MemoryStream stream = CreateZipStream();
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        List<global::docx.DocxZipEntry> entries = reader.ReadEntries(stream);

        Assert.That(entries.Select(entry => entry.FullName), Is.EqualTo(new string[]
        {
            "[Content_Types].xml",
            "_rels/.rels",
            "word/",
            "word/document.xml",
            "word/media/image1.bin"
        }));
        Assert.That(entries[0].Name, Is.EqualTo("[Content_Types].xml"));
        Assert.That(entries[0].Length, Is.GreaterThan(0));
        Assert.That(entries[0].CompressedLength, Is.GreaterThan(0));
        Assert.That(entries[2].Name, Is.EqualTo(""));
        Assert.That(entries[2].IsDirectory, Is.True);
        Assert.That(entries[3].Name, Is.EqualTo("document.xml"));
        Assert.That(entries[3].IsDirectory, Is.False);
    }

    [Test]
    public void ReadEntries_LeavesInputStreamOpen()
    {
        MemoryStream stream = CreateZipStream();
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        reader.ReadEntries(stream);

        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void ReadEntriesFromFile_ReturnsZipEntryMetadata()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "zip-reader-test.docx");
        MemoryStream stream = CreateZipStream();

        File.WriteAllBytes(path, stream.ToArray());

        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();
        List<global::docx.DocxZipEntry> entries = reader.ReadEntriesFromFile(path);

        Assert.That(entries.Count, Is.EqualTo(5));
        Assert.That(entries[1].FullName, Is.EqualTo("_rels/.rels"));
    }

    [Test]
    public void HasEntry_ReturnsWhetherEntryExists()
    {
        MemoryStream stream = CreateZipStream();
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        Assert.That(reader.HasEntry(stream, "word/document.xml"), Is.True);

        stream.Position = 0;

        Assert.That(reader.HasEntry(stream, "word/styles.xml"), Is.False);
    }

    [Test]
    public void ReadXmlEntry_ReturnsUtf8XmlText()
    {
        MemoryStream stream = CreateZipStream();
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        string xml = reader.ReadXmlEntry(stream, "word/document.xml");

        Assert.That(xml, Is.EqualTo("<w:document />"));
        Assert.That(stream.CanRead, Is.True);
    }

    [Test]
    public void ReadBinaryEntry_ReturnsExactBytes()
    {
        MemoryStream stream = CreateZipStream();
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        byte[] bytes = reader.ReadBinaryEntry(stream, "word/media/image1.bin");

        Assert.That(bytes, Is.EqualTo(new byte[] { 0, 1, 2, 254, 255 }));
    }

    [Test]
    public void ReadEntryHelpers_ThrowWhenEntryIsMissing()
    {
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        Assert.Throws<FileNotFoundException>(() => reader.ReadXmlEntry(CreateZipStream(), "word/missing.xml"));
        Assert.Throws<FileNotFoundException>(() => reader.ReadBinaryEntry(CreateZipStream(), "word/media/missing.bin"));
    }

    [Test]
    public void ZipReader_RejectsNullArguments()
    {
        global::docx.DocxZipReader reader = new global::docx.DocxZipReader();

        Assert.Throws<ArgumentNullException>(() => reader.ReadEntries(null!));
        Assert.Throws<ArgumentNullException>(() => reader.ReadEntriesFromFile(null!));
        Assert.Throws<ArgumentNullException>(() => reader.HasEntry(CreateZipStream(), null!));
        Assert.Throws<ArgumentNullException>(() => reader.ReadXmlEntry(null!, "word/document.xml"));
        Assert.Throws<ArgumentNullException>(() => reader.ReadXmlEntry(CreateZipStream(), null!));
        Assert.Throws<ArgumentNullException>(() => reader.ReadBinaryEntry(null!, "word/media/image1.bin"));
        Assert.Throws<ArgumentNullException>(() => reader.ReadBinaryEntry(CreateZipStream(), null!));
    }

    static MemoryStream CreateZipStream()
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            AddEntry(archive, "[Content_Types].xml", "<Types />");
            AddEntry(archive, "_rels/.rels", "<Relationships />");
            archive.CreateEntry("word/");
            AddEntry(archive, "word/document.xml", "<w:document />");
            AddEntry(archive, "word/media/image1.bin", new byte[] { 0, 1, 2, 254, 255 });
        }

        stream.Position = 0;
        return stream;
    }

    static void AddEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);

        using (Stream entryStream = entry.Open())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }

    static void AddEntry(ZipArchive archive, string name, byte[] bytes)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name);

        using (Stream entryStream = entry.Open())
        {
            entryStream.Write(bytes, 0, bytes.Length);
        }
    }
}
