namespace docx;

internal class DocxUnknownElement
{
    internal string Xml;
    internal string Prefix;
    internal string LocalName;
    internal string NamespaceUri;

    internal DocxUnknownElement(string xml, string prefix, string localName, string namespaceUri)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (localName == "")
        {
            throw new ArgumentException("Element local name is required.", "localName");
        }

        this.Xml = xml;
        this.Prefix = prefix;
        this.LocalName = localName;
        this.NamespaceUri = namespaceUri;
    }

    internal static DocxUnknownElement Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.Reader.NodeType != System.Xml.XmlNodeType.Element)
        {
            throw new InvalidOperationException("Reader is not positioned on an element.");
        }

        string prefix = reader.Reader.Prefix;
        string localName = reader.LocalName();
        string namespaceUri = reader.NamespaceUri();
        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);

        return new DocxUnknownElement(buffer.GetXml(), prefix, localName, namespaceUri);
    }

    internal static DocxUnknownElement ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain an element.");
        }

        return Parse(reader);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool IsElement(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        return this.LocalName == localName && this.NamespaceUri == namespaceUri;
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
        buffer.WriteTo(writer);
    }
}
