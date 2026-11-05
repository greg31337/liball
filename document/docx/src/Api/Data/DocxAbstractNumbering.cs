namespace docx;

/// <summary>
/// Represents abstract numbering data used by a DOCX document.
/// </summary>
public class DocxAbstractNumbering
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxNumberingLevel> Levels = new List<DocxNumberingLevel>();
    /// <summary>
    /// The abstract numbering identifier stored in the document.
    /// </summary>
    public string AbstractNumberingId = "";
    /// <summary>
    /// The nsid value stored in the document model.
    /// </summary>
    public string Nsid = "";
    /// <summary>
    /// The multi level type value stored in the document model.
    /// </summary>
    public string MultiLevelType = "";
    /// <summary>
    /// The template code value stored in the document model.
    /// </summary>
    public string TemplateCode = "";
    /// <summary>
    /// The style link value stored in the document model.
    /// </summary>
    public string StyleLink = "";
    /// <summary>
    /// The numbering style link value stored in the document model.
    /// </summary>
    public string NumberingStyleLink = "";

    /// <summary>
    /// Creates an abstract numbering instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxAbstractNumbering(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        this.Levels = new List<DocxNumberingLevel>();
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxAbstractNumbering Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("abstractNum", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:abstractNum.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxAbstractNumbering(buffer.GetXml());
    }

    internal static DocxAbstractNumbering CreateSingleLevel(string abstractNumberingId, string multiLevelType, DocxNumberingLevel level)
    {
        if (abstractNumberingId == null)
        {
            throw new ArgumentNullException("abstractNumberingId");
        }

        if (multiLevelType == null)
        {
            throw new ArgumentNullException("multiLevelType");
        }

        if (level == null)
        {
            throw new ArgumentNullException("level");
        }

        if (abstractNumberingId == "")
        {
            throw new ArgumentException("Abstract numbering id is required.", "abstractNumberingId");
        }

        DocxAbstractNumbering abstractNumbering = new DocxAbstractNumbering("");
        abstractNumbering.AbstractNumberingId = abstractNumberingId;
        abstractNumbering.MultiLevelType = multiLevelType;
        abstractNumbering.AddLevel(level);
        return abstractNumbering;
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
        this.Levels = new List<DocxNumberingLevel>();
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("abstractNum", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.AbstractNumberingId = reader.GetAttribute("abstractNumId", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int abstractDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == abstractDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == abstractDepth + 1)
            {
                bool isLevel = reader.IsElement("lvl", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isLevel)
                {
                    AddParsedLevel(DocxNumberingLevelFromBuffer(child));
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
        this.AbstractNumberingId = "";
        this.Nsid = "";
        this.MultiLevelType = "";
        this.TemplateCode = "";
        this.StyleLink = "";
        this.NumberingStyleLink = "";
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

        if (reader.IsElement("nsid", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Nsid = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("multiLevelType", DocxConstants.NamespaceWordprocessingMl))
        {
            this.MultiLevelType = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tmpl", DocxConstants.NamespaceWordprocessingMl))
        {
            this.TemplateCode = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("styleLink", DocxConstants.NamespaceWordprocessingMl))
        {
            this.StyleLink = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("numStyleLink", DocxConstants.NamespaceWordprocessingMl))
        {
            this.NumberingStyleLink = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
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

    internal void AddLevel(DocxNumberingLevel level)
    {
        if (level == null)
        {
            throw new ArgumentNullException("level");
        }

        this.Levels.Add(level);
        this.Xml = "";
    }

    internal void AddParsedLevel(DocxNumberingLevel level)
    {
        if (level == null)
        {
            throw new ArgumentNullException("level");
        }

        this.Levels.Add(level);
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

        foreach (DocxNumberingLevel level in this.Levels)
        {
            level.ClearRawXml();
        }
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

        writer.StartElement("w", "abstractNum", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "abstractNumId", this.AbstractNumberingId);
        WriteValueElement(writer, "nsid", this.Nsid);
        WriteValueElement(writer, "multiLevelType", this.MultiLevelType);
        WriteValueElement(writer, "tmpl", this.TemplateCode);
        WriteValueElement(writer, "styleLink", this.StyleLink);
        WriteValueElement(writer, "numStyleLink", this.NumberingStyleLink);
        WriteLevelsTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteLevelsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxNumberingLevel level in this.Levels)
        {
            level.WriteTo(writer);
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
            if (!IsManagedAbstractNumberingChild(child))
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

    internal static bool IsManagedAbstractNumberingChild(DocxXmlBuffer child)
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

        return IsManagedAbstractNumberingChildName(reader.LocalName());
    }

    internal static bool IsManagedAbstractNumberingChildName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "nsid"
            || localName == "multiLevelType"
            || localName == "tmpl"
            || localName == "styleLink"
            || localName == "numStyleLink"
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

    internal int LevelCount()
    {
        return this.Levels.Count;
    }

    internal DocxNumberingLevel GetLevel(int index)
    {
        return this.Levels[index];
    }

    internal DocxNumberingLevel? FindLevelByIndex(string levelIndex)
    {
        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        foreach (DocxNumberingLevel level in this.Levels)
        {
            if (level.LevelIndex == levelIndex)
            {
                return level;
            }
        }

        return null;
    }

    internal bool HasAnyDefinition()
    {
        return this.AbstractNumberingId != ""
            || this.Nsid != ""
            || this.MultiLevelType != ""
            || this.TemplateCode != ""
            || this.StyleLink != ""
            || this.NumberingStyleLink != ""
            || this.Levels.Count > 0;
    }
}
