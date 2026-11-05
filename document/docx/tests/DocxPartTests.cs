using System.IO.Compression;
using System.Text;
using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxPartTests
{
    [Test]
    public void ConstructorAndGetBytes_CopyEagerPartBytes()
    {
        byte[] source = new byte[] { 1, 2, 3 };
        global::docx.DocxPart part = new global::docx.DocxPart("word/document.xml", "application/xml", source, false);

        source[0] = 9;
        byte[] firstRead = part.GetBytes();

        Assert.That(part.PartName, Is.EqualTo("/word/document.xml"));
        Assert.That(part.ContentType, Is.EqualTo("application/xml"));
        Assert.That(part.IsRelationshipPart, Is.False);
        Assert.That(part.IsLazy, Is.False);
        Assert.That(firstRead, Is.EqualTo(new byte[] { 1, 2, 3 }));

        firstRead[1] = 8;

        Assert.That(part.GetBytes(), Is.EqualTo(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void ReplaceTextAndReplaceBytes_UpdateContentAndClearLazyState()
    {
        global::docx.DocxPart part = global::docx.DocxPart.CreateLazy(
            "/word/document.xml",
            "application/xml",
            CreatePackageBytesWithEntry("word/document.xml", Encoding.UTF8.GetBytes("lazy")),
            "word/document.xml",
            false);
        byte[] replacement = new byte[] { 4, 5, 6 };

        part.ReplaceText("updated");

        Assert.That(part.GetText(), Is.EqualTo("updated"));
        Assert.That(part.IsLazy, Is.False);
        Assert.That(part.PackageBytes, Is.Empty);
        Assert.That(part.ZipEntryName, Is.EqualTo(""));

        part.ReplaceBytes(replacement);
        replacement[0] = 9;
        part.SetContentType("application/custom+xml");

        Assert.That(part.GetBytes(), Is.EqualTo(new byte[] { 4, 5, 6 }));
        Assert.That(part.ContentType, Is.EqualTo("application/custom+xml"));
    }

    [Test]
    public void LazyPart_LoadsBytesOnceFromPackageEntry()
    {
        global::docx.DocxPart part = global::docx.DocxPart.CreateLazy(
            "/word/document.xml",
            "application/xml",
            CreatePackageBytesWithEntry("word/document.xml", Encoding.UTF8.GetBytes("loaded")),
            "word/document.xml",
            false);

        Assert.That(part.IsLazy, Is.True);
        Assert.That(part.Bytes, Is.Empty);
        Assert.That(part.LoadCount, Is.EqualTo(0));

        Assert.That(part.GetText(), Is.EqualTo("loaded"));
        Assert.That(part.IsLazy, Is.False);
        Assert.That(part.PackageBytes, Is.Empty);
        Assert.That(part.ZipEntryName, Is.EqualTo(""));
        Assert.That(part.LoadCount, Is.EqualTo(1));

        Assert.That(part.GetBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("loaded")));
        Assert.That(part.LoadCount, Is.EqualTo(1));
    }

    [Test]
    public void LazyPart_ThrowsWhenPackageEntryIsMissing()
    {
        global::docx.DocxPart part = global::docx.DocxPart.CreateLazy(
            "/word/missing.xml",
            "application/xml",
            CreatePackageBytesWithEntry("word/document.xml", Encoding.UTF8.GetBytes("loaded")),
            "word/missing.xml",
            false);

        Assert.Throws<InvalidDataException>(() => part.GetBytes());
    }

    [Test]
    public void Part_RejectsInvalidArguments()
    {
        byte[] bytes = new byte[] { 1 };

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart(null!, "application/xml", bytes, false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", null!, bytes, false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "application/xml", null!, false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy(null!, "application/xml", bytes, "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", null!, bytes, "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", "application/xml", null!, "word/document.xml", false));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CreateLazy("/word/document.xml", "application/xml", bytes, null!, false));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "application/xml", bytes, false).ReplaceText(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "application/xml", bytes, false).ReplaceBytes(null!));
        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxPart("/word/document.xml", "application/xml", bytes, false).SetContentType(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxPart.CopyBytes(null!));
    }

    static byte[] CreatePackageBytesWithEntry(string entryName, byte[] bytes)
    {
        MemoryStream stream = new MemoryStream();

        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
        {
            ZipArchiveEntry entry = archive.CreateEntry(entryName);

            using (Stream entryStream = entry.Open())
            {
                entryStream.Write(bytes, 0, bytes.Length);
            }
        }

        return stream.ToArray();
    }
}
