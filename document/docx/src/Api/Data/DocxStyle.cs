namespace docx;

/// <summary>
/// Represents style data used by a DOCX document.
/// </summary>
public class DocxStyle
{
    /// <summary>
    /// The type paragraph identifier used in DOCX package data.
    /// </summary>
    public const string TypeParagraph = "paragraph";
    /// <summary>
    /// The type character identifier used in DOCX package data.
    /// </summary>
    public const string TypeCharacter = "character";
    /// <summary>
    /// The type table identifier used in DOCX package data.
    /// </summary>
    public const string TypeTable = "table";
    /// <summary>
    /// The type numbering identifier used in DOCX package data.
    /// </summary>
    public const string TypeNumbering = "numbering";

    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The type value stored in the document model.
    /// </summary>
    public string Type = "";
    /// <summary>
    /// The style identifier stored in the document.
    /// </summary>
    public string StyleId = "";
    /// <summary>
    /// Whether default was explicitly supplied.
    /// </summary>
    public bool HasDefault;
    /// <summary>
    /// Whether the default behavior is enabled.
    /// </summary>
    public bool Default;
    /// <summary>
    /// Whether custom style was explicitly supplied.
    /// </summary>
    public bool HasCustomStyle;
    /// <summary>
    /// Whether the custom style behavior is enabled.
    /// </summary>
    public bool CustomStyle;
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// The alternate names associated with the style.
    /// </summary>
    public List<string> Aliases = new List<string>();
    /// <summary>
    /// The user interface priority stored for the style.
    /// </summary>
    public string UiPriority = "";
    /// <summary>
    /// Whether hidden was explicitly supplied.
    /// </summary>
    public bool HasHidden;
    /// <summary>
    /// Whether the style is hidden from all user interface lists.
    /// </summary>
    public bool Hidden;
    /// <summary>
    /// Whether semi hidden was explicitly supplied.
    /// </summary>
    public bool HasSemiHidden;
    /// <summary>
    /// Whether the style is hidden from the main style gallery.
    /// </summary>
    public bool SemiHidden;
    /// <summary>
    /// Whether unhide when used was explicitly supplied.
    /// </summary>
    public bool HasUnhideWhenUsed;
    /// <summary>
    /// Whether the style becomes visible after it is used.
    /// </summary>
    public bool UnhideWhenUsed;
    /// <summary>
    /// Whether quick format was explicitly supplied.
    /// </summary>
    public bool HasQuickFormat;
    /// <summary>
    /// Whether the style is available in the quick style gallery.
    /// </summary>
    public bool QuickFormat;
    /// <summary>
    /// Whether locked was explicitly supplied.
    /// </summary>
    public bool HasLocked;
    /// <summary>
    /// Whether the style is locked against user interface changes.
    /// </summary>
    public bool Locked;
    /// <summary>
    /// The based on value stored in the document model.
    /// </summary>
    public string BasedOn = "";
    /// <summary>
    /// The next value stored in the document model.
    /// </summary>
    public string Next = "";
    /// <summary>
    /// The link value stored in the document model.
    /// </summary>
    public string Link = "";
    internal string ParagraphPropertiesXml = "";
    internal string RunPropertiesXml = "";
    internal string TablePropertiesXml = "";
    internal string NumberingPropertiesXml = "";
    /// <summary>
    /// The paragraph format value stored in the document model.
    /// </summary>
    public DocxParagraphFormat ParagraphFormat = new DocxParagraphFormat();
    /// <summary>
    /// The run format value stored in the document model.
    /// </summary>
    public DocxRunFormat RunFormat = new DocxRunFormat();
    /// <summary>
    /// The table format value stored in the document model.
    /// </summary>
    public DocxTableFormat TableFormat = new DocxTableFormat();
    /// <summary>
    /// The numbering level value stored in the document model.
    /// </summary>
    public string NumberingLevel = "";
    /// <summary>
    /// The numbering identifier stored in the document.
    /// </summary>
    public string NumberingId = "";

    /// <summary>
    /// Creates a style instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxStyle(string xml)
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

    internal static DocxStyle Create(string type, string styleId, string name)
    {
        DocxStyle style = new DocxStyle("");
        style.SetIdentity(type, styleId, name);
        return style;
    }

