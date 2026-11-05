namespace docx;

/// <summary>
/// Represents table grid data used by a DOCX document.
/// </summary>
public class DocxTableGrid
{
    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    /// <summary>
    /// The table grid column widths.
    /// </summary>
    public List<string> ColumnWidths;

    /// <summary>
    /// Creates a table grid instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxTableGrid(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        this.ColumnWidths = new List<string>();
        LoadFromXml();
    }

    internal static DocxTableGrid Create()
    {
        return new DocxTableGrid("");
    }

    internal static DocxTableGrid Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("tblGrid", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:tblGrid.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxTableGrid(buffer.GetXml());
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
        this.ColumnWidths = new List<string>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("tblGrid", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int gridDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == gridDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == gridDepth + 1)
            {
                if (reader.IsElement("gridCol", DocxConstants.NamespaceWordprocessingMl))
                {
                    AddParsedColumnWidth(reader.GetAttribute("w", DocxConstants.NamespaceWordprocessingMl));
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);
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

    internal void AddColumnWidth(string width)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        this.ColumnWidths.Add(width);
        this.Xml = "";
    }

    internal void AddParsedColumnWidth(string width)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        this.ColumnWidths.Add(width);
    }

    internal void SetColumnWidth(int index, string width)
    {
        if (width == null)
        {
            throw new ArgumentNullException("width");
        }

        this.ColumnWidths[index] = width;
        this.Xml = "";
    }

    internal void SetColumnWidths(List<string> widths)
    {
        if (widths == null)
        {
            throw new ArgumentNullException("widths");
        }

        foreach (string width in widths)
        {
            if (width == null)
            {
                throw new ArgumentNullException("widths");
            }
        }

        this.ColumnWidths = new List<string>();

        foreach (string width in widths)
        {
            this.ColumnWidths.Add(width);
        }

        this.Xml = "";
    }

    internal void ClearColumnWidths()
    {
        this.ColumnWidths = new List<string>();
        this.Xml = "";
    }

    internal string GetTotalWidth()
    {
        if (this.ColumnWidths.Count == 0)
        {
            return "";
        }

        long total = 0;

        foreach (string width in this.ColumnWidths)
        {
            if (width == "")
            {
                return "";
            }

            long value = 0;

            if (!long.TryParse(width, out value))
            {
                return "";
            }

            if ((value > 0 && total > long.MaxValue - value)
                || (value < 0 && total < long.MinValue - value))
            {
                return "";
            }

            total = total + value;
        }

        return total.ToString();
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

        writer.StartElement("w", "tblGrid", DocxConstants.NamespaceWordprocessingMl);
        WriteColumnsTo(writer);
        WriteUnknownChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteColumnsTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (string width in this.ColumnWidths)
        {
            writer.StartElement("w", "gridCol", DocxConstants.NamespaceWordprocessingMl);

            if (width != "")
            {
                writer.WriteAttribute("w", "w", DocxConstants.NamespaceWordprocessingMl, width);
            }

            writer.EndElement();
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
            if (!IsGridColumnChild(child))
            {
                child.WriteTo(writer);
            }
        }
    }

    internal static bool IsGridColumnChild(DocxXmlBuffer child)
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

        return reader.IsElement("gridCol", DocxConstants.NamespaceWordprocessingMl);
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int ColumnCount()
    {
        return this.ColumnWidths.Count;
    }

    internal string GetColumnWidth(int index)
    {
        return this.ColumnWidths[index];
    }
}
