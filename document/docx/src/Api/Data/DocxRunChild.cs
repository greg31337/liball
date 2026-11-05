namespace docx;

/// <summary>
/// Holds one direct run child together with any parsed typed model.
/// </summary>
public class DocxRunChild
{
    internal bool UseTypedModel;
    /// <summary>
    /// The child kind.
    /// </summary>
    public DocxRunChildKind Kind;
    /// <summary>
    /// The child XML used for kinds without a typed model.
    /// </summary>
    public string Xml;
    /// <summary>
    /// The text model when this is a text child, or null.
    /// </summary>
    public DocxText? Text;
    /// <summary>
    /// The break model for tab, carriage-return, break, hyphen, or symbol children, or null.
    /// </summary>
    public DocxBreak? Break;
    /// <summary>
    /// The drawing model when this is a drawing child, or null.
    /// </summary>
    public DocxDrawing? Drawing;

    /// <summary>
    /// Creates a run child from one XML element.
    /// </summary>
    /// <param name="xml">The child XML.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>xml</c> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>xml</c> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <c>xml</c> does not contain an element.</exception>
    public DocxRunChild(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (xml.Trim() == "")
        {
            throw new ArgumentException("Run child XML is required.", "xml");
        }

        this.UseTypedModel = false;
        this.Kind = DocxRunChildKind.PreservedUnknown;
        this.Xml = xml;
        this.Text = null;
        this.Break = null;
        this.Drawing = null;
        LoadTypedModel();
    }

    internal void LoadTypedModel()
    {
        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.MoveToContent())
        {
            throw new InvalidOperationException("Run child XML does not contain an element.");
        }

        if (reader.IsElement("t", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxRunChildKind.Text;
            this.Text = DocxText.ParseXml(this.Xml);
            return;
        }

        if (DocxBreak.IsSupported(reader))
        {
            this.Break = DocxBreak.ParseXml(this.Xml);

            if (reader.IsElement("tab", DocxConstants.NamespaceWordprocessingMl))
            {
                this.Kind = DocxRunChildKind.Tab;
                return;
            }

            if (reader.IsElement("cr", DocxConstants.NamespaceWordprocessingMl))
            {
                this.Kind = DocxRunChildKind.CarriageReturn;
                return;
            }

            if (reader.IsElement("br", DocxConstants.NamespaceWordprocessingMl))
            {
                this.Kind = DocxRunChildKind.Break;
                return;
            }

            if (reader.IsElement("sym", DocxConstants.NamespaceWordprocessingMl))
            {
                this.Kind = DocxRunChildKind.Symbol;
                return;
            }

            this.Kind = DocxRunChildKind.Hyphen;
            return;
        }

        if (reader.IsElement("fldChar", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxRunChildKind.FieldCharacter;
            return;
        }

        if (reader.IsElement("instrText", DocxConstants.NamespaceWordprocessingMl) || reader.IsElement("delInstrText", DocxConstants.NamespaceWordprocessingMl))
        {
            this.Kind = DocxRunChildKind.FieldInstruction;
            return;
        }

        if (DocxDrawing.IsSupported(reader))
        {
            this.Kind = DocxRunChildKind.Drawing;
            this.Drawing = DocxDrawing.ParseXml(this.Xml);
            return;
        }

        if (DocxRevision.IsSupported(reader))
        {
            this.Kind = DocxRunChildKind.Revision;
        }
    }

    internal static DocxRunChild FromText(DocxText value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxRunChild child = new DocxRunChild(value.WriteXml());
        child.Text = value;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxRunChild FromBreak(DocxBreak value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxRunChild child = new DocxRunChild(value.WriteXml());
        child.Break = value;
        child.UseTypedModel = true;
        return child;
    }

    internal static DocxRunChild FromDrawing(DocxDrawing value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        DocxRunChild child = new DocxRunChild(value.WriteXml());
        child.Drawing = value;
        child.UseTypedModel = true;
        return child;
    }

    internal string WriteXml()
    {
        if (!this.UseTypedModel)
        {
            return this.Xml;
        }

        if (this.Text != null)
        {
            return this.Text.WriteXml();
        }

        if (this.Break != null)
        {
            return this.Break.WriteXml();
        }

        if (this.Drawing != null)
        {
            return this.Drawing.WriteXml();
        }

        return this.Xml;
    }
}
