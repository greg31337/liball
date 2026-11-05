using System.Xml;

namespace docx;

/// <summary>
/// Traverses, extracts, accepts, rejects, and configures tracked revisions.
/// </summary>
public class DocxRevisionManagement : IDocxRevisionManagement
{
    /// <summary>
    /// Creates a revision management service.
    /// </summary>
    public DocxRevisionManagement()
    {
    }

    /// <summary>
    /// Returns revisions from the main document and all supported related stories.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The revisions in story and XML order.</returns>
    public List<DocxRevisionInfo> GetDocumentRevisions(DocxDocument document)
    {
        ValidateDocument(document);
        List<DocxRevisionInfo> revisions = new List<DocxRevisionInfo>();
        HashSet<string> partNames = new HashSet<string>();

        if (document.MainDocumentPart != null)
        {
            AddStoryRevisions(revisions, partNames, document.MainDocumentPart.PartName, document.Body.WriteXml());
        }

        foreach (DocxHeader header in document.Headers)
        {
            AddStoryRevisions(revisions, partNames, header.PartName, header.WriteXml());
        }

        foreach (DocxFooter footer in document.Footers)
        {
            AddStoryRevisions(revisions, partNames, footer.PartName, footer.WriteXml());
        }

        if (document.FootnotesPart != null)
        {
            AddStoryRevisions(revisions, partNames, document.FootnotesPart.PartName, document.Footnotes.WriteXml());
        }

        if (document.EndnotesPart != null)
        {
            AddStoryRevisions(revisions, partNames, document.EndnotesPart.PartName, document.Endnotes.WriteXml());
        }

        if (document.CommentsPart != null)
        {
            AddStoryRevisions(revisions, partNames, document.CommentsPart.PartName, document.Comments.WriteXml());
        }

        return revisions;
    }

    /// <summary>
    /// Returns revisions nested anywhere in a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The revisions in XML order.</returns>
    public List<DocxRevisionInfo> GetParagraphRevisions(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        return GetRevisionsFromXml(paragraph.WriteXml(), "");
    }

    /// <summary>
    /// Returns revisions nested anywhere in a run.
    /// </summary>
    /// <param name="run">The run to inspect.</param>
    /// <returns>The revisions in XML order.</returns>
    public List<DocxRevisionInfo> GetRunRevisions(DocxRun run)
    {
        ValidateRun(run);
        return GetRevisionsFromXml(run.WriteXml(), "");
    }

    /// <summary>
    /// Returns document revisions of one semantic kind.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="kind">The revision kind to select.</param>
    /// <returns>The matching revisions in story and XML order.</returns>
    public List<DocxRevisionInfo> GetDocumentRevisionsByKind(DocxDocument document, DocxRevisionKind kind)
    {
        ValidateRevisionKind(kind);
        List<DocxRevisionInfo> revisions = GetDocumentRevisions(document);
        List<DocxRevisionInfo> matches = new List<DocxRevisionInfo>();

        foreach (DocxRevisionInfo revision in revisions)
        {
            if (revision.Kind == kind)
            {
                matches.Add(revision);
            }
        }

        return matches;
    }

