namespace docx;

/// <summary>
/// Represents the block content of a footer part.
/// </summary>
public class DocxFooter : IDocxTextContent
{
    internal string Xml;
    internal string PartName;
    internal string RelationshipId;
    internal List<DocxBlock> Blocks;
    internal List<DocxXmlBuffer> UnknownChildren;

    /// <summary>
    /// Creates a footer instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxFooter(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PartName = "";
        this.RelationshipId = "";
        this.Blocks = new List<DocxBlock>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadBlocksFromXml();
    }

    internal static DocxFooter Create()
    {
        return new DocxFooter(CreateMinimalXml());
    }

    internal static string CreateMinimalXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteMinimal(writer);
        return writer.GetXml();
    }

    internal static void WriteMinimal(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "ftr", DocxConstants.NamespaceWordprocessingMl);
        writer.EndElement();
    }

    internal static DocxFooter Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("ftr", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:ftr.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxFooter(buffer.GetXml());
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void SetPartInfo(string partName, string relationshipId)
    {
        if (partName == null)
        {
            throw new ArgumentNullException("partName");
        }

        if (relationshipId == null)
        {
            throw new ArgumentNullException("relationshipId");
        }

        this.PartName = DocxPartName.Normalize(partName);
        this.RelationshipId = relationshipId;
    }

    internal void LoadBlocksFromXml()
    {
        this.Blocks = new List<DocxBlock>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("ftr", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int footerDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == footerDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == footerDepth + 1)
            {
                if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
                {
                    AddParsedBlock(DocxBlock.FromParagraph(DocxParagraph.Parse(reader)));
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
                {
                    AddParsedBlock(DocxBlock.FromTable(DocxTable.Parse(reader)));
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                this.UnknownChildren.Add(DocxXmlBuffer.Capture(reader));
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void AddParsedBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Add(block);
    }

    internal void AddBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Add(block);
        this.Xml = "";
    }

    internal void InsertBlock(int index, DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Insert(index, block);
        this.Xml = "";
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
        this.Xml = "";
    }

    internal DocxBlock RemoveBlockAt(int index)
    {
        DocxBlock block = this.Blocks[index];
        this.Blocks.RemoveAt(index);
        this.Xml = "";
        return block;
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
            new DocxXmlBuffer(this.Xml).WriteTo(writer);
            return;
        }

        writer.StartElement("w", "ftr", DocxConstants.NamespaceWordprocessingMl);

        foreach (DocxBlock block in this.Blocks)
        {
            block.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }

        writer.EndElement();
    }

    internal int BlockCount()
    {
        return this.Blocks.Count;
    }

    internal DocxBlock GetBlock(int index)
    {
        return this.Blocks[index];
    }

    internal int ParagraphCount()
    {
        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsParagraph())
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int TableCount()
    {
        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsTable())
            {
                count = count + 1;
            }
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

        return DocxTextExtractor.ExtractFooter(this, options);
    }
}
