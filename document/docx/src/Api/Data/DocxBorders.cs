namespace docx;

/// <summary>
/// Represents borders data used by a DOCX document.
/// </summary>
public class DocxBorders
{
    internal string Xml;
    /// <summary>
    /// The single val value stored in the document model.
    /// </summary>
    public string SingleVal;
    /// <summary>
    /// The single color value stored in the document model.
    /// </summary>
    public string SingleColor;
    /// <summary>
    /// The single size value stored in the document model.
    /// </summary>
    public string SingleSize;
    /// <summary>
    /// The single space value stored in the document model.
    /// </summary>
    public string SingleSpace;
    /// <summary>
    /// The top val value stored in the document model.
    /// </summary>
    public string TopVal;
    /// <summary>
    /// The top color value stored in the document model.
    /// </summary>
    public string TopColor;
    /// <summary>
    /// The top size value stored in the document model.
    /// </summary>
    public string TopSize;
    /// <summary>
    /// The top space value stored in the document model.
    /// </summary>
    public string TopSpace;
    /// <summary>
    /// The left val value stored in the document model.
    /// </summary>
    public string LeftVal;
    /// <summary>
    /// The left color value stored in the document model.
    /// </summary>
    public string LeftColor;
    /// <summary>
    /// The left size value stored in the document model.
    /// </summary>
    public string LeftSize;
    /// <summary>
    /// The left space value stored in the document model.
    /// </summary>
    public string LeftSpace;
    /// <summary>
    /// The bottom val value stored in the document model.
    /// </summary>
    public string BottomVal;
    /// <summary>
    /// The bottom color value stored in the document model.
    /// </summary>
    public string BottomColor;
    /// <summary>
    /// The bottom size value stored in the document model.
    /// </summary>
    public string BottomSize;
    /// <summary>
    /// The bottom space value stored in the document model.
    /// </summary>
    public string BottomSpace;
    /// <summary>
    /// The right val value stored in the document model.
    /// </summary>
    public string RightVal;
    /// <summary>
    /// The right color value stored in the document model.
    /// </summary>
    public string RightColor;
    /// <summary>
    /// The right size value stored in the document model.
    /// </summary>
    public string RightSize;
    /// <summary>
    /// The right space value stored in the document model.
    /// </summary>
    public string RightSpace;
    /// <summary>
    /// The inside horizontal val value stored in the document model.
    /// </summary>
    public string InsideHorizontalVal;
    /// <summary>
    /// The inside horizontal color value stored in the document model.
    /// </summary>
    public string InsideHorizontalColor;
    /// <summary>
    /// The inside horizontal size value stored in the document model.
    /// </summary>
    public string InsideHorizontalSize;
    /// <summary>
    /// The inside horizontal space value stored in the document model.
    /// </summary>
    public string InsideHorizontalSpace;
    /// <summary>
    /// The inside vertical val value stored in the document model.
    /// </summary>
    public string InsideVerticalVal;
    /// <summary>
    /// The inside vertical color value stored in the document model.
    /// </summary>
    public string InsideVerticalColor;
    /// <summary>
    /// The inside vertical size value stored in the document model.
    /// </summary>
    public string InsideVerticalSize;
    /// <summary>
    /// The inside vertical space value stored in the document model.
    /// </summary>
    public string InsideVerticalSpace;
    /// <summary>
    /// The between val value stored in the document model.
    /// </summary>
    public string BetweenVal;
    /// <summary>
    /// The between color value stored in the document model.
    /// </summary>
    public string BetweenColor;
    /// <summary>
    /// The between size value stored in the document model.
    /// </summary>
    public string BetweenSize;
    /// <summary>
    /// The between space value stored in the document model.
    /// </summary>
    public string BetweenSpace;
    /// <summary>
    /// The bar val value stored in the document model.
    /// </summary>
    public string BarVal;
    /// <summary>
    /// The bar color value stored in the document model.
    /// </summary>
    public string BarColor;
    /// <summary>
    /// The bar size value stored in the document model.
    /// </summary>
    public string BarSize;
    /// <summary>
    /// The bar space value stored in the document model.
    /// </summary>
    public string BarSpace;

    /// <summary>
    /// Creates a borders instance.
    /// </summary>
    public DocxBorders()
    {
        this.Xml = "";
        this.SingleVal = "";
        this.SingleColor = "";
        this.SingleSize = "";
        this.SingleSpace = "";
        this.TopVal = "";
        this.TopColor = "";
        this.TopSize = "";
        this.TopSpace = "";
        this.LeftVal = "";
        this.LeftColor = "";
        this.LeftSize = "";
        this.LeftSpace = "";
        this.BottomVal = "";
        this.BottomColor = "";
        this.BottomSize = "";
        this.BottomSpace = "";
        this.RightVal = "";
        this.RightColor = "";
        this.RightSize = "";
        this.RightSpace = "";
        this.InsideHorizontalVal = "";
        this.InsideHorizontalColor = "";
        this.InsideHorizontalSize = "";
        this.InsideHorizontalSpace = "";
        this.InsideVerticalVal = "";
        this.InsideVerticalColor = "";
        this.InsideVerticalSize = "";
        this.InsideVerticalSpace = "";
        this.BetweenVal = "";
        this.BetweenColor = "";
        this.BetweenSize = "";
        this.BetweenSpace = "";
        this.BarVal = "";
        this.BarColor = "";
        this.BarSize = "";
        this.BarSpace = "";
    }

