namespace docx;

/// <summary>
/// Represents table format data used by a DOCX document.
/// </summary>
public class DocxTableFormat
{
    internal string Xml;
    /// <summary>
    /// The style identifier stored in the document.
    /// </summary>
    public string StyleId;
    /// <summary>
    /// The width value stored in the document model.
    /// </summary>
    public string Width;
    /// <summary>
    /// The width type value stored in the document model.
    /// </summary>
    public string WidthType;
    /// <summary>
    /// The alignment value stored in the document model.
    /// </summary>
    public string Alignment;
    /// <summary>
    /// The layout type value stored in the document model.
    /// </summary>
    public string LayoutType;
    /// <summary>
    /// The indent width value stored in the document model.
    /// </summary>
    public string IndentWidth;
    /// <summary>
    /// The indent type value stored in the document model.
    /// </summary>
    public string IndentType;
    /// <summary>
    /// The spacing between adjacent table cells.
    /// </summary>
    public string CellSpacingWidth;
    /// <summary>
    /// The unit used for spacing between adjacent table cells.
    /// </summary>
    public string CellSpacingType;
    /// <summary>
    /// The table caption.
    /// </summary>
    public string Caption;
    /// <summary>
    /// The table description used by accessibility tools.
    /// </summary>
    public string Description;
    /// <summary>
    /// The look value value stored in the document model.
    /// </summary>
    public string LookValue;
    /// <summary>
    /// Whether first row was explicitly supplied.
    /// </summary>
    public bool HasFirstRow;
    /// <summary>
    /// Whether the first row behavior is enabled.
    /// </summary>
    public bool FirstRow;
    /// <summary>
    /// Whether last row was explicitly supplied.
    /// </summary>
    public bool HasLastRow;
    /// <summary>
    /// Whether the last row behavior is enabled.
    /// </summary>
    public bool LastRow;
    /// <summary>
    /// Whether first column was explicitly supplied.
    /// </summary>
    public bool HasFirstColumn;
    /// <summary>
    /// Whether the first column behavior is enabled.
    /// </summary>
    public bool FirstColumn;
    /// <summary>
    /// Whether last column was explicitly supplied.
    /// </summary>
    public bool HasLastColumn;
    /// <summary>
    /// Whether the last column behavior is enabled.
    /// </summary>
    public bool LastColumn;
    /// <summary>
    /// Whether no horizontal band was explicitly supplied.
    /// </summary>
    public bool HasNoHorizontalBand;
    /// <summary>
    /// Whether the no horizontal band behavior is enabled.
    /// </summary>
    public bool NoHorizontalBand;
    /// <summary>
    /// Whether no vertical band was explicitly supplied.
    /// </summary>
    public bool HasNoVerticalBand;
    /// <summary>
    /// Whether the no vertical band behavior is enabled.
    /// </summary>
    public bool NoVerticalBand;
    /// <summary>
    /// The cell margin top width value stored in the document model.
    /// </summary>
    public string CellMarginTopWidth;
    /// <summary>
    /// The cell margin top type value stored in the document model.
    /// </summary>
    public string CellMarginTopType;
    /// <summary>
    /// The cell margin left width value stored in the document model.
    /// </summary>
    public string CellMarginLeftWidth;
    /// <summary>
    /// The cell margin left type value stored in the document model.
    /// </summary>
    public string CellMarginLeftType;
    /// <summary>
    /// The cell margin bottom width value stored in the document model.
    /// </summary>
    public string CellMarginBottomWidth;
    /// <summary>
    /// The cell margin bottom type value stored in the document model.
    /// </summary>
    public string CellMarginBottomType;
    /// <summary>
    /// The cell margin right width value stored in the document model.
    /// </summary>
    public string CellMarginRightWidth;
    /// <summary>
    /// The cell margin right type value stored in the document model.
    /// </summary>
    public string CellMarginRightType;
    /// <summary>
    /// The borders value stored in the document model.
    /// </summary>
    public DocxBorders Borders;
    /// <summary>
    /// The shading value stored in the document model.
    /// </summary>
    public DocxShading Shading;

