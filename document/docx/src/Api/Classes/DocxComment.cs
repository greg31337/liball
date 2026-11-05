namespace docx;

/// <summary>
/// Represents one document comment and its block content.
/// </summary>
public class DocxComment : IDocxTextContent
{
    internal string Xml;
    internal string Id;
    internal string Author;
    internal string Date;
    internal string Initials;
    internal List<DocxBlock> Blocks;
    internal List<DocxXmlBuffer> UnknownChildren;

    /// <summary>
    /// Creates a comment instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxComment(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Id = "";
        this.Author = "";
        this.Date = "";
        this.Initials = "";
        this.Blocks = new List<DocxBlock>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        LoadFromXml();
    }

    internal static DocxComment Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("comment", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:comment.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxComment(buffer.GetXml());
    }

    internal static DocxComment ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("XML does not contain an element.");
        }

        return Parse(reader);
    }

    internal void LoadFromXml()
    {
        this.Id = "";
        this.Author = "";
        this.Date = "";
        this.Initials = "";
        this.Blocks = new List<DocxBlock>();
        this.UnknownChildren = new List<DocxXmlBuffer>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("comment", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        this.Id = reader.GetAttribute("id", DocxConstants.NamespaceWordprocessingMl);
        this.Author = reader.GetAttribute("author", DocxConstants.NamespaceWordprocessingMl);
        this.Date = reader.GetAttribute("date", DocxConstants.NamespaceWordprocessingMl);
        this.Initials = reader.GetAttribute("initials", DocxConstants.NamespaceWordprocessingMl);

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int rootDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == rootDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == rootDepth + 1)
            {
                if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Blocks.Add(DocxBlock.FromParagraph(DocxParagraph.Parse(reader)));
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("tbl", DocxConstants.NamespaceWordprocessingMl))
                {
                    this.Blocks.Add(DocxBlock.FromTable(DocxTable.Parse(reader)));
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

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
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

        return DocxTextExtractor.ExtractComment(this, options);
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    internal static DocxComment Create(string id, string author, string initials, string date, string text)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (date == null)
        {
            throw new ArgumentNullException("date");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }

        DocxComment comment = new DocxComment("");
        comment.Id = id;
        comment.Author = author;
        comment.Initials = initials;
        comment.Date = date;
        comment.AddParagraph(CreateTextParagraph(text));
        return comment;
    }

    internal static DocxParagraph CreateTextParagraph(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "p", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "r", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteElementText("w", "t", DocxConstants.NamespaceWordprocessingMl, text);
        writer.EndElement();
        writer.EndElement();
        return new DocxParagraph(writer.GetXml());
    }

    internal void AddParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        AddBlock(DocxBlock.FromParagraph(paragraph));
    }

    internal void AddBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Xml = "";
        this.Blocks.Add(block);
    }

    internal void ReplaceText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        this.Xml = "";
        this.Blocks = new List<DocxBlock>();
        this.UnknownChildren = new List<DocxXmlBuffer>();
        AddParagraph(CreateTextParagraph(text));
    }

    internal void SetReviewMetadata(string author, string initials, string date)
    {
        if (author == null)
        {
            throw new ArgumentNullException("author");
        }

        if (initials == null)
        {
            throw new ArgumentNullException("initials");
        }

        if (date == null)
        {
            throw new ArgumentNullException("date");
        }

        this.Xml = "";
        this.Author = author;
        this.Initials = initials;
        this.Date = date;
    }

    internal DocxBlock RemoveBlockAt(int index)
    {
        DocxBlock block = this.Blocks[index];
        this.Xml = "";
        this.Blocks.RemoveAt(index);
        return block;
    }

    internal static string CreateRangeStartXml(string id)
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteRangeStartTo(writer, id);
        return writer.GetXml();
    }

    internal static string CreateRangeEndXml(string id)
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteRangeEndTo(writer, id);
        return writer.GetXml();
    }

    internal static string CreateReferenceXml(string id)
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteReferenceTo(writer, id);
        return writer.GetXml();
    }

    internal static DocxRun CreateReferenceRun(string id)
    {
        return new DocxRun(CreateReferenceXml(id));
    }

    internal static void WriteRangeStartTo(DocxXmlWriter writer, string id)
    {
        WriteRangeMarkerTo(writer, "commentRangeStart", id);
    }

    internal static void WriteRangeEndTo(DocxXmlWriter writer, string id)
    {
        WriteRangeMarkerTo(writer, "commentRangeEnd", id);
    }

    internal static void WriteRangeMarkerTo(DocxXmlWriter writer, string localName, string id)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (localName == "")
        {
            throw new ArgumentException("Range marker local name is required.", "localName");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "id", DocxConstants.NamespaceWordprocessingMl, id);
        writer.EndElement();
    }

    internal static void WriteReferenceTo(DocxXmlWriter writer, string id)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }

        writer.StartElement("w", "r", DocxConstants.NamespaceWordprocessingMl);
        writer.StartElement("w", "commentReference", DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", "id", DocxConstants.NamespaceWordprocessingMl, id);
        writer.EndElement();
        writer.EndElement();
    }

    internal string WriteXml()
    {
        if (this.Xml != "")
        {
            return this.Xml;
        }

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

        writer.StartElement("w", "comment", DocxConstants.NamespaceWordprocessingMl);

        if (this.Id != "")
        {
            writer.WriteAttribute("w", "id", DocxConstants.NamespaceWordprocessingMl, this.Id);
        }

        if (this.Author != "")
        {
            writer.WriteAttribute("w", "author", DocxConstants.NamespaceWordprocessingMl, this.Author);
        }

        if (this.Date != "")
        {
            writer.WriteAttribute("w", "date", DocxConstants.NamespaceWordprocessingMl, this.Date);
        }

        if (this.Initials != "")
        {
            writer.WriteAttribute("w", "initials", DocxConstants.NamespaceWordprocessingMl, this.Initials);
        }

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
}
