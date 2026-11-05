namespace docx;

/// <summary>
/// Represents a WordprocessingML paragraph and its ordered inline content.
/// </summary>
public class DocxParagraph : IDocxTextContent, IDocxParagraphModel
{
    internal string Xml;
    internal string PropertiesXml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxRun> Runs;
    internal List<DocxHyperlink> Hyperlinks;
    internal List<DocxBookmark> Bookmarks;
    internal List<DocxField> Fields;
    internal List<DocxRevision> Revisions;
    internal List<DocxContentControl> ContentControls;
    internal List<DocxUnknownElement> UnknownElements;
    internal DocxParagraphFormat Format;
    internal DocxSection? Section;

    /// <summary>
    /// Creates a paragraph instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxParagraph(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.PropertiesXml = "";
        this.Children = new List<DocxXmlBuffer>();
        this.Runs = new List<DocxRun>();
        this.Hyperlinks = new List<DocxHyperlink>();
        this.Bookmarks = new List<DocxBookmark>();
        this.Fields = new List<DocxField>();
        this.Revisions = new List<DocxRevision>();
        this.ContentControls = new List<DocxContentControl>();
        this.UnknownElements = new List<DocxUnknownElement>();
        this.Format = new DocxParagraphFormat();
        this.Section = null;
        LoadFromXml();
    }

    internal static DocxParagraph Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("p", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:p.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxParagraph(buffer.GetXml());
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
        this.Runs = new List<DocxRun>();
        this.Hyperlinks = new List<DocxHyperlink>();
        this.Bookmarks = new List<DocxBookmark>();
        this.Fields = new List<DocxField>();
        this.Revisions = new List<DocxRevision>();
        this.ContentControls = new List<DocxContentControl>();
        this.UnknownElements = new List<DocxUnknownElement>();
        this.Format = new DocxParagraphFormat();
        this.Section = null;

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

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

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == paragraphDepth + 1)
            {
                bool isRun = reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl);
                bool isHyperlink = reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl);
                bool isBookmark = DocxBookmark.IsSupported(reader);
                bool isField = DocxField.IsSupported(reader);
                bool isRevision = DocxRevision.IsSupported(reader);
                bool isContentControl = DocxContentControl.IsSupported(reader);
                bool isUnknown = !isRun && !isHyperlink && !isBookmark && !isField && !isContentControl;

                if (reader.IsElement("pPr", DocxConstants.NamespaceWordprocessingMl))
                {
                    DocxXmlBuffer properties = DocxXmlBuffer.Capture(reader);
                    this.PropertiesXml = properties.GetXml();
                    this.Format = DocxParagraphFormat.ParseXml(this.PropertiesXml);
                    this.Section = DocxSectionFromPropertiesXml(this.PropertiesXml);
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddChild(child);

                if (isRun)
                {
                    DocxRun run = DocxRunFromBuffer(child);
                    AddRun(run);
                    run.ParentXml = child.GetXml();
                }

                if (isHyperlink)
                {
                    this.Hyperlinks.Add(DocxHyperlinkFromBuffer(child));
                }

                if (isBookmark)
                {
                    this.Bookmarks.Add(DocxBookmarkFromBuffer(child));
                }

                if (isField)
                {
                    this.Fields.Add(DocxFieldFromBuffer(child));
                }

                if (isRevision)
                {
                    this.Revisions.Add(DocxRevisionFromBuffer(child));
                }

                if (isContentControl)
                {
                    this.ContentControls.Add(DocxContentControlFromBuffer(child));
                }

                if (isUnknown)
                {
                    this.UnknownElements.Add(DocxUnknownElementFromBuffer(child));
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
    }

    internal void AddRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        run.ParentParagraph = this;
        run.ParentRunIndex = this.Runs.Count;
        run.ParentXml = run.WriteXml();
        this.Runs.Add(run);
    }

    internal void AddHyperlink(DocxHyperlink hyperlink)
    {
        if (hyperlink == null)
        {
            throw new ArgumentNullException("hyperlink");
        }

        this.Hyperlinks.Add(hyperlink);
        this.Children.Add(new DocxXmlBuffer(hyperlink.WriteXml()));
    }

    internal void AddBookmark(DocxBookmark bookmark)
    {
        if (bookmark == null)
        {
            throw new ArgumentNullException("bookmark");
        }

        this.Bookmarks.Add(bookmark);
        this.Children.Add(new DocxXmlBuffer(bookmark.WriteXml()));
    }

    internal void AddField(DocxField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        this.Fields.Add(field);
        this.Children.Add(new DocxXmlBuffer(field.WriteXml()));
    }

    internal void AddRevision(DocxRevision revision)
    {
        if (revision == null)
        {
            throw new ArgumentNullException("revision");
        }

        this.Revisions.Add(revision);
        this.UnknownElements.Add(DocxUnknownElement.ParseXml(revision.WriteXml()));
        this.Children.Add(new DocxXmlBuffer(revision.WriteXml()));
    }

    internal void AddUnknownElement(DocxUnknownElement element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        this.UnknownElements.Add(element);
        this.Children.Add(new DocxXmlBuffer(element.WriteXml()));
    }

    internal DocxDrawing AddInlineImage(DocxImage image, long widthEmu, long heightEmu)
    {
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        DocxDrawing drawing = DocxDrawing.CreateInlineImage(image, widthEmu, heightEmu);
        string xml = drawing.WriteXml();
        this.Children.Add(new DocxXmlBuffer(xml));
        DocxRun run = new DocxRun(xml);
        AddRun(run);
        run.ParentXml = xml;
        return drawing;
    }

    internal DocxRun AddFootnoteReference(DocxFootnote footnote)
    {
        if (footnote == null)
        {
            throw new ArgumentNullException("footnote");
        }

        return AddFootnoteReferenceById(footnote.Id);
    }

    internal DocxRun AddFootnoteReferenceById(string id)
    {
        DocxRun run = DocxFootnote.CreateReferenceRun(id);
        string xml = run.WriteXml();
        this.Children.Add(new DocxXmlBuffer(xml));
        AddRun(run);
        run.ParentXml = xml;
        return run;
    }

    internal DocxRun AddEndnoteReference(DocxEndnote endnote)
    {
        if (endnote == null)
        {
            throw new ArgumentNullException("endnote");
        }

        return AddEndnoteReferenceById(endnote.Id);
    }

    internal DocxRun AddEndnoteReferenceById(string id)
    {
        DocxRun run = DocxEndnote.CreateReferenceRun(id);
        string xml = run.WriteXml();
        this.Children.Add(new DocxXmlBuffer(xml));
        AddRun(run);
        run.ParentXml = xml;
        return run;
    }

    internal DocxRun AddCommentRange(DocxComment comment)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        return AddCommentRangeById(comment.Id);
    }

    internal DocxRun AddCommentRangeById(string id)
    {
        AddCommentRangeStart(id);
        AddCommentRangeEnd(id);
        return AddCommentReferenceById(id);
    }

    internal DocxUnknownElement AddCommentRangeStart(string id)
    {
        string xml = DocxComment.CreateRangeStartXml(id);
        DocxUnknownElement element = DocxUnknownElement.ParseXml(xml);
        this.Children.Insert(0, new DocxXmlBuffer(xml));
        this.UnknownElements.Add(element);
        return element;
    }

    internal DocxUnknownElement AddCommentRangeEnd(string id)
    {
        string xml = DocxComment.CreateRangeEndXml(id);
        DocxUnknownElement element = DocxUnknownElement.ParseXml(xml);
        this.Children.Add(new DocxXmlBuffer(xml));
        this.UnknownElements.Add(element);
        return element;
    }

    internal DocxRun AddCommentReference(DocxComment comment)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        return AddCommentReferenceById(comment.Id);
    }

    internal DocxRun AddCommentReferenceById(string id)
    {
        DocxRun run = DocxComment.CreateReferenceRun(id);
        string xml = run.WriteXml();
        this.Children.Add(new DocxXmlBuffer(xml));
        AddRun(run);
        run.ParentXml = xml;
        return run;
    }

    internal void SynchronizeRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        int currentRunIndex = 0;
        int matchingChildIndex = -1;
        int matchingRunIndex = -1;

        for (int childIndex = 0; childIndex < this.Children.Count; childIndex = childIndex + 1)
        {
            string childXml = this.Children[childIndex].GetXml();
            DocxXmlReader reader = DocxXmlReader.Create(childXml);

            if (!reader.MoveToContent() || !reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                continue;
            }

            if (childXml == run.ParentXml && matchingChildIndex == -1)
            {
                matchingChildIndex = childIndex;
                matchingRunIndex = currentRunIndex;
            }

            if (childXml == run.ParentXml && currentRunIndex == run.ParentRunIndex)
            {
                matchingChildIndex = childIndex;
                matchingRunIndex = currentRunIndex;
                break;
            }

            currentRunIndex = currentRunIndex + 1;
        }

        if (matchingChildIndex == -1)
        {
            return;
        }

        string runXml = run.WriteXml();
        this.Children[matchingChildIndex] = new DocxXmlBuffer(runXml);
        this.Runs[matchingRunIndex] = run;
        run.ParentParagraph = this;
        run.ParentRunIndex = matchingRunIndex;
        run.ParentXml = runXml;
        this.Xml = WriteXml();
    }

    internal static DocxRun DocxRunFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxRun.Parse(reader);
    }

    internal static DocxHyperlink DocxHyperlinkFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxHyperlink.Parse(reader);
    }

    internal static DocxBookmark DocxBookmarkFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxBookmark.ParseXml(buffer.GetXml());
    }

    internal static DocxField DocxFieldFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxField.Parse(reader);
    }

    internal static DocxUnknownElement DocxUnknownElementFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxUnknownElement.ParseXml(buffer.GetXml());
    }

    internal static DocxRevision DocxRevisionFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        return DocxRevision.ParseXml(buffer.GetXml());
    }

    internal static DocxContentControl DocxContentControlFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxContentControl.Parse(reader);
    }

    internal static DocxSection? DocxSectionFromPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml == "")
        {
            return null;
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.ReadToElement("pPr", DocxConstants.NamespaceWordprocessingMl))
        {
            return null;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return null;
        }

        int propertiesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == propertiesDepth)
            {
                return null;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == propertiesDepth + 1 && reader.IsElement("sectPr", DocxConstants.NamespaceWordprocessingMl))
            {
                return DocxSection.Parse(reader);
            }

            hasNode = reader.Read();
        }

        return null;
    }

    internal void SetPropertiesXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.PropertiesXml = xml;
        this.Format = DocxParagraphFormat.ParseXml(xml);
        this.Section = DocxSectionFromPropertiesXml(xml);
    }

    /// <summary>
    /// Replaces the direct paragraph format model.
    /// </summary>
    /// <param name="format">The replacement paragraph format.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>format</c> is null.</exception>
    public void SetFormat(DocxParagraphFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.Format = format;
        this.PropertiesXml = "";
    }

    internal void SetSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        this.Section = section;
        this.PropertiesXml = "";
    }

    internal void ClearSection()
    {
        this.Section = null;
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
    /// Returns whether direct paragraph formatting is present.
    /// </summary>
    /// <returns>True when direct formatting is present; otherwise false.</returns>
    public bool HasFormat()
    {
        return this.Format.HasAnyFormatting();
    }

    /// <summary>
    /// Returns the direct paragraph format model.
    /// </summary>
    /// <returns>The paragraph format model.</returns>
    public DocxParagraphFormat GetFormat()
    {
        return this.Format;
    }

    internal bool HasSection()
    {
        return this.Section != null;
    }

    internal void ApplyNumbering(string numberingId, string levelIndex)
    {
        if (numberingId == null)
        {
            throw new ArgumentNullException("numberingId");
        }

        if (levelIndex == null)
        {
            throw new ArgumentNullException("levelIndex");
        }

        if (numberingId == "")
        {
            throw new ArgumentException("Numbering id is required.", "numberingId");
        }

        if (levelIndex == "")
        {
            throw new ArgumentException("Level index is required.", "levelIndex");
        }

        this.Format.SetNumbering(levelIndex, numberingId);
        this.PropertiesXml = "";
    }

    internal void ApplyNumberingInstance(DocxNumberingInstance instance, string levelIndex)
    {
        if (instance == null)
        {
            throw new ArgumentNullException("instance");
        }

        ApplyNumbering(instance.NumberingId, levelIndex);
    }

    internal void ClearNumbering()
    {
        this.Format.ClearNumbering();
        this.PropertiesXml = "";
    }

    internal bool HasNumbering()
    {
        return this.Format.HasNumbering();
    }

    /// <summary>
    /// Returns all direct paragraph children in document order.
    /// </summary>
    /// <returns>A new list containing the paragraph children.</returns>
    public List<DocxParagraphChild> GetChildren()
    {
        List<DocxParagraphChild> children = new List<DocxParagraphChild>();

        foreach (DocxXmlBuffer child in this.Children)
        {
            children.Add(new DocxParagraphChild(child.GetXml()));
        }

        return children;
    }

    /// <summary>
    /// Returns the number of direct paragraph children.
    /// </summary>
    /// <returns>The child count.</returns>
    public int ChildCount()
    {
        return this.Children.Count;
    }

    /// <summary>
    /// Returns a direct paragraph child by position.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The requested child.</returns>
    public DocxParagraphChild GetChild(int index)
    {
        return new DocxParagraphChild(this.Children[index].GetXml());
    }

    /// <summary>
    /// Appends a direct paragraph child.
    /// </summary>
    /// <param name="child">The child to append.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public void AddParagraphChild(DocxParagraphChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxParagraphChild> children = GetChildren();
        children.Add(child);
        ApplyParagraphChildren(children);
    }

    /// <summary>
    /// Inserts a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based insertion position.</param>
    /// <param name="child">The child to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public void InsertParagraphChild(int index, DocxParagraphChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxParagraphChild> children = GetChildren();
        children.Insert(index, child);
        ApplyParagraphChildren(children);
    }

    /// <summary>
    /// Inserts a run at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="run">The run to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>run</c> is null.</exception>
    public void InsertRun(int index, DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        InsertParagraphChild(index, DocxParagraphChild.FromRun(run));
    }

    /// <summary>
    /// Inserts a hyperlink at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="hyperlink">The hyperlink to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>hyperlink</c> is null.</exception>
    public void InsertHyperlink(int index, DocxHyperlink hyperlink)
    {
        if (hyperlink == null)
        {
            throw new ArgumentNullException("hyperlink");
        }

        InsertParagraphChild(index, DocxParagraphChild.FromHyperlink(hyperlink));
    }

    /// <summary>
    /// Inserts a bookmark marker at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="bookmark">The bookmark marker to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>bookmark</c> is null.</exception>
    public void InsertBookmark(int index, DocxBookmark bookmark)
    {
        if (bookmark == null)
        {
            throw new ArgumentNullException("bookmark");
        }

        InsertParagraphChild(index, DocxParagraphChild.FromBookmark(bookmark));
    }

    /// <summary>
    /// Inserts a simple field at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="field">The field to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    public void InsertField(int index, DocxField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        InsertParagraphChild(index, DocxParagraphChild.FromField(field));
    }

    /// <summary>
    /// Inserts a content control at a direct child position.
    /// </summary>
    /// <param name="index">The zero-based child insertion position.</param>
    /// <param name="contentControl">The content control to insert.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    public void InsertContentControl(int index, DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        InsertParagraphChild(index, DocxParagraphChild.FromContentControl(contentControl));
    }

    /// <summary>
    /// Replaces a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="child">The replacement child.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>child</c> is null.</exception>
    public DocxParagraphChild ReplaceParagraphChild(int index, DocxParagraphChild child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        List<DocxParagraphChild> children = GetChildren();
        DocxParagraphChild replaced = children[index];
        children[index] = child;
        ApplyParagraphChildren(children);
        return replaced;
    }

    /// <summary>
    /// Replaces a direct paragraph child with a run.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="run">The replacement run.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>run</c> is null.</exception>
    public DocxParagraphChild ReplaceChildWithRun(int index, DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        return ReplaceParagraphChild(index, DocxParagraphChild.FromRun(run));
    }

    /// <summary>
    /// Replaces a direct paragraph child with a hyperlink.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="hyperlink">The replacement hyperlink.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>hyperlink</c> is null.</exception>
    public DocxParagraphChild ReplaceChildWithHyperlink(int index, DocxHyperlink hyperlink)
    {
        if (hyperlink == null)
        {
            throw new ArgumentNullException("hyperlink");
        }

        return ReplaceParagraphChild(index, DocxParagraphChild.FromHyperlink(hyperlink));
    }

    /// <summary>
    /// Replaces a direct paragraph child with a bookmark marker.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="bookmark">The replacement bookmark marker.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>bookmark</c> is null.</exception>
    public DocxParagraphChild ReplaceChildWithBookmark(int index, DocxBookmark bookmark)
    {
        if (bookmark == null)
        {
            throw new ArgumentNullException("bookmark");
        }

        return ReplaceParagraphChild(index, DocxParagraphChild.FromBookmark(bookmark));
    }

    /// <summary>
    /// Replaces a direct paragraph child with a simple field.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="field">The replacement field.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>field</c> is null.</exception>
    public DocxParagraphChild ReplaceChildWithField(int index, DocxField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        return ReplaceParagraphChild(index, DocxParagraphChild.FromField(field));
    }

    /// <summary>
    /// Replaces a direct paragraph child with a content control.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <param name="contentControl">The replacement content control.</param>
    /// <returns>The replaced child.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>contentControl</c> is null.</exception>
    public DocxParagraphChild ReplaceChildWithContentControl(int index, DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        return ReplaceParagraphChild(index, DocxParagraphChild.FromContentControl(contentControl));
    }

    /// <summary>
    /// Moves a direct paragraph child to another position.
    /// </summary>
    /// <param name="sourceIndex">The current zero-based child position.</param>
    /// <param name="destinationIndex">The resulting zero-based child position.</param>
    public void MoveParagraphChild(int sourceIndex, int destinationIndex)
    {
        List<DocxParagraphChild> children = GetChildren();
        DocxParagraphChild child = children[sourceIndex];

        if (destinationIndex < 0 || destinationIndex >= children.Count)
        {
            throw new ArgumentOutOfRangeException("destinationIndex");
        }

        if (sourceIndex == destinationIndex)
        {
            return;
        }

        children.RemoveAt(sourceIndex);
        children.Insert(destinationIndex, child);
        ApplyParagraphChildren(children);
    }

    /// <summary>
    /// Removes a direct paragraph child.
    /// </summary>
    /// <param name="index">The zero-based child position.</param>
    /// <returns>The removed child.</returns>
    public DocxParagraphChild RemoveParagraphChildAt(int index)
    {
        List<DocxParagraphChild> children = GetChildren();
        DocxParagraphChild removed = children[index];
        children.RemoveAt(index);
        ApplyParagraphChildren(children);
        return removed;
    }

    internal void ApplyParagraphChildren(List<DocxParagraphChild> children)
    {
        if (children == null)
        {
            throw new ArgumentNullException("children");
        }

        this.Children = new List<DocxXmlBuffer>();

        foreach (DocxParagraphChild child in children)
        {
            this.Children.Add(new DocxXmlBuffer(child.WriteXml()));
        }

        this.Xml = WriteXml();
        LoadFromXml();
    }

    /// <summary>
    /// Returns direct proofing-error markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the proofing markers.</returns>
    public List<DocxParagraphChild> GetProofingMarkers()
    {
        return GetChildrenByKind(DocxParagraphChildKind.ProofingMarker);
    }

    /// <summary>
    /// Returns the number of direct proofing-error markers.
    /// </summary>
    /// <returns>The proofing-marker count.</returns>
    public int ProofingMarkerCount()
    {
        return GetProofingMarkers().Count;
    }

    /// <summary>
    /// Returns a direct proofing-error marker by position.
    /// </summary>
    /// <param name="index">The zero-based proofing-marker position.</param>
    /// <returns>The requested proofing marker.</returns>
    public DocxParagraphChild GetProofingMarker(int index)
    {
        return GetProofingMarkers()[index];
    }

    /// <summary>
    /// Returns direct permission-range markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the permission-range markers.</returns>
    public List<DocxParagraphChild> GetPermissionRanges()
    {
        return GetChildrenByKind(DocxParagraphChildKind.PermissionRange);
    }

    /// <summary>
    /// Returns the number of direct permission-range markers.
    /// </summary>
    /// <returns>The permission-range marker count.</returns>
    public int PermissionRangeCount()
    {
        return GetPermissionRanges().Count;
    }

    /// <summary>
    /// Returns a direct permission-range marker by position.
    /// </summary>
    /// <param name="index">The zero-based permission-range marker position.</param>
    /// <returns>The requested permission-range marker.</returns>
    public DocxParagraphChild GetPermissionRange(int index)
    {
        return GetPermissionRanges()[index];
    }

    /// <summary>
    /// Returns direct smart-tag children in paragraph order.
    /// </summary>
    /// <returns>A new list containing the smart-tag children.</returns>
    public List<DocxParagraphChild> GetSmartTags()
    {
        return GetChildrenByKind(DocxParagraphChildKind.SmartTag);
    }

    /// <summary>
    /// Returns the number of direct smart-tag children.
    /// </summary>
    /// <returns>The smart-tag count.</returns>
    public int SmartTagCount()
    {
        return GetSmartTags().Count;
    }

    /// <summary>
    /// Returns a direct smart-tag child by position.
    /// </summary>
    /// <param name="index">The zero-based smart-tag position.</param>
    /// <returns>The requested smart-tag child.</returns>
    public DocxParagraphChild GetSmartTag(int index)
    {
        return GetSmartTags()[index];
    }

    /// <summary>
    /// Returns direct custom XML markup children in paragraph order.
    /// </summary>
    /// <returns>A new list containing the custom XML children.</returns>
    public List<DocxParagraphChild> GetCustomXmlMarkup()
    {
        return GetChildrenByKind(DocxParagraphChildKind.CustomXml);
    }

    /// <summary>
    /// Returns the number of direct custom XML markup children.
    /// </summary>
    /// <returns>The custom XML markup count.</returns>
    public int CustomXmlMarkupCount()
    {
        return GetCustomXmlMarkup().Count;
    }

    /// <summary>
    /// Returns a direct custom XML markup child by position.
    /// </summary>
    /// <param name="index">The zero-based custom XML markup position.</param>
    /// <returns>The requested custom XML markup child.</returns>
    public DocxParagraphChild GetCustomXmlMarkupChild(int index)
    {
        return GetCustomXmlMarkup()[index];
    }

    /// <summary>
    /// Returns preserved direct children not represented by another child kind.
    /// </summary>
    /// <returns>A new list containing the preserved unknown children.</returns>
    public List<DocxParagraphChild> GetPreservedUnknownChildren()
    {
        return GetChildrenByKind(DocxParagraphChildKind.PreservedUnknown);
    }

    /// <summary>
    /// Returns the number of preserved direct children not represented by another kind.
    /// </summary>
    /// <returns>The preserved unknown child count.</returns>
    public int PreservedUnknownChildCount()
    {
        return GetPreservedUnknownChildren().Count;
    }

    /// <summary>
    /// Returns a preserved unknown child by position.
    /// </summary>
    /// <param name="index">The zero-based preserved unknown child position.</param>
    /// <returns>The requested preserved unknown child.</returns>
    public DocxParagraphChild GetPreservedUnknownChild(int index)
    {
        return GetPreservedUnknownChildren()[index];
    }

    internal List<DocxParagraphChild> GetChildrenByKind(DocxParagraphChildKind kind)
    {
        List<DocxParagraphChild> matching = new List<DocxParagraphChild>();

        foreach (DocxParagraphChild child in GetChildren())
        {
            if (child.Kind == kind)
            {
                matching.Add(child);
            }
        }

        return matching;
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

    internal int RunElementCount()
    {
        return CountChildElements("r", DocxConstants.NamespaceWordprocessingMl);
    }

    /// <summary>
    /// Returns all direct runs in paragraph order.
    /// </summary>
    /// <returns>A new list containing the runs.</returns>
    public List<DocxRun> GetRuns()
    {
        return new List<DocxRun>(this.Runs);
    }

    /// <summary>
    /// Returns the number of direct runs.
    /// </summary>
    /// <returns>The run count.</returns>
    public int RunCount()
    {
        return this.Runs.Count;
    }

    /// <summary>
    /// Returns a direct run by its position among runs.
    /// </summary>
    /// <param name="index">The zero-based run position.</param>
    /// <returns>The requested run.</returns>
    public DocxRun GetRun(int index)
    {
        return this.Runs[index];
    }

    internal int HyperlinkElementCount()
    {
        return CountChildElements("hyperlink", DocxConstants.NamespaceWordprocessingMl);
    }

    /// <summary>
    /// Returns all direct hyperlinks in paragraph order.
    /// </summary>
    /// <returns>A new list containing the hyperlinks.</returns>
    public List<DocxHyperlink> GetHyperlinks()
    {
        return new List<DocxHyperlink>(this.Hyperlinks);
    }

    /// <summary>
    /// Returns the number of direct hyperlinks.
    /// </summary>
    /// <returns>The hyperlink count.</returns>
    public int HyperlinkCount()
    {
        return this.Hyperlinks.Count;
    }

    /// <summary>
    /// Returns a direct hyperlink by its position among hyperlinks.
    /// </summary>
    /// <param name="index">The zero-based hyperlink position.</param>
    /// <returns>The requested hyperlink.</returns>
    public DocxHyperlink GetHyperlink(int index)
    {
        return this.Hyperlinks[index];
    }

    internal int ContentControlElementCount()
    {
        return CountChildElements("sdt", DocxConstants.NamespaceWordprocessingMl);
    }

    /// <summary>
    /// Returns all direct content controls in paragraph order.
    /// </summary>
    /// <returns>A new list containing the content controls.</returns>
    public List<DocxContentControl> GetContentControls()
    {
        return new List<DocxContentControl>(this.ContentControls);
    }

    /// <summary>
    /// Returns the number of direct content controls.
    /// </summary>
    /// <returns>The content-control count.</returns>
    public int ContentControlCount()
    {
        return this.ContentControls.Count;
    }

    /// <summary>
    /// Returns a direct content control by its position among content controls.
    /// </summary>
    /// <param name="index">The zero-based content-control position.</param>
    /// <returns>The requested content control.</returns>
    public DocxContentControl GetContentControl(int index)
    {
        return this.ContentControls[index];
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        int count = 0;
        List<DocxXmlBuffer> filledChildren = new List<DocxXmlBuffer>();

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && DocxContentControl.IsSupported(reader))
            {
                DocxContentControl contentControl = DocxContentControlFromBuffer(child);
                int filledCount = contentControl.FillContentControls(values);
                count = count + filledCount;

                if (filledCount > 0)
                {
                    filledChildren.Add(new DocxXmlBuffer(contentControl.WriteXml()));
                }
                else
                {
                    filledChildren.Add(child);
                }
            }
            else
            {
                filledChildren.Add(child);
            }
        }

        if (count > 0)
        {
            this.Children = filledChildren;
            this.Xml = WriteXml();
            LoadFromXml();
        }

        return count;
    }

    internal int ResolveHyperlinks(DocxRelationshipSet relationships)
    {
        if (relationships == null)
        {
            throw new ArgumentNullException("relationships");
        }

        int count = 0;

        foreach (DocxHyperlink hyperlink in this.Hyperlinks)
        {
            if (hyperlink.ResolveRelationship(relationships))
            {
                count = count + 1;
            }
        }

        return count;
    }

    /// <summary>
    /// Returns all direct bookmark markers in paragraph order.
    /// </summary>
    /// <returns>A new list containing the bookmark markers.</returns>
    public List<DocxBookmark> GetBookmarks()
    {
        return new List<DocxBookmark>(this.Bookmarks);
    }

    /// <summary>
    /// Returns the number of direct bookmark markers.
    /// </summary>
    /// <returns>The bookmark count.</returns>
    public int BookmarkCount()
    {
        return this.Bookmarks.Count;
    }

    /// <summary>
    /// Returns a bookmark marker by position.
    /// </summary>
    /// <param name="index">The zero-based bookmark position.</param>
    /// <returns>The requested bookmark marker.</returns>
    public DocxBookmark GetBookmark(int index)
    {
        return this.Bookmarks[index];
    }

    internal int CountBookmarks(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        int count = 0;

        foreach (DocxBookmark bookmark in this.Bookmarks)
        {
            if (bookmark.Kind == kind)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int BookmarkStartCount()
    {
        return CountBookmarks(DocxBookmark.KindStart);
    }

    internal int BookmarkEndCount()
    {
        return CountBookmarks(DocxBookmark.KindEnd);
    }

    internal DocxBookmark? FindBookmarkStartByName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (DocxBookmark bookmark in this.Bookmarks)
        {
            if (bookmark.IsStart() && bookmark.Name == name)
            {
                return bookmark;
            }
        }

        return null;
    }

    internal DocxBookmark? FindBookmarkEndById(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxBookmark bookmark in this.Bookmarks)
        {
            if (bookmark.IsEnd() && bookmark.Id == id)
            {
                return bookmark;
            }
        }

        return null;
    }

    internal int FieldElementCount()
    {
        return CountChildElements("fldSimple", DocxConstants.NamespaceWordprocessingMl);
    }

    /// <summary>
    /// Returns all direct simple fields in paragraph order.
    /// </summary>
    /// <returns>A new list containing the fields.</returns>
    public List<DocxField> GetFields()
    {
        return new List<DocxField>(this.Fields);
    }

    /// <summary>
    /// Returns the number of direct simple fields.
    /// </summary>
    /// <returns>The field count.</returns>
    public int FieldCount()
    {
        return this.Fields.Count;
    }

    /// <summary>
    /// Returns a direct simple field by its position among fields.
    /// </summary>
    /// <param name="index">The zero-based field position.</param>
    /// <returns>The requested field.</returns>
    public DocxField GetField(int index)
    {
        return this.Fields[index];
    }

    internal int RevisionCount()
    {
        return this.Revisions.Count;
    }

    internal DocxRevision GetRevision(int index)
    {
        return this.Revisions[index];
    }

    internal int CountRevisions(string kind)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (kind == "")
        {
            throw new ArgumentException("Revision kind is required.", "kind");
        }

        int count = 0;

        foreach (DocxRevision revision in this.Revisions)
        {
            if (revision.IsKind(kind))
            {
                count = count + 1;
            }
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

        foreach (DocxXmlBuffer child in this.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxRunFromBuffer(child).GetTextWithOptions(options);
            }
            else if (reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + DocxHyperlinkFromBuffer(child).GetTextWithOptions(options);
            }
            else if (DocxField.IsSupported(reader) && options.IncludeFieldResults)
            {
                text = text + DocxFieldFromBuffer(child).GetTextWithOptions(options);
            }
            else if (DocxRevision.IsSupported(reader))
            {
                text = text + DocxRevisionFromBuffer(child).GetTextWithOptions(options);
            }
            else if (DocxContentControl.IsSupported(reader))
            {
                text = text + DocxContentControlFromBuffer(child).GetTextWithOptions(options);
            }
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

    internal int ReplaceText(string searchText, string replacementText)
    {
        return DocxTextReplacer.Replace(this, searchText, replacementText);
    }

    internal int ReplacePlaceholders(Dictionary<string, string> values)
    {
        return DocxTemplate.ReplacePlaceholders(this, values);
    }

    internal int ReplacePlaceholdersWithDelimiters(Dictionary<string, string> values, string prefix, string suffix)
    {
        return DocxTemplate.ReplacePlaceholders(this, values, prefix, suffix);
    }

    internal int UnknownElementCount()
    {
        return this.UnknownElements.Count;
    }

    internal DocxUnknownElement GetUnknownElement(int index)
    {
        return this.UnknownElements[index];
    }

    internal int CountUnknownElements(string localName, string namespaceUri)
    {
        DocxXmlReader.ValidateName(localName, namespaceUri);

        int count = 0;

        foreach (DocxUnknownElement element in this.UnknownElements)
        {
            if (element.IsElement(localName, namespaceUri))
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal string AllocateBookmarkId()
    {
        int candidate = 0;

        while (ContainsBookmarkId(candidate.ToString(System.Globalization.CultureInfo.InvariantCulture)))
        {
            candidate = candidate + 1;
        }

        return candidate.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal bool ContainsBookmarkId(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxBookmark bookmark in this.Bookmarks)
        {
            if (bookmark.Id == id)
            {
                return true;
            }
        }

        return false;
    }

    internal DocxBookmark AddBookmarkStart(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        string id = AllocateBookmarkId();
        DocxBookmark bookmark = DocxBookmark.CreateStart(id, name);
        AddBookmark(bookmark);
        return bookmark;
    }

    internal DocxBookmark AddBookmarkEnd(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        DocxBookmark bookmark = DocxBookmark.CreateEnd(id);
        AddBookmark(bookmark);
        return bookmark;
    }

    internal string AddBookmarkPair(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        string id = AllocateBookmarkId();
        AddBookmark(DocxBookmark.CreateStart(id, name));
        AddBookmark(DocxBookmark.CreateEnd(id));
        return id;
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

        writer.StartElement("w", "p", DocxConstants.NamespaceWordprocessingMl);
        WritePropertiesTo(writer);
        WriteChildrenTo(writer);
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

        if (this.Format.HasAnyFormatting() || this.Section != null)
        {
            writer.StartElement("w", "pPr", DocxConstants.NamespaceWordprocessingMl);
            this.Format.WriteChildrenTo(writer);
            WriteSectionTo(writer);
            writer.EndElement();
        }
    }

    internal void WriteSectionTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Section != null)
        {
            this.Section.WriteTo(writer);
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
}
