namespace docx;

/// <summary>
/// Represents table cell data used by a DOCX document.
/// </summary>
public class DocxTableCell : IDocxTextContent
{
    internal string Xml;
    internal string PropertiesXml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxBlock> Blocks;
    internal List<DocxParagraph> Paragraphs;
    internal List<DocxTable> Tables;
    internal string Width = "";
    internal string WidthType = "";
    internal string VerticalAlignment = "";
    internal string GridSpan = "";
    internal string HorizontalMerge = "";
    internal string VerticalMerge = "";
    internal string TextDirection = "";
    internal string MarginTopWidth = "";
    internal string MarginTopType = "";
    internal string MarginLeftWidth = "";
    internal string MarginLeftType = "";
    internal string MarginBottomWidth = "";
    internal string MarginBottomType = "";
    internal string MarginRightWidth = "";
    internal string MarginRightType = "";
    internal DocxBorders Borders = new DocxBorders();
    internal DocxShading Shading = new DocxShading();

    /// <summary>
    /// Creates a table cell instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxTableCell(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Paragraphs = new List<DocxParagraph>();
        this.Tables = new List<DocxTable>();
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxTableCell Create()
    {
        return new DocxTableCell("");
    }

    internal static DocxTableCell Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tc", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tc.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxTableCell(buffer.GetXml());
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
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Blocks = new List<DocxBlock>();
        this.Paragraphs = new List<DocxParagraph>();
        this.Tables = new List<DocxTable>();
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("tc", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int cellDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == cellDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == cellDepth + 1)
            {
                if (reader.IsElement("tcPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    ReadPropertiesXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                bool isParagraph = reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl);
                bool isTable = reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);

                if (isParagraph)
                {
                    AddParsedParagraph(DocxParagraphFromBuffer(child));
                }
                else if (isTable)
                {
                    AddParsedTable(DocxTableFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void InitializeParsedState()
    {
        this.Width = "";
        this.WidthType = "";
        this.VerticalAlignment = "";
        this.GridSpan = "";
        this.HorizontalMerge = "";
        this.VerticalMerge = "";
        this.TextDirection = "";
        this.MarginTopWidth = "";
        this.MarginTopType = "";
        this.MarginLeftWidth = "";
        this.MarginLeftType = "";
        this.MarginBottomWidth = "";
        this.MarginBottomType = "";
        this.MarginRightWidth = "";
        this.MarginRightType = "";
        this.Borders = new DocxBorders();
        this.Shading = new DocxShading();
    }

    internal void ReadPropertiesXml(string xml)
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

        if (!reader.ReadToElement("tcPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1)
            {
                if (reader.IsElement("tcBorders", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Borders = DocxBorders.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("shd", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Shading = DocxShading.Parse(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tcMar", DocxConstants.NamespaceWordprocessingMl))
                {
                    ReadMargins(reader);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                ReadProperty(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadProperty(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("tcW", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Width = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.WidthType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("vAlign", DocxConstants.NamespaceWordprocessingMl))
        {
            this.VerticalAlignment = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("gridSpan", DocxConstants.NamespaceWordprocessingMl))
        {
            this.GridSpan = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("hMerge", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HorizontalMerge = ReadMergeValue(reader);
        }
        else if (reader.IsElement("vMerge", DocxConstants.NamespaceWordprocessingMl))
        {
            this.VerticalMerge = ReadMergeValue(reader);
        }
        else if (reader.IsElement("textDirection", DocxConstants.NamespaceWordprocessingMl))
        {
            this.TextDirection = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void ReadMargins(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tcMar", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tcMar.");
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
                ReadMargin(reader);
                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void ReadMargin(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.IsElement("top", DocxConstants.NamespaceWordprocessingMl))
        {
            this.MarginTopWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.MarginTopType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("left", DocxConstants.NamespaceWordprocessingMl) || reader.IsElement("start", DocxConstants.NamespaceWordprocessingMl))
        {
            this.MarginLeftWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.MarginLeftType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("bottom", DocxConstants.NamespaceWordprocessingMl))
        {
            this.MarginBottomWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.MarginBottomType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("right", DocxConstants.NamespaceWordprocessingMl) || reader.IsElement("end", DocxConstants.NamespaceWordprocessingMl))
        {
            this.MarginRightWidth = reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl);
            this.MarginRightType = reader.GetAttribute("type", DocxConstants.NamespaceWordprocessingMl);
        }
    }

    internal void SetPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.PropertiesXml = xml;
        InitializeParsedState();
        ReadPropertiesXml(xml);
        this.Xml = "";
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
        ClearRawProperties();
    }

    internal void SetVerticalAlignment(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.VerticalAlignment = value;
        ClearRawProperties();
    }

    internal void SetGridSpan(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.GridSpan = value;
        ClearRawProperties();
    }

    internal void SetHorizontalMerge(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.HorizontalMerge = value;
        ClearRawProperties();
    }

    internal void SetVerticalMerge(string value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        this.VerticalMerge = value;
        ClearRawProperties();
    }

    internal void ClearHorizontalMerge()
    {
        this.HorizontalMerge = "";
        ClearRawProperties();
    }

    internal void ClearVerticalMerge()
    {
        this.VerticalMerge = "";
        ClearRawProperties();
    }

    internal DocxTableCellFormat CreateFormat()
    {
        DocxTableCellFormat format = new DocxTableCellFormat();
        format.Width = this.Width;
        format.WidthType = this.WidthType;
        format.GridSpan = this.GridSpan;
        format.HorizontalMerge = this.HorizontalMerge;
        format.VerticalMerge = this.VerticalMerge;
        format.VerticalAlignment = this.VerticalAlignment;
        format.TextDirection = this.TextDirection;
        format.MarginTopWidth = this.MarginTopWidth;
        format.MarginTopType = this.MarginTopType;
        format.MarginLeftWidth = this.MarginLeftWidth;
        format.MarginLeftType = this.MarginLeftType;
        format.MarginBottomWidth = this.MarginBottomWidth;
        format.MarginBottomType = this.MarginBottomType;
        format.MarginRightWidth = this.MarginRightWidth;
        format.MarginRightType = this.MarginRightType;
        format.Borders = this.Borders;
        format.Shading = this.Shading;
        return format;
    }

    internal void ApplyFormat(DocxTableCellFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        if (format.Borders == null)
        {
            throw new ArgumentNullException("format.Borders");
        }

        if (format.Shading == null)
        {
            throw new ArgumentNullException("format.Shading");
        }

        this.Width = format.Width;
        this.WidthType = format.WidthType;
        this.GridSpan = format.GridSpan;
        this.HorizontalMerge = format.HorizontalMerge;
        this.VerticalMerge = format.VerticalMerge;
        this.VerticalAlignment = format.VerticalAlignment;
        this.TextDirection = format.TextDirection;
        this.MarginTopWidth = format.MarginTopWidth;
        this.MarginTopType = format.MarginTopType;
        this.MarginLeftWidth = format.MarginLeftWidth;
        this.MarginLeftType = format.MarginLeftType;
        this.MarginBottomWidth = format.MarginBottomWidth;
        this.MarginBottomType = format.MarginBottomType;
        this.MarginRightWidth = format.MarginRightWidth;
        this.MarginRightType = format.MarginRightType;
        this.Borders = format.Borders;
        this.Shading = format.Shading;
        ClearRawProperties();
    }

    internal void ClearRawProperties()
    {
        this.PropertiesXml = "";
        this.Xml = "";
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);

        if (IsParagraphChild(child))
        {
            AddParagraph(DocxParagraphFromBuffer(child));
        }
        else if (IsTableChild(child))
        {
            AddTable(DocxTableFromBuffer(child));
        }

        this.Xml = "";
    }

    internal void AddParsedChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (this.Blocks.Count == 1
            && this.Blocks[0].IsParagraph()
            && this.Paragraphs.Count == 1
            && !this.Paragraphs[0].HasXml()
            && this.Paragraphs[0].GetText() == "")
        {
            this.Blocks[0] = DocxBlock.FromParagraph(paragraph);
            RebuildTypedContent();
            this.Xml = "";
            return;
        }

        this.Paragraphs.Add(paragraph);
        this.Blocks.Add(DocxBlock.FromParagraph(paragraph));
        this.Xml = "";
    }

    internal void AddParsedParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        this.Paragraphs.Add(paragraph);
        this.Blocks.Add(DocxBlock.FromParagraph(paragraph));
    }

    internal void AddTable(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (this.Blocks.Count > 0 && this.Blocks[this.Blocks.Count - 1].IsParagraph())
        {
            this.Blocks.Insert(this.Blocks.Count - 1, DocxBlock.FromTable(table));
            RebuildTypedContent();
        }
        else
        {
            this.Tables.Add(table);
            this.Blocks.Add(DocxBlock.FromTable(table));
        }

        this.Xml = "";
    }

    internal DocxTable AddNewTable(int rows, int columns)
    {
        DocxTable table = DocxTable.Create(rows, columns);
        AddTable(table);
        return table;
    }

    internal void InsertTable(int index, DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        int blockIndex = FindTableBlockIndex(index);
        this.Blocks.Insert(blockIndex, DocxBlock.FromTable(table));
        RebuildTypedContent();
        this.Xml = "";
    }

    internal DocxTable InsertNewTable(int index, int rows, int columns)
    {
        DocxTable table = DocxTable.Create(rows, columns);
        InsertTable(index, table);
        return table;
    }

    internal void AddParsedTable(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        this.Tables.Add(table);
        this.Blocks.Add(DocxBlock.FromTable(table));
    }

    internal int FindTableBlockIndex(int tableIndex)
    {
        if (tableIndex < 0 || tableIndex > this.Tables.Count)
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }

        int currentTableIndex = 0;
        int blockIndex = 0;

        while (blockIndex < this.Blocks.Count)
        {
            if (this.Blocks[blockIndex].IsTable())
            {
                if (currentTableIndex == tableIndex)
                {
                    return blockIndex;
                }

                currentTableIndex = currentTableIndex + 1;
            }

            blockIndex = blockIndex + 1;
        }

        if (this.Blocks.Count > 0 && this.Blocks[this.Blocks.Count - 1].IsParagraph())
        {
            return this.Blocks.Count - 1;
        }

        return this.Blocks.Count;
    }

    internal void InsertBlock(int index, DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (!block.IsParagraph() && !block.IsTable())
        {
            throw new ArgumentException("A table cell block must contain a paragraph or table.", "block");
        }

        this.Blocks.Insert(index, block);
        RebuildTypedContent();
        this.Xml = "";
    }

    internal DocxBlock RemoveBlockAt(int index)
    {
        DocxBlock block = this.Blocks[index];
        this.Blocks.RemoveAt(index);
        RebuildTypedContent();
        this.Xml = "";
        return block;
    }

    internal void MoveBlock(int sourceIndex, int destinationIndex)
    {
        if (sourceIndex == destinationIndex)
        {
            return;
        }

        DocxBlock block = this.Blocks[sourceIndex];
        this.Blocks.RemoveAt(sourceIndex);
        this.Blocks.Insert(destinationIndex, block);
        RebuildTypedContent();
        this.Xml = "";
    }

    internal void RebuildTypedContent()
    {
        this.Paragraphs.Clear();
        this.Tables.Clear();

        foreach (DocxBlock block in this.Blocks)
        {
            DocxParagraph? paragraph = block.GetParagraph();
            DocxTable? table = block.GetTable();

            if (paragraph != null)
            {
                this.Paragraphs.Add(paragraph);
            }
            else if (table != null)
            {
                this.Tables.Add(table);
            }
        }
    }

    internal int BlockCount()
    {
        return this.Blocks.Count;
    }

    internal DocxBlock GetBlock(int index)
    {
        return this.Blocks[index];
    }

    internal static DocxParagraph DocxParagraphFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxParagraph.Parse(reader);
    }

    internal static DocxTable DocxTableFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxTable.Parse(reader);
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int ParagraphCount()
    {
        return this.Paragraphs.Count;
    }

    internal DocxParagraph GetParagraph(int index)
    {
        return this.Paragraphs[index];
    }

    internal int TableCount()
    {
        return this.Tables.Count;
    }

    internal DocxTable GetTable(int index)
    {
        return this.Tables[index];
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = 0;

        foreach (DocxParagraph paragraph in this.Paragraphs)
        {
            count = count + paragraph.FillContentControls(values);
        }

        foreach (DocxTable table in this.Tables)
        {
            count = count + table.FillContentControls(values);
        }

        if (count > 0)
        {
            this.Xml = "";
        }

        return count;
    }

    internal bool HasProperties()
    {
        return this.PropertiesXml != "";
    }

    internal bool HasAnyFormatting()
    {
        return this.Width != ""
            || this.WidthType != ""
            || this.VerticalAlignment != ""
            || this.GridSpan != ""
            || this.HorizontalMerge != ""
            || this.VerticalMerge != ""
            || this.TextDirection != ""
            || this.MarginTopWidth != ""
            || this.MarginTopType != ""
            || this.MarginLeftWidth != ""
            || this.MarginLeftType != ""
            || this.MarginBottomWidth != ""
            || this.MarginBottomType != ""
            || this.MarginRightWidth != ""
            || this.MarginRightType != ""
            || this.Borders.HasAnyBorder()
            || this.Shading.HasAnyShading();
    }

    internal bool HasHorizontalMerge()
    {
        return IsHorizontalMergeStart() || IsHorizontalMergeContinuation();
    }

    internal bool IsHorizontalMergeStart()
    {
        if (this.HorizontalMerge == "restart")
        {
            return true;
        }

        return GetGridSpanCount() > 1;
    }

    internal bool IsHorizontalMergeContinuation()
    {
        return this.HorizontalMerge == "continue";
    }

    internal bool HasVerticalMerge()
    {
        return IsVerticalMergeStart() || IsVerticalMergeContinuation();
    }

    internal bool IsVerticalMergeStart()
    {
        return this.VerticalMerge == "restart";
    }

    internal bool IsVerticalMergeContinuation()
    {
        return this.VerticalMerge == "continue";
    }

    internal int GetGridSpanCount()
    {
        if (this.GridSpan == "")
        {
            return 1;
        }

        int span = 0;

        if (!int.TryParse(this.GridSpan, out span))
        {
            return 1;
        }

        if (span < 1)
        {
            return 1;
        }

        return span;
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

        return GetTextFromBlocks(options);
    }

    internal string GetTextFromBlocks(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasText = false;

        foreach (DocxBlock block in this.Blocks)
        {
            if (hasText)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + block.GetTextWithOptions(options);
            hasText = true;
        }

        return text;
    }

    internal string GetTextFromChildren(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasText = false;
        int paragraphIndex = 0;
        int tableIndex = 0;

        foreach (DocxXmlBuffer child in this.Children)
        {
            string childText = "";

            if (IsParagraphChild(child))
            {
                if (paragraphIndex < this.Paragraphs.Count)
                {
                    childText = this.Paragraphs[paragraphIndex].GetTextWithOptions(options);
                }
                else
                {
                    childText = DocxParagraphFromBuffer(child).GetTextWithOptions(options);
                }

                paragraphIndex = paragraphIndex + 1;
            }
            else if (IsTableChild(child))
            {
                if (tableIndex < this.Tables.Count)
                {
                    childText = this.Tables[tableIndex].GetTextWithOptions(options);
                }
                else
                {
                    childText = DocxTableFromBuffer(child).GetTextWithOptions(options);
                }

                tableIndex = tableIndex + 1;
            }
            else
            {
                continue;
            }

            if (hasText)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + childText;
            hasText = true;
        }

        return text;
    }

    internal string GetTextFromTypedChildren(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasText = false;

        foreach (DocxParagraph paragraph in this.Paragraphs)
        {
            if (hasText)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + paragraph.GetTextWithOptions(options);
            hasText = true;
        }

        foreach (DocxTable table in this.Tables)
        {
            if (hasText)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + table.GetTextWithOptions(options);
            hasText = true;
        }

        return text;
    }

    internal int AcceptRevisions()
    {
        string xml = WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.AcceptRevisionXml(xml);
        LoadFromXml();
        return count;
    }

    internal int RejectRevisions()
    {
        string xml = WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.RejectRevisionXml(xml);
        LoadFromXml();
        return count;
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

        if (this.Xml != "" && !HasChangedContent())
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
            return;
        }

        writer.StartElement("w", "tc", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteBlocksTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WritePropertiesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.PropertiesXml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.PropertiesXml);
            buffer.WriteTo(writer);
            return;
        }

        if (!HasAnyFormatting())
        {
            return;
        }

        writer.StartElement("w", "tcPr", DocxConstants.NamespaceWordprocessingMl);
        WriteWidthTo(writer);
        WriteValueElement(writer, "gridSpan", this.GridSpan);
        WriteValueElement(writer, "hMerge", this.HorizontalMerge);
        WriteValueElement(writer, "vMerge", this.VerticalMerge);
        WriteBordersTo(writer);
        WriteShadingTo(writer);
        WriteMarginsTo(writer);
        WriteValueElement(writer, "textDirection", this.TextDirection);
        WriteValueElement(writer, "vAlign", this.VerticalAlignment);
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

        writer.StartElement("w", "tcW", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", this.Width);
        WriteOptionalAttribute(writer, "type", this.WidthType);
        writer.EndElement();
    }

    internal void WriteBordersTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Borders.HasAnyBorder())
        {
            this.Borders.WriteTo(writer, "tcBorders");
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

    internal void WriteMarginsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.MarginTopWidth == ""
            && this.MarginTopType == ""
            && this.MarginLeftWidth == ""
            && this.MarginLeftType == ""
            && this.MarginBottomWidth == ""
            && this.MarginBottomType == ""
            && this.MarginRightWidth == ""
            && this.MarginRightType == "")
        {
            return;
        }

        writer.StartElement("w", "tcMar", DocxConstants.NamespaceWordprocessingMl);
        WriteMarginTo(writer, "top", this.MarginTopWidth, this.MarginTopType);
        WriteMarginTo(writer, "left", this.MarginLeftWidth, this.MarginLeftType);
        WriteMarginTo(writer, "bottom", this.MarginBottomWidth, this.MarginBottomType);
        WriteMarginTo(writer, "right", this.MarginRightWidth, this.MarginRightType);
        writer.EndElement();
    }

    internal void WriteMarginTo(DocxXmlWriter writer, string localName, string width, string widthType)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
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

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "w", width);
        WriteOptionalAttribute(writer, "type", widthType);
        writer.EndElement();
    }

    internal void WriteParagraphsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxParagraph paragraph in this.Paragraphs)
        {
            paragraph.WriteTo(writer);
        }
    }

    internal void WriteBlocksTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxBlock block in this.Blocks)
        {
            block.WriteTo(writer);
        }
    }

    internal bool HasChangedContent()
    {
        if (this.Xml == "")
        {
            return true;
        }

        foreach (DocxBlock block in this.Blocks)
        {
            DocxParagraph? paragraph = block.GetParagraph();
            DocxTable? table = block.GetTable();

            if (paragraph != null && !paragraph.HasXml())
            {
                return true;
            }

            if (table != null && table.HasChangedContent())
            {
                return true;
            }
        }

        return false;
    }

    internal void WriteTablesTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxTable table in this.Tables)
        {
            table.WriteTo(writer);
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
            if (!IsParagraphChild(child) && !IsTableChild(child))
            {
                child.WriteTo(writer);
            }
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

    internal static string ReadMergeValue(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);

        if (value == "")
        {
            return "continue";
        }

        return value;
    }

    internal static bool IsParagraphChild(DocxXmlBuffer child)
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

        return reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static bool IsTableChild(DocxXmlBuffer child)
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

        return reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl);
    }
}
