namespace docx;

/// <summary>
/// Represents section format data used by a DOCX document.
/// </summary>
public class DocxSectionFormat
{
    /// <summary>
    /// The header footer reference default identifier used in DOCX package data.
    /// </summary>
    public const string HeaderFooterReferenceDefault = "default";
    /// <summary>
    /// The header footer reference even identifier used in DOCX package data.
    /// </summary>
    public const string HeaderFooterReferenceEven = "even";
    /// <summary>
    /// The header footer reference first identifier used in DOCX package data.
    /// </summary>
    public const string HeaderFooterReferenceFirst = "first";
    /// <summary>
    /// The page orientation portrait identifier used in DOCX package data.
    /// </summary>
    public const string PageOrientationPortrait = "portrait";
    /// <summary>
    /// The page orientation landscape identifier used in DOCX package data.
    /// </summary>
    public const string PageOrientationLandscape = "landscape";
    /// <summary>
    /// The page size letter width identifier used in DOCX package data.
    /// </summary>
    public const string PageSizeLetterWidth = "12240";
    /// <summary>
    /// The page size letter height identifier used in DOCX package data.
    /// </summary>
    public const string PageSizeLetterHeight = "15840";
    /// <summary>
    /// The page size a4 width identifier used in DOCX package data.
    /// </summary>
    public const string PageSizeA4Width = "11906";
    /// <summary>
    /// The page size a4 height identifier used in DOCX package data.
    /// </summary>
    public const string PageSizeA4Height = "16838";
    /// <summary>
    /// The page number format decimal identifier used in DOCX package data.
    /// </summary>
    public const string PageNumberFormatDecimal = "decimal";

    internal string Xml;
    /// <summary>
    /// The default header relationship identifier stored in the document.
    /// </summary>
    public string DefaultHeaderRelationshipId;
    /// <summary>
    /// The even header relationship identifier stored in the document.
    /// </summary>
    public string EvenHeaderRelationshipId;
    /// <summary>
    /// The first header relationship identifier stored in the document.
    /// </summary>
    public string FirstHeaderRelationshipId;
    /// <summary>
    /// The default footer relationship identifier stored in the document.
    /// </summary>
    public string DefaultFooterRelationshipId;
    /// <summary>
    /// The even footer relationship identifier stored in the document.
    /// </summary>
    public string EvenFooterRelationshipId;
    /// <summary>
    /// The first footer relationship identifier stored in the document.
    /// </summary>
    public string FirstFooterRelationshipId;
    /// <summary>
    /// The section type value stored in the document model.
    /// </summary>
    public string SectionType;
    /// <summary>
    /// The page width value stored in the document model.
    /// </summary>
    public string PageWidth;
    /// <summary>
    /// The page height value stored in the document model.
    /// </summary>
    public string PageHeight;
    /// <summary>
    /// The page orientation value stored in the document model.
    /// </summary>
    public string PageOrientation;
    /// <summary>
    /// The page code value stored in the document model.
    /// </summary>
    public string PageCode;
    /// <summary>
    /// The margin top value stored in the document model.
    /// </summary>
    public string MarginTop;
    /// <summary>
    /// The margin right value stored in the document model.
    /// </summary>
    public string MarginRight;
    /// <summary>
    /// The margin bottom value stored in the document model.
    /// </summary>
    public string MarginBottom;
    /// <summary>
    /// The margin left value stored in the document model.
    /// </summary>
    public string MarginLeft;
    /// <summary>
    /// The margin header value stored in the document model.
    /// </summary>
    public string MarginHeader;
    /// <summary>
    /// The margin footer value stored in the document model.
    /// </summary>
    public string MarginFooter;
    /// <summary>
    /// The margin gutter value stored in the document model.
    /// </summary>
    public string MarginGutter;
    /// <summary>
    /// The column count value stored in the document model.
    /// </summary>
    public string ColumnCount;
    /// <summary>
    /// The column space value stored in the document model.
    /// </summary>
    public string ColumnSpace;
    /// <summary>
    /// Whether equal width columns was explicitly supplied.
    /// </summary>
    public bool HasEqualWidthColumns;
    /// <summary>
    /// Whether the equal width columns behavior is enabled.
    /// </summary>
    public bool EqualWidthColumns;
    /// <summary>
    /// Whether column separator was explicitly supplied.
    /// </summary>
    public bool HasColumnSeparator;
    /// <summary>
    /// Whether the column separator behavior is enabled.
    /// </summary>
    public bool ColumnSeparator;
    /// <summary>
    /// The explicitly sized columns in document order.
    /// </summary>
    public List<DocxSectionColumn> Columns;
    /// <summary>
    /// The doc grid type value stored in the document model.
    /// </summary>
    public string DocGridType;
    /// <summary>
    /// The doc grid line pitch value stored in the document model.
    /// </summary>
    public string DocGridLinePitch;
    /// <summary>
    /// The doc grid char space value stored in the document model.
    /// </summary>
    public string DocGridCharSpace;
    /// <summary>
    /// Whether title page was explicitly supplied.
    /// </summary>
    public bool HasTitlePage;
    /// <summary>
    /// Whether the title page behavior is enabled.
    /// </summary>
    public bool TitlePage;
    /// <summary>
    /// The page number start value stored in the document model.
    /// </summary>
    public string PageNumberStart;
    /// <summary>
    /// The page number format value stored in the document model.
    /// </summary>
    public string PageNumberFormat;
    /// <summary>
    /// The page number chapter style value stored in the document model.
    /// </summary>
    public string PageNumberChapterStyle;
    /// <summary>
    /// The page number chapter separator value stored in the document model.
    /// </summary>
    public string PageNumberChapterSeparator;
    /// <summary>
    /// The page borders value stored in the document model.
    /// </summary>
    public DocxBorders PageBorders;

