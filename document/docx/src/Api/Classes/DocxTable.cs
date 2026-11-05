namespace docx;

/// <summary>
/// Represents table data used by a DOCX document.
/// </summary>
public class DocxTable : IDocxTextContent, IDocxTableModel
{
    internal const string InactiveVerticalMergeValue = "\0";

    internal string Xml;
    internal string PropertiesXml;
    internal List<DocxXmlBuffer> Children;
    internal DocxTableFormat Format;
    internal DocxTableGrid Grid;
    internal List<DocxTableRow> Rows;

    /// <summary>
    /// Creates a table instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxTable(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Format = new DocxTableFormat();
        this.Grid = DocxTableGrid.Create();
        this.Rows = new List<DocxTableRow>();
        LoadFromXml();
    }

    internal static DocxTable Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tbl.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxTable(buffer.GetXml());
    }

    internal static DocxTable Create(int rows, int columns)
    {
        ValidateCount("rows", rows);
        ValidateCount("columns", columns);

        DocxTable table = new DocxTable("");
        int rowIndex = 0;

        while (rowIndex < rows)
        {
            table.AddRowWithCells(columns);
            rowIndex = rowIndex + 1;
        }

        return table;
    }

    internal static void ValidateCount(string name, int count)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(name);
        }
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
        this.Format = new DocxTableFormat();
        this.Grid = DocxTableGrid.Create();
        this.Rows = new List<DocxTableRow>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("tbl", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int tableDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == tableDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == tableDepth + 1)
            {
                bool isGrid = reader.IsElement("tblGrid", DocxConstants.NamespaceWordprocessingMl);
                bool isRow = reader.IsElement("tr", DocxConstants.NamespaceWordprocessingMl);

                if (reader.IsElement("tblPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    this.Format = DocxTableFormat.ParseXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);

                if (isGrid)
                {
                    this.Grid = DocxTableGridFromBuffer(child);
                }
                else if (isRow)
                {
                    AddParsedRow(DocxTableRowFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void AddChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);

        if (IsTableGridChild(child))
        {
            this.Grid = DocxTableGridFromBuffer(child);
        }
        else if (IsTableRowChild(child))
        {
            AddRow(DocxTableRowFromBuffer(child));
        }
    }

    internal void AddParsedChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddRow(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        this.Rows.Add(row);
        this.Xml = "";
    }

    internal DocxTableRow AddEmptyRow()
    {
        DocxTableRow row = DocxTableRow.Create();
        AddRow(row);
        return row;
    }

    internal DocxTableRow AddRowWithCells(int cellCount)
    {
        DocxTableRow row = CreateRow(cellCount);
        AddRow(row);
        return row;
    }

    internal void InsertRow(int index, DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        this.Rows.Insert(index, row);
        this.Xml = "";
    }

    internal DocxTableRow InsertEmptyRow(int index)
    {
        DocxTableRow row = DocxTableRow.Create();
        InsertRow(index, row);
        return row;
    }

    internal DocxTableRow InsertRowWithCells(int index, int cellCount)
    {
        DocxTableRow row = CreateRow(cellCount);
        InsertRow(index, row);
        return row;
    }

    internal DocxTableRow RemoveRowAt(int index)
    {
        DocxTableRow row = this.Rows[index];
        this.Rows.RemoveAt(index);
        this.Xml = "";
        return row;
    }

    internal void MoveRow(int sourceIndex, int destinationIndex)
    {
        if (sourceIndex == destinationIndex)
        {
            return;
        }

        DocxTableRow row = this.Rows[sourceIndex];
        this.Rows.RemoveAt(sourceIndex);
        this.Rows.Insert(destinationIndex, row);
        this.Xml = "";
    }

    internal bool HasChangedContent()
    {
        if (this.Xml == "")
        {
            return true;
        }

        foreach (DocxTableRow row in this.Rows)
        {
            if (row.HasChangedContent())
            {
                return true;
            }
        }

        return false;
    }

    internal static DocxTableRow CreateRow(int cellCount)
    {
        ValidateCount("cellCount", cellCount);

        DocxTableRow row = DocxTableRow.Create();
        int cellIndex = 0;

        while (cellIndex < cellCount)
        {
            DocxTableCell cell = row.AddNewCell();
            cell.AddParagraph(new DocxParagraph(""));
            cellIndex = cellIndex + 1;
        }

        return row;
    }

    internal void AddParsedRow(DocxTableRow row)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        this.Rows.Add(row);
    }

    internal void SetGrid(DocxTableGrid grid)
    {
        if (grid == null)
        {
            throw new ArgumentNullException("grid");
        }

        this.Grid = grid;
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

        this.Format.SetWidth(width, widthType);
        this.PropertiesXml = "";
        this.Xml = "";
    }

    internal void SetGridColumnWidths(List<string> widths)
    {
        if (widths == null)
        {
            throw new ArgumentNullException("widths");
        }

        DocxTableGrid grid = DocxTableGrid.Create();
        grid.SetColumnWidths(widths);
        SetGrid(grid);
    }

    internal void SetWidthFromGrid(string widthType)
    {
        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        string width = this.Grid.GetTotalWidth();

        if (width == "")
        {
            throw new InvalidOperationException("Grid column widths must be numeric to calculate table width.");
        }

        SetWidth(width, widthType);
    }

    internal void ApplyGridColumnWidthsToCells(string widthType)
    {
        if (widthType == null)
        {
            throw new ArgumentNullException("widthType");
        }

        foreach (DocxTableRow row in this.Rows)
        {
            int cellIndex = 0;

            while (cellIndex < row.CellCount() && cellIndex < this.Grid.ColumnCount())
            {
                DocxTableCell cell = row.GetCell(cellIndex);
                cell.SetWidth(this.Grid.GetColumnWidth(cellIndex), widthType);
                cellIndex = cellIndex + 1;
            }
        }
    }

    internal static DocxTableGrid DocxTableGridFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxTableGrid.Parse(reader);
    }

    internal static DocxTableRow DocxTableRowFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxTableRow.Parse(reader);
    }

    internal void SetPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.PropertiesXml = xml;
        this.Format = DocxTableFormat.ParseXml(xml);
    }

    /// <summary>
    /// Replaces the direct table format model.
    /// </summary>
    /// <param name="format">The replacement table format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    public void SetFormat(DocxTableFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.Format = format;
        this.PropertiesXml = "";
    }

    internal bool HasProperties()
    {
        return this.PropertiesXml != "";
    }

    internal string GetPropertiesXml()
    {
        return this.PropertiesXml;
    }

    /// <summary>
    /// Returns whether direct table formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasFormat()
    {
        return this.Format.HasAnyFormatting();
    }

    /// <summary>
    /// Returns the direct table format model.
    /// </summary>
    /// <returns>The table format model.</returns>
    public DocxTableFormat GetFormat()
    {
        return this.Format;
    }

    internal bool HasGrid()
    {
        return this.Grid.HasXml();
    }

    /// <summary>
    /// Returns the table grid definition.
    /// </summary>
    /// <returns>The table grid definition.</returns>
    public DocxTableGrid GetGrid()
    {
        return this.Grid;
    }

    internal int RowCount()
    {
        return this.Rows.Count;
    }

    /// <summary>
    /// Returns a table row by position.
    /// </summary>
    /// <param name="index">The zero-based row position.</param>
    /// <returns>The requested table row.</returns>
    public DocxTableRow GetRow(int index)
    {
        return this.Rows[index];
    }

    /// <summary>
    /// Repeats a template row for each supplied data row.
    /// </summary>
    /// <param name="templateRowIndex">The zero-based template row position.</param>
    /// <param name="data">The values used to populate repeated rows.</param>
    /// <returns>The number of rows inserted.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>data</c> is null.</exception>
    public int RepeatRow(int templateRowIndex, DocxTableData data)
    {
        return DocxTemplate.RepeatTableRow(this, templateRowIndex, data);
    }

    internal int RepeatRowWithDelimiters(int templateRowIndex, DocxTableData data, string prefix, string suffix)
    {
        return DocxTemplate.RepeatTableRow(this, templateRowIndex, data, prefix, suffix);
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = 0;

        foreach (DocxTableRow row in this.Rows)
        {
            count = count + row.FillContentControls(values);
        }

        if (count > 0)
        {
            this.Xml = "";
        }

        return count;
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

        string text = "";
        bool hasRow = false;

        foreach (DocxTableRow row in this.Rows)
        {
            if (hasRow)
            {
                text = text + options.TableRowSeparator;
            }

            text = text + GetRowText(row, options);
            hasRow = true;
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

    internal static string GetRowText(DocxTableRow row, DocxTextOptions options)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasCell = false;

        foreach (DocxTableCell cell in row.Cells)
        {
            if (hasCell)
            {
                text = text + options.TableCellSeparator;
            }

            text = text + cell.GetTextWithOptions(options);
            hasCell = true;
        }

        return text;
    }

    internal List<List<string>> ToTextMatrix()
    {
        DocxTextOptions options = new DocxTextOptions();
        return ToTextMatrixWithOptions(options);
    }

    internal List<List<string>> ToTextMatrixWithOptions(DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<List<string>> matrix = new List<List<string>>();
        List<string> verticalValues = new List<string>();

        foreach (DocxTableRow row in this.Rows)
        {
            List<string> projectedRow = ProjectRowText(row, options, verticalValues);
            matrix.Add(projectedRow);
        }

        return matrix;
    }

    internal static List<string> ProjectRowText(DocxTableRow row, DocxTextOptions options, List<string> verticalValues)
    {
        if (row == null)
        {
            throw new ArgumentNullException("row");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (verticalValues == null)
        {
            throw new ArgumentNullException("verticalValues");
        }

        List<string> projectedRow = new List<string>();
        int columnIndex = 0;

        foreach (DocxTableCell cell in row.Cells)
        {
            string cellText = cell.GetTextWithOptions(options);
            int span = cell.GetGridSpanCount();
            int spanIndex = 0;

            while (spanIndex < span)
            {
                while (verticalValues.Count <= columnIndex)
                {
                    verticalValues.Add(InactiveVerticalMergeValue);
                }

                string projectedText = cellText;

                if (cell.IsVerticalMergeContinuation() && verticalValues[columnIndex] != InactiveVerticalMergeValue)
                {
                    projectedText = verticalValues[columnIndex];
                }

                projectedRow.Add(projectedText);

                if (cell.IsVerticalMergeStart())
                {
                    verticalValues[columnIndex] = projectedText;
                }
                else if (!cell.IsVerticalMergeContinuation())
                {
                    verticalValues[columnIndex] = InactiveVerticalMergeValue;
                }

                spanIndex = spanIndex + 1;
                columnIndex = columnIndex + 1;
            }
        }

        return projectedRow;
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int CountChildElements(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        int count = 0;

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement(localName, namespaceUri))
            {
                count = count + 1;
            }
        }

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

        writer.StartElement("w", "tbl", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteGridTo(writer);
        WriteUnknownChildrenTo(writer);
        WriteRowsTo(writer);
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

        if (this.Format.HasAnyFormatting())
        {
            this.Format.WriteTo(writer);
        }
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxXmlBuffer child in this.Children)
        {
            child.WriteTo(writer);
        }
    }

    internal void WriteGridTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Grid.HasXml() || this.Grid.ColumnCount() > 0)
        {
            this.Grid.WriteTo(writer);
        }
    }

    internal void WriteRowsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxTableRow row in this.Rows)
        {
            row.WriteTo(writer);
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
            if (!IsManagedTableChild(child))
            {
                child.WriteTo(writer);
            }
        }
    }

    internal static bool IsManagedTableChild(DocxXmlBuffer child)
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

        return IsTableGridChild(child) || IsTableRowChild(child);
    }

    internal static bool IsTableGridChild(DocxXmlBuffer child)
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

        return reader.IsElement("tblGrid", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static bool IsTableRowChild(DocxXmlBuffer child)
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

        return reader.IsElement("tr", DocxConstants.NamespaceWordprocessingMl);
    }
}
