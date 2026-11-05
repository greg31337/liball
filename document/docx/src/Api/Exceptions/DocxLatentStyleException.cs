namespace docx;

/// <summary>
/// Represents latent style exception data used by a DOCX document.
/// </summary>
public class DocxLatentStyleException : System.Exception
{
    internal string Xml;
    /// <summary>
    /// The name value stored in the document model.
    /// </summary>
    public string Name = "";
    /// <summary>
    /// Whether locked was explicitly supplied.
    /// </summary>
    public bool HasLocked;
    /// <summary>
    /// Whether the locked behavior is enabled.
    /// </summary>
    public bool Locked;
    /// <summary>
    /// The ui priority value stored in the document model.
    /// </summary>
    public string UiPriority = "";
    /// <summary>
    /// Whether semi hidden was explicitly supplied.
    /// </summary>
    public bool HasSemiHidden;
    /// <summary>
    /// Whether the semi hidden behavior is enabled.
    /// </summary>
    public bool SemiHidden;
    /// <summary>
    /// Whether unhide when used was explicitly supplied.
    /// </summary>
    public bool HasUnhideWhenUsed;
    /// <summary>
    /// Whether the unhide when used behavior is enabled.
    /// </summary>
    public bool UnhideWhenUsed;
    /// <summary>
    /// Whether quick format was explicitly supplied.
    /// </summary>
    public bool HasQuickFormat;
    /// <summary>
    /// Whether the quick format behavior is enabled.
    /// </summary>
    public bool QuickFormat;

    /// <summary>
    /// Creates a latent style exception instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxLatentStyleException(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        InitializeParsedState();
        LoadFromXml();
    }

    internal static DocxLatentStyleException Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("lsdException", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:lsdException.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return ParseXml(buffer.GetXml());
    }

    internal static DocxLatentStyleException ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        return new DocxLatentStyleException(xml);
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
        InitializeParsedState();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("lsdException", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        ReadAttributes(reader);
    }

    internal void InitializeParsedState()
    {
        this.Name = "";
        this.HasLocked = false;
        this.Locked = false;
        this.UiPriority = "";
        this.HasSemiHidden = false;
        this.SemiHidden = false;
        this.HasUnhideWhenUsed = false;
        this.UnhideWhenUsed = false;
        this.HasQuickFormat = false;
        this.QuickFormat = false;
    }

    internal void ReadAttributes(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("lsdException", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:lsdException.");
        }

        this.Name = reader.GetAttribute("name", DocxConstants.NamespaceWordprocessingMl);
        this.UiPriority = reader.GetAttribute("uiPriority", DocxConstants.NamespaceWordprocessingMl);
        ReadLockedAttribute(reader);
        ReadSemiHiddenAttribute(reader);
        ReadUnhideWhenUsedAttribute(reader);
        ReadQuickFormatAttribute(reader);
    }

    internal void ReadLockedAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("locked", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasLocked = true;
            this.Locked = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadSemiHiddenAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("semiHidden", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasSemiHidden = true;
            this.SemiHidden = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadUnhideWhenUsedAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("unhideWhenUsed", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasUnhideWhenUsed = true;
            this.UnhideWhenUsed = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal void ReadQuickFormatAttribute(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        string value = reader.GetAttribute("qFormat", DocxConstants.NamespaceWordprocessingMl);

        if (value != "")
        {
            this.HasQuickFormat = true;
            this.QuickFormat = DocxXmlAttributes.ParseBool(value);
        }
    }

    internal bool HasAnySettings()
    {
        return this.Name != ""
            || this.HasLocked
            || this.UiPriority != ""
            || this.HasSemiHidden
            || this.HasUnhideWhenUsed
            || this.HasQuickFormat;
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
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

        writer.StartElement("w", "lsdException", DocxConstants.NamespaceWordprocessingMl);
        WriteOptionalAttribute(writer, "name", this.Name);
        WriteOptionalBoolAttribute(writer, "locked", this.HasLocked, this.Locked);
        WriteOptionalAttribute(writer, "uiPriority", this.UiPriority);
        WriteOptionalBoolAttribute(writer, "semiHidden", this.HasSemiHidden, this.SemiHidden);
        WriteOptionalBoolAttribute(writer, "unhideWhenUsed", this.HasUnhideWhenUsed, this.UnhideWhenUsed);
        WriteOptionalBoolAttribute(writer, "qFormat", this.HasQuickFormat, this.QuickFormat);
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

    internal void WriteOptionalBoolAttribute(DocxXmlWriter writer, string localName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (hasValue)
        {
            DocxXmlAttributes.WriteBool(writer, "w", localName, DocxConstants.NamespaceWordprocessingMl, value);
        }
    }
}