    /// <summary>
    /// Creates a section format instance.
    /// </summary>
    public DocxSectionFormat()
    {
        this.Xml = "";
        this.DefaultHeaderRelationshipId = "";
        this.EvenHeaderRelationshipId = "";
        this.FirstHeaderRelationshipId = "";
        this.DefaultFooterRelationshipId = "";
        this.EvenFooterRelationshipId = "";
        this.FirstFooterRelationshipId = "";
        this.SectionType = "";
        this.PageWidth = "";
        this.PageHeight = "";
        this.PageOrientation = "";
        this.PageCode = "";
        this.MarginTop = "";
        this.MarginRight = "";
        this.MarginBottom = "";
        this.MarginLeft = "";
        this.MarginHeader = "";
        this.MarginFooter = "";
        this.MarginGutter = "";
        this.ColumnCount = "";
        this.ColumnSpace = "";
        this.HasEqualWidthColumns = false;
        this.EqualWidthColumns = false;
        this.HasColumnSeparator = false;
        this.ColumnSeparator = false;
        this.Columns = new List<DocxSectionColumn>();
        this.DocGridType = "";
        this.DocGridLinePitch = "";
        this.DocGridCharSpace = "";
        this.HasTitlePage = false;
        this.TitlePage = false;
        this.PageNumberStart = "";
        this.PageNumberFormat = "";
        this.PageNumberChapterStyle = "";
        this.PageNumberChapterSeparator = "";
        this.PageBorders = new DocxBorders();
    }

    internal static DocxSectionFormat Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:sectPr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxSectionFormat ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxSectionFormat format = new DocxSectionFormat();
        format.Xml = xml;

        if (xml == "")
        {
            return format;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return format;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return format;
        }

