namespace docx;

/// <summary>
/// Represents the footnotes part and its note definitions.
/// </summary>
public class DocxFootnotes : IDocxTextContent
{
    internal string Xml;
    internal List<DocxFootnote> Footnotes;
    internal List<DocxXmlBuffer> UnknownChildren;

    /// <summary>
    /// Creates a footnotes instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxFootnotes(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Footnotes = new List<DocxFootnote>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadFromXml();
    }

    internal static DocxFootnotes Create()
    {
        return new DocxFootnotes("");
    }

    internal static DocxFootnotes Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("footnotes", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:footnotes.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxFootnotes(buffer.GetXml());
    }

    internal static DocxFootnotes ParseXml(string xml)
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

    internal void LoadFromXml()
    {
        this.Footnotes = new List<DocxFootnote>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("footnotes", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int rootDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == rootDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == rootDepth + 1)
            {
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);

                if (IsFootnoteBuffer(child))
                {
                    this.Footnotes.Add(DocxFootnote.ParseXml(child.GetXml()));
                }
                else
                {
                    this.UnknownChildren.Add(child);
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal bool IsFootnoteBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        return reader.MoveToContent() && reader.IsElement("footnote", DocxConstants.NamespaceWordprocessingMl);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "" || this.Footnotes.Count > 0 || this.UnknownChildren.Count > 0;
    }

    internal int FootnoteCount()
    {
        return this.Footnotes.Count;
    }

    internal DocxFootnote GetFootnote(int index)
    {
        return this.Footnotes[index];
    }

    internal DocxFootnote? FindFootnoteById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxFootnote footnote in this.Footnotes)
        {
            if (footnote.Id == id)
            {
                return footnote;
            }
        }

        return null;
    }

    internal int CountFootnotesByType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        int count = 0;

        foreach (DocxFootnote footnote in this.Footnotes)
        {
            if (footnote.Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <returns>The resulting text.</returns>
    public string GetText()
    {
        DocxTextOptions options = new DocxTextOptions();
        return GetTextWithOptions(options);
    }

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <param name="options">The options that control this operation.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string GetTextWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return DocxTextExtractor.ExtractFootnotes(this, options);
    }

    internal DocxFootnote CreateFootnote(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return CreateFootnoteWithId(AllocateFootnoteId(), text);
    }

    internal DocxFootnote CreateFootnoteWithId(string id, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Footnote id is required.", "id");
        }

        DocxFootnote footnote = DocxFootnote.Create(id, text);
        AddFootnote(footnote);
        return footnote;
    }

    internal void AddFootnote(DocxFootnote footnote)
    {
        if (footnote == null)
        {
            throw new ArgumentNullException("footnote");
        }

        if (footnote.Id == "")
        {
            throw new ArgumentException("Footnote id is required.", "footnote");
        }

        if (FindFootnoteById(footnote.Id) != null)
        {
            throw new InvalidOperationException("Footnote id already exists.");
        }

        this.Xml = "";
        this.Footnotes.Add(footnote);
    }

    internal bool RemoveFootnoteById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        for (int index = 0; index < this.Footnotes.Count; index = index + 1)
        {
            if (this.Footnotes[index].Id == id)
            {
                this.Xml = "";
                this.Footnotes.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal string AllocateFootnoteId()
    {
        int highestId = 0;

        foreach (DocxFootnote footnote in this.Footnotes)
        {
            if (IsNonNegativeInteger(footnote.Id))
            {
                int id = ParseNonNegativeInteger(footnote.Id);

                if (id > highestId)
                {
                    highestId = id;
                }
            }
        }

        if (highestId == int.MaxValue)
        {
            throw new InvalidOperationException("No footnote id is available.");
        }

        return (highestId + 1).ToString();
    }

    internal static bool IsNonNegativeInteger(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return false;
        }

        for (int index = 0; index < value.Length; index = index + 1)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
        }

        int parsed = 0;
        return int.TryParse(value, out parsed);
    }

    internal static int ParseNonNegativeInteger(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!IsNonNegativeInteger(value))
        {
            throw new ArgumentException("Value must be a non-negative integer.", "value");
        }

        int parsed = 0;

        if (!int.TryParse(value, out parsed))
        {
            throw new ArgumentException("Value must be a non-negative integer.", "value");
        }

        return parsed;
    }

    internal string WriteXml()
    {
        if (this.Xml != "")
        {
            return this.Xml;
        }

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

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "footnotes", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxFootnote footnote in this.Footnotes)
        {
            footnote.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
