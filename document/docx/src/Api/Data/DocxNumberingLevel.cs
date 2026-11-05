namespace docx;

/// <summary>
/// Represents numbering level data used by a DOCX document.
/// </summary>
public class DocxNumberingLevel
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The level index value stored in the document model.
    /// </summary>
    public string LevelIndex = "";
    /// <summary>
    /// The start value stored in the document model.
    /// </summary>
    public string Start = "";
    /// <summary>
    /// The number format value stored in the document model.
    /// </summary>
    public string NumberFormat = "";
    /// <summary>
    /// The level text value stored in the document model.
    /// </summary>
    public string LevelText = "";
    /// <summary>
    /// The level justification value stored in the document model.
    /// </summary>
    public string LevelJustification = "";
    /// <summary>
    /// The suffix value stored in the document model.
    /// </summary>
    public string Suffix = "";
    internal string ParagraphPropertiesXml = "";
    internal string RunPropertiesXml = "";
    /// <summary>
    /// The paragraph format value stored in the document model.
    /// </summary>
    public DocxParagraphFormat ParagraphFormat = new DocxParagraphFormat();
    /// <summary>
    /// The run format value stored in the document model.
    /// </summary>
    public DocxRunFormat RunFormat = new DocxRunFormat();

    /// <summary>
    /// Creates a numbering level instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxNumberingLevel(string xml)
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

    internal static DocxNumberingLevel Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("lvl", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:lvl.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxNumberingLevel(buffer.GetXml());
    }

    internal static DocxNumberingLevel Create(string levelIndex, string start, string numberFormat, string levelText)
    {
        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        if (levelText == null)
        {
            throw new ArgumentNullException("levelText");
        }

        if (levelIndex == "")
        {
            throw new ArgumentException("Level index is required.", "levelIndex");
        }

        if (numberFormat == "")
        {
            throw new ArgumentException("Number format is required.", "numberFormat");
        }

        DocxNumberingLevel level = new DocxNumberingLevel("");
        level.LevelIndex = levelIndex;
        level.Start = start;
        level.NumberFormat = numberFormat;
        level.LevelText = levelText;
        return level;
    }

    internal void SetParagraphIndent(string left, string hanging)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (hanging == null)
        {
            throw new ArgumentNullException("hanging");
        }

        this.ParagraphFormat.SetIndent(left, "", "", hanging);
        this.ParagraphPropertiesXml = "";
        this.Xml = "";
    }

    internal void SetRunFonts(string ascii, string highAnsi, string eastAsia, string complexScript)
    {
        if (ascii == null)
        {
            throw new ArgumentNullException("ascii");
        }

        if (highAnsi == null)
        {
            throw new ArgumentNullException("highAnsi");
        }

        if (eastAsia == null)
        {
            throw new ArgumentNullException("eastAsia");
        }

        if (complexScript == null)
        {
            throw new ArgumentNullException("complexScript");
        }

        this.RunFormat.SetFonts(ascii, highAnsi, eastAsia, complexScript);
        this.RunPropertiesXml = "";
        this.Xml = "";
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

        if (!reader.ReadToElement("lvl", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.LevelIndex = reader.GetAttribute("ilvl", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int levelDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == levelDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == levelDepth + 1)
            {
                bool isParagraphProperties = reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl);
                bool isRunProperties = reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isParagraphProperties)
                {
                    this.ParagraphPropertiesXml = child.GetXml();
                    this.ParagraphFormat = DocxParagraphFormat.ParseXml(this.ParagraphPropertiesXml);
                }
                else if (isRunProperties)
                {
                    this.RunPropertiesXml = child.GetXml();
                    this.RunFormat = DocxRunFormat.ParseXml(this.RunPropertiesXml);
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
        this.Start = "";
        this.NumberFormat = "";
        this.LevelText = "";
        this.LevelJustification = "";
        this.Suffix = "";
        this.ParagraphPropertiesXml = "";
        this.RunPropertiesXml = "";
        this.ParagraphFormat = new DocxParagraphFormat();
        this.RunFormat = new DocxRunFormat();
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

        if (reader.IsElement("start", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Start = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("numFmt", DocxConstants.NamespaceWordprocessingMl))
        {
            this.NumberFormat = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("lvlText", DocxConstants.NamespaceWordprocessingMl))
        {
            this.LevelText = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("lvlJc", DocxConstants.NamespaceWordprocessingMl))
        {
            this.LevelJustification = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("suff", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Suffix = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
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

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
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

        writer.StartElement("w", "lvl", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "ilvl", this.LevelIndex);
        WriteValueElement(writer, "start", this.Start);
        WriteValueElement(writer, "numFmt", this.NumberFormat);
        WriteValueElement(writer, "lvlText", this.LevelText);
        WriteValueElement(writer, "lvlJc", this.LevelJustification);
        WriteValueElement(writer, "suff", this.Suffix);
        WriteParagraphPropertiesTo(writer);
        WriteRunPropertiesTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteParagraphPropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.ParagraphPropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.ParagraphPropertiesXml);
            buffer.WriteTo(writer);
            return;
        }

        if (this.ParagraphFormat.HasAnyFormatting())
        {
            this.ParagraphFormat.WriteTo(writer);
        }
    }

    internal void WriteRunPropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.RunPropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.RunPropertiesXml);
            buffer.WriteTo(writer);
            return;
        }

        if (this.RunFormat.HasAnyFormatting())
        {
            this.RunFormat.WriteTo(writer);
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
            if (!IsManagedLevelChild(child))
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

    internal static bool IsManagedLevelChild(DocxXmlBuffer child)
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

        return IsManagedLevelChildName(reader.LocalName());
    }

    internal static bool IsManagedLevelChildName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "start"
            || localName == "numFmt"
            || localName == "lvlText"
            || localName == "lvlJc"
            || localName == "suff"
            || localName == "pPr"
            || localName == "rPr";
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal bool HasParagraphFormat()
    {
        return this.ParagraphPropertiesXml != "" || this.ParagraphFormat.HasAnyFormatting();
    }

    internal bool HasRunFormat()
    {
        return this.RunPropertiesXml != "" || this.RunFormat.HasAnyFormatting();
    }

    internal bool HasAnyDefinition()
    {
        return this.LevelIndex != ""
            || this.Start != ""
            || this.NumberFormat != ""
            || this.LevelText != ""
            || this.LevelJustification != ""
            || this.Suffix != ""
            || HasParagraphFormat()
            || HasRunFormat();
    }
}