    internal static DocxStyle Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("style", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:style.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxStyle(buffer.GetXml());
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

        if (!reader.ReadToElement("style", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        ReadAttributes(reader);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int styleDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == styleDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == styleDepth + 1)
            {
                if (reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    ReadParagraphProperties(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    ReadRunProperties(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tblPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    ReadTableProperties(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("numPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    ReadNumberingProperties(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                ReadChild(reader);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void InitializeParsedState()
    {
        this.Type = "";
        this.StyleId = "";
        this.HasDefault = false;
        this.Default = false;
        this.HasCustomStyle = false;
        this.CustomStyle = false;
        this.Name = "";
        this.Aliases = new List<string>();
        this.UiPriority = "";
        this.HasHidden = false;
        this.Hidden = false;
        this.HasSemiHidden = false;
        this.SemiHidden = false;
        this.HasUnhideWhenUsed = false;
        this.UnhideWhenUsed = false;
        this.HasQuickFormat = false;
        this.QuickFormat = false;
        this.HasLocked = false;
        this.Locked = false;
        this.BasedOn = "";
        this.Next = "";
        this.Link = "";
        this.ParagraphPropertiesXml = "";
        this.RunPropertiesXml = "";
        this.TablePropertiesXml = "";
        this.NumberingPropertiesXml = "";
        this.ParagraphFormat = new DocxParagraphFormat();
        this.RunFormat = new DocxRunFormat();
        this.TableFormat = new DocxTableFormat();
        this.NumberingLevel = "";
        this.NumberingId = "";
    }

    internal void ReadAttributes(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("style", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:style.");
        }

        this.Type = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        this.StyleId = reader.GetAttribute("styleId", DocxConstants.NamespaceWordprocessingMl);
        ReadDefaultAttribute(reader);
        ReadCustomStyleAttribute(reader);
    }

    internal void ReadChild(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("name", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Name = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("aliases", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadAliases(reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl));
        }
        else if (reader.IsElement("uiPriority", DocxConstants.NamespaceWordprocessingMl))
        {
            this.UiPriority = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("hidden", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasHidden = true;
            this.Hidden = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("semiHidden", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasSemiHidden = true;
            this.SemiHidden = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("unhideWhenUsed", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasUnhideWhenUsed = true;
            this.UnhideWhenUsed = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("qFormat", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasQuickFormat = true;
            this.QuickFormat = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("locked", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasLocked = true;
            this.Locked = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("basedOn", DocxConstants.NamespaceWordprocessingMl))
        {
            this.BasedOn = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("next", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Next = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("link", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Link = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void ReadAliases(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Aliases = new List<string>();
        string[] values = value.Split(new char[] { ',' });

        foreach (string aliasValue in values)
        {
            string alias = aliasValue.Trim();

            if (alias != "")
            {
                this.Aliases.Add(alias);
            }
        }
    }

    internal void ReadDefaultAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("default", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasDefault = true;
            this.Default = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadCustomStyleAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("customStyle", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasCustomStyle = true;
            this.CustomStyle = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadParagraphProperties(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pPr.");
        }

        DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
        AddChild(child);
        this.ParagraphPropertiesXml = child.GetXml();
        this.ParagraphFormat = DocxParagraphFormat.ParseXml(this.ParagraphPropertiesXml);
        CopyNumberingFromParagraphFormat();
    }

    internal void ReadRunProperties(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("rPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:rPr.");
        }

        DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
        AddChild(child);
        this.RunPropertiesXml = child.GetXml();
        this.RunFormat = DocxRunFormat.ParseXml(this.RunPropertiesXml);
    }

    internal void ReadTableProperties(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tblPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tblPr.");
        }

        DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
        AddChild(child);
        this.TablePropertiesXml = child.GetXml();
        this.TableFormat = DocxTableFormat.ParseXml(this.TablePropertiesXml);
    }

    internal void ReadNumberingProperties(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("numPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:numPr.");
        }

        DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
        AddChild(child);
        this.NumberingPropertiesXml = child.GetXml();
        ReadNumberingPropertiesXml(this.NumberingPropertiesXml);
    }

    internal void ReadNumberingPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("numPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        DocxParagraphFormat format = new DocxParagraphFormat();
        format.ReadNumbering(reader);
        this.NumberingLevel = format.NumberingLevel;
        this.NumberingId = format.NumberingId;
    }

    internal void CopyNumberingFromParagraphFormat()
    {
        this.NumberingLevel = this.ParagraphFormat.NumberingLevel;
        this.NumberingId = this.ParagraphFormat.NumberingId;
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddUnknownChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
        ClearRawXml();
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal bool HasIdentity()
    {
        return this.Type != ""
            || this.StyleId != ""
            || this.Name != ""
            || this.Aliases.Count > 0
            || this.UiPriority != ""
            || this.HasHidden
            || this.HasSemiHidden
            || this.HasUnhideWhenUsed
            || this.HasQuickFormat
            || this.HasLocked
            || this.BasedOn != ""
            || this.Next != ""
            || this.Link != ""
            || this.HasDefault
            || this.HasCustomStyle;
    }

    internal bool IsParagraphStyle()
    {
        return this.Type == TypeParagraph;
    }

    internal bool IsCharacterStyle()
    {
        return this.Type == TypeCharacter;
    }

    internal bool IsTableStyle()
    {
        return this.Type == TypeTable;
    }

    internal bool IsNumberingStyle()
    {
        return this.Type == TypeNumbering;
    }

    internal bool HasParagraphFormat()
    {
        return this.ParagraphFormat.HasAnyFormatting();
    }

    internal bool HasRunFormat()
    {
        return this.RunFormat.HasAnyFormatting();
    }

    internal bool HasTableFormat()
    {
        return this.TableFormat.HasAnyFormatting();
    }

    internal bool HasNumbering()
    {
        return this.NumberingLevel != "" || this.NumberingId != "";
    }

    internal void SetIdentity(string type, string styleId, string name)
    {
        SetType(type);
        SetStyleId(styleId);
        SetName(name);
    }

    internal void SetType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!IsSupportedStyleType(value))
        {
            throw new ArgumentException("Style type is not supported.", "value");
        }

        this.Type = value;
        ClearRawXml();
    }

    internal void SetStyleId(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            throw new ArgumentException("Style id is required.", "value");
        }

        this.StyleId = value;
        ClearRawXml();
    }

    internal void SetName(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Name = value;
        ClearRawXml();
    }

    internal void SetBasedOn(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.BasedOn = value;
        ClearRawXml();
    }

    internal void SetNext(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Next = value;
        ClearRawXml();
    }

    internal void SetLink(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Link = value;
        ClearRawXml();
    }

    internal void SetDefault(bool value)
    {
        this.HasDefault = true;
        this.Default = value;
        ClearRawXml();
    }

    internal void ClearDefault()
    {
        this.HasDefault = false;
        this.Default = false;
        ClearRawXml();
    }

    internal void SetCustomStyle(bool value)
    {
        this.HasCustomStyle = true;
        this.CustomStyle = value;
        ClearRawXml();
    }

    internal void ClearCustomStyle()
    {
        this.HasCustomStyle = false;
        this.CustomStyle = false;
        ClearRawXml();
    }

    internal void SetParagraphFormat(DocxParagraphFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.ParagraphFormat = format;
        this.ParagraphPropertiesXml = "";
        CopyNumberingFromParagraphFormat();
        ClearRawXml();
    }

    internal void SetRunFormat(DocxRunFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.RunFormat = format;
        this.RunPropertiesXml = "";
        ClearRawXml();
    }

    internal void SetTableFormat(DocxTableFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.TableFormat = format;
        this.TablePropertiesXml = "";
        ClearRawXml();
    }

    internal void SetNumbering(string level, string id)
    {
        if (level == null)
        {
            throw new ArgumentNullException("level");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        this.NumberingLevel = level;
        this.NumberingId = id;
        this.NumberingPropertiesXml = "";
        ClearRawXml();
    }

    internal void ClearRawXml()
    {
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

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "style", DocxConstants.NamespaceWordprocessingMl);
        WriteAttributesTo(writer);
        WriteMetadataTo(writer);
        WritePropertiesTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteAttributesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteOptionalAttribute(writer, "type", this.Type);
        WriteOptionalAttribute(writer, "styleId", this.StyleId);

        if (this.HasDefault)
        {
            DocxXmlAttributes.WriteBool(writer, "w", "default", DocxConstants.NamespaceWordprocessingMl, this.Default);
        }

        if (this.HasCustomStyle)
        {
            DocxXmlAttributes.WriteBool(writer, "w", "customStyle", DocxConstants.NamespaceWordprocessingMl, this.CustomStyle);
        }
    }

    internal void WriteMetadataTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteValueElement(writer, "name", this.Name);
        WriteValueElement(writer, "aliases", string.Join(",", this.Aliases));
        WriteValueElement(writer, "basedOn", this.BasedOn);
        WriteValueElement(writer, "next", this.Next);
        WriteValueElement(writer, "link", this.Link);
        WriteToggleElement(writer, "hidden", this.HasHidden, this.Hidden);
        WriteValueElement(writer, "uiPriority", this.UiPriority);
        WriteToggleElement(writer, "semiHidden", this.HasSemiHidden, this.SemiHidden);
        WriteToggleElement(writer, "unhideWhenUsed", this.HasUnhideWhenUsed, this.UnhideWhenUsed);
        WriteToggleElement(writer, "qFormat", this.HasQuickFormat, this.QuickFormat);
        WriteToggleElement(writer, "locked", this.HasLocked, this.Locked);
    }

    internal void WritePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteParagraphPropertiesTo(writer);
        WriteRunPropertiesTo(writer);
        WriteTablePropertiesTo(writer);
        WriteDirectNumberingPropertiesTo(writer);
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

    internal void WriteTablePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.TablePropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.TablePropertiesXml);
            buffer.WriteTo(writer);
            return;
        }

        if (this.TableFormat.HasAnyFormatting())
        {
            this.TableFormat.WriteTo(writer);
        }
    }

    internal void WriteDirectNumberingPropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.NumberingPropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.NumberingPropertiesXml);
            buffer.WriteTo(writer);
            return;
        }

        if (!ShouldWriteDirectNumberingProperties())
        {
            return;
        }

        writer.StartElement("w", "numPr", DocxConstants.NamespaceWordprocessingMl);
        WriteNumberingValueElement(writer, "ilvl", this.NumberingLevel);
        WriteNumberingValueElement(writer, "numId", this.NumberingId);
        writer.EndElement();
    }

    internal bool ShouldWriteDirectNumberingProperties()
    {
        if (!HasNumbering())
        {
            return false;
        }

        if (!this.ParagraphFormat.HasAnyFormatting())
        {
            return true;
        }

        if (this.ParagraphFormat.NumberingLevel != this.NumberingLevel)
        {
            return true;
        }

        if (this.ParagraphFormat.NumberingId != this.NumberingId)
        {
            return true;
        }

        return false;
    }

    internal void WriteUnknownChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (!IsManagedStyleChild(child))
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

    internal void WriteNumberingValueElement(DocxXmlWriter writer, string localName, string value)
    {
        WriteValueElement(writer, localName, value);
    }

    internal void WriteToggleElement(DocxXmlWriter writer, string localName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (!hasValue)
        {
            return;
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        DocxXmlAttributes.WriteBool(writer, "w", "val", DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal static bool IsManagedStyleChild(DocxXmlBuffer child)
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

        return IsManagedStyleChildName(reader.LocalName());
    }

    internal static bool IsManagedStyleChildName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        return localName == "name"
            || localName == "aliases"
            || localName == "basedOn"
            || localName == "next"
            || localName == "link"
            || localName == "uiPriority"
            || localName == "hidden"
            || localName == "semiHidden"
            || localName == "unhideWhenUsed"
            || localName == "qFormat"
            || localName == "locked"
            || localName == "pPr"
            || localName == "rPr"
            || localName == "tblPr"
            || localName == "numPr";
    }

    internal static bool IsSupportedStyleType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        return value == TypeParagraph
            || value == TypeCharacter
            || value == TypeTable
            || value == TypeNumbering;
    }
}
