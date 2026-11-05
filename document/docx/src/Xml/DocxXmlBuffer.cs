using System.Xml;

namespace docx;

internal class DocxXmlBuffer
{
    internal string Xml;

    internal DocxXmlBuffer(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
    }

    internal static DocxXmlBuffer Capture(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return Capture(reader.Reader);
    }

    internal static DocxXmlBuffer Capture(XmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.NodeType != XmlNodeType.Element)
        {
            throw new InvalidOperationException("Reader is not positioned on an element.");
        }

        string xml = reader.ReadOuterXml();
        return new DocxXmlBuffer(xml);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool IsEmpty()
    {
        return this.Xml == "";
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteToXmlWriter(writer.Writer);
    }

    internal void WriteToXmlWriter(XmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.WriteRaw(this.Xml);
    }
}
