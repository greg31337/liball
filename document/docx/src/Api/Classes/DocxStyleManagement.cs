namespace docx;

/// <summary>
/// Provides style discovery, relationship resolution, mutation, diagnostics, and import operations.
/// </summary>
public class DocxStyleManagement : IDocxStyleManagement
{
    /// <summary>
    /// Creates a style management service.
    /// </summary>
    public DocxStyleManagement()
    {
    }

    /// <summary>
    /// Returns all defined styles in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>A new list containing all styles.</returns>
    public List<DocxStyle> GetStyles(DocxStyles styles)
    {
        ValidateStyles(styles);
        List<DocxStyle> result = new List<DocxStyle>();

        for (int index = 0; index < styles.StyleCount(); index = index + 1)
        {
            result.Add(styles.GetStyle(index));
        }

        return result;
    }

    /// <summary>
    /// Returns styles of one WordprocessingML type in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>A new list containing matching styles.</returns>
    public List<DocxStyle> GetStylesByType(DocxStyles styles, string type)
    {
        ValidateStyles(styles);
        ValidateText(type, "type");
        List<DocxStyle> result = new List<DocxStyle>();

        for (int index = 0; index < styles.StyleCount(); index = index + 1)
        {
            DocxStyle style = styles.GetStyle(index);

            if (style.Type == type)
            {
                result.Add(style);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds a style by identifier.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="styleId">The style identifier to find.</param>
    /// <returns>The matching style, or null.</returns>
    public DocxStyle? FindStyleById(DocxStyles styles, string styleId)
    {
        ValidateStyles(styles);
        ValidateText(styleId, "styleId");
        return styles.FindById(styleId);
    }

    /// <summary>
    /// Finds a style by name.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The style name to find.</param>
    /// <returns>The matching style, or null.</returns>
    public DocxStyle? FindStyleByName(DocxStyles styles, string name)
    {
        ValidateStyles(styles);
        ValidateText(name, "name");
        return FindByNameOrAlias(styles, name, "", false);
    }

    /// <summary>
    /// Finds a style by identifier and type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="styleId">The style identifier to find.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The matching style, or null.</returns>
    public DocxStyle? FindStyleByIdAndType(DocxStyles styles, string styleId, string type)
    {
        ValidateStyles(styles);
        ValidateText(styleId, "styleId");
        ValidateText(type, "type");
        return styles.FindByIdAndType(styleId, type);
    }

    /// <summary>
    /// Finds a style by name and type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The style name to find.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The matching style, or null.</returns>
    public DocxStyle? FindStyleByNameAndType(DocxStyles styles, string name, string type)
    {
        ValidateStyles(styles);
        ValidateText(name, "name");
        ValidateText(type, "type");
        return FindByNameOrAlias(styles, name, type, true);
    }

    /// <summary>
    /// Returns the default style for a type.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="type">The style type to match.</param>
    /// <returns>The default matching style, or null.</returns>
    public DocxStyle? GetDefaultStyle(DocxStyles styles, string type)
    {
        ValidateStyles(styles);
        ValidateText(type, "type");

        for (int index = 0; index < styles.StyleCount(); index = index + 1)
        {
            DocxStyle style = styles.GetStyle(index);

            if (style.Type == type && style.HasDefault && style.Default)
            {
                return style;
            }
        }

        return null;
    }

    /// <summary>
    /// Returns all latent style exceptions in document order.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <returns>A new list containing latent style exceptions.</returns>
    public List<DocxLatentStyleException> GetLatentStyleExceptions(DocxStyles styles)
    {
        ValidateStyles(styles);
        List<DocxLatentStyleException> result = new List<DocxLatentStyleException>();

        for (int index = 0; index < styles.LatentStyles.ExceptionCount(); index = index + 1)
        {
            result.Add(styles.LatentStyles.GetException(index));
        }

        return result;
    }

    /// <summary>
    /// Finds a latent style exception by name.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="name">The latent style name to find.</param>
    /// <returns>The matching latent style exception, or null.</returns>
    public DocxLatentStyleException? FindLatentStyleException(DocxStyles styles, string name)
    {
        ValidateStyles(styles);
        ValidateText(name, "name");

        for (int index = 0; index < styles.LatentStyles.ExceptionCount(); index = index + 1)
        {
            DocxLatentStyleException exception = styles.LatentStyles.GetException(index);

            if (exception.Name == name)
            {
                return exception;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the direct based-on style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The based-on style, or null.</returns>
    public DocxStyle? ResolveBasedOnStyle(DocxStyles styles, DocxStyle style)
    {
        ValidateStyles(styles);
        ValidateStyle(style);

        if (style.BasedOn == "")
        {
            return null;
        }

        return styles.FindById(style.BasedOn);
    }

    /// <summary>
    /// Returns the based-on ancestors nearest first, stopping safely at cycles.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose ancestors are resolved.</param>
    /// <returns>A new list containing based-on ancestors.</returns>
    public List<DocxStyle> GetBasedOnChain(DocxStyles styles, DocxStyle style)
    {
        ValidateStyles(styles);
        ValidateStyle(style);
        List<DocxStyle> result = new List<DocxStyle>();
        List<string> visited = new List<string>();

        if (style.StyleId != "")
        {
            visited.Add(style.StyleId);
        }

        DocxStyle? current = ResolveBasedOnStyle(styles, style);

        while (current != null && !visited.Contains(current.StyleId))
        {
            result.Add(current);
            visited.Add(current.StyleId);
            current = ResolveBasedOnStyle(styles, current);
        }

        return result;
    }

    /// <summary>
    /// Resolves the linked paragraph or character style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The linked style, or null.</returns>
    public DocxStyle? ResolveLinkedStyle(DocxStyles styles, DocxStyle style)
    {
        ValidateStyles(styles);
        ValidateStyle(style);

        if (style.Link == "")
        {
            return null;
        }

        return styles.FindById(style.Link);
    }

    /// <summary>
    /// Resolves the next paragraph style.
    /// </summary>
    /// <param name="styles">The styles model to inspect.</param>
    /// <param name="style">The style whose relationship is resolved.</param>
    /// <returns>The next style, or null.</returns>
    public DocxStyle? ResolveNextStyle(DocxStyles styles, DocxStyle style)
    {
        ValidateStyles(styles);
        ValidateStyle(style);

        if (style.Next == "")
        {
            return null;
        }

        return styles.FindById(style.Next);
    }

    /// <summary>
    /// Maps a built-in style identifier to its English WordprocessingML name.
    /// </summary>
    /// <param name="styleId">The built-in style identifier.</param>
    /// <returns>The mapped name, or the supplied identifier when no mapping is known.</returns>
    public string GetBuiltInStyleName(string styleId)
    {
        ValidateText(styleId, "styleId");
        return MapBuiltInName(styleId);
    }

    /// <summary>
    /// Maps an English built-in style name to its WordprocessingML identifier.
    /// </summary>
    /// <param name="name">The built-in style name.</param>
    /// <returns>The mapped identifier, or the supplied name when no mapping is known.</returns>
    public string GetBuiltInStyleId(string name)
    {
        ValidateText(name, "name");
        return MapBuiltInId(name);
    }

    /// <summary>
    /// Creates and persists a style definition.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="type">The style type.</param>
    /// <param name="styleId">The new style identifier.</param>
    /// <param name="name">The new style name.</param>
    /// <returns>The new style.</returns>
    public DocxStyle CreateStyle(DocxDocument document, string type, string styleId, string name)
    {
        ValidateDocument(document);
        DocxStyle style = document.Styles.CreateStyle(type, styleId, name);
        document.WriteStylesPart();
        return style;
    }

    /// <summary>
    /// Persists edits made to a style data object.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="style">The style to add or replace.</param>
    /// <returns>True when an existing style was replaced; otherwise false.</returns>
    public bool UpdateStyle(DocxDocument document, DocxStyle style)
    {
        ValidateDocument(document);
        ValidateStyle(style);
        style.ClearRawXml();
        bool replaced = document.Styles.AddOrReplaceStyle(style);
        document.WriteStylesPart();
        return replaced;
    }

    /// <summary>
    /// Persists edits made to the document latent style defaults.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    public void UpdateLatentStyles(DocxDocument document)
    {
        ValidateDocument(document);
        document.Styles.UpdateLatentStyles();
        document.WriteStylesPart();
    }

    /// <summary>
    /// Adds or replaces and persists one latent style exception.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="exception">The latent style exception to store.</param>
    public void UpdateLatentStyleException(DocxDocument document, DocxLatentStyleException exception)
    {
        ValidateDocument(document);

        if (exception == null)
        {
            throw new ArgumentNullException("exception");
        }

        if (exception.Name == "")
        {
            throw new ArgumentException("Latent style exception name is required.", "exception");
        }

        exception.ClearRawXml();
        document.Styles.LatentStyles.ReplaceException(exception);
        document.Styles.UpdateLatentStyles();
        document.WriteStylesPart();
    }

    /// <summary>
    /// Deletes and persists one latent style exception.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="name">The latent style name to delete.</param>
    /// <returns>True when an exception was deleted; otherwise false.</returns>
    public bool DeleteLatentStyleException(DocxDocument document, string name)
    {
        ValidateDocument(document);
        ValidateText(name, "name");
        bool removed = document.Styles.LatentStyles.RemoveException(name);

        if (removed)
        {
            document.Styles.UpdateLatentStyles();
            document.WriteStylesPart();
        }

        return removed;
    }

    /// <summary>
    /// Reports references and document content that use a style.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="styleId">The style identifier to diagnose.</param>
    /// <returns>The usage and reference diagnostics.</returns>
    public DocxValidationReport GetStyleDeleteDiagnostics(DocxDocument document, string styleId)
    {
        ValidateDocument(document);
        ValidateText(styleId, "styleId");
        DocxValidationReport report = document.Styles.GetDeleteDiagnostics(styleId);

        foreach (DocxPart part in document.Package.Parts)
        {
            if (IsStyleUsagePart(part))
            {
                AddPartUsageDiagnostics(report, part, styleId);
            }
        }

        return report;
    }

    /// <summary>
    /// Deletes a style and returns its pre-deletion usage diagnostics.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="styleId">The style identifier to delete.</param>
    /// <returns>The usage and reference diagnostics.</returns>
    public DocxValidationReport DeleteStyle(DocxDocument document, string styleId)
    {
        DocxValidationReport report = GetStyleDeleteDiagnostics(document, styleId);
        document.Styles.RemoveStyleById(styleId);
        document.Styles.RemoveStyleChildById(styleId);
        document.WriteStylesPart();
        return report;
    }

    /// <summary>
    /// Reports whether a style can be imported without an identifier conflict.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="style">The style to inspect.</param>
    /// <returns>The import diagnostics.</returns>
    public DocxValidationReport GetStyleImportDiagnostics(DocxDocument document, DocxStyle style)
    {
        ValidateDocument(document);
        ValidateStyle(style);
        return document.Styles.GetImportDiagnostics(style);
    }

    /// <summary>
    /// Imports and persists one non-conflicting style.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="style">The style to import.</param>
    /// <returns>True when the style was imported; otherwise false.</returns>
    public bool ImportStyle(DocxDocument document, DocxStyle style)
    {
        ValidateDocument(document);
        ValidateStyle(style);
        bool imported = document.Styles.ImportStyle(style);

        if (imported)
        {
            document.WriteStylesPart();
        }

        return imported;
    }

    /// <summary>
    /// Imports and persists all non-conflicting styles from a styles model.
    /// </summary>
    /// <param name="document">The target document.</param>
    /// <param name="source">The source styles model.</param>
    /// <returns>The number of imported styles.</returns>
    public int ImportStyles(DocxDocument document, DocxStyles source)
    {
        ValidateDocument(document);
        ValidateStyles(source);
        int imported = document.Styles.ImportStyles(source);

        if (imported > 0)
        {
            document.WriteStylesPart();
        }

        return imported;
    }

    internal static void ValidateStyles(DocxStyles styles)
    {
        if (styles == null)
        {
            throw new ArgumentNullException("styles");
        }
    }

    internal static void ValidateStyle(DocxStyle style)
    {
        if (style == null)
        {
            throw new ArgumentNullException("style");
        }
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateText(string value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    internal static bool IsStyleUsagePart(DocxPart part)
    {
        if (part == null)
        {
            throw new ArgumentNullException("part");
        }

        return part.ContentType == DocxConstants.ContentTypeMainDocument
            || part.ContentType == DocxConstants.ContentTypeTemplateMainDocument
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledMainDocument
            || part.ContentType == DocxConstants.ContentTypeMacroEnabledTemplateMainDocument
            || part.ContentType == DocxConstants.ContentTypeHeader
            || part.ContentType == DocxConstants.ContentTypeFooter
            || part.ContentType == DocxConstants.ContentTypeFootnotes
            || part.ContentType == DocxConstants.ContentTypeEndnotes
            || part.ContentType == DocxConstants.ContentTypeComments;
    }

    internal static DocxStyle? FindByNameOrAlias(DocxStyles styles, string name, string type, bool matchType)
    {
        for (int styleIndex = 0; styleIndex < styles.StyleCount(); styleIndex = styleIndex + 1)
        {
            DocxStyle style = styles.GetStyle(styleIndex);

            if (matchType && style.Type != type)
            {
                continue;
            }

            if (style.Name == name)
            {
                return style;
            }

            foreach (string alias in style.Aliases)
            {
                if (alias == name)
                {
                    return style;
                }
            }
        }

        return null;
    }

    internal static void AddPartUsageDiagnostics(DocxValidationReport report, DocxPart part, string styleId)
    {
        if (report == null)
        {
            throw new ArgumentNullException("report");
        }

        DocxXmlReader reader = DocxXmlReader.Create(part.GetText());
        bool paragraphReported = false;
        bool runReported = false;
        bool tableReported = false;
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element
                && reader.NamespaceUri() == DocxConstants.NamespaceWordprocessingMl
                && reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl) == styleId)
            {
                string localName = reader.LocalName();

                if (localName == "pStyle" && !paragraphReported)
                {
                    report.Add(DocxErrorSeverity.Warning, DocxStyles.DeleteParagraphUsageCode, "Style is used by paragraph content.", part.PartName);
                    paragraphReported = true;
                }
                else if (localName == "rStyle" && !runReported)
                {
                    report.Add(DocxErrorSeverity.Warning, DocxStyles.DeleteRunUsageCode, "Style is used by run content.", part.PartName);
                    runReported = true;
                }
                else if (localName == "tblStyle" && !tableReported)
                {
                    report.Add(DocxErrorSeverity.Warning, DocxStyles.DeleteTableUsageCode, "Style is used by table content.", part.PartName);
                    tableReported = true;
                }
            }

            hasNode = reader.Read();
        }
    }

    internal static string MapBuiltInName(string styleId)
    {
        if (styleId == "Normal") return "Normal";
        if (styleId == "NoSpacing") return "No Spacing";
        if (styleId == "Title") return "Title";
        if (styleId == "Subtitle") return "Subtitle";
        if (styleId == "Quote") return "Quote";
        if (styleId == "IntenseQuote") return "Intense Quote";
        if (styleId == "ListParagraph") return "List Paragraph";
        if (styleId == "Caption") return "Caption";
        if (styleId == "TOCHeading") return "TOC Heading";
        if (styleId == "Heading1") return "heading 1";
        if (styleId == "Heading2") return "heading 2";
        if (styleId == "Heading3") return "heading 3";
        if (styleId == "Heading4") return "heading 4";
        if (styleId == "Heading5") return "heading 5";
        if (styleId == "Heading6") return "heading 6";
        if (styleId == "Heading7") return "heading 7";
        if (styleId == "Heading8") return "heading 8";
        if (styleId == "Heading9") return "heading 9";
        return styleId;
    }

    internal static string MapBuiltInId(string name)
    {
        if (string.Equals(name, "Normal", StringComparison.OrdinalIgnoreCase)) return "Normal";
        if (string.Equals(name, "No Spacing", StringComparison.OrdinalIgnoreCase)) return "NoSpacing";
        if (string.Equals(name, "Title", StringComparison.OrdinalIgnoreCase)) return "Title";
        if (string.Equals(name, "Subtitle", StringComparison.OrdinalIgnoreCase)) return "Subtitle";
        if (string.Equals(name, "Quote", StringComparison.OrdinalIgnoreCase)) return "Quote";
        if (string.Equals(name, "Intense Quote", StringComparison.OrdinalIgnoreCase)) return "IntenseQuote";
        if (string.Equals(name, "List Paragraph", StringComparison.OrdinalIgnoreCase)) return "ListParagraph";
        if (string.Equals(name, "Caption", StringComparison.OrdinalIgnoreCase)) return "Caption";
        if (string.Equals(name, "TOC Heading", StringComparison.OrdinalIgnoreCase)) return "TOCHeading";
        if (string.Equals(name, "heading 1", StringComparison.OrdinalIgnoreCase)) return "Heading1";
        if (string.Equals(name, "heading 2", StringComparison.OrdinalIgnoreCase)) return "Heading2";
        if (string.Equals(name, "heading 3", StringComparison.OrdinalIgnoreCase)) return "Heading3";
        if (string.Equals(name, "heading 4", StringComparison.OrdinalIgnoreCase)) return "Heading4";
        if (string.Equals(name, "heading 5", StringComparison.OrdinalIgnoreCase)) return "Heading5";
        if (string.Equals(name, "heading 6", StringComparison.OrdinalIgnoreCase)) return "Heading6";
        if (string.Equals(name, "heading 7", StringComparison.OrdinalIgnoreCase)) return "Heading7";
        if (string.Equals(name, "heading 8", StringComparison.OrdinalIgnoreCase)) return "Heading8";
        if (string.Equals(name, "heading 9", StringComparison.OrdinalIgnoreCase)) return "Heading9";
        return name;
    }
}
