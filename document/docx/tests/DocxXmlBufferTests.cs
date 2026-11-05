using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxXmlBufferTests
{
    [Test]
    public void Capture_StoresCurrentElementOuterXmlAndAdvancesReader()
    {
        string unknown = "<x:unknown xmlns:x=\"urn:x\" a=\"1\"><x:child>value</x:child></x:unknown>";
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root>" + unknown + "<known /></root>");

        Assert.That(reader.ReadToElement("unknown", "urn:x"), Is.True);

        global::docx.DocxXmlBuffer buffer = global::docx.DocxXmlBuffer.Capture(reader);

        Assert.That(buffer.GetXml(), Is.EqualTo(unknown));
        Assert.That(reader.LocalName(), Is.EqualTo("known"));
    }

    [Test]
    public void WriteTo_RoundTripsRawSubtreeThroughDocxXmlWriter()
    {
        string unknown = "<x:unknown xmlns:x=\"urn:x\" a=\"1\"><x:child>value</x:child></x:unknown>";
        global::docx.DocxXmlBuffer buffer = new global::docx.DocxXmlBuffer(unknown);
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        writer.StartElementWithoutPrefix("root", "");
        buffer.WriteTo(writer);
        writer.EndElement();

        Assert.That(writer.GetXml(), Is.EqualTo("<root>" + unknown + "</root>"));
    }

    [Test]
    public void WriteTo_RoundTripsRawSubtreeThroughExternalXmlWriter()
    {
        string unknown = "<x:unknown xmlns:x=\"urn:x\"><x:child /></x:unknown>";
        StringWriter stringWriter = new StringWriter();
        global::docx.DocxXmlBuffer buffer = new global::docx.DocxXmlBuffer(unknown);

        using (System.Xml.XmlWriter xmlWriter = System.Xml.XmlWriter.Create(stringWriter, global::docx.DocxXmlWriter.CreateSettings()))
        {
            xmlWriter.WriteStartElement("root");
            buffer.WriteToXmlWriter(xmlWriter);
            xmlWriter.WriteEndElement();
        }

        Assert.That(stringWriter.ToString(), Is.EqualTo("<root>" + unknown + "</root>"));
    }

    [Test]
    public void Capture_SelfClosingElementPreservesAttributesAndAdvancesReader()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root><x:empty xmlns:x=\"urn:x\" x:data=\"1\" /><next /></root>");

        Assert.That(reader.ReadToElement("empty", "urn:x"), Is.True);

        global::docx.DocxXmlBuffer buffer = global::docx.DocxXmlBuffer.Capture(reader);

        Assert.That(buffer.GetXml(), Is.EqualTo("<x:empty xmlns:x=\"urn:x\" x:data=\"1\" />"));
        Assert.That(reader.LocalName(), Is.EqualTo("next"));
    }

    [Test]
    public void Buffer_ReportsEmptyRawXml()
    {
        global::docx.DocxXmlBuffer empty = new global::docx.DocxXmlBuffer("");
        global::docx.DocxXmlBuffer value = new global::docx.DocxXmlBuffer("<x />");

        Assert.That(empty.IsEmpty(), Is.True);
        Assert.That(value.IsEmpty(), Is.False);
    }

    [Test]
    public void Capture_RejectsNullReadersAndNonElementPositions()
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlBuffer.Capture((global::docx.DocxXmlReader)null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxXmlBuffer.Capture((System.Xml.XmlReader)null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxXmlBuffer.Capture(reader));
    }

    [Test]
    public void Buffer_RejectsNullXmlAndWriters()
    {
        global::docx.DocxXmlBuffer buffer = new global::docx.DocxXmlBuffer("<x />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxXmlBuffer(null!));
        Assert.Throws<ArgumentNullException>(() => buffer.WriteTo((global::docx.DocxXmlWriter)null!));
        Assert.Throws<ArgumentNullException>(() => buffer.WriteToXmlWriter((System.Xml.XmlWriter)null!));
    }
}
