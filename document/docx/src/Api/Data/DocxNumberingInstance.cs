namespace docx;

/// <summary>
/// Represents numbering instance data used by a DOCX document.
/// </summary>
public class DocxNumberingInstance
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxNumberingLevelOverride> LevelOverrides = new List<DocxNumberingLevelOverride>();
    /// <summary>
    /// The numbering identifier stored in the document.
    /// </summary>
    public string NumberingId = "";
    /// <summary>
    /// The abstract numbering identifier stored in the document.
    /// </summary>
    public string AbstractNumberingId = "";

    /// <summary>
    /// Creates a numbering instance instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxNumberingInstance(string xml)
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

    internal static DocxNumberingInstance Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("num", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:num.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxNumberingInstance(buffer.GetXml());
    }

    internal static DocxNumberingInstance Create(string numberingId, string abstractNumberingId)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (abstractNumberingId == null)
        {
            throw new ArgumentNullException("abstractNumberingId");
        }

        if (numberingId == "")
        {
            throw new ArgumentException("Numbering id is required.", "numberingId");
        }

        if (abstractNumberingId == "")
        {
            throw new ArgumentException("Abstract numbering id is required.", "abstractNumberingId");
        }

        DocxNumberingInstance instance = new DocxNumberingInstance("");
        instance.NumberingId = numberingId;
        instance.AbstractNumberingId = abstractNumberingId;
        return instance;
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

        if (!reader.ReadToElement("num", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.NumberingId = reader.GetAttribute("numId", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int numberDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == numberDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == numberDepth + 1)
            {
                bool isLevelOverride = reader.IsElement("lvlOverride", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isLevelOverride)
                {
                    AddParsedLevelOverride(DocxNumberingLevelOverrideFromBuffer(child));
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
        this.LevelOverrides = new List<DocxNumberingLevelOverride>();
        this.NumberingId = "";
        this.AbstractNumberingId = "";
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

        if (reader.IsElement("abstractNumId", DocxConstants.NamespaceWordprocessingMl))
        {
            this.AbstractNumberingId = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
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

    internal void AddLevelOverride(DocxNumberingLevelOverride levelOverride)
    {
        if (levelOverride == null)
        {
            throw new ArgumentNullException("levelOverride");
        }

        this.LevelOverrides.Add(levelOverride);
        this.Xml = "";
    }

    internal void AddParsedLevelOverride(DocxNumberingLevelOverride levelOverride)
    {
        if (levelOverride == null)
        {
            throw new ArgumentNullException("levelOverride");
        }

        this.LevelOverrides.Add(levelOverride);
    }

    internal static DocxNumberingLevelOverride DocxNumberingLevelOverrideFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxNumberingLevelOverride.Parse(reader);
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

        foreach (DocxNumberingLevelOverride levelOverride in this.LevelOverrides)
        {
            levelOverride.ClearRawXml();
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

        writer.StartElement("w", "num", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "numId", this.NumberingId);
        WriteValueElement(writer, "abstractNumId", this.AbstractNumberingId);
        WriteLevelOverridesTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteLevelOverridesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxNumberingLevelOverride levelOverride in this.LevelOverrides)
        {
            levelOverride.WriteTo(writer);
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
            if (!IsManagedNumberingInstanceChild(child))
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

    internal static bool IsManagedNumberingInstanceChild(DocxXmlBuffer child)
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

        return IsManagedNumberingInstanceChildName(reader.LocalName());
    }

    internal static bool IsManagedNumberingInstanceChildName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "abstractNumId"
            || localName == "lvlOverride";
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int LevelOverrideCount()
    {
        return this.LevelOverrides.Count;
    }

    internal DocxNumberingLevelOverride GetLevelOverride(int index)
    {
        return this.LevelOverrides[index];
    }

    internal DocxNumberingLevelOverride? FindLevelOverrideByIndex(string levelIndex)
    {
        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        foreach (DocxNumberingLevelOverride levelOverride in this.LevelOverrides)
        {
            if (levelOverride.LevelIndex == levelIndex)
            {
                return levelOverride;
            }
        }

        return null;
    }

    internal bool HasAnyDefinition()
    {
        return this.NumberingId != ""
            || this.AbstractNumberingId != ""
            || this.LevelOverrides.Count > 0;
    }
}