    internal static DocxBorders Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupportedRoot(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on a supported border element.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxBorders ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxBorders borders = new DocxBorders();
        borders.Xml = xml;

        if (xml == "")
        {
            return borders;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!ReadToSupportedRoot(reader))
        {
            return borders;
        }

        if (reader.IsElement("bdr", DocxConstants.NamespaceWordprocessingMl))
        {
            borders.ReadBorderElement(reader);
            return borders;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return borders;
        }

        int bordersDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == bordersDepth)
            {
                return borders;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == bordersDepth + 1)
            {
                borders.ReadBorderElement(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }

        return borders;
    }

    internal static bool ReadToSupportedRoot(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.MoveToContent() && IsSupportedRoot(reader))
        {
            return true;
        }

        while (reader.Read())
        {
            if (IsSupportedRoot(reader))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsSupportedRoot(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("bdr", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("pBdr", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("tblBorders", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("tcBorders", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("pgBorders", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void ReadBorderElement(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("bdr", DocxConstants.NamespaceWordprocessingMl))
        {
            this.SingleVal = ReadVal(reader);
            this.SingleColor = ReadColor(reader);
            this.SingleSize = ReadSize(reader);
            this.SingleSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("top", DocxConstants.NamespaceWordprocessingMl))
        {
            this.TopVal = ReadVal(reader);
            this.TopColor = ReadColor(reader);
            this.TopSize = ReadSize(reader);
            this.TopSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("left", DocxConstants.NamespaceWordprocessingMl))
        {
            this.LeftVal = ReadVal(reader);
            this.LeftColor = ReadColor(reader);
            this.LeftSize = ReadSize(reader);
            this.LeftSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("bottom", DocxConstants.NamespaceWordprocessingMl))
        {
            this.BottomVal = ReadVal(reader);
            this.BottomColor = ReadColor(reader);
            this.BottomSize = ReadSize(reader);
            this.BottomSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("right", DocxConstants.NamespaceWordprocessingMl))
        {
            this.RightVal = ReadVal(reader);
            this.RightColor = ReadColor(reader);
            this.RightSize = ReadSize(reader);
            this.RightSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("insideH", DocxConstants.NamespaceWordprocessingMl))
        {
            this.InsideHorizontalVal = ReadVal(reader);
            this.InsideHorizontalColor = ReadColor(reader);
            this.InsideHorizontalSize = ReadSize(reader);
            this.InsideHorizontalSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("insideV", DocxConstants.NamespaceWordprocessingMl))
        {
            this.InsideVerticalVal = ReadVal(reader);
            this.InsideVerticalColor = ReadColor(reader);
            this.InsideVerticalSize = ReadSize(reader);
            this.InsideVerticalSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("between", DocxConstants.NamespaceWordprocessingMl))
        {
            this.BetweenVal = ReadVal(reader);
            this.BetweenColor = ReadColor(reader);
            this.BetweenSize = ReadSize(reader);
            this.BetweenSpace = ReadSpace(reader);
        }
        else if (reader.IsElement("bar", DocxConstants.NamespaceWordprocessingMl))
        {
            this.BarVal = ReadVal(reader);
            this.BarColor = ReadColor(reader);
            this.BarSize = ReadSize(reader);
            this.BarSpace = ReadSpace(reader);
        }
    }

    internal static string ReadVal(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static string ReadColor(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.GetAttribute("color", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static string ReadSize(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.GetAttribute("sz", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static string ReadSpace(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.GetAttribute("space", DocxConstants.NamespaceWordprocessingMl);
    }

    internal bool HasAnyBorder()
    {
        return this.SingleVal != ""
            || this.SingleColor != ""
            || this.SingleSize != ""
            || this.SingleSpace != ""
            || this.TopVal != ""
            || this.TopColor != ""
            || this.TopSize != ""
            || this.TopSpace != ""
            || this.LeftVal != ""
            || this.LeftColor != ""
            || this.LeftSize != ""
            || this.LeftSpace != ""
            || this.BottomVal != ""
            || this.BottomColor != ""
            || this.BottomSize != ""
            || this.BottomSpace != ""
            || this.RightVal != ""
            || this.RightColor != ""
            || this.RightSize != ""
            || this.RightSpace != ""
            || this.InsideHorizontalVal != ""
            || this.InsideHorizontalColor != ""
            || this.InsideHorizontalSize != ""
            || this.InsideHorizontalSpace != ""
            || this.InsideVerticalVal != ""
            || this.InsideVerticalColor != ""
            || this.InsideVerticalSize != ""
            || this.InsideVerticalSpace != ""
            || this.BetweenVal != ""
            || this.BetweenColor != ""
            || this.BetweenSize != ""
            || this.BetweenSpace != ""
            || this.BarVal != ""
            || this.BarColor != ""
            || this.BarSize != ""
            || this.BarSpace != "";
    }

    internal void SetSingle(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.SingleVal = val;
        this.SingleColor = color;
        this.SingleSize = size;
        this.SingleSpace = space;
    }

    internal void SetTop(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.TopVal = val;
        this.TopColor = color;
        this.TopSize = size;
        this.TopSpace = space;
    }

    internal void SetLeft(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.LeftVal = val;
        this.LeftColor = color;
        this.LeftSize = size;
        this.LeftSpace = space;
    }

    internal void SetBottom(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.BottomVal = val;
        this.BottomColor = color;
        this.BottomSize = size;
        this.BottomSpace = space;
    }

    internal void SetRight(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.RightVal = val;
        this.RightColor = color;
        this.RightSize = size;
        this.RightSpace = space;
    }

    internal void SetInsideHorizontal(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.InsideHorizontalVal = val;
        this.InsideHorizontalColor = color;
        this.InsideHorizontalSize = size;
        this.InsideHorizontalSpace = space;
    }

    internal void SetInsideVertical(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.InsideVerticalVal = val;
        this.InsideVerticalColor = color;
        this.InsideVerticalSize = size;
        this.InsideVerticalSpace = space;
    }

    internal void SetBetween(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.BetweenVal = val;
        this.BetweenColor = color;
        this.BetweenSize = size;
        this.BetweenSpace = space;
    }

    internal void SetBar(string val, string color, string size, string space)
    {
        ValidateBorderValues(val, color, size, space);
        this.BarVal = val;
        this.BarColor = color;
        this.BarSize = size;
        this.BarSpace = space;
    }

    internal static void ValidateBorderValues(string val, string color, string size, string space)
    {
        if (val == null)
        {
            throw new ArgumentNullException("val");
        }

        if (color == null)
        {
            throw new ArgumentNullException("color");
        }

        if (size == null)
        {
            throw new ArgumentNullException("size");
        }

        if (space == null)
        {
            throw new ArgumentNullException("space");
        }
    }

    internal string WriteXml(string rootName)
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer, rootName);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer, string rootName)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        ValidateRootName(rootName);

        if (rootName == "bdr")
        {
            WriteBorderElement(writer, "bdr", this.SingleVal, this.SingleColor, this.SingleSize, this.SingleSpace);
            return;
        }

        writer.StartElement("w", rootName, DocxConstants.NamespaceWordprocessingMl);
        WriteBorderElement(writer, "top", this.TopVal, this.TopColor, this.TopSize, this.TopSpace);
        WriteBorderElement(writer, "left", this.LeftVal, this.LeftColor, this.LeftSize, this.LeftSpace);
        WriteBorderElement(writer, "bottom", this.BottomVal, this.BottomColor, this.BottomSize, this.BottomSpace);
        WriteBorderElement(writer, "right", this.RightVal, this.RightColor, this.RightSize, this.RightSpace);
        WriteBorderElement(writer, "insideH", this.InsideHorizontalVal, this.InsideHorizontalColor, this.InsideHorizontalSize, this.InsideHorizontalSpace);
        WriteBorderElement(writer, "insideV", this.InsideVerticalVal, this.InsideVerticalColor, this.InsideVerticalSize, this.InsideVerticalSpace);
        WriteBorderElement(writer, "between", this.BetweenVal, this.BetweenColor, this.BetweenSize, this.BetweenSpace);
        WriteBorderElement(writer, "bar", this.BarVal, this.BarColor, this.BarSize, this.BarSpace);
        writer.EndElement();
    }

    internal void WriteBorderElement(DocxXmlWriter writer, string elementName, string val, string color, string size, string space)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        ValidateBorderValues(val, color, size, space);

        if (val == "" && color == "" && size == "" && space == "")
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", val);
        WriteOptionalAttribute(writer, "color", color);
        WriteOptionalAttribute(writer, "sz", size);
        WriteOptionalAttribute(writer, "space", space);
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

    internal static void ValidateRootName(string rootName)
    {
        if (rootName == null)
        {
            throw new ArgumentNullException("rootName");
        }

        if (!IsSupportedRootName(rootName))
        {
            throw new ArgumentException("Border root element is not supported.", "rootName");
        }
    }

    internal static bool IsSupportedRootName(string rootName)
    {
        if (rootName == null)
        {
            throw new ArgumentNullException("rootName");
        }

        return rootName == "bdr"
            || rootName == "pBdr"
            || rootName == "tblBorders"
            || rootName == "tcBorders"
            || rootName == "pgBorders";
    }
}
