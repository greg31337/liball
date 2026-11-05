using System.Xml;

namespace docx;

internal class DocxXmlWriter
{
    internal XmlWriter Writer;
    internal StringWriter? Output;

    internal DocxXmlWriter(XmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        this.Writer = writer;
        this.Output = null;
    }

    internal static DocxXmlWriter Create()
    {
        StringWriter stringWriter = new StringWriter();
        XmlWriter writer = XmlWriter.Create(stringWriter, CreateSettings());
        DocxXmlWriter docxWriter = new DocxXmlWriter(writer);
        docxWriter.Output = stringWriter;
        return docxWriter;
    }

    internal static XmlWriterSettings CreateSettings()
    {
        XmlWriterSettings settings = new XmlWriterSettings();
        settings.OmitXmlDeclaration = true;
        return settings;
    }

    internal string GetXml()
    {
        if (this.Output == null)
        {
            throw new InvalidOperationException("This writer does not own a string output.");
        }

        this.Writer.Flush();
        return this.Output.ToString();
    }

    internal void StartDocument()
    {
        this.Writer.WriteStartDocument();
    }

    internal void EndDocument()
    {
        this.Writer.WriteEndDocument();
    }

    internal void StartElement(string prefix, string localName, string namespaceUri)
    {
        ValidateName(prefix, localName, namespaceUri);
        this.Writer.WriteStartElement(prefix, localName, namespaceUri);
    }

    internal void StartElementWithoutPrefix(string localName, string namespaceUri)
    {
        StartElement("", localName, namespaceUri);
    }

    internal void EndElement()
    {
        this.Writer.WriteEndElement();
    }

    internal void WriteElementText(string prefix, string localName, string namespaceUri, string text)
    {
        ValidateName(prefix, localName, namespaceUri);

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        this.Writer.WriteStartElement(prefix, localName, namespaceUri);
        this.Writer.WriteString(text);
        this.Writer.WriteEndElement();
    }

    internal void WriteElementTextWithoutPrefix(string localName, string namespaceUri, string text)
    {
        WriteElementText("", localName, namespaceUri, text);
    }

    internal void WriteAttribute(string prefix, string localName, string namespaceUri, string value)
    {
        ValidateName(prefix, localName, namespaceUri);

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Writer.WriteAttributeString(prefix, localName, namespaceUri, value);
    }

    internal void WriteAttributeWithoutPrefix(string localName, string value)
    {
        WriteAttribute("", localName, "", value);
    }

    internal void WriteNamespace(string prefix, string namespaceUri)
    {
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (namespaceUri == null)
        {
            throw new ArgumentNullException("namespaceUri");
        }

        if (prefix == "")
        {
            this.Writer.WriteAttributeString("xmlns", namespaceUri);
        }
        else
        {
            this.Writer.WriteAttributeString("xmlns", prefix, "", namespaceUri);
        }
    }

    internal void WriteText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        this.Writer.WriteString(text);
    }

    internal void Flush()
    {
        this.Writer.Flush();
    }

    internal static void ValidateName(string prefix, string localName, string namespaceUri)
    {
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
            throw new ArgumentException("Local name is required.", "localName");
        }
    }
}
