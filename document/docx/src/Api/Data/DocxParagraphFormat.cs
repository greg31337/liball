namespace docx;

/// <summary>
/// Represents paragraph format data used by a DOCX document.
/// </summary>
public class DocxParagraphFormat
{
    internal string Xml;
    /// <summary>
    /// The style identifier stored in the document.
    /// </summary>
    public string StyleId;
    /// <summary>
    /// The alignment value stored in the document model.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// The spacing before value stored in the document model.
    /// </summary>
    public string SpacingBefore;
    /// <summary>
    /// The spacing before value in hundredths of a line.
    /// </summary>
    public string SpacingBeforeLines;
    /// <summary>
    /// The automatic spacing-before setting.
    /// </summary>
    public string SpacingBeforeAuto;
    /// <summary>
    /// The spacing after value stored in the document model.
    /// </summary>
    public string SpacingAfter;
    /// <summary>
    /// The spacing after value in hundredths of a line.
    /// </summary>
    public string SpacingAfterLines;
    /// <summary>
    /// The automatic spacing-after setting.
    /// </summary>
    public string SpacingAfterAuto;
    /// <summary>
    /// The spacing line value stored in the document model.
    /// </summary>
    public string SpacingLine;
    /// <summary>
    /// The spacing line rule value stored in the document model.
    /// </summary>
    public string SpacingLineRule;
    /// <summary>
    /// The indent left value stored in the document model.
    /// </summary>
    public string IndentLeft;
    /// <summary>
    /// The indent right value stored in the document model.
    /// </summary>
    public string IndentRight;
    /// <summary>
    /// The logical start indentation value.
    /// </summary>
    public string IndentStart;
    /// <summary>
    /// The logical end indentation value.
    /// </summary>
    public string IndentEnd;
    /// <summary>
    /// The left indentation in character units.
    /// </summary>
    public string IndentLeftCharacters;
    /// <summary>
    /// The right indentation in character units.
    /// </summary>
    public string IndentRightCharacters;
    /// <summary>
    /// The logical start indentation in character units.
    /// </summary>
    public string IndentStartCharacters;
    /// <summary>
    /// The logical end indentation in character units.
    /// </summary>
    public string IndentEndCharacters;
    /// <summary>
    /// The first line value stored in the document model.
    /// </summary>
    public string FirstLine;
    /// <summary>
    /// The first-line indentation in character units.
    /// </summary>
    public string FirstLineCharacters;
    /// <summary>
    /// The hanging value stored in the document model.
    /// </summary>
    public string Hanging;
    /// <summary>
    /// The hanging indentation in character units.
    /// </summary>
    public string HangingCharacters;
    /// <summary>
    /// The numbering level value stored in the document model.
    /// </summary>
    public string NumberingLevel;
    /// <summary>
    /// The numbering identifier stored in the document.
    /// </summary>
    public string NumberingId;
    /// <summary>
    /// The outline level value stored in the document model.
    /// </summary>
    public string OutlineLevel;
    /// <summary>
    /// Whether keep next was explicitly supplied.
    /// </summary>
    public bool HasKeepNext;
    /// <summary>
    /// Whether the keep next behavior is enabled.
    /// </summary>
    public bool KeepNext;
    /// <summary>
    /// Whether keep lines was explicitly supplied.
    /// </summary>
    public bool HasKeepLines;
    /// <summary>
    /// Whether the keep lines behavior is enabled.
    /// </summary>
    public bool KeepLines;
    /// <summary>
    /// Whether page break before was explicitly supplied.
    /// </summary>
    public bool HasPageBreakBefore;
    /// <summary>
    /// Whether the page break before behavior is enabled.
    /// </summary>
    public bool PageBreakBefore;
    /// <summary>
    /// Whether widow control was explicitly supplied.
    /// </summary>
    public bool HasWidowControl;
    /// <summary>
    /// Whether the widow control behavior is enabled.
    /// </summary>
    public bool WidowControl;
    /// <summary>
    /// Whether contextual spacing was explicitly supplied.
    /// </summary>
    public bool HasContextualSpacing;
    /// <summary>
    /// Whether contextual paragraph spacing is enabled.
    /// </summary>
    public bool ContextualSpacing;
    /// <summary>
    /// Whether bidirectional paragraph layout was explicitly supplied.
    /// </summary>
    public bool HasBidirectional;
    /// <summary>
    /// Whether bidirectional paragraph layout is enabled.
    /// </summary>
    public bool Bidirectional;
    /// <summary>
    /// The paragraph text-flow direction.
    /// </summary>
    public string TextDirection;
    /// <summary>
    /// The custom paragraph tab stops in document order.
    /// </summary>
    public List<DocxParagraphTab> Tabs;
    /// <summary>
    /// The borders value stored in the document model.
    /// </summary>
    public DocxBorders Borders;
    /// <summary>
    /// The shading value stored in the document model.
    /// </summary>
    public DocxShading Shading;

