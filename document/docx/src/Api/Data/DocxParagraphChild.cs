namespace docx;

/// <summary>
/// Holds one direct paragraph child together with any parsed typed model.
/// </summary>
public class DocxParagraphChild
{
    internal bool UseTypedModel;
    /// <summary>
    /// The child kind.
    /// </summary>
    public DocxParagraphChildKind Kind;
    /// <summary>
    /// The child XML used for kinds without a typed model.
    /// </summary>
    public string Xml;
    /// <summary>
    /// The run model when this is a run child, or null.
    /// </summary>
    public DocxRun? Run;
    /// <summary>
    /// The hyperlink model when this is a hyperlink child, or null.
    /// </summary>
    public DocxHyperlink? Hyperlink;
    /// <summary>
    /// The bookmark model when this is a bookmark child, or null.
    /// </summary>
    public DocxBookmark? Bookmark;
    /// <summary>
    /// The field model when this is a field child, or null.
    /// </summary>
    public DocxField? Field;
    /// <summary>
    /// The content-control model when this is a content-control child, or null.
    /// </summary>
    public DocxContentControl? ContentControl;

    /// <summary>
    /// Creates a paragraph child from one XML element.
    /// </summary>
    /// <param name="xml">The child XML.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>xml</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>xml</c> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <c>xml</c> does not contain an element.</exception>
    public DocxParagraphChild(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml.Trim() == "")
        {
            throw new ArgumentException("Paragraph child XML is required.", "xml");
        }

        this.Kind = DocxParagraphChildKind.PreservedUnknown;
        this.Xml = xml;
        this.UseTypedModel = false;
        this.Run = null;
        this.Hyperlink = null;
        this.Bookmark = null;
        this.Field = null;
        this.ContentControl = null;
        LoadTypedModel();
    }

    internal void LoadTypedModel()
    {
        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("Paragraph child XML does not contain an element.");
        }

        if (reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.Run;
            this.Run = new DocxRun(this.Xml);
            return;
        }

        if (reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.Hyperlink;
            this.Hyperlink = new DocxHyperlink(this.Xml);
            return;
        }

        if (DocxBookmark.IsSupported(reader))
        {
            this.Kind = DocxParagraphChildKind.Bookmark;
            this.Bookmark = DocxBookmark.ParseXml(this.Xml);
            return;
        }

        if (DocxField.IsSupported(reader))
        {
            this.Kind = DocxParagraphChildKind.Field;
            this.Field = new DocxField(this.Xml);
            return;
        }

        if (DocxContentControl.IsSupported(reader))
        {
            this.Kind = DocxParagraphChildKind.ContentControl;
            this.ContentControl = new DocxContentControl(this.Xml);
            return;
        }

        if (reader.IsElement("proofErr", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.ProofingMarker;
            return;
        }

        if (reader.IsElement("permStart", DocxConstants.NamespaceWordprocessingMl) || reader.IsElement("permEnd", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.PermissionRange;
            return;
        }

        if (reader.IsElement("smartTag", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.SmartTag;
            return;
        }

        if (reader.IsElement("customXml", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlInsRangeStart", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlInsRangeEnd", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlDelRangeStart", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlDelRangeEnd", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlMoveFromRangeStart", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlMoveFromRangeEnd", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlMoveToRangeStart", DocxConstants.NamespaceWordprocessingMl)
            || reader.IsElement("customXmlMoveToRangeEnd", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxParagraphChildKind.CustomXml;
            return;
        }

        if (DocxRevision.IsSupported(reader))
        {
            this.Kind = DocxParagraphChildKind.Revision;
        }
    }

    internal static DocxParagraphChild FromRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        DocxParagraphChild child = new DocxParagraphChild(run.WriteXml());
        child.Run = run;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxParagraphChild FromHyperlink(DocxHyperlink hyperlink)
    {
        if (hyperlink == null)
        {
            throw new ArgumentNullException("hyperlink");
        }

        DocxParagraphChild child = new DocxParagraphChild(hyperlink.WriteXml());
        child.Hyperlink = hyperlink;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxParagraphChild FromBookmark(DocxBookmark bookmark)
    {
        if (bookmark == null)
        {
            throw new ArgumentNullException("bookmark");
        }

        DocxParagraphChild child = new DocxParagraphChild(bookmark.WriteXml());
        child.Bookmark = bookmark;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxParagraphChild FromField(DocxField field)
    {
        if (field == null)
        {
            throw new ArgumentNullException("field");
        }

        DocxParagraphChild child = new DocxParagraphChild(field.WriteXml());
        child.Field = field;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxParagraphChild FromContentControl(DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        DocxParagraphChild child = new DocxParagraphChild(contentControl.WriteXml());
        child.ContentControl = contentControl;
        child.UseTypedModel = true;
        return child;
    }

    internal string WriteXml()
    {
        if (!this.UseTypedModel)
        {
            return this.Xml;
        }

        if (this.Run != null)
        {
            return this.Run.WriteXml();
        }

        if (this.Hyperlink != null)
        {
            return this.Hyperlink.WriteXml();
        }

        if (this.Bookmark != null)
        {
            return this.Bookmark.WriteXml();
        }

        if (this.Field != null)
        {
            return this.Field.WriteXml();
        }

        if (this.ContentControl != null)
        {
            return this.ContentControl.WriteXml();
        }

        return this.Xml;
    }
}