    /// <summary>
    /// Finds the first document revision with an identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The revision identifier.</param>
    /// <returns>The matching revision, or null.</returns>
    public DocxRevisionInfo? FindDocumentRevisionById(DocxDocument document, string id)
    {
        ValidateDocument(document);

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        foreach (DocxRevisionInfo revision in GetDocumentRevisions(document))
        {
            if (revision.Id == id)
            {
                return revision;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts selected document stories with explicit revision visibility.
    /// </summary>
    /// <param name="document">The document to extract.</param>
    /// <param name="includeInsertedText">Whether inserted and move-to content is included.</param>
    /// <param name="includeDeletedText">Whether deleted and move-from content is included.</param>
    /// <returns>The extracted text.</returns>
    public string ExtractDocumentRevisionText(DocxDocument document, bool includeInsertedText, bool includeDeletedText)
    {
        ValidateDocument(document);
        DocxTextOptions options = CreateRevisionTextOptions(includeInsertedText, includeDeletedText);
        DocxTextProcessor processor = new DocxTextProcessor();
        return processor.ExtractDocument(document, options);
    }

    /// <summary>
    /// Extracts paragraph text with explicit revision visibility.
    /// </summary>
    /// <param name="paragraph">The paragraph to extract.</param>
    /// <param name="includeInsertedText">Whether inserted and move-to content is included.</param>
    /// <param name="includeDeletedText">Whether deleted and move-from content is included.</param>
    /// <returns>The extracted text.</returns>
    public string ExtractParagraphRevisionText(DocxParagraph paragraph, bool includeInsertedText, bool includeDeletedText)
    {
        ValidateParagraph(paragraph);
        DocxTextOptions options = CreateRevisionTextOptions(includeInsertedText, includeDeletedText);
        return paragraph.GetTextWithOptions(options);
    }

    /// <summary>
    /// Accepts every revision in the main document and supported related stories.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The number of accepted revision elements.</returns>
    public int AcceptDocumentRevisions(DocxDocument document)
    {
        ValidateDocument(document);
        return TransformDocumentRevisions(document, true);
    }

    /// <summary>
    /// Rejects every revision in the main document and supported related stories.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <returns>The number of rejected revision elements.</returns>
    public int RejectDocumentRevisions(DocxDocument document)
    {
        ValidateDocument(document);
        return TransformDocumentRevisions(document, false);
    }

    /// <summary>
    /// Accepts revisions within a range of direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first selected child.</param>
    /// <param name="endIndex">The exclusive selected end position.</param>
    /// <returns>The number of accepted revision elements.</returns>
    public int AcceptParagraphChildRange(DocxParagraph paragraph, int startIndex, int endIndex)
    {
        ValidateParagraph(paragraph);
        return TransformParagraphChildRange(paragraph, startIndex, endIndex, true);
    }

    /// <summary>
    /// Rejects revisions within a range of direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first selected child.</param>
    /// <param name="endIndex">The exclusive selected end position.</param>
    /// <returns>The number of rejected revision elements.</returns>
    public int RejectParagraphChildRange(DocxParagraph paragraph, int startIndex, int endIndex)
    {
        ValidateParagraph(paragraph);
        return TransformParagraphChildRange(paragraph, startIndex, endIndex, false);
    }

    /// <summary>
    /// Returns whether the document enables change tracking for subsequent edits.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>True when change tracking is enabled.</returns>
    public bool IsTrackChangesEnabled(DocxDocument document)
    {
        ValidateDocument(document);
        DocxSettings settings = document.GetSettings();
        return settings.HasTrackRevisions && settings.TrackRevisions;
    }

    /// <summary>
    /// Enables or disables change tracking for subsequent edits.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether change tracking is enabled.</param>
    public void SetTrackChangesEnabled(DocxDocument document, bool enabled)
    {
        ValidateDocument(document);
        DocxSettings settings = document.GetOrCreateSettings();
        settings.SetTrackRevisions(enabled);
        document.WriteSettingsPart();
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateParagraph(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }
    }

    internal static void ValidateRun(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }
    }

    internal static void ValidateRevisionKind(DocxRevisionKind kind)
    {
        switch (kind)
        {
            case DocxRevisionKind.Inserted:
            case DocxRevisionKind.Deleted:
            case DocxRevisionKind.MoveFrom:
            case DocxRevisionKind.MoveTo:
            case DocxRevisionKind.FormattingChange:
                return;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    internal static DocxTextOptions CreateRevisionTextOptions(bool includeInsertedText, bool includeDeletedText)
    {
        DocxTextOptions options = new DocxTextOptions();
        options.IncludeInsertedText = includeInsertedText;
        options.IncludeDeletedText = includeDeletedText;
        return options;
    }

    internal static List<DocxRevisionInfo> GetRevisionsFromXml(string xml, string partName)
    {
        List<DocxRevisionInfo> revisions = new List<DocxRevisionInfo>();

        if (xml == "")
        {
            return revisions;
        }

        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(xml);

        if (document.DocumentElement != null)
        {
            AddRevisionNodes(revisions, document.DocumentElement, partName);
        }

        return revisions;
    }

    internal static void AddRevisionNodes(List<DocxRevisionInfo> revisions, XmlNode node, string partName)
    {
        if (node.NodeType == XmlNodeType.Element
            && node.NamespaceURI == DocxConstants.NamespaceWordprocessingMl
            && DocxRevision.IsRevisionLocalName(node.LocalName))
        {
            DocxRevision revision = DocxRevision.ParseXml(GetStandaloneNodeXml(node));
            DocxTextOptions options = CreateRevisionTextOptions(true, true);
            revisions.Add(new DocxRevisionInfo(
                MapRevisionKind(revision.Kind),
                revision.LocalName,
                revision.Id,
                revision.Author,
                revision.Date,
                partName,
                revision.GetTextWithOptions(options),
                revision.WriteXml()));
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            AddRevisionNodes(revisions, child, partName);
        }
    }

    internal static DocxRevisionKind MapRevisionKind(string kind)
    {
        switch (kind)
        {
            case DocxRevision.KindInserted:
                return DocxRevisionKind.Inserted;
            case DocxRevision.KindDeleted:
                return DocxRevisionKind.Deleted;
            case DocxRevision.KindMoveFrom:
                return DocxRevisionKind.MoveFrom;
            case DocxRevision.KindMoveTo:
                return DocxRevisionKind.MoveTo;
            default:
                return DocxRevisionKind.FormattingChange;
        }
    }

    internal static void AddStoryRevisions(List<DocxRevisionInfo> revisions, HashSet<string> partNames, string partName, string xml)
    {
        if (partName == "" || partNames.Contains(partName))
        {
            return;
        }

        partNames.Add(partName);

        foreach (DocxRevisionInfo revision in GetRevisionsFromXml(xml, partName))
        {
            revisions.Add(revision);
        }
    }

    internal static int TransformDocumentRevisions(DocxDocument document, bool accept)
    {
        DocxTemplate.UpdateMainDocumentBody(document);
        List<DocxPart> parts = GetDocumentRevisionParts(document);
        int count = 0;

        foreach (DocxPart part in parts)
        {
            string xml = part.GetText();
            int partCount = DocxRevision.CountRevisionElementsXml(xml);

            if (partCount > 0)
            {
                string transformedXml;

                if (accept)
                {
                    transformedXml = DocxRevision.AcceptRevisionXml(xml);
                }
                else
                {
                    transformedXml = DocxRevision.RejectRevisionXml(xml);
                }

                document.Package.ReplaceXmlPart(part.PartName, part.ContentType, transformedXml);
                count = count + partCount;
            }
        }

        if (count > 0)
        {
            document.ParseMainDocument();
            document.ParseHeaders();
            document.ParseFooters();
            document.ParseFootnotes();
            document.ParseEndnotes();
            document.ParseComments();
        }

        return count;
    }

    internal static List<DocxPart> GetDocumentRevisionParts(DocxDocument document)
    {
        List<DocxPart> parts = new List<DocxPart>();
        HashSet<string> partNames = new HashSet<string>();
        AddRevisionPart(parts, partNames, document.MainDocumentPart);

        foreach (DocxHeader header in document.Headers)
        {
            AddRevisionPart(parts, partNames, document.Package.FindPart(header.PartName));
        }

        foreach (DocxFooter footer in document.Footers)
        {
            AddRevisionPart(parts, partNames, document.Package.FindPart(footer.PartName));
        }

        AddRevisionPart(parts, partNames, document.FootnotesPart);
        AddRevisionPart(parts, partNames, document.EndnotesPart);
        AddRevisionPart(parts, partNames, document.CommentsPart);
        return parts;
    }

    internal static void AddRevisionPart(List<DocxPart> parts, HashSet<string> partNames, DocxPart? part)
    {
        if (part == null || partNames.Contains(part.PartName))
        {
            return;
        }

        partNames.Add(part.PartName);
        parts.Add(part);
    }

    internal static int TransformParagraphChildRange(DocxParagraph paragraph, int startIndex, int endIndex, bool accept)
    {
        int childCount = paragraph.ChildCount();

        if (startIndex < 0 || startIndex > childCount)
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (endIndex < startIndex || endIndex > childCount)
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        List<DocxParagraphChild> children = paragraph.GetChildren();
        List<DocxParagraphChild> transformedChildren = new List<DocxParagraphChild>();
        int count = 0;
        int index = 0;

        foreach (DocxParagraphChild child in children)
        {
            if (index < startIndex || index >= endIndex)
            {
                transformedChildren.Add(child);
            }
            else
            {
                count = count + AppendTransformedParagraphChild(transformedChildren, child, accept);
            }

            index = index + 1;
        }

        if (count > 0)
        {
            paragraph.ApplyParagraphChildren(transformedChildren);
        }

        return count;
    }

    internal static int AppendTransformedParagraphChild(List<DocxParagraphChild> children, DocxParagraphChild child, bool accept)
    {
        string xml = child.WriteXml();
        int count = DocxRevision.CountRevisionElementsXml(xml);

        if (count == 0)
        {
            children.Add(child);
            return 0;
        }

        if (child.Kind == DocxParagraphChildKind.Run)
        {
            DocxRun run = new DocxRun(xml);

            if (accept)
            {
                run.AcceptRevisions();
            }
            else
            {
                run.RejectRevisions();
            }

            children.Add(new DocxParagraphChild(run.WriteXml()));
            return count;
        }

        if (child.Kind == DocxParagraphChildKind.Hyperlink)
        {
            DocxHyperlink hyperlink = new DocxHyperlink(xml);

            if (accept)
            {
                hyperlink.AcceptRevisions();
            }
            else
            {
                hyperlink.RejectRevisions();
            }

            children.Add(new DocxParagraphChild(hyperlink.WriteXml()));
            return count;
        }

        if (child.Kind == DocxParagraphChildKind.Field)
        {
            DocxField field = new DocxField(xml);

            if (accept)
            {
                field.AcceptRevisions();
            }
            else
            {
                field.RejectRevisions();
            }

            children.Add(new DocxParagraphChild(field.WriteXml()));
            return count;
        }

        string transformedXml;

        if (accept)
        {
            transformedXml = DocxRevision.AcceptRevisionXml(xml);
        }
        else
        {
            transformedXml = DocxRevision.RejectRevisionXml(xml);
        }

        AppendParagraphChildFragment(children, transformedXml);
        return count;
    }

    internal static void AppendParagraphChildFragment(List<DocxParagraphChild> children, string xml)
    {
        if (xml == "")
        {
            return;
        }

        string wrapperXml = "<w:root xmlns:w=\"" + DocxConstants.NamespaceWordprocessingMl + "\">" + xml + "</w:root>";
        XmlDocument document = new XmlDocument();
        document.PreserveWhitespace = true;
        document.LoadXml(wrapperXml);

        if (document.DocumentElement == null)
        {
            return;
        }

        foreach (XmlNode node in document.DocumentElement.ChildNodes)
        {
            if (node.NodeType == XmlNodeType.Element)
            {
                children.Add(new DocxParagraphChild(GetStandaloneNodeXml(node)));
            }
        }
    }

    internal static string GetStandaloneNodeXml(XmlNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        XmlDocument document = new XmlDocument();
        XmlNode imported = document.ImportNode(node, true);
        document.AppendChild(imported);
        XmlElement? root = document.DocumentElement;

        if (root == null)
        {
            return "";
        }

        Dictionary<string, string> namespaces = new Dictionary<string, string>();
        CollectNodeNamespaces(node, namespaces);

        foreach (KeyValuePair<string, string> item in namespaces)
        {
            if (root.GetAttributeNode("xmlns:" + item.Key) == null)
            {
                XmlAttribute declaration = document.CreateAttribute("xmlns", item.Key, "http://www.w3.org/2000/xmlns/");
                declaration.Value = item.Value;
                root.Attributes.Append(declaration);
            }
        }

        return root.OuterXml;
    }

    internal static void CollectNodeNamespaces(XmlNode node, Dictionary<string, string> namespaces)
    {
        if (node.Prefix != "" && node.NamespaceURI != "" && !namespaces.ContainsKey(node.Prefix))
        {
            namespaces.Add(node.Prefix, node.NamespaceURI);
        }

        if (node.Attributes != null)
        {
            foreach (XmlAttribute attribute in node.Attributes)
            {
                if (attribute.Prefix != "" && attribute.Prefix != "xmlns" && attribute.NamespaceURI != "" && !namespaces.ContainsKey(attribute.Prefix))
                {
                    namespaces.Add(attribute.Prefix, attribute.NamespaceURI);
                }
            }
        }

        foreach (XmlNode child in node.ChildNodes)
        {
            CollectNodeNamespaces(child, namespaces);
        }
    }
}