    /// <summary>
    /// Creates a paragraph format instance.
    /// </summary>
    public DocxParagraphFormat()
    {
        this.Xml = "";
        this.StyleId = "";
        this.Alignment = "";
        this.SpacingBefore = "";
        this.SpacingBeforeLines = "";
        this.SpacingBeforeAuto = "";
        this.SpacingAfter = "";
        this.SpacingAfterLines = "";
        this.SpacingAfterAuto = "";
        this.SpacingLine = "";
        this.SpacingLineRule = "";
        this.IndentLeft = "";
        this.IndentRight = "";
        this.IndentStart = "";
        this.IndentEnd = "";
        this.IndentLeftCharacters = "";
        this.IndentRightCharacters = "";
        this.IndentStartCharacters = "";
        this.IndentEndCharacters = "";
        this.FirstLine = "";
        this.FirstLineCharacters = "";
        this.Hanging = "";
        this.HangingCharacters = "";
        this.NumberingLevel = "";
        this.NumberingId = "";
        this.OutlineLevel = "";
        this.HasKeepNext = false;
        this.KeepNext = false;
        this.HasKeepLines = false;
        this.KeepLines = false;
        this.HasPageBreakBefore = false;
        this.PageBreakBefore = false;
        this.HasWidowControl = false;
        this.WidowControl = false;
        this.HasContextualSpacing = false;
        this.ContextualSpacing = false;
        this.HasBidirectional = false;
        this.Bidirectional = false;
        this.TextDirection = "";
        this.Tabs = new List<DocxParagraphTab>();
        this.Borders = new DocxBorders();
        this.Shading = new DocxShading();
    }

    internal static DocxParagraphFormat Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pPr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxParagraphFormat ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxParagraphFormat format = new DocxParagraphFormat();
        format.Xml = xml;

