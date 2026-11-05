namespace docx;

/// <summary>
/// Represents table row data used by a DOCX document.
/// </summary>
public class DocxTableRow
{
    internal string Xml;
    internal string PropertiesXml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxTableCell> Cells;
    /// <summary>
    /// The height value stored in the document model.
    /// </summary>
    public string Height = "";
    /// <summary>
    /// The height rule value stored in the document model.
    /// </summary>
    public string HeightRule = "";
    /// <summary>
    /// Whether cant split was explicitly supplied.
    /// </summary>
    public bool HasCantSplit;
    /// <summary>
    /// Whether the cant split behavior is enabled.
    /// </summary>
    public bool CantSplit;
    /// <summary>
    /// Whether table header was explicitly supplied.
    /// </summary>
    public bool HasTableHeader;
    /// <summary>
    /// Whether the table header behavior is enabled.
    /// </summary>
    public bool TableHeader;

    /// <summary>
    /// Creates a table row instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxTableRow(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Cells = new List<DocxTableCell>();
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxTableRow Create()
    {
        return new DocxTableRow("");
    }

    internal static DocxTableRow Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxTableRow(buffer.GetXml());
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
        this.Cells = new List<DocxTableCell>();
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("tr", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int rowDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == rowDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == rowDepth + 1)
            {
                if (reader.IsElement("trPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    ReadPropertiesXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                bool isCell = reader.IsElement("tc", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);

                if (isCell)
                {
                    AddParsedCell(DocxTableCellFromBuffer(child));
                }

                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void InitializeParsedState()
    {
        this.Height = "";
        this.HeightRule = "";
        this.HasCantSplit = false;
        this.CantSplit = false;
        this.HasTableHeader = false;
        this.TableHeader = false;
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

        if (!reader.ReadToElement("trPr", DocxConstants.NamespaceWordprocessingMl))
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

        if (reader.IsElement("trHeight", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Height = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.HeightRule = reader.GetAttribute("hRule", DocxConstants.NamespaceWordprocessingMl);
        }
        else if (reader.IsElement("cantSplit", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasCantSplit = true;
            this.CantSplit = DocxRunFormat.ReadToggleValue(reader);
        }
        else if (reader.IsElement("tblHeader", DocxConstants.NamespaceWordprocessingMl))
        {
            this.HasTableHeader = true;
            this.TableHeader = DocxRunFormat.ReadToggleValue(reader);
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

    internal void SetHeight(string value, string rule)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (rule == null)
        {
            throw new ArgumentNullException("rule");
        }

        this.Height = value;
        this.HeightRule = rule;
        ClearRawProperties();
    }

    internal void SetCantSplit(bool value)
    {
        this.HasCantSplit = true;
        this.CantSplit = value;
        ClearRawProperties();
    }

    internal void ClearCantSplit()
    {
        this.HasCantSplit = false;
        this.CantSplit = false;
        ClearRawProperties();
    }

    internal void SetTableHeader(bool value)
    {
        this.HasTableHeader = true;
        this.TableHeader = value;
        ClearRawProperties();
    }

    internal void ClearTableHeader()
    {
        this.HasTableHeader = false;
        this.TableHeader = false;
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

        if (IsCellChild(child))
        {
            AddCell(DocxTableCellFromBuffer(child));
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

    internal void AddCell(DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        this.Cells.Add(cell);
        this.Xml = "";
    }

    internal DocxTableCell AddNewCell()
    {
        DocxTableCell cell = DocxTableCell.Create();
        AddCell(cell);
        return cell;
    }

    internal void InsertCell(int index, DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        this.Cells.Insert(index, cell);
        this.Xml = "";
    }

    internal DocxTableCell InsertNewCell(int index)
    {
        DocxTableCell cell = DocxTableCell.Create();
        InsertCell(index, cell);
        return cell;
    }

    internal DocxTableCell RemoveCellAt(int index)
    {
        DocxTableCell cell = this.Cells[index];
        this.Cells.RemoveAt(index);
        this.Xml = "";
        return cell;
    }

    internal void MoveCell(int sourceIndex, int destinationIndex)
    {
        if (sourceIndex == destinationIndex)
        {
            return;
        }

        DocxTableCell cell = this.Cells[sourceIndex];
        this.Cells.RemoveAt(sourceIndex);
        this.Cells.Insert(destinationIndex, cell);
        this.Xml = "";
    }

    internal bool HasChangedContent()
    {
        if (this.Xml == "")
        {
            return true;
        }

        foreach (DocxTableCell cell in this.Cells)
        {
            if (cell.HasChangedContent())
            {
                return true;
            }
        }

        return false;
    }

    internal void AddParsedCell(DocxTableCell cell)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        this.Cells.Add(cell);
    }

    internal static DocxTableCell DocxTableCellFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxTableCell.Parse(reader);
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

    internal int CellElementCount()
    {
        return CountChildElements("tc", DocxConstants.NamespaceWordprocessingMl);
    }

    internal int CellCount()
    {
        return this.Cells.Count;
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

    internal DocxTableCell GetCell(int index)
    {
        return this.Cells[index];
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = 0;

        foreach (DocxTableCell cell in this.Cells)
        {
            count = count + cell.FillContentControls(values);
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
        return this.Height != ""
            || this.HeightRule != ""
            || this.HasCantSplit
            || this.HasTableHeader;
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

        writer.StartElement("w", "tr", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteCellsTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteCellsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxTableCell cell in this.Cells)
        {
            cell.WriteTo(writer);
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
            if (!IsCellChild(child))
            {
                child.WriteTo(writer);
            }
        }
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

        writer.StartElement("w", "trPr", DocxConstants.NamespaceWordprocessingMl);
        WriteHeightTo(writer);
        WriteToggleProperty(writer, "cantSplit", this.HasCantSplit, this.CantSplit);
        WriteToggleProperty(writer, "tblHeader", this.HasTableHeader, this.TableHeader);
        writer.EndElement();
    }

    internal void WriteHeightTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Height == "" && this.HeightRule == "")
        {
            return;
        }

        writer.StartElement("w", "trHeight", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "val", this.Height);
        WriteOptionalAttribute(writer, "hRule", this.HeightRule);
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

    internal static bool IsCellChild(DocxXmlBuffer child)
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

        return reader.IsElement("tc", DocxConstants.NamespaceWordprocessingMl);
    }
}