    /// <summary>
    /// Creates a table format instance.
    /// </summary>
    public DocxTableFormat()
    {
        this.Xml = "";
        this.StyleId = "";
        this.Width = "";
        this.WidthType = "";
        this.Alignment = "";
        this.LayoutType = "";
        this.IndentWidth = "";
        this.IndentType = "";
        this.CellSpacingWidth = "";
        this.CellSpacingType = "";
        this.Caption = "";
        this.Description = "";
        this.LookValue = "";
        this.HasFirstRow = false;
        this.FirstRow = false;
        this.HasLastRow = false;
        this.LastRow = false;
        this.HasFirstColumn = false;
        this.FirstColumn = false;
        this.HasLastColumn = false;
        this.LastColumn = false;
        this.HasNoHorizontalBand = false;
        this.NoHorizontalBand = false;
        this.HasNoVerticalBand = false;
        this.NoVerticalBand = false;
        this.CellMarginTopWidth = "";
        this.CellMarginTopType = "";
        this.CellMarginLeftWidth = "";
        this.CellMarginLeftType = "";
        this.CellMarginBottomWidth = "";
        this.CellMarginBottomType = "";
        this.CellMarginRightWidth = "";
        this.CellMarginRightType = "";
        this.Borders = new DocxBorders();
        this.Shading = new DocxShading();
    }

    internal static DocxTableFormat Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tblPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tblPr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxTableFormat ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxTableFormat format = new DocxTableFormat();
        format.Xml = xml;

        if (xml == "")
        {
            return format;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("tblPr", DocxConstants.NamespaceWordprocessingMl))
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
                if (reader.IsElement("tblCellMar", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.ReadCellMargins(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tblBorders", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.Borders = DocxBorders.Parse(reader);
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

        if (reader.IsElement("tblStyle", DocxConstants.NamespaceWordprocessingMl))
        {
            this.StyleId = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblW", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Width = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.WidthType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("jc", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Alignment = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblLayout", DocxConstants.NamespaceWordprocessingMl))
        {
            this.LayoutType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblInd", DocxConstants.NamespaceWordprocessingMl))
        {
            this.IndentWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.IndentType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblCellSpacing", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CellSpacingWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.CellSpacingType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblCaption", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Caption = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblDescription", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Description = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("tblLook", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadTableLook(reader);
        }
    }

    internal void ReadTableLook(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tblLook", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tblLook.");
        }

        this.LookValue = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        ReadLookFirstRow(reader);
        ReadLookLastRow(reader);
        ReadLookFirstColumn(reader);
        ReadLookLastColumn(reader);
        ReadLookNoHorizontalBand(reader);
        ReadLookNoVerticalBand(reader);
    }