        if (xml == "")
        {
            return format;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("pPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return format;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return format;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return format;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1)
            {
                if (reader.IsElement("numPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.ReadNumbering(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("pBdr", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.Borders = DocxBorders.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tabs", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.ReadTabs(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("shd", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.Shading = DocxShading.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                format.ReadProperty(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }

        return format;
    }

    internal void ReadProperty(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("pStyle", DocxConstants.NamespaceWordprocessingMl))
        {
            this.StyleId = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("jc", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Alignment = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("spacing", DocxConstants.NamespaceWordprocessingMl))
        {
            this.SpacingBefore = reader.GetAttribute("before", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingBeforeLines = reader.GetAttribute("beforeLines", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingBeforeAuto = reader.GetAttribute("beforeAutospacing", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingAfter = reader.GetAttribute("after", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingAfterLines = reader.GetAttribute("afterLines", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingAfterAuto = reader.GetAttribute("afterAutospacing", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingLine = reader.GetAttribute("line", DocxConstants.NamespaceWordprocessingMl);
            this.SpacingLineRule = reader.GetAttribute("lineRule", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("ind", DocxConstants.NamespaceWordprocessingMl))
        {
            this.IndentLeft = reader.GetAttribute("left", DocxConstants.NamespaceWordprocessingMl);
            this.IndentRight = reader.GetAttribute("right", DocxConstants.NamespaceWordprocessingMl);
            this.IndentStart = reader.GetAttribute("start", DocxConstants.NamespaceWordprocessingMl);
            this.IndentEnd = reader.GetAttribute("end", DocxConstants.NamespaceWordprocessingMl);
            this.IndentLeftCharacters = reader.GetAttribute("leftChars", DocxConstants.NamespaceWordprocessingMl);
            this.IndentRightCharacters = reader.GetAttribute("rightChars", DocxConstants.NamespaceWordprocessingMl);
            this.IndentStartCharacters = reader.GetAttribute("startChars", DocxConstants.NamespaceWordprocessingMl);
            this.IndentEndCharacters = reader.GetAttribute("endChars", DocxConstants.NamespaceWordprocessingMl);
            this.FirstLine = reader.GetAttribute("firstLine", DocxConstants.NamespaceWordprocessingMl);
            this.FirstLineCharacters = reader.GetAttribute("firstLineChars", DocxConstants.NamespaceWordprocessingMl);
            this.Hanging = reader.GetAttribute("hanging", DocxConstants.NamespaceWordprocessingMl);
            this.HangingCharacters = reader.GetAttribute("hangingChars", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("keepNext", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasKeepNext = true;
            this.KeepNext = ReadToggleValue(reader);
        }
        else if (reader.IsElement("keepLines", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasKeepLines = true;
            this.KeepLines = ReadToggleValue(reader);
        }
        else if (reader.IsElement("pageBreakBefore", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasPageBreakBefore = true;
            this.PageBreakBefore = ReadToggleValue(reader);
        }
        else if (reader.IsElement("widowControl", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasWidowControl = true;
            this.WidowControl = ReadToggleValue(reader);
        }
        else if (reader.IsElement("contextualSpacing", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasContextualSpacing = true;
            this.ContextualSpacing = ReadToggleValue(reader);
        }
        else if (reader.IsElement("bidi", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasBidirectional = true;
            this.Bidirectional = ReadToggleValue(reader);
        }
        else if (reader.IsElement("textDirection", DocxConstants.NamespaceWordprocessingMl))
        {
            this.TextDirection = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("outlineLvl", DocxConstants.NamespaceWordprocessingMl))
        {
            this.OutlineLevel = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void ReadNumbering(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("numPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:numPr.");
        }

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int numberingDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == numberingDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == numberingDepth + 1)
            {
                if (reader.IsElement("ilvl", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.NumberingLevel = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
                }
                else if (reader.IsElement("numId", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.NumberingId = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadTabs(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tabs", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tabs.");
        }

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int tabsDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == tabsDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element
                && reader.Depth() == tabsDepth + 1
                && reader.IsElement("tab", DocxConstants.NamespaceWordprocessingMl))
            {
                this.Tabs.Add(new DocxParagraphTab(
                    reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl),
                    reader.GetAttribute("pos", DocxConstants.NamespaceWordprocessingMl),
                    reader.GetAttribute("leader", DocxConstants.NamespaceWordprocessingMl)));
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal static bool ReadToggleValue(DocxXmlReader reader)
    {
        return DocxRunFormat.ReadToggleValue(reader);
    }

    internal bool HasAnyFormatting()
    {
        return this.StyleId != ""
            || this.Alignment != ""
            || this.SpacingBefore != ""
            || this.SpacingBeforeLines != ""
            || this.SpacingBeforeAuto != ""
            || this.SpacingAfter != ""
            || this.SpacingAfterLines != ""
            || this.SpacingAfterAuto != ""
            || this.SpacingLine != ""
            || this.SpacingLineRule != ""
            || this.IndentLeft != ""
            || this.IndentRight != ""
            || this.IndentStart != ""
            || this.IndentEnd != ""
            || this.IndentLeftCharacters != ""
            || this.IndentRightCharacters != ""
            || this.IndentStartCharacters != ""
            || this.IndentEndCharacters != ""
            || this.FirstLine != ""
            || this.FirstLineCharacters != ""
            || this.Hanging != ""
            || this.HangingCharacters != ""
            || this.NumberingLevel != ""
            || this.NumberingId != ""
            || this.OutlineLevel != ""
            || this.HasKeepNext
            || this.HasKeepLines
            || this.HasPageBreakBefore
            || this.HasWidowControl
            || this.HasContextualSpacing
            || this.HasBidirectional
            || this.TextDirection != ""
            || this.Tabs.Count > 0
            || this.Borders.HasAnyBorder()
            || this.Shading.HasAnyShading();
    }

    internal void SetStyleId(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.StyleId = value;
    }

    internal void SetAlignment(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Alignment = value;
    }

    internal void SetSpacing(string before, string after, string line, string lineRule)
    {
        if (before == null)
        {
            throw new ArgumentNullException("before");
        }

        if (after == null)
        {
            throw new ArgumentNullException("after");
        }

        if (line == null)
        {
            throw new ArgumentNullException("line");
        }

        if (lineRule == null)
        {
            throw new ArgumentNullException("lineRule");
        }

        this.SpacingBefore = before;
        this.SpacingAfter = after;
        this.SpacingLine = line;
        this.SpacingLineRule = lineRule;
    }

    internal void SetIndent(string left, string right, string firstLine, string hanging)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (firstLine == null)
        {
            throw new ArgumentNullException("firstLine");
        }

        if (hanging == null)
        {
            throw new ArgumentNullException("hanging");
        }

        this.IndentLeft = left;
        this.IndentRight = right;
        this.FirstLine = firstLine;
        this.Hanging = hanging;
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
    }

    internal void ClearNumbering()
    {
        this.NumberingLevel = "";
        this.NumberingId = "";
    }

    internal bool HasNumbering()
    {
        return this.NumberingLevel != "" || this.NumberingId != "";
    }

    internal void SetOutlineLevel(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.OutlineLevel = value;
    }

    internal void SetKeepNext(bool value)
    {
        this.HasKeepNext = true;
        this.KeepNext = value;
    }

    internal void ClearKeepNext()
    {
        this.HasKeepNext = false;
        this.KeepNext = false;
    }

    internal void SetKeepLines(bool value)
    {
        this.HasKeepLines = true;
        this.KeepLines = value;
    }

    internal void ClearKeepLines()
    {
        this.HasKeepLines = false;
        this.KeepLines = false;
    }

    internal void SetPageBreakBefore(bool value)
    {
        this.HasPageBreakBefore = true;
        this.PageBreakBefore = value;
    }

    internal void ClearPageBreakBefore()
    {
        this.HasPageBreakBefore = false;
        this.PageBreakBefore = false;
    }

    internal void SetWidowControl(bool value)
    {
        this.HasWidowControl = true;
        this.WidowControl = value;
    }

    internal void ClearWidowControl()
    {
        this.HasWidowControl = false;
        this.WidowControl = false;
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

        writer.StartElement("w", "pPr", DocxConstants.NamespaceWordprocessingMl);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteStringProperty(writer, "pStyle", "val", this.StyleId);
        WriteToggleProperty(writer, "keepNext", this.HasKeepNext, this.KeepNext);
        WriteToggleProperty(writer, "keepLines", this.HasKeepLines, this.KeepLines);
        WriteToggleProperty(writer, "pageBreakBefore", this.HasPageBreakBefore, this.PageBreakBefore);
        WriteToggleProperty(writer, "widowControl", this.HasWidowControl, this.WidowControl);
        WriteNumberingTo(writer);
        WriteBordersTo(writer);
        WriteShadingTo(writer);
        WriteTabsTo(writer);
        WriteToggleProperty(writer, "bidi", this.HasBidirectional, this.Bidirectional);
        WriteSpacingTo(writer);
        WriteIndentTo(writer);
        WriteToggleProperty(writer, "contextualSpacing", this.HasContextualSpacing, this.ContextualSpacing);
        WriteStringProperty(writer, "jc", "val", this.Alignment);
        WriteStringProperty(writer, "textDirection", "val", this.TextDirection);
        WriteStringProperty(writer, "outlineLvl", "val", this.OutlineLevel);
    }

    internal void WriteStringProperty(DocxXmlWriter writer, string elementName, string attributeName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", attributeName, DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal void WriteToggleProperty(DocxXmlWriter writer, string elementName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (!hasValue)
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);

        if (!value)
        {
            writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, "0");
        }

        writer.EndElement();
    }

    internal void WriteSpacingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.SpacingBefore == ""
            && this.SpacingBeforeLines == ""
            && this.SpacingBeforeAuto == ""
            && this.SpacingAfter == ""
            && this.SpacingAfterLines == ""
            && this.SpacingAfterAuto == ""
            && this.SpacingLine == ""
            && this.SpacingLineRule == "")
        {
            return;
        }

        writer.StartElement("w", "spacing", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "before", this.SpacingBefore);
        WriteOptionalAttribute(writer, "beforeLines", this.SpacingBeforeLines);
        WriteOptionalAttribute(writer, "beforeAutospacing", this.SpacingBeforeAuto);
        WriteOptionalAttribute(writer, "after", this.SpacingAfter);
        WriteOptionalAttribute(writer, "afterLines", this.SpacingAfterLines);
        WriteOptionalAttribute(writer, "afterAutospacing", this.SpacingAfterAuto);
        WriteOptionalAttribute(writer, "line", this.SpacingLine);
        WriteOptionalAttribute(writer, "lineRule", this.SpacingLineRule);
        writer.EndElement();
    }

    internal void WriteIndentTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.IndentLeft == ""
            && this.IndentRight == ""
            && this.IndentStart == ""
            && this.IndentEnd == ""
            && this.IndentLeftCharacters == ""
            && this.IndentRightCharacters == ""
            && this.IndentStartCharacters == ""
            && this.IndentEndCharacters == ""
            && this.FirstLine == ""
            && this.FirstLineCharacters == ""
            && this.Hanging == ""
            && this.HangingCharacters == "")
        {
            return;
        }

        writer.StartElement("w", "ind", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "left", this.IndentLeft);
        WriteOptionalAttribute(writer, "right", this.IndentRight);
        WriteOptionalAttribute(writer, "start", this.IndentStart);
        WriteOptionalAttribute(writer, "end", this.IndentEnd);
        WriteOptionalAttribute(writer, "leftChars", this.IndentLeftCharacters);
        WriteOptionalAttribute(writer, "rightChars", this.IndentRightCharacters);
        WriteOptionalAttribute(writer, "startChars", this.IndentStartCharacters);
        WriteOptionalAttribute(writer, "endChars", this.IndentEndCharacters);
        WriteOptionalAttribute(writer, "firstLine", this.FirstLine);
        WriteOptionalAttribute(writer, "firstLineChars", this.FirstLineCharacters);
        WriteOptionalAttribute(writer, "hanging", this.Hanging);
        WriteOptionalAttribute(writer, "hangingChars", this.HangingCharacters);
        writer.EndElement();
    }

    internal void WriteTabsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Tabs.Count == 0)
        {
            return;
        }

        writer.StartElement("w", "tabs", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxParagraphTab tab in this.Tabs)
        {
            writer.StartElement("w", "tab", DocxConstants.NamespaceWordprocessingMl);
            WriteOptionalAttribute(writer, "val", tab.Alignment);
            WriteOptionalAttribute(writer, "pos", tab.Position);
            WriteOptionalAttribute(writer, "leader", tab.Leader);
            writer.EndElement();
        }

        writer.EndElement();
    }

    internal void WriteNumberingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.NumberingLevel == "" && this.NumberingId == "")
        {
            return;
        }

        writer.StartElement("w", "numPr", DocxConstants.NamespaceWordprocessingMl);
        WriteStringProperty(writer, "ilvl", "val", this.NumberingLevel);
        WriteStringProperty(writer, "numId", "val", this.NumberingId);
        writer.EndElement();
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

    internal void WriteBordersTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Borders.HasAnyBorder())
        {
            this.Borders.WriteTo(writer, "pBdr");
        }
    }

    internal void WriteShadingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Shading.HasAnyShading())
        {
            this.Shading.WriteTo(writer);
        }
    }
}
