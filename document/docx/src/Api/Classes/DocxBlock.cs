namespace docx;

/// <summary>
/// Represents one paragraph, table, content control, or preserved block-level element.
/// </summary>
public class DocxBlock : IDocxTextContent, IDocxBlockModel
{
    internal const string KindParagraph = "paragraph";
    internal const string KindTable = "table";
    internal const string KindContentControl = "contentControl";
    internal const string KindPreserved = "preserved";

    internal string Kind;
    internal string Xml;
    internal DocxParagraph? Paragraph;
    internal DocxTable? Table;
    internal DocxContentControl? ContentControl;

    /// <summary>
    /// Creates a block instance.
    /// </summary>
    /// <param name="kind">The kind used by the operation.</param>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an argument is empty, malformed, or not supported.</exception>
    public DocxBlock(string kind, string xml)
    {
        if (kind == null)
        {
            throw new ArgumentNullException("kind");
        }

        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (kind == "")
        {
            throw new ArgumentException("Block kind is required.", "kind");
        }

        this.Kind = kind;
        this.Xml = xml;
        this.Paragraph = null;
        this.Table = null;
        this.ContentControl = null;
    }

    internal static DocxBlock FromParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        DocxBlock block = new DocxBlock(KindParagraph, paragraph.GetXml());
        block.Paragraph = paragraph;
        return block;
    }

    internal static DocxBlock FromTable(DocxTable table)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        DocxBlock block = new DocxBlock(KindTable, table.GetXml());
        block.Table = table;
        return block;
    }

    internal static DocxBlock FromContentControl(DocxContentControl contentControl)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        DocxBlock block = new DocxBlock(KindContentControl, contentControl.GetXml());
        block.ContentControl = contentControl;
        return block;
    }

    /// <summary>
    /// Returns whether this block contains a paragraph.
    /// </summary>
    /// <returns><c>true</c> when the block contains a paragraph.</returns>
    public bool IsParagraph()
    {
        return this.Kind == KindParagraph;
    }

    /// <summary>
    /// Returns whether this block contains a table.
    /// </summary>
    /// <returns><c>true</c> when the block contains a table.</returns>
    public bool IsTable()
    {
        return this.Kind == KindTable;
    }

    /// <summary>
    /// Returns whether this block contains a content control.
    /// </summary>
    /// <returns><c>true</c> when the block contains a content control.</returns>
    public bool IsContentControl()
    {
        return this.Kind == KindContentControl;
    }

    /// <summary>
    /// Returns whether this block contains preserved block-level markup that is not otherwise modeled.
    /// </summary>
    /// <returns><c>true</c> when the block contains preserved markup.</returns>
    public bool IsPreserved()
    {
        return this.Kind == KindPreserved;
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    /// <summary>
    /// Returns the paragraph stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The paragraph, or null.</returns>
    public DocxParagraph? GetParagraph()
    {
        return this.Paragraph;
    }

    /// <summary>
    /// Returns the table stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The table, or null.</returns>
    public DocxTable? GetTable()
    {
        return this.Table;
    }

    /// <summary>
    /// Returns the content control stored in this block, or null when the block has another kind.
    /// </summary>
    /// <returns>The content control, or null.</returns>
    public DocxContentControl? GetContentControl()
    {
        return this.ContentControl;
    }

    /// <summary>
    /// Returns the preserved block-level XML, or an empty string for a typed block.
    /// </summary>
    /// <returns>The preserved XML, or an empty string.</returns>
    public string GetPreservedXml()
    {
        if (!IsPreserved())
        {
            return "";
        }

        return this.Xml;
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

        if (this.Paragraph != null)
        {
            return this.Paragraph.GetTextWithOptions(options);
        }

        if (this.Table != null)
        {
            return this.Table.GetTextWithOptions(options);
        }

        if (this.ContentControl != null)
        {
            return this.ContentControl.GetTextWithOptions(options);
        }

        return "";
    }

    internal int FillContentControls(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        if (this.Paragraph != null)
        {
            int count = this.Paragraph.FillContentControls(values);
            this.Xml = this.Paragraph.GetXml();
            return count;
        }

        if (this.Table != null)
        {
            int count = this.Table.FillContentControls(values);
            this.Xml = this.Table.WriteXml();
            return count;
        }

        if (this.ContentControl != null)
        {
            int count = this.ContentControl.FillContentControls(values);
            this.Xml = this.ContentControl.WriteXml();
            return count;
        }

        return 0;
    }

    internal int AcceptRevisions()
    {
        if (this.Paragraph != null)
        {
            int count = this.Paragraph.AcceptRevisions();
            this.Xml = this.Paragraph.GetXml();
            return count;
        }

        if (this.Table != null)
        {
            int count = this.Table.AcceptRevisions();
            this.Xml = this.Table.GetXml();
            return count;
        }

        if (this.ContentControl != null)
        {
            int count = DocxRevision.CountRevisionElementsXml(this.ContentControl.WriteXml());

            if (count == 0)
            {
                return 0;
            }

            this.ContentControl.Xml = DocxRevision.AcceptRevisionXml(this.ContentControl.WriteXml());
            this.ContentControl.LoadFromXml();
            this.Xml = this.ContentControl.GetXml();
            return count;
        }

        int rawCount = DocxRevision.CountRevisionElementsXml(this.Xml);

        if (rawCount == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.AcceptRevisionXml(this.Xml);
        return rawCount;
    }

    internal int RejectRevisions()
    {
        if (this.Paragraph != null)
        {
            int count = this.Paragraph.RejectRevisions();
            this.Xml = this.Paragraph.GetXml();
            return count;
        }

        if (this.Table != null)
        {
            int count = this.Table.RejectRevisions();
            this.Xml = this.Table.GetXml();
            return count;
        }

        if (this.ContentControl != null)
        {
            int count = DocxRevision.CountRevisionElementsXml(this.ContentControl.WriteXml());

            if (count == 0)
            {
                return 0;
            }

            this.ContentControl.Xml = DocxRevision.RejectRevisionXml(this.ContentControl.WriteXml());
            this.ContentControl.LoadFromXml();
            this.Xml = this.ContentControl.GetXml();
            return count;
        }

        int rawCount = DocxRevision.CountRevisionElementsXml(this.Xml);

        if (rawCount == 0)
        {
            return 0;
        }

        this.Xml = DocxRevision.RejectRevisionXml(this.Xml);
        return rawCount;
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (this.Paragraph != null)
        {
            this.Paragraph.WriteTo(writer);
            return;
        }

        if (this.Table != null)
        {
            this.Table.WriteTo(writer);
            return;
        }

        if (this.ContentControl != null)
        {
            this.ContentControl.WriteTo(writer);
            return;
        }

        if (this.Xml != "")
        {
            DocxXmlBuffer buffer = new DocxXmlBuffer(this.Xml);
            buffer.WriteTo(writer);
        }
    }
}
