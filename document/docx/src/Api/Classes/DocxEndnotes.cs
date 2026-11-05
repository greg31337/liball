namespace docx;

/// <summary>
/// Represents the endnotes part and its note definitions.
/// </summary>
public class DocxEndnotes : IDocxTextContent
{
    internal string Xml;
    internal List<DocxEndnote> Endnotes;
    internal List<DocxXmlBuffer> UnknownChildren;

    /// <summary>
    /// Creates an endnotes instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxEndnotes(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Endnotes = new List<DocxEndnote>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadFromXml();
    }

    internal static DocxEndnotes Create()
    {
        return new DocxEndnotes("");
    }

    internal static DocxEndnotes Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("endnotes", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:endnotes.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxEndnotes(buffer.GetXml());
    }

    internal static DocxEndnotes ParseXml(string xml)
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
        this.Endnotes = new List<DocxEndnote>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("endnotes", DocxConstants.NamespaceWordprocessingMl))
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

                if (IsEndnoteBuffer(child))
                {
                    this.Endnotes.Add(DocxEndnote.ParseXml(child.GetXml()));
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

    internal bool IsEndnoteBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        return reader.MoveToContent() && reader.IsElement("endnote", DocxConstants.NamespaceWordprocessingMl);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "" || this.Endnotes.Count > 0 || this.UnknownChildren.Count > 0;
    }

    internal int EndnoteCount()
    {
        return this.Endnotes.Count;
    }

    internal DocxEndnote GetEndnote(int index)
    {
        return this.Endnotes[index];
    }

    internal DocxEndnote? FindEndnoteById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxEndnote endnote in this.Endnotes)
        {
            if (endnote.Id == id)
            {
                return endnote;
            }
        }

        return null;
    }

    internal int CountEndnotesByType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        int count = 0;

        foreach (DocxEndnote endnote in this.Endnotes)
        {
            if (endnote.Type == type)
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

        return DocxTextExtractor.ExtractEndnotes(this, options);
    }

    internal DocxEndnote CreateEndnote(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        return CreateEndnoteWithId(AllocateEndnoteId(), text);
    }

    internal DocxEndnote CreateEndnoteWithId(string id, string text)
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
            throw new ArgumentException("Endnote id is required.", "id");
        }

        DocxEndnote endnote = DocxEndnote.Create(id, text);
        AddEndnote(endnote);
        return endnote;
    }

    internal void AddEndnote(DocxEndnote endnote)
    {
        if (endnote == null)
        {
            throw new ArgumentNullException("endnote");
        }

        if (endnote.Id == "")
        {
            throw new ArgumentException("Endnote id is required.", "endnote");
        }

        if (FindEndnoteById(endnote.Id) != null)
        {
            throw new InvalidOperationException("Endnote id already exists.");
        }

        this.Xml = "";
        this.Endnotes.Add(endnote);
    }

    internal bool RemoveEndnoteById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        for (int index = 0; index < this.Endnotes.Count; index = index + 1)
        {
            if (this.Endnotes[index].Id == id)
            {
                this.Xml = "";
                this.Endnotes.RemoveAt(index);
                return true;
            }
        }

        return false;
    }

    internal string AllocateEndnoteId()
    {
        int highestId = 0;

        foreach (DocxEndnote endnote in this.Endnotes)
        {
            if (IsNonNegativeInteger(endnote.Id))
            {
                int id = ParseNonNegativeInteger(endnote.Id);

                if (id > highestId)
                {
                    highestId = id;
                }
            }
        }

        if (highestId == int.MaxValue)
        {
            throw new InvalidOperationException("No endnote id is available.");
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

        writer.StartElement("w", "endnotes", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxEndnote endnote in this.Endnotes)
        {
            endnote.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }
}
