namespace docx;

/// <summary>
/// Represents document defaults data used by a DOCX document.
/// </summary>
public class DocxDocumentDefaults
{
    internal string Xml;
    internal string ParagraphPropertiesXml;
    internal string RunPropertiesXml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The paragraph format value stored in the document model.
    /// </summary>
    public DocxParagraphFormat ParagraphFormat;
    /// <summary>
    /// The run format value stored in the document model.
    /// </summary>
    public DocxRunFormat RunFormat;

    /// <summary>
    /// Creates a document defaults instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxDocumentDefaults(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.ParagraphPropertiesXml = "";
        this.RunPropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.ParagraphFormat = new DocxParagraphFormat();
        this.RunFormat = new DocxRunFormat();
        LoadFromXml();
    }

    internal static DocxDocumentDefaults Create()
    {
        return new DocxDocumentDefaults("");
    }

    internal static DocxDocumentDefaults Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("docDefaults", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:docDefaults.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxDocumentDefaults ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return new DocxDocumentDefaults(xml);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadFromXml()
    {
        this.ParagraphPropertiesXml = "";
        this.RunPropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.ParagraphFormat = new DocxParagraphFormat();
        this.RunFormat = new DocxRunFormat();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("docDefaults", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int defaultsDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == defaultsDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == defaultsDepth + 1)
            {
                bool isParagraphDefault = reader.IsElement("pPrDefault", DocxConstants.NamespaceWordprocessingMl);
                bool isRunDefault = reader.IsElement("rPrDefault", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                this.Children.Add(child);

                if (isParagraphDefault)
                {
                    DocxXmlReader childReader = DocxXmlReader.Create(child.GetXml());
                    childReader.MoveToContent();
                    ReadParagraphDefault(childReader);
                }
                else if (isRunDefault)
                {
                    DocxXmlReader childReader = DocxXmlReader.Create(child.GetXml());
                    childReader.MoveToContent();
                    ReadRunDefault(childReader);
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadParagraphDefault(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pPrDefault", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pPrDefault.");
        }

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int defaultDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == defaultDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == defaultDepth + 1)
            {
                if (reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
                    this.ParagraphPropertiesXml = buffer.GetXml();
                    this.ParagraphFormat = DocxParagraphFormat.ParseXml(this.ParagraphPropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadRunDefault(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("rPrDefault", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:rPrDefault.");
        }

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int defaultDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == defaultDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == defaultDepth + 1)
            {
                if (reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
                    this.RunPropertiesXml = buffer.GetXml();
                    this.RunFormat = DocxRunFormat.ParseXml(this.RunPropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal bool HasParagraphFormat()
    {
        return this.ParagraphPropertiesXml != "" || this.ParagraphFormat.HasAnyFormatting();
    }

    internal bool HasRunFormat()
    {
        return this.RunPropertiesXml != "" || this.RunFormat.HasAnyFormatting();
    }

    internal bool HasAnyDefaults()
    {
        return HasParagraphFormat() || HasRunFormat();
    }

    internal bool ShouldWrite()
    {
        return this.Xml != "" || this.Children.Count > 0 || HasAnyDefaults();
    }

    internal void SetParagraphFormat(DocxParagraphFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.ParagraphFormat = format;
        this.ParagraphPropertiesXml = "";
        this.Xml = "";
    }

    internal void ClearParagraphFormat()
    {
        this.ParagraphFormat = new DocxParagraphFormat();
        this.ParagraphPropertiesXml = "";
        this.Xml = "";
    }

    internal void SetRunFormat(DocxRunFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.RunFormat = format;
        this.RunPropertiesXml = "";
        this.Xml = "";
    }

    internal void ClearRunFormat()
    {
        this.RunFormat = new DocxRunFormat();
        this.RunPropertiesXml = "";
        this.Xml = "";
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

        writer.StartElement("w", "docDefaults", DocxConstants.NamespaceWordprocessingMl);

        bool paragraphWritten = false;
        bool runWritten = false;

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (IsChild(child, "pPrDefault"))
            {
                if (!paragraphWritten && HasParagraphFormat())
                {
                    if (this.ParagraphPropertiesXml != "")
                    {
                        child.WriteTo(writer);
                    }
                    else
                    {
                        WriteParagraphDefaultTo(writer);
                    }
                }

                paragraphWritten = true;
            }
            else if (IsChild(child, "rPrDefault"))
            {
                if (!runWritten && HasRunFormat())
                {
                    if (this.RunPropertiesXml != "")
                    {
                        child.WriteTo(writer);
                    }
                    else
                    {
                        WriteRunDefaultTo(writer);
                    }
                }

                runWritten = true;
            }
            else
            {
                child.WriteTo(writer);
            }
        }

        if (!paragraphWritten && HasParagraphFormat())
        {
            WriteParagraphDefaultTo(writer);
        }

        if (!runWritten && HasRunFormat())
        {
            WriteRunDefaultTo(writer);
        }

        writer.EndElement();
    }

    internal void WriteParagraphDefaultTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "pPrDefault", DocxConstants.NamespaceWordprocessingMl);
        this.ParagraphFormat.WriteTo(writer);
        writer.EndElement();
    }

    internal void WriteRunDefaultTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "rPrDefault", DocxConstants.NamespaceWordprocessingMl);
        this.RunFormat.WriteTo(writer);
        writer.EndElement();
    }

    internal static bool IsChild(DocxXmlBuffer child, string localName)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return false;
        }

        return reader.IsElement(localName, DocxConstants.NamespaceWordprocessingMl);
    }
}
