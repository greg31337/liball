using System.Text;
using System.Xml;
using NUnit.Framework;

namespace pptx.Tests;

[TestFixture]
public class PptxPartTests
{
    [Test]
    public void Constructor_NormalizesPartNameCopiesBytesAndStartsClean()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<p:presentation/>");

        PptxPart part = new PptxPart("ppt/presentation.xml", PptxContentTypes.Presentation, bytes);
        bytes[0] = (byte)'!';

        Assert.That(part.PartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(part.ContentType, Is.EqualTo(PptxContentTypes.Presentation));
        Assert.That(part.ByteCount(), Is.EqualTo("<p:presentation/>".Length));
        Assert.That(Encoding.UTF8.GetString(part.Bytes), Is.EqualTo("<p:presentation/>"));
        Assert.That(part.Relationships.SourcePartName, Is.EqualTo("/ppt/presentation.xml"));
        Assert.That(part.HasRelationships(), Is.False);
        Assert.That(part.HasXmlDocument, Is.False);
        Assert.That(part.IsDirty, Is.False);
    }

    [Test]
    public void GetBytes_ReturnsCopy()
    {
        PptxPart part = new PptxPart("/ppt/media/image1.bin", "application/octet-stream", new byte[] { 1, 2, 3 });

        byte[] copy = part.GetBytes();
        copy[0] = 9;

        Assert.That(part.Bytes[0], Is.EqualTo(1));
    }

    [Test]
    public void Mutators_UpdateDataAndMarkDirty()
    {
        PptxPart part = new PptxPart("/ppt/presentation.xml", PptxContentTypes.Presentation, Encoding.UTF8.GetBytes("old"));

        part.SetBytes(Encoding.UTF8.GetBytes("new"));
        Assert.That(Encoding.UTF8.GetString(part.Bytes), Is.EqualTo("new"));
        Assert.That(part.IsDirty, Is.True);

        part.MarkClean();
        part.SetContentType(PptxContentTypes.Xml);

        Assert.That(part.ContentType, Is.EqualTo(PptxContentTypes.Xml));
        Assert.That(part.IsDirty, Is.True);

        part.MarkClean();
        PptxRelationshipCollection relationships = new PptxRelationshipCollection("/ppt/presentation.xml");
        relationships.Add(new PptxRelationship(
            "/ppt/presentation.xml",
            "rId1",
            PptxRelationshipTypes.Slide,
            "slides/slide1.xml",
            PptxRelationshipTargetMode.Internal,
            "/ppt/slides/slide1.xml",
            ""));
        part.SetRelationships(relationships);

        Assert.That(part.HasRelationships(), Is.True);
        Assert.That(part.IsDirty, Is.True);
    }

    [Test]
    public void LoadXmlDocument_ParsesBytesWithoutMarkingDirty()
    {
        string xml = "<root><child value=\"1\"/></root>";
        PptxPart part = new PptxPart("/ppt/custom/item1.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes(xml));

        XmlDocument document = part.LoadXmlDocument();

        Assert.That(part.HasXmlDocument, Is.True);
        Assert.That(document.DocumentElement!.LocalName, Is.EqualTo("root"));
        Assert.That(part.IsDirty, Is.False);
    }

    [Test]
    public void SetAndClearXmlDocument_UpdateXmlStateAndMarkDirty()
    {
        PptxPart part = new PptxPart("/ppt/custom/item1.xml", PptxContentTypes.Xml, Encoding.UTF8.GetBytes("<root/>"));
        XmlDocument document = new XmlDocument();
        document.LoadXml("<root><child/></root>");

        part.SetXmlDocument(document);

        Assert.That(part.HasXmlDocument, Is.True);
        Assert.That(part.XmlDocument.DocumentElement!.ChildNodes.Count, Is.EqualTo(1));
        Assert.That(part.IsDirty, Is.True);

        part.MarkClean();
        part.ClearXmlDocument();

        Assert.That(part.HasXmlDocument, Is.False);
        Assert.That(part.IsDirty, Is.True);
    }

    [Test]
    public void IsXmlContentType_DetectsXmlContentTypes()
    {
        PptxPart xml = new PptxPart("/ppt/presentation.xml", PptxContentTypes.Presentation, new byte[0]);
        PptxPart plainXml = new PptxPart("/ppt/custom/item1.xml", PptxContentTypes.Xml, new byte[0]);
        PptxPart image = new PptxPart("/ppt/media/image1.png", PptxContentTypes.Png, new byte[0]);

        Assert.That(xml.IsXmlContentType(), Is.True);
        Assert.That(plainXml.IsXmlContentType(), Is.True);
        Assert.That(image.IsXmlContentType(), Is.False);
    }

    [Test]
    public void Constructor_RejectsInvalidPartNameAndContentType()
    {
        Assert.Throws<ArgumentException>(() => new PptxPart("/ppt/../presentation.xml", PptxContentTypes.Presentation, new byte[0]));

        ArgumentException exception = Assert.Throws<ArgumentException>(
            () => new PptxPart("/ppt/presentation.xml", "not-a-content-type", new byte[0]))!;

        Assert.That(exception.Message, Does.Contain("CT031"));
    }
}
