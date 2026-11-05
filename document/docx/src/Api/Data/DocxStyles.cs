namespace docx;

/// <summary>
/// Represents styles data used by a DOCX document.
/// </summary>
public class DocxStyles
{
    /// <summary>
    /// The delete missing style code identifier used in DOCX package data.
    /// </summary>
    public const string DeleteMissingStyleCode = "styles.delete.missing";
    /// <summary>
    /// The delete default style code identifier used in DOCX package data.
    /// </summary>
    public const string DeleteDefaultStyleCode = "styles.delete.default";
    /// <summary>
    /// The delete based on reference code identifier used in DOCX package data.
    /// </summary>
    public const string DeleteBasedOnReferenceCode = "styles.delete.referenced-by-based-on";
    /// <summary>
    /// The delete next reference code identifier used in DOCX package data.
    /// </summary>
    public const string DeleteNextReferenceCode = "styles.delete.referenced-by-next";
    /// <summary>
    /// The delete link reference code identifier used in DOCX package data.
    /// </summary>
    public const string DeleteLinkReferenceCode = "styles.delete.referenced-by-link";
    /// <summary>
    /// The paragraph usage diagnostic code identifier.
    /// </summary>
    public const string DeleteParagraphUsageCode = "styles.delete.used-by-paragraph";
    /// <summary>
    /// The run usage diagnostic code identifier.
    /// </summary>
    public const string DeleteRunUsageCode = "styles.delete.used-by-run";
    /// <summary>
    /// The table usage diagnostic code identifier.
    /// </summary>
    public const string DeleteTableUsageCode = "styles.delete.used-by-table";
    /// <summary>
    /// The import missing style id code identifier used in DOCX package data.
    /// </summary>
    public const string ImportMissingStyleIdCode = "styles.import.missing-style-id";
    /// <summary>
    /// The import conflict code identifier used in DOCX package data.
    /// </summary>
    public const string ImportConflictCode = "styles.import.conflict";

    internal string Xml;
    internal List<DocxXmlBuffer> Children;
    internal List<DocxStyle> Styles;
    /// <summary>
    /// The document defaults value stored in the document model.
    /// </summary>
    public DocxDocumentDefaults DocumentDefaults;
    /// <summary>
    /// The latent styles value stored in the document model.
    /// </summary>
    public DocxLatentStyles LatentStyles;