        int sectionDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == sectionDepth)
            {
                return format;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == sectionDepth + 1)
            {
                if (reader.IsElement("pgBorders", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.PageBorders = DocxBorders.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("cols", DocxConstants.NamespaceWordprocessingMl))
                {
                    format.ReadColumns(reader);
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

        if (reader.IsElement("headerReference", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadHeaderReference(reader);
        }
        else if (reader.IsElement("footerReference", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadFooterReference(reader);
        }
        else if (reader.IsElement("type", DocxConstants.NamespaceWordprocessingMl))
        {
            this.SectionType = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("pgSz", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadPageSize(reader);
        }
        else if (reader.IsElement("pgMar", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadPageMargins(reader);
        }
        else if (reader.IsElement("cols", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadColumns(reader);
        }
        else if (reader.IsElement("docGrid", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadDocGrid(reader);
        }
        else if (reader.IsElement("titlePg", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasTitlePage = true;
            this.TitlePage = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("pgNumType", DocxConstants.NamespaceWordprocessingMl))
        {
            ReadPageNumbering(reader);
        }
    }

    internal void ReadHeaderReference(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("headerReference", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:headerReference.");
        }

        SetHeaderReference(
            reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl),
            reader.GetAttribute("id", DocxConstants.NamespaceRelationships));
    }

    internal void ReadFooterReference(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("footerReference", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:footerReference.");
        }

        SetFooterReference(
            reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl),
            reader.GetAttribute("id", DocxConstants.NamespaceRelationships));
    }

    internal void ReadPageSize(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pgSz", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pgSz.");
        }

        this.PageWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
        this.PageHeight = reader.GetAttribute("h", DocxConstants.NamespaceWordprocessingMl);
        this.PageOrientation = reader.GetAttribute("orient", DocxConstants.NamespaceWordprocessingMl);
        this.PageCode = reader.GetAttribute("code", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void ReadPageMargins(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pgMar", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pgMar.");
        }

        this.MarginTop = reader.GetAttribute("top", DocxConstants.NamespaceWordprocessingMl);
        this.MarginRight = reader.GetAttribute("right", DocxConstants.NamespaceWordprocessingMl);
        this.MarginBottom = reader.GetAttribute("bottom", DocxConstants.NamespaceWordprocessingMl);
        this.MarginLeft = reader.GetAttribute("left", DocxConstants.NamespaceWordprocessingMl);
        this.MarginHeader = reader.GetAttribute("header", DocxConstants.NamespaceWordprocessingMl);
        this.MarginFooter = reader.GetAttribute("footer", DocxConstants.NamespaceWordprocessingMl);
        this.MarginGutter = reader.GetAttribute("gutter", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void ReadColumns(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("cols", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:cols.");
        }

        this.ColumnCount = reader.GetAttribute("num", DocxConstants.NamespaceWordprocessingMl);
        this.ColumnSpace = reader.GetAttribute("space", DocxConstants.NamespaceWordprocessingMl);
        ReadEqualWidthColumns(reader);
        ReadColumnSeparator(reader);
        this.Columns = new List<DocxSectionColumn>();

        if (reader.Reader.IsEmptyElement)
        {
            reader.Read();
            return;
        }

        int columnsDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == columnsDepth)
            {
                reader.Read();
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element
                && reader.Depth() == columnsDepth + 1
                && reader.IsElement("col", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxSectionColumn column = new DocxSectionColumn();
                column.Width = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
                column.Space = reader.GetAttribute("space", DocxConstants.NamespaceWordprocessingMl);
                this.Columns.Add(column);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadDocGrid(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("docGrid", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:docGrid.");
        }

        this.DocGridType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        this.DocGridLinePitch = reader.GetAttribute("linePitch", DocxConstants.NamespaceWordprocessingMl);
        this.DocGridCharSpace = reader.GetAttribute("charSpace", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void ReadPageNumbering(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("pgNumType", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:pgNumType.");
        }

        this.PageNumberStart = reader.GetAttribute("start", DocxConstants.NamespaceWordprocessingMl);
        this.PageNumberFormat = reader.GetAttribute("fmt", DocxConstants.NamespaceWordprocessingMl);
        this.PageNumberChapterStyle = reader.GetAttribute("chapStyle", DocxConstants.NamespaceWordprocessingMl);
        this.PageNumberChapterSeparator = reader.GetAttribute("chapSep", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void ReadEqualWidthColumns(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("equalWidth", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasEqualWidthColumns = true;
            this.EqualWidthColumns = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadColumnSeparator(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("sep", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasColumnSeparator = true;
            this.ColumnSeparator = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal bool HasAnyFormatting()
    {
        return HasHeaderReferences()
            || HasFooterReferences()
            || this.SectionType != ""
            || this.PageWidth != ""
            || this.PageHeight != ""
            || this.PageOrientation != ""
            || this.PageCode != ""
            || this.MarginTop != ""
            || this.MarginRight != ""
            || this.MarginBottom != ""
            || this.MarginLeft != ""
            || this.MarginHeader != ""
            || this.MarginFooter != ""
            || this.MarginGutter != ""
            || this.ColumnCount != ""
            || this.ColumnSpace != ""
            || this.HasEqualWidthColumns
            || this.HasColumnSeparator
            || this.Columns.Count > 0
            || this.DocGridType != ""
            || this.DocGridLinePitch != ""
            || this.DocGridCharSpace != ""
            || this.HasTitlePage
            || this.PageNumberStart != ""
            || this.PageNumberFormat != ""
            || this.PageNumberChapterStyle != ""
            || this.PageNumberChapterSeparator != ""
            || this.PageBorders.HasAnyBorder();
    }

    internal bool HasHeaderReferences()
    {
        return this.DefaultHeaderRelationshipId != ""
            || this.EvenHeaderRelationshipId != ""
            || this.FirstHeaderRelationshipId != "";
    }

    internal bool HasFooterReferences()
    {
        return this.DefaultFooterRelationshipId != ""
            || this.EvenFooterRelationshipId != ""
            || this.FirstFooterRelationshipId != "";
    }

    internal void SetHeaderReference(string referenceType, string relationshipId)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (referenceType == HeaderFooterReferenceEven)
        {
            this.EvenHeaderRelationshipId = relationshipId;
        }
        else if (referenceType == HeaderFooterReferenceFirst)
        {
            this.FirstHeaderRelationshipId = relationshipId;
        }
        else
        {
            this.DefaultHeaderRelationshipId = relationshipId;
        }
    }

    internal void SetFooterReference(string referenceType, string relationshipId)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (referenceType == HeaderFooterReferenceEven)
        {
            this.EvenFooterRelationshipId = relationshipId;
        }
        else if (referenceType == HeaderFooterReferenceFirst)
        {
            this.FirstFooterRelationshipId = relationshipId;
        }
        else
        {
            this.DefaultFooterRelationshipId = relationshipId;
        }
    }

    internal string GetHeaderReference(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        if (referenceType == HeaderFooterReferenceEven)
        {
            return this.EvenHeaderRelationshipId;
        }

        if (referenceType == HeaderFooterReferenceFirst)
        {
            return this.FirstHeaderRelationshipId;
        }

        return this.DefaultHeaderRelationshipId;
    }

    internal string GetFooterReference(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        if (referenceType == HeaderFooterReferenceEven)
        {
            return this.EvenFooterRelationshipId;
        }

        if (referenceType == HeaderFooterReferenceFirst)
        {
            return this.FirstFooterRelationshipId;
        }

        return this.DefaultFooterRelationshipId;
    }

    internal void ClearHeaderReference(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        SetHeaderReference(referenceType, "");
    }

    internal void ClearFooterReference(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        SetFooterReference(referenceType, "");
    }

    internal bool IsHeaderLinkedToPrevious(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        return GetHeaderReference(referenceType) == "";
    }

    internal bool IsFooterLinkedToPrevious(string referenceType)
    {
        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        return GetFooterReference(referenceType) == "";
    }

    internal void SetSectionType(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.SectionType = value;
    }

    internal void SetPageSize(string width, string height, string orientation, string code)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        if (height == null)
        {
            throw new ArgumentNullException("height");
        }

        if (orientation == null)
        {
            throw new ArgumentNullException("orientation");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        this.PageWidth = width;
        this.PageHeight = height;
        this.PageOrientation = orientation;
        this.PageCode = code;
    }

    internal void SetPageDimensions(string width, string height)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        if (height == null)
        {
            throw new ArgumentNullException("height");
        }

        this.PageWidth = width;
        this.PageHeight = height;
    }

    internal void SetPageOrientation(string orientation)
    {
        if (orientation == null)
        {
            throw new ArgumentNullException("orientation");
        }

        this.PageOrientation = orientation;
    }

    internal void SetPortraitOrientation()
    {
        this.PageOrientation = PageOrientationPortrait;
    }

    internal void SetLandscapeOrientation()
    {
        this.PageOrientation = PageOrientationLandscape;
    }

    internal void SetLetterPagePortrait()
    {
        SetPageSize(PageSizeLetterWidth, PageSizeLetterHeight, PageOrientationPortrait, "");
    }

    internal void SetLetterPageLandscape()
    {
        SetPageSize(PageSizeLetterHeight, PageSizeLetterWidth, PageOrientationLandscape, "");
    }

    internal void SetA4PagePortrait()
    {
        SetPageSize(PageSizeA4Width, PageSizeA4Height, PageOrientationPortrait, "");
    }

    internal void SetA4PageLandscape()
    {
        SetPageSize(PageSizeA4Height, PageSizeA4Width, PageOrientationLandscape, "");
    }

    internal void SetPageMargins(string top, string right, string bottom, string left, string header, string footer, string gutter)
    {
        if (top == null)
        {
            throw new ArgumentNullException("top");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (bottom == null)
        {
            throw new ArgumentNullException("bottom");
        }

        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (footer == null)
        {
            throw new ArgumentNullException("footer");
        }

        if (gutter == null)
        {
            throw new ArgumentNullException("gutter");
        }

        this.MarginTop = top;
        this.MarginRight = right;
        this.MarginBottom = bottom;
        this.MarginLeft = left;
        this.MarginHeader = header;
        this.MarginFooter = footer;
        this.MarginGutter = gutter;
    }

    internal void SetPageSideMargins(string top, string right, string bottom, string left)
    {
        if (top == null)
        {
            throw new ArgumentNullException("top");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (bottom == null)
        {
            throw new ArgumentNullException("bottom");
        }

        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        this.MarginTop = top;
        this.MarginRight = right;
        this.MarginBottom = bottom;
        this.MarginLeft = left;
    }

    internal void SetHeaderFooterMargins(string header, string footer)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }

        if (footer == null)
        {
            throw new ArgumentNullException("footer");
        }

        this.MarginHeader = header;
        this.MarginFooter = footer;
    }

    internal void SetGutter(string gutter)
    {
        if (gutter == null)
        {
            throw new ArgumentNullException("gutter");
        }

        this.MarginGutter = gutter;
    }

    internal void SetColumns(string count, string space)
    {
        if (count == null)
        {
            throw new ArgumentNullException("count");
        }

        if (space == null)
        {
            throw new ArgumentNullException("space");
        }

        this.ColumnCount = count;
        this.ColumnSpace = space;
    }

    internal void SetColumnLayout(string count, string space, bool equalWidth, bool separator)
    {
        if (count == null)
        {
            throw new ArgumentNullException("count");
        }

        if (space == null)
        {
            throw new ArgumentNullException("space");
        }

        SetColumns(count, space);
        SetEqualWidthColumns(equalWidth);
        SetColumnSeparator(separator);
    }

    internal void SetSingleColumn()
    {
        this.ColumnCount = "1";
        this.ColumnSpace = "";
        ClearEqualWidthColumns();
        ClearColumnSeparator();
    }

    internal void SetEqualWidthColumns(bool value)
    {
        this.HasEqualWidthColumns = true;
        this.EqualWidthColumns = value;
    }

    internal void ClearEqualWidthColumns()
    {
        this.HasEqualWidthColumns = false;
        this.EqualWidthColumns = false;
    }

    internal void SetColumnSeparator(bool value)
    {
        this.HasColumnSeparator = true;
        this.ColumnSeparator = value;
    }

    internal void ClearColumnSeparator()
    {
        this.HasColumnSeparator = false;
        this.ColumnSeparator = false;
    }

    internal void SetDocGrid(string gridType, string linePitch, string charSpace)
    {
        if (gridType == null)
        {
            throw new ArgumentNullException("gridType");
        }

        if (linePitch == null)
        {
            throw new ArgumentNullException("linePitch");
        }

        if (charSpace == null)
        {
            throw new ArgumentNullException("charSpace");
        }

        this.DocGridType = gridType;
        this.DocGridLinePitch = linePitch;
        this.DocGridCharSpace = charSpace;
    }

    internal void SetTitlePage(bool value)
    {
        this.HasTitlePage = true;
        this.TitlePage = value;
    }

    internal void ClearTitlePage()
    {
        this.HasTitlePage = false;
        this.TitlePage = false;
    }

    internal void SetPageNumbering(string start, string numberFormat, string chapterStyle, string chapterSeparator)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        if (chapterStyle == null)
        {
            throw new ArgumentNullException("chapterStyle");
        }

        if (chapterSeparator == null)
        {
            throw new ArgumentNullException("chapterSeparator");
        }

        this.PageNumberStart = start;
        this.PageNumberFormat = numberFormat;
        this.PageNumberChapterStyle = chapterStyle;
        this.PageNumberChapterSeparator = chapterSeparator;
    }

    internal void SetPageNumberStart(string start)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        this.PageNumberStart = start;
    }

    internal void SetPageNumberFormat(string numberFormat)
    {
        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        this.PageNumberFormat = numberFormat;
    }

    internal void RestartPageNumbering(string start, string numberFormat)
    {
        if (start == null)
        {
            throw new ArgumentNullException("start");
        }

        if (numberFormat == null)
        {
            throw new ArgumentNullException("numberFormat");
        }

        this.PageNumberStart = start;
        this.PageNumberFormat = numberFormat;
        this.PageNumberChapterStyle = "";
        this.PageNumberChapterSeparator = "";
    }

    internal void ClearPageNumbering()
    {
        this.PageNumberStart = "";
        this.PageNumberFormat = "";
        this.PageNumberChapterStyle = "";
        this.PageNumberChapterSeparator = "";
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

        writer.StartElement("w", "sectPr", DocxConstants.NamespaceWordprocessingMl);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteStringProperty(writer, "type", "val", this.SectionType);
        WriteHeaderReferencesTo(writer);
        WriteFooterReferencesTo(writer);
        WritePageSizeTo(writer);
        WritePageMarginsTo(writer);
        WritePageBordersTo(writer);
        WriteColumnsTo(writer);
        WriteDocGridTo(writer);
        WriteTitlePageTo(writer);
        WritePageNumberingTo(writer);
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

    internal void WriteHeaderReferencesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteReferenceTo(writer, "headerReference", HeaderFooterReferenceDefault, this.DefaultHeaderRelationshipId);
        WriteReferenceTo(writer, "headerReference", HeaderFooterReferenceEven, this.EvenHeaderRelationshipId);
        WriteReferenceTo(writer, "headerReference", HeaderFooterReferenceFirst, this.FirstHeaderRelationshipId);
    }

    internal void WriteFooterReferencesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteReferenceTo(writer, "footerReference", HeaderFooterReferenceDefault, this.DefaultFooterRelationshipId);
        WriteReferenceTo(writer, "footerReference", HeaderFooterReferenceEven, this.EvenFooterRelationshipId);
        WriteReferenceTo(writer, "footerReference", HeaderFooterReferenceFirst, this.FirstFooterRelationshipId);
    }

    internal void WriteReferenceTo(DocxXmlWriter writer, string elementName, string referenceType, string relationshipId)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (elementName == null)
        {
            throw new ArgumentNullException("elementName");
        }

        if (referenceType == null)
        {
            throw new ArgumentNullException("referenceType");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        if (relationshipId == "")
        {
            return;
        }

        writer.StartElement("w", elementName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "type", DocxConstants.NamespaceWordprocessingMl, referenceType);
        writer.WriteAttribute("r", "id", DocxConstants.NamespaceRelationships, relationshipId);
        writer.EndElement();
    }

    internal void WritePageSizeTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.PageWidth == "" && this.PageHeight == "" && this.PageOrientation == "" && this.PageCode == "")
        {
            return;
        }

        writer.StartElement("w", "pgSz", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", this.PageWidth);
        WriteOptionalAttribute(writer, "h", this.PageHeight);
        WriteOptionalAttribute(writer, "orient", this.PageOrientation);
        WriteOptionalAttribute(writer, "code", this.PageCode);
        writer.EndElement();
    }

    internal void WritePageMarginsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.MarginTop == ""
            && this.MarginRight == ""
            && this.MarginBottom == ""
            && this.MarginLeft == ""
            && this.MarginHeader == ""
            && this.MarginFooter == ""
            && this.MarginGutter == "")
        {
            return;
        }

        writer.StartElement("w", "pgMar", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "top", this.MarginTop);
        WriteOptionalAttribute(writer, "right", this.MarginRight);
        WriteOptionalAttribute(writer, "bottom", this.MarginBottom);
        WriteOptionalAttribute(writer, "left", this.MarginLeft);
        WriteOptionalAttribute(writer, "header", this.MarginHeader);
        WriteOptionalAttribute(writer, "footer", this.MarginFooter);
        WriteOptionalAttribute(writer, "gutter", this.MarginGutter);
        writer.EndElement();
    }

    internal void WriteColumnsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.ColumnCount == ""
            && this.ColumnSpace == ""
            && !this.HasEqualWidthColumns
            && !this.HasColumnSeparator
            && this.Columns.Count == 0)
        {
            return;
        }

        writer.StartElement("w", "cols", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "num", this.ColumnCount);
        WriteOptionalAttribute(writer, "space", this.ColumnSpace);
        WriteOptionalBoolAttribute(writer, "equalWidth", this.HasEqualWidthColumns, this.EqualWidthColumns);
        WriteOptionalBoolAttribute(writer, "sep", this.HasColumnSeparator, this.ColumnSeparator);

        foreach (DocxSectionColumn column in this.Columns)
        {
            writer.StartElement("w", "col", DocxConstants.NamespaceWordprocessingMl);
            WriteOptionalAttribute(writer, "w", column.Width);
            WriteOptionalAttribute(writer, "space", column.Space);
            writer.EndElement();
        }

        writer.EndElement();
    }

    internal void WriteDocGridTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.DocGridType == "" && this.DocGridLinePitch == "" && this.DocGridCharSpace == "")
        {
            return;
        }

        writer.StartElement("w", "docGrid", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "type", this.DocGridType);
        WriteOptionalAttribute(writer, "linePitch", this.DocGridLinePitch);
        WriteOptionalAttribute(writer, "charSpace", this.DocGridCharSpace);
        writer.EndElement();
    }

    internal void WriteTitlePageTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (!this.HasTitlePage)
        {
            return;
        }

        writer.StartElement("w", "titlePg", DocxConstants.NamespaceWordprocessingMl);

        if (!this.TitlePage)
        {
            writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, "0");
        }

        writer.EndElement();
    }

    internal void WritePageNumberingTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.PageNumberStart == ""
            && this.PageNumberFormat == ""
            && this.PageNumberChapterStyle == ""
            && this.PageNumberChapterSeparator == "")
        {
            return;
        }

        writer.StartElement("w", "pgNumType", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "start", this.PageNumberStart);
        WriteOptionalAttribute(writer, "fmt", this.PageNumberFormat);
        WriteOptionalAttribute(writer, "chapStyle", this.PageNumberChapterStyle);
        WriteOptionalAttribute(writer, "chapSep", this.PageNumberChapterSeparator);
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

    internal void WritePageBordersTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.PageBorders.HasAnyBorder())
        {
            this.PageBorders.WriteTo(writer, "pgBorders");
        }
    }
}
