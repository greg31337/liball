namespace docx;

/// <summary>
/// Represents the ordered paragraphs and tables in the main document body.
/// </summary>
public class DocxBody : IDocxTextContent, IDocxBodyModel
{
    internal string Xml;
    internal List<DocxBlock> Blocks;
    internal List<DocxSection> Sections;
    internal DocxSection? FinalSection;

    /// <summary>
    /// Creates a body instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxBody(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Blocks = new List<DocxBlock>();
        this.Sections = new List<DocxSection>();
        this.FinalSection = null;
        LoadBlocksFromXml();
        LoadSectionsFromXml();
    }

    internal static DocxBody Create()
    {
        return new DocxBody("");
    }

    internal static DocxBody Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("body", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:body.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxBody(buffer.GetXml());
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadBlocksFromXml()
    {
        this.Blocks = new List<DocxBlock>();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("body", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int bodyDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == bodyDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == bodyDepth + 1)
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

                if (DocxContentControl.IsSupported(reader))
                {
                    AddParsedBlock(DocxBlock.FromContentControl(DocxContentControl.Parse(reader)));
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (!reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer preserved = DocxXmlBuffer.Capture(reader);
                    AddParsedBlock(new DocxBlock(DocxBlock.KindPreserved, preserved.GetXml()));
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void LoadSectionsFromXml()
    {
        this.Sections = new List<DocxSection>();
        this.FinalSection = null;

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("body", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int bodyDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == bodyDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == bodyDepth + 1)
            {
                if (reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer section = DocxXmlBuffer.Capture(reader);
                    DocxSection parsedSection = DocxSectionFromBuffer(section);
                    AddParsedSection(parsedSection);
                    this.FinalSection = parsedSection;
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                if (reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer paragraph = DocxXmlBuffer.Capture(reader);
                    AddSectionsFromParagraphXml(paragraph.GetXml());
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                reader.ReadPastCurrentElement();
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal void AddSectionsFromParagraphXml(string xml)
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

        if (!reader.ReadToElement("p", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int paragraphDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == paragraphDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxXmlBuffer section = DocxXmlBuffer.Capture(reader);
                AddParsedSection(DocxSectionFromBuffer(section));
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    /// <summary>
    /// Appends a block to the body.
    /// </summary>
    /// <param name="block">The block to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    public void AddBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Add(block);
        CompleteBlockMutation();
    }

    internal void AddParsedBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Add(block);
    }

    /// <summary>
    /// Inserts a block at a body position.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    public void InsertBlock(int index, DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        this.Blocks.Insert(index, block);
        CompleteBlockMutation();
    }

    /// <summary>
    /// Inserts a paragraph at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="paragraph">The paragraph to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>paragraph</c> is null.</exception>
    public void InsertParagraph(int index, DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        InsertBlock(index, DocxBlock.FromParagraph(paragraph));
    }

    /// <summary>
    /// Inserts a table at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="table">The table to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    public void InsertTable(int index, DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        InsertBlock(index, DocxBlock.FromTable(table));
    }

    /// <summary>
    /// Inserts a content control at a body block position.
    /// </summary>
    /// <param name="index">The zero-based block insertion position.</param>
    /// <param name="contentControl">The content control to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    public void InsertContentControl(int index, DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        InsertBlock(index, DocxBlock.FromContentControl(contentControl));
    }

    /// <summary>
    /// Replaces a body block.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="block">The replacement block.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>block</c> is null.</exception>
    public DocxBlock ReplaceBlock(int index, DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        DocxBlock replaced = this.Blocks[index];
        this.Blocks[index] = block;
        CompleteBlockMutation();
        return replaced;
    }

    /// <summary>
    /// Replaces a body block with a paragraph.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="paragraph">The replacement paragraph.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>paragraph</c> is null.</exception>
    public DocxBlock ReplaceBlockWithParagraph(int index, DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        return ReplaceBlock(index, DocxBlock.FromParagraph(paragraph));
    }

    /// <summary>
    /// Replaces a body block with a table.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="table">The replacement table.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>table</c> is null.</exception>
    public DocxBlock ReplaceBlockWithTable(int index, DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        return ReplaceBlock(index, DocxBlock.FromTable(table));
    }

    /// <summary>
    /// Replaces a body block with a content control.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <param name="contentControl">The replacement content control.</param>
    /// <returns>The replaced block.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    public DocxBlock ReplaceBlockWithContentControl(int index, DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        return ReplaceBlock(index, DocxBlock.FromContentControl(contentControl));
    }

    /// <summary>
    /// Moves a block to another body position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based block position.</param>
    /// <param name="destinationIndex">The resulting zero-based block position.</param>
    public void MoveBlock(int sourceIndex, int destinationIndex)
    {
        DocxBlock block = this.Blocks[sourceIndex];

        if (destinationIndex < 0 || destinationIndex >= this.Blocks.Count)
        {
            throw new ArgumentOutOfRangeException("destinationIndex");
        }

        if (sourceIndex == destinationIndex)
        {
            return;
        }

        this.Blocks.RemoveAt(sourceIndex);
        this.Blocks.Insert(destinationIndex, block);
        CompleteBlockMutation();
    }

    /// <summary>
    /// Removes a body block.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveBlockAt(int index)
    {
        DocxBlock removed = this.Blocks[index];
        this.Blocks.RemoveAt(index);
        CompleteBlockMutation();
        return removed;
    }

    internal DocxTable CreateAndAddTable(int rows, int columns)
    {
        DocxTable table = DocxTable.Create(rows, columns);
        AddBlock(DocxBlock.FromTable(table));
        return table;
    }

    internal DocxTable CreateAndInsertTable(int index, int rows, int columns)
    {
        DocxTable table = DocxTable.Create(rows, columns);
        InsertBlock(index, DocxBlock.FromTable(table));
        return table;
    }

    internal DocxParagraph AddSectionBreak(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        DocxParagraph paragraph = new DocxParagraph("");
        paragraph.SetSection(section);
        AddBlock(DocxBlock.FromParagraph(paragraph));
        return paragraph;
    }

    internal void AddSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        this.Sections.Add(section);
        this.Xml = "";
    }

    /// <summary>
    /// Sets or replaces the final section properties.
    /// </summary>
    /// <param name="section">The final section properties.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>section</c> is null.</exception>
    public void SetFinalSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        this.FinalSection = section;
        CompleteBlockMutation();
    }

    internal void ClearFinalSection()
    {
        RemoveFinalSection();
    }

    /// <summary>
    /// Removes the final section properties.
    /// </summary>
    /// <returns><c>true</c> when final section properties were removed.</returns>
    public bool RemoveFinalSection()
    {
        if (this.FinalSection == null)
        {
            return false;
        }

        this.FinalSection = null;
        CompleteBlockMutation();
        return true;
    }

    internal void CompleteBlockMutation()
    {
        this.Xml = "";
        this.Sections = new List<DocxSection>();

        foreach (DocxBlock block in this.Blocks)
        {
            DocxParagraph? paragraph = block.GetParagraph();

            if (paragraph != null && paragraph.Section != null)
            {
                this.Sections.Add(paragraph.Section);
            }
        }

        if (this.FinalSection != null)
        {
            this.Sections.Add(this.FinalSection);
        }
    }

    internal void AddParsedSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        this.Sections.Add(section);
    }

    internal static DocxSection DocxSectionFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxSection.Parse(reader);
    }

    /// <summary>
    /// Returns all body blocks in document order.
    /// </summary>
    /// <returns>A new list containing the body blocks.</returns>
    public List<DocxBlock> GetBlocks()
    {
        return new List<DocxBlock>(this.Blocks);
    }

    /// <summary>
    /// Returns the number of body blocks.
    /// </summary>
    /// <returns>The block count.</returns>
    public int BlockCount()
    {
        return this.Blocks.Count;
    }

    /// <summary>
    /// Returns a body block by position.
    /// </summary>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The requested block.</returns>
    public DocxBlock GetBlock(int index)
    {
        return this.Blocks[index];
    }

    /// <summary>
    /// Returns all paragraphs in body order.
    /// </summary>
    /// <returns>A new list containing the body paragraphs.</returns>
    public List<DocxParagraph> GetParagraphs()
    {
        List<DocxParagraph> paragraphs = new List<DocxParagraph>();

        foreach (DocxBlock block in this.Blocks)
        {
            DocxParagraph? paragraph = block.GetParagraph();

            if (paragraph != null)
            {
                paragraphs.Add(paragraph);
            }
        }

        return paragraphs;
    }

    /// <summary>
    /// Returns the number of body paragraphs.
    /// </summary>
    /// <returns>The paragraph count.</returns>
    public int ParagraphCount()
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

    /// <summary>
    /// Returns a paragraph by its position among body paragraphs.
    /// </summary>
    /// <param name="index">The zero-based paragraph position.</param>
    /// <returns>The requested paragraph.</returns>
    public DocxParagraph GetParagraph(int index)
    {
        return GetParagraphs()[index];
    }

    /// <summary>
    /// Returns all tables in body order.
    /// </summary>
    /// <returns>A new list containing the body tables.</returns>
    public List<DocxTable> GetTables()
    {
        List<DocxTable> tables = new List<DocxTable>();

        foreach (DocxBlock block in this.Blocks)
        {
            DocxTable? table = block.GetTable();

            if (table != null)
            {
                tables.Add(table);
            }
        }

        return tables;
    }

    /// <summary>
    /// Returns the number of body tables.
    /// </summary>
    /// <returns>The table count.</returns>
    public int TableCount()
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
    /// Returns a table by its position among body tables.
    /// </summary>
    /// <param name="index">The zero-based table position.</param>
    /// <returns>The requested table.</returns>
    public DocxTable GetTable(int index)
    {
        return GetTables()[index];
    }

    /// <summary>
    /// Returns all direct body content controls in body order.
    /// </summary>
    /// <returns>A new list containing the direct body content controls.</returns>
    public List<DocxContentControl> GetContentControls()
    {
        List<DocxContentControl> contentControls = new List<DocxContentControl>();

        foreach (DocxBlock block in this.Blocks)
        {
            DocxContentControl? contentControl = block.GetContentControl();

            if (contentControl != null)
            {
                contentControls.Add(contentControl);
            }
        }

        return contentControls;
    }

    /// <summary>
    /// Returns the number of direct body content controls.
    /// </summary>
    /// <returns>The content-control count.</returns>
    public int ContentControlCount()
    {
        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsContentControl())
            {
                count = count + 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Returns a direct body content control by its position among content controls.
    /// </summary>
    /// <param name="index">The zero-based content-control position.</param>
    /// <returns>The requested content control.</returns>
    public DocxContentControl GetContentControl(int index)
    {
        return GetContentControls()[index];
    }

    /// <summary>
    /// Returns all body sections in document order, including final section properties.
    /// </summary>
    /// <returns>A new list containing the body sections.</returns>
    public List<DocxSection> GetSections()
    {
        return new List<DocxSection>(this.Sections);
    }

    /// <summary>
    /// Returns the number of body sections, including final section properties.
    /// </summary>
    /// <returns>The section count.</returns>
    public int SectionCount()
    {
        return this.Sections.Count;
    }

    /// <summary>
    /// Returns a section by position.
    /// </summary>
    /// <param name="index">The zero-based section position.</param>
    /// <returns>The requested section.</returns>
    public DocxSection GetSection(int index)
    {
        return this.Sections[index];
    }

    internal void ClearSectionOwnerProperties(int sectionIndex)
    {
        DocxSection section = this.Sections[sectionIndex];
        int currentSectionIndex = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            DocxParagraph? paragraph = block.GetParagraph();

            if (paragraph != null && paragraph.Section != null)
            {
                if (currentSectionIndex == sectionIndex)
                {
                    paragraph.Section = section;
                    paragraph.PropertiesXml = "";
                    return;
                }

                currentSectionIndex = currentSectionIndex + 1;
            }
        }
    }

    /// <summary>
    /// Returns whether the body has final section properties.
    /// </summary>
    /// <returns><c>true</c> when final section properties are present.</returns>
    public bool HasFinalSection()
    {
        return this.FinalSection != null;
    }

    /// <summary>
    /// Returns the final section properties, or null when they are absent.
    /// </summary>
    /// <returns>The final section properties, or null.</returns>
    public DocxSection? GetFinalSection()
    {
        return this.FinalSection;
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
        bool hasBlock = false;

        foreach (DocxBlock block in this.Blocks)
        {
            if (block.IsPreserved())
            {
                continue;
            }

            if (hasBlock)
            {
                text = text + options.ParagraphSeparator;
            }

            text = text + block.GetTextWithOptions(options);
            hasBlock = true;
        }

        return text;
    }

    internal List<DocxTextMatch> FindText(string searchText)
    {
        return DocxTextFinder.Find(this, searchText);
    }

    internal List<DocxTextMatch> FindTextWithOptions(string searchText, DocxTextOptions options)
    {
        return DocxTextFinder.Find(this, searchText, options);
    }

    internal int ReplacePlaceholders(Dictionary<string, string> values)
    {
        return DocxTemplate.ReplacePlaceholders(this, values);
    }

    internal int ReplacePlaceholdersWithDelimiters(Dictionary<string, string> values, string prefix, string suffix)
    {
        return DocxTemplate.ReplacePlaceholders(this, values, prefix, suffix);
    }

    internal int RemoveBlock(string key)
    {
        return DocxTemplate.RemoveBlock(this, key);
    }

    internal int RemoveBlockWithDelimiters(string key, string prefix, string suffix)
    {
        return DocxTemplate.RemoveBlock(this, key, prefix, suffix);
    }

    internal int InsertAtBookmark(string bookmarkName, DocxBody sourceBody)
    {
        return DocxMerger.InsertAtBookmark(this, bookmarkName, sourceBody);
    }

    internal int Append(DocxBody sourceBody)
    {
        return DocxMerger.Append(this, sourceBody);
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = 0;

        foreach (DocxBlock block in this.Blocks)
        {
            count = count + block.FillContentControls(values);
        }

        if (count > 0)
        {
            this.Xml = WriteXml();
            LoadBlocksFromXml();
            LoadSectionsFromXml();
        }

        return count;
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
        LoadBlocksFromXml();
        LoadSectionsFromXml();
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
        LoadBlocksFromXml();
        LoadSectionsFromXml();
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

        writer.StartElement("w", "body", DocxConstants.NamespaceWordprocessingMl);
        WriteBlocksTo(writer);
        WriteFinalSectionTo(writer);
        writer.EndElement();
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

    internal void WriteFinalSectionTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.FinalSection != null)
        {
            this.FinalSection.WriteTo(writer);
        }
    }
}