    /// <summary>
    /// Creates a styles instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxStyles(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        this.Children = new List<DocxXmlBuffer>();
        this.Styles = new List<DocxStyle>();
        this.DocumentDefaults = DocxDocumentDefaults.Create();
        this.LatentStyles = DocxLatentStyles.Create();
        LoadFromXml();
    }

    internal static DocxStyles Create()
    {
        return new DocxStyles("");
    }

    internal static DocxStyles Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!reader.IsElement("styles", DocxConstants.NamespaceWordprocessingMl))
        {
            throw new InvalidOperationException("Reader is not positioned on w:styles.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxStyles(buffer.GetXml());
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
        this.Styles = new List<DocxStyle>();
        this.DocumentDefaults = DocxDocumentDefaults.Create();
        this.LatentStyles = DocxLatentStyles.Create();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("styles", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int stylesDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == stylesDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == stylesDepth + 1)
            {
                bool isStyle = reader.IsElement("style", DocxConstants.NamespaceWordprocessingMl);
                bool isDocumentDefaults = reader.IsElement("docDefaults", DocxConstants.NamespaceWordprocessingMl);
                bool isLatentStyles = reader.IsElement("latentStyles", DocxConstants.NamespaceWordprocessingMl);
                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                AddParsedChild(child);

                if (isStyle)
                {
                    AddParsedStyle(DocxStyleFromBuffer(child));
                }
                else if (isDocumentDefaults)
                {
                    this.DocumentDefaults = DocxDocumentDefaultsFromBuffer(child);
                }
                else if (isLatentStyles)
                {
                    this.LatentStyles = DocxLatentStylesFromBuffer(child);
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
        ClearRawXml();
    }

    internal void AddParsedChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.Children.Add(child);
    }

    internal void AddStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        this.Styles.Add(style);
        ClearRawXml();
    }

    internal void AddParsedStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        this.Styles.Add(style);
    }

    internal DocxStyle CreateStyle(string type, string styleId, string name)
    {
        DocxStyle style = DocxStyle.Create(type, styleId, name);

        if (ContainsStyleId(style.StyleId))
        {
            throw new InvalidOperationException("Style id already exists.");
        }

        AddStyle(style);
        return style;
    }

    internal bool ReplaceStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        if (style.StyleId == "")
        {
            throw new ArgumentException("Style id is required.", "style");
        }

        for (int index = 0; index < this.Styles.Count; index = index + 1)
        {
            if (this.Styles[index].StyleId == style.StyleId)
            {
                this.Styles[index] = style;
                ClearRawXml();
                return true;
            }
        }

        return false;
    }

    internal bool AddOrReplaceStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        if (ReplaceStyle(style))
        {
            return true;
        }

        AddStyle(style);
        return false;
    }

    internal DocxValidationReport GetImportDiagnostics(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        DocxValidationReport report = new DocxValidationReport();

        if (style.StyleId == "")
        {
            report.Add(
                DocxErrorSeverity.Error,
                ImportMissingStyleIdCode,
                "Imported style id is required.",
                GetStyleLocation(style.StyleId));
            return report;
        }

        DocxStyle? existing = FindById(style.StyleId);

        if (existing != null && !StylesEqual(existing, style))
        {
            report.Add(
                DocxErrorSeverity.Error,
                ImportConflictCode,
                "Imported style conflicts with an existing style id.",
                GetStyleLocation(style.StyleId));
        }

        return report;
    }

    internal bool HasImportConflict(DocxStyle style)
    {
        DocxValidationReport report = GetImportDiagnostics(style);
        return report.HasErrors();
    }

    internal bool ImportStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        if (style.StyleId == "")
        {
            throw new ArgumentException("Style id is required.", "style");
        }

        DocxStyle? existing = FindById(style.StyleId);

        if (existing != null)
        {
            if (StylesEqual(existing, style))
            {
                return false;
            }

            throw new InvalidOperationException("Imported style conflicts with an existing style id.");
        }

        AddStyle(CloneStyle(style));
        return true;
    }

    internal int ImportStyles(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }

        int imported = 0;

        foreach (DocxStyle style in styles.Styles)
        {
            if (!HasImportConflict(style) && ImportStyle(style))
            {
                imported = imported + 1;
            }
        }

        return imported;
    }

    internal static bool StylesEqual(DocxStyle first, DocxStyle second)
    {
        if (first == null)
        {
            throw new ArgumentNullException("first");
        }

        if (second == null)
        {
            throw new ArgumentNullException("second");
        }

        return first.WriteXml() == second.WriteXml();
    }

    internal static DocxStyle CloneStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }

        return new DocxStyle(style.WriteXml());
    }

    internal DocxValidationReport GetDeleteDiagnostics(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        DocxValidationReport report = new DocxValidationReport();
        DocxStyle? target = FindById(styleId);

        if (target == null)
        {
            report.Add(
                DocxErrorSeverity.Warning,
                DeleteMissingStyleCode,
                "Style does not exist.",
                GetStyleLocation(styleId));
            return report;
        }

        if (target.HasDefault && target.Default)
        {
            report.Add(
                DocxErrorSeverity.Warning,
                DeleteDefaultStyleCode,
                "Style is marked as the default style for its type.",
                GetStyleLocation(styleId));
        }

        AddReferenceDiagnostics(report, target);
        return report;
    }

    internal DocxValidationReport DeleteStyle(string styleId)
    {
        DocxValidationReport report = GetDeleteDiagnostics(styleId);
        RemoveStyleById(styleId);
        RemoveStyleChildById(styleId);
        return report;
    }

    internal bool RemoveStyleById(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        for (int index = 0; index < this.Styles.Count; index = index + 1)
        {
            if (this.Styles[index].StyleId == styleId)
            {
                this.Styles.RemoveAt(index);
                ClearRawXml();
                return true;
            }
        }

        return false;
    }

    internal bool RemoveStyleChildById(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        for (int index = 0; index < this.Children.Count; index = index + 1)
        {
            if (IsStyleChildWithId(this.Children[index], styleId))
            {
                this.Children.RemoveAt(index);
                ClearRawXml();
                return true;
            }
        }

        return false;
    }

    internal bool IsStyleChildWithId(DocxXmlBuffer child, string styleId)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

        if (!reader.MoveToContent())
        {
            return false;
        }

        if (!reader.IsElement("style", DocxConstants.NamespaceWordprocessingMl))
        {
            return false;
        }

        string childStyleId = reader.GetAttribute("styleId", DocxConstants.NamespaceWordprocessingMl);
        return childStyleId == styleId;
    }

    internal void AddReferenceDiagnostics(DocxValidationReport report, DocxStyle target)
    {
        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        foreach (DocxStyle style in this.Styles)
        {
            if (style.StyleId == target.StyleId)
            {
                continue;
            }

            if (style.BasedOn == target.StyleId)
            {
                AddReferenceDiagnostic(report, DeleteBasedOnReferenceCode, "Style is referenced by basedOn.", style);
            }

            if (style.Next == target.StyleId)
            {
                AddReferenceDiagnostic(report, DeleteNextReferenceCode, "Style is referenced by next.", style);
            }

            if (style.Link == target.StyleId)
            {
                AddReferenceDiagnostic(report, DeleteLinkReferenceCode, "Style is referenced by link.", style);
            }
        }
    }

    internal void AddReferenceDiagnostic(DocxValidationReport report, string code, string message, DocxStyle referencingStyle)
    {
        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        if (code == null)
        {
            throw new ArgumentNullException("code");
        }

        if (message == null)
        {
            throw new ArgumentNullException("message");
        }

        if (referencingStyle == null)
        {
            throw new ArgumentNullException("referencingStyle");
        }

        report.Add(DocxErrorSeverity.Warning, code, message, GetStyleLocation(referencingStyle.StyleId));
    }

    internal static string GetStyleLocation(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        return "/word/styles.xml#styleId=" + styleId;
    }

    internal static DocxStyle DocxStyleFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxStyle.Parse(reader);
    }

    internal static DocxDocumentDefaults DocxDocumentDefaultsFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxDocumentDefaults.Parse(reader);
    }

    internal static DocxLatentStyles DocxLatentStylesFromBuffer(DocxXmlBuffer buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException("buffer");
        }

        DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());
        reader.MoveToContent();
        return DocxLatentStyles.Parse(reader);
    }

    internal void ClearRawXml()
    {
        this.Xml = "";
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

        writer.StartElement("w", "styles", DocxConstants.NamespaceWordprocessingMl);
        WriteNonStyleChildrenTo(writer);
        WriteStyleChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteNonStyleChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        bool documentDefaultsWritten = false;
        bool latentStylesWritten = false;

        foreach (DocxXmlBuffer child in this.Children)
        {
            if (IsDocumentDefaultsChild(child))
            {
                if (!documentDefaultsWritten && this.DocumentDefaults.ShouldWrite())
                {
                    this.DocumentDefaults.WriteTo(writer);
                }

                documentDefaultsWritten = true;
            }
            else if (IsLatentStylesChild(child))
            {
                if (!latentStylesWritten && this.LatentStyles.HasAnySettings())
                {
                    this.LatentStyles.WriteTo(writer);
                }

                latentStylesWritten = true;
            }
            else if (!IsStyleChild(child))
            {
                child.WriteTo(writer);
            }
        }

        if (!documentDefaultsWritten && this.DocumentDefaults.ShouldWrite())
        {
            this.DocumentDefaults.WriteTo(writer);
        }

        if (!latentStylesWritten && this.LatentStyles.HasAnySettings())
        {
            this.LatentStyles.WriteTo(writer);
        }
    }

    internal void WriteStyleChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        foreach (DocxStyle style in this.Styles)
        {
            style.WriteTo(writer);
        }
    }

    internal static bool IsStyleChild(DocxXmlBuffer child)
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

        return reader.IsElement("style", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static bool IsDocumentDefaultsChild(DocxXmlBuffer child)
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

        return reader.IsElement("docDefaults", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static bool IsLatentStylesChild(DocxXmlBuffer child)
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

        return reader.IsElement("latentStyles", DocxConstants.NamespaceWordprocessingMl);
    }

    internal void UpdateLatentStyles()
    {
        this.LatentStyles.ClearRawXml();
        ClearRawXml();
    }

    internal void SetParagraphDefaults(DocxParagraphFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.DocumentDefaults.SetParagraphFormat(format);
        ClearRawXml();
    }

    internal void ClearParagraphDefaults()
    {
        this.DocumentDefaults.ClearParagraphFormat();
        ClearRawXml();
    }

    internal void SetRunDefaults(DocxRunFormat format)
    {
        if (format == null)
        {
            throw new ArgumentNullException("format");
        }

        this.DocumentDefaults.SetRunFormat(format);
        ClearRawXml();
    }

    internal void ClearRunDefaults()
    {
        this.DocumentDefaults.ClearRunFormat();
        ClearRawXml();
    }

    internal int ChildCount()
    {
        return this.Children.Count;
    }

    internal string GetChildXml(int index)
    {
        return this.Children[index].GetXml();
    }

    internal int StyleCount()
    {
        return this.Styles.Count;
    }

    internal int CountStylesByType(string type)
    {
        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        int count = 0;

        foreach (DocxStyle style in this.Styles)
        {
            if (style.Type == type)
            {
                count = count + 1;
            }
        }

        return count;
    }

    internal int ParagraphStyleCount()
    {
        return CountStylesByType(DocxStyle.TypeParagraph);
    }

    internal int CharacterStyleCount()
    {
        return CountStylesByType(DocxStyle.TypeCharacter);
    }

    internal int TableStyleCount()
    {
        return CountStylesByType(DocxStyle.TypeTable);
    }

    internal int NumberingStyleCount()
    {
        return CountStylesByType(DocxStyle.TypeNumbering);
    }

    internal DocxStyle GetStyle(int index)
    {
        return this.Styles[index];
    }

    internal bool HasDocumentDefaults()
    {
        return this.DocumentDefaults.HasXml();
    }

    internal bool HasLatentStyles()
    {
        return this.LatentStyles.HasXml();
    }

    internal DocxStyle? FindById(string styleId)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        foreach (DocxStyle style in this.Styles)
        {
            if (style.StyleId == styleId)
            {
                return style;
            }
        }

        return null;
    }

    internal DocxStyle? FindByName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        foreach (DocxStyle style in this.Styles)
        {
            if (style.Name == name)
            {
                return style;
            }
        }

        return null;
    }

    internal DocxStyle? FindByIdAndType(string styleId, string type)
    {
        if (styleId == null)
        {
            throw new ArgumentNullException("styleId");
        }

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        foreach (DocxStyle style in this.Styles)
        {
            if (style.StyleId == styleId && style.Type == type)
            {
                return style;
            }
        }

        return null;
    }

    internal DocxStyle? FindByNameAndType(string name, string type)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (type == null)
        {
            throw new ArgumentNullException("type");
        }

        foreach (DocxStyle style in this.Styles)
        {
            if (style.Name == name && style.Type == type)
            {
                return style;
            }
        }

        return null;
    }

    internal bool ContainsStyleId(string styleId)
    {
        return FindById(styleId) != null;
    }

    internal bool ContainsStyleName(string name)
    {
        return FindByName(name) != null;
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
}
