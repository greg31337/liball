namespace docx;

/// <summary>
/// Represents numbering level override data used by a DOCX document.
/// </summary>
public class DocxNumberingLevelOverride
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The level index value stored in the document model.
    /// </summary>
    public string LevelIndex = "";
    /// <summary>
    /// The start override value stored in the document model.
    /// </summary>
    public string StartOverride = "";
    /// <summary>
    /// The level value stored in the document model.
    /// </summary>
    public DocxNumberingLevel Level = new DocxNumberingLevel("");

    /// <summary>
    /// Creates a numbering level override instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxNumberingLevelOverride(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxNumberingLevelOverride Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("lvlOverride", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:lvlOverride.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxNumberingLevelOverride(buffer.GetXml());
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
        this.Children = new List<DocxXmlBuffer>();
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("lvlOverride", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.LevelIndex = reader.GetAttribute("ilvl", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int overrideDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == overrideDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == overrideDepth + 1)
            {
                bool isLevel = reader.IsElement("lvl", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isLevel)
                {
                    this.Level = DocxNumberingLevelFromBuffer(child);
                }
                else
                {
                    ReadChild(child);
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void InitializeParsedState()
    {
        this.LevelIndex = "";
        this.StartOverride = "";
        this.Level = new DocxNumberingLevel("");
    }

    internal void ReadChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return;
        }

        if (reader.IsElement("startOverride", DocxConstants.NamespaceWordprocessingMl))
        {
            this.StartOverride = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal static DocxNumberingLevel DocxNumberingLevelFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxNumberingLevel.Parse(reader);
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
        this.Level.ClearRawXml();
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

        writer.StartElement("w", "lvlOverride", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "ilvl", this.LevelIndex);
        WriteValueElement(writer, "startOverride", this.StartOverride);
        WriteLevelTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteLevelTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Level.HasXml() || this.Level.HasAnyDefinition())
        {
            this.Level.WriteTo(writer);
        }
    }

    internal void WriteUnknownChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (!IsManagedLevelOverrideChild(child))
            {
                child.WriteTo(writer);
            }
        }
    }

    internal void WriteOptionalAttribute(DocxXmlWriter writer, string localName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value != "")
        {
            writer.WriteAttribute("w", localName, DocxConstants.NamespaceWordprocessingMl, value);
        }
    }

    internal void WriteValueElement(DocxXmlWriter writer, string localName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal static bool IsManagedLevelOverrideChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (reader.NamespaceUri() != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        return IsManagedLevelOverrideChildName(reader.LocalName());
    }

    internal static bool IsManagedLevelOverrideChildName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "startOverride"
            || localName == "lvl";
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal bool HasLevel()
    {
        return this.Level.HasXml() || this.Level.HasAnyDefinition();
    }

    internal bool HasAnyOverride()
    {
        return this.LevelIndex != ""
            || this.StartOverride != ""
            || HasLevel();
    }
}
