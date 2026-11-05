namespace docx;

/// <summary>
/// Represents a text node and its XML whitespace-preservation setting.
/// </summary>
public class DocxText
{
    internal string Xml;
    /// <summary>
    /// The value value stored in the document model.
    /// </summary>
    public string Value;
    /// <summary>
    /// Whether the preserve space behavior is enabled.
    /// </summary>
    public bool PreserveSpace;

    /// <summary>
    /// Creates a text instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="preserveSpace">The preserve space used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxText(string xml, string value, bool preserveSpace)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Xml = xml;
        this.Value = value;
        this.PreserveSpace = preserveSpace;
    }

    internal static DocxText Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("t", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:t.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxText ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent() || !reader.IsElement("t", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("XML does not contain a w:t element.");
        }

        string space = reader.GetAttribute("space", DocxConstants.NamespaceXml);
        bool preserveSpace = space == "preserve";
        string value = reader.Reader.ReadElementContentAsString();

        return new DocxText(xml, value, preserveSpace);
    }

    internal static DocxText Create(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return new DocxText("", value, ShouldPreserveSpace(value));
    }

    internal static bool ShouldPreserveSpace(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return false;
        }

        if (value[0] == ' ')
        {
            return true;
        }

        if (value[value.Length - 1] == ' ')
        {
            return true;
        }

        return false;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal string GetText()
    {
        return this.Value;
    }

    internal bool HasText()
    {
        return this.Value != "";
    }

    internal void SetText(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Value = value;
    }

    internal void SetPreserveSpace(bool preserveSpace)
    {
        this.PreserveSpace = preserveSpace;
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

        writer.StartElement("w", "t", DocxConstants.NamespaceWordprocessingMl);

        if (this.PreserveSpace)
        {
            writer.WriteAttribute("xml", "space", DocxConstants.NamespaceXml, "preserve");
        }

        writer.WriteText(this.Value);
        writer.EndElement();
    }
}