    internal void ReadCellMargins(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tblCellMar", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tblCellMar.");
        }

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int marginsDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == marginsDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == marginsDepth + 1)
            {
                ReadCellMarginElement(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadCellMarginElement(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("top", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CellMarginTopWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.CellMarginTopType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("left", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CellMarginLeftWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.CellMarginLeftType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("bottom", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CellMarginBottomWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.CellMarginBottomType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("right", DocxConstants.NamespaceWordprocessingMl))
        {
            this.CellMarginRightWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.CellMarginRightType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void ReadLookFirstRow(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("firstRow", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasFirstRow = true;
            this.FirstRow = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadLookLastRow(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("lastRow", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasLastRow = true;
            this.LastRow = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadLookFirstColumn(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("firstColumn", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasFirstColumn = true;
            this.FirstColumn = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadLookLastColumn(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("lastColumn", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasLastColumn = true;
            this.LastColumn = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadLookNoHorizontalBand(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("noHBand", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasNoHorizontalBand = true;
            this.NoHorizontalBand = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadLookNoVerticalBand(DocxXmlReader reader)
    {
        string value = reader.GetAttribute("noVBand", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasNoVerticalBand = true;
            this.NoVerticalBand = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal bool HasAnyFormatting()
    {
        return this.StyleId != ""
            || this.Width != ""
            || this.WidthType != ""
            || this.Alignment != ""
            || this.LayoutType != ""
            || this.IndentWidth != ""
            || this.IndentType != ""
            || this.CellSpacingWidth != ""
            || this.CellSpacingType != ""
            || this.Caption != ""
            || this.Description != ""
            || this.LookValue != ""
            || this.HasFirstRow
            || this.HasLastRow
            || this.HasFirstColumn
            || this.HasLastColumn
            || this.HasNoHorizontalBand
            || this.HasNoVerticalBand
            || this.CellMarginTopWidth != ""
            || this.CellMarginTopType != ""
            || this.CellMarginLeftWidth != ""
            || this.CellMarginLeftType != ""
            || this.CellMarginBottomWidth != ""
            || this.CellMarginBottomType != ""
            || this.CellMarginRightWidth != ""
            || this.CellMarginRightType != ""
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

    internal void SetWidth(string width, string widthType)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        this.Width = width;
        this.WidthType = widthType;
    }

    internal void SetAlignment(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.Alignment = value;
    }

    internal void SetLayoutType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.LayoutType = value;
    }

    internal void SetIndent(string width, string widthType)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        this.IndentWidth = width;
        this.IndentType = widthType;
    }

    internal void SetLookValue(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.LookValue = value;
    }

    internal void SetFirstRow(bool value)
    {
        this.HasFirstRow = true;
        this.FirstRow = value;
    }

    internal void ClearFirstRow()
    {
        this.HasFirstRow = false;
        this.FirstRow = false;
    }

    internal void SetLastRow(bool value)
    {
        this.HasLastRow = true;
        this.LastRow = value;
    }

    internal void ClearLastRow()
    {
        this.HasLastRow = false;
        this.LastRow = false;
    }

    internal void SetFirstColumn(bool value)
    {
        this.HasFirstColumn = true;
        this.FirstColumn = value;
    }

    internal void ClearFirstColumn()
    {
        this.HasFirstColumn = false;
        this.FirstColumn = false;
    }

    internal void SetLastColumn(bool value)
    {
        this.HasLastColumn = true;
        this.LastColumn = value;
    }

    internal void ClearLastColumn()
    {
        this.HasLastColumn = false;
        this.LastColumn = false;
    }

    internal void SetNoHorizontalBand(bool value)
    {
        this.HasNoHorizontalBand = true;
        this.NoHorizontalBand = value;
    }

    internal void ClearNoHorizontalBand()
    {
        this.HasNoHorizontalBand = false;
        this.NoHorizontalBand = false;
    }

    internal void SetNoVerticalBand(bool value)
    {
        this.HasNoVerticalBand = true;
        this.NoVerticalBand = value;
    }

    internal void ClearNoVerticalBand()
    {
        this.HasNoVerticalBand = false;
        this.NoVerticalBand = false;
    }

    internal void SetCellMargins(string topWidth, string topType, string leftWidth, string leftType, string bottomWidth, string bottomType, string rightWidth, string rightType)
    {
        if (topWidth == null)
        {
            throw new ArgumentNullException("topWidth");
        }

        if (topType == null)
        {
            throw new ArgumentNullException("topType");
        }

        if (leftWidth == null)
        {
            throw new ArgumentNullException("leftWidth");
        }

        if (leftType == null)
        {
            throw new ArgumentNullException("leftType");
        }

        if (bottomWidth == null)
        {
            throw new ArgumentNullException("bottomWidth");
        }

        if (bottomType == null)
        {
            throw new ArgumentNullException("bottomType");
        }

        if (rightWidth == null)
        {
            throw new ArgumentNullException("rightWidth");
        }

        if (rightType == null)
        {
            throw new ArgumentNullException("rightType");
        }

        this.CellMarginTopWidth = topWidth;
        this.CellMarginTopType = topType;
        this.CellMarginLeftWidth = leftWidth;
        this.CellMarginLeftType = leftType;
        this.CellMarginBottomWidth = bottomWidth;
        this.CellMarginBottomType = bottomType;
        this.CellMarginRightWidth = rightWidth;
        this.CellMarginRightType = rightType;
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

        writer.StartElement("w", "tblPr", DocxConstants.NamespaceWordprocessingMl);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteStringProperty(writer, "tblStyle", "val", this.StyleId);
        WriteWidthTo(writer);
        WriteStringProperty(writer, "jc", "val", this.Alignment);
        WriteCellSpacingTo(writer);
        WriteIndentTo(writer);
        WriteBordersTo(writer);
        WriteShadingTo(writer);
        WriteStringProperty(writer, "tblLayout", "type", this.LayoutType);
        WriteCellMarginsTo(writer);
        WriteTableLookTo(writer);
        WriteStringProperty(writer, "tblCaption", "val", this.Caption);
        WriteStringProperty(writer, "tblDescription", "val", this.Description);
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

    internal void WriteWidthTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Width == "" && this.WidthType == "")
        {
            return;
        }

        writer.StartElement("w", "tblW", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", this.Width);
        WriteOptionalAttribute(writer, "type", this.WidthType);
        writer.EndElement();
    }

    internal void WriteIndentTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.IndentWidth == "" && this.IndentType == "")
        {
            return;
        }

        writer.StartElement("w", "tblInd", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", this.IndentWidth);
        WriteOptionalAttribute(writer, "type", this.IndentType);
        writer.EndElement();
    }

    internal void WriteTableLookTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.LookValue == ""
            && !this.HasFirstRow
            && !this.HasLastRow
            && !this.HasFirstColumn
            && !this.HasLastColumn
            && !this.HasNoHorizontalBand
            && !this.HasNoVerticalBand)
        {
            return;
        }

        writer.StartElement("w", "tblLook", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.LookValue);
        WriteOptionalBoolAttribute(writer, "firstRow", this.HasFirstRow, this.FirstRow);
        WriteOptionalBoolAttribute(writer, "lastRow", this.HasLastRow, this.LastRow);
        WriteOptionalBoolAttribute(writer, "firstColumn", this.HasFirstColumn, this.FirstColumn);
        WriteOptionalBoolAttribute(writer, "lastColumn", this.HasLastColumn, this.LastColumn);
        WriteOptionalBoolAttribute(writer, "noHBand", this.HasNoHorizontalBand, this.NoHorizontalBand);
        WriteOptionalBoolAttribute(writer, "noVBand", this.HasNoVerticalBand, this.NoVerticalBand);
        writer.EndElement();
    }

    internal void WriteCellSpacingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.CellSpacingWidth == "" && this.CellSpacingType == "")
        {
            return;
        }

        writer.StartElement("w", "tblCellSpacing", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", this.CellSpacingWidth);
        WriteOptionalAttribute(writer, "type", this.CellSpacingType);
        writer.EndElement();
    }

    internal void WriteCellMarginsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.CellMarginTopWidth == ""
            && this.CellMarginTopType == ""
            && this.CellMarginLeftWidth == ""
            && this.CellMarginLeftType == ""
            && this.CellMarginBottomWidth == ""
            && this.CellMarginBottomType == ""
            && this.CellMarginRightWidth == ""
            && this.CellMarginRightType == "")
        {
            return;
        }

        writer.StartElement("w", "tblCellMar", DocxConstants.NamespaceWordprocessingMl);
        WriteCellMarginElement(writer, "top", this.CellMarginTopWidth, this.CellMarginTopType);
        WriteCellMarginElement(writer, "left", this.CellMarginLeftWidth, this.CellMarginLeftType);
        WriteCellMarginElement(writer, "bottom", this.CellMarginBottomWidth, this.CellMarginBottomType);
        WriteCellMarginElement(writer, "right", this.CellMarginRightWidth, this.CellMarginRightType);
        writer.EndElement();
    }

    internal void WriteCellMarginElement(DocxXmlWriter writer, string elementName, string width, string widthType)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        if (width == "" && widthType == "")
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", width);
        WriteOptionalAttribute(writer, "type", widthType);
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

    internal void WriteOptionalBoolAttribute(DocxXmlWriter writer, string localName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (hasValue)
        {
            DocxXmlAttributes.WriteBool(writer, "w", localName, DocxConstants.NamespaceWordprocessingMl, value);
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
            this.Borders.WriteTo(writer, "tblBorders");
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
