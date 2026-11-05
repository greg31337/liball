using System.IO;
using System.Xml;

namespace docx;

internal class DocxMerger
{
    /// <summary>
    /// Appends content from another document and remaps dependent package resources.
    /// </summary>
    /// <param name="target">The target used by the operation.</param>
    /// <param name="source">The source document or value to copy from.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int Append(DocxDocument target, DocxDocument source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        DocxMergeContext context = PrepareDocumentMerge(target, source);
        return AppendPreparedDocument(target, source, context);
    }

    internal static int AppendPreparedDocument(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        int count = Append(target.Body, source.Body, context);

        if (count > 0)
        {
            DocxTemplate.UpdateMainDocumentBody(target);
            WriteImportedParts(target, context);
        }

        return count;
    }

    /// <summary>
    /// Appends content from another document and remaps dependent package resources.
    /// </summary>
    /// <param name="targetBody">The target body used by the operation.</param>
    /// <param name="sourceBody">The source body used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int Append(DocxBody targetBody, DocxBody sourceBody)
    {
        if (targetBody == null)
        {
            throw new ArgumentNullException("targetBody");
        }

        if (sourceBody == null)
        {
            throw new ArgumentNullException("sourceBody");
        }

        int count = 0;

        foreach (DocxBlock block in sourceBody.Blocks)
        {
            targetBody.Blocks.Add(CloneBlock(block));
            count = count + 1;
        }

        if (count > 0)
        {
            targetBody.Xml = targetBody.WriteXml();
            targetBody.LoadBlocksFromXml();
            targetBody.LoadSectionsFromXml();
        }

        return count;
    }

    internal static int Append(DocxBody targetBody, DocxBody sourceBody, DocxMergeContext context)
    {
        if (targetBody == null)
        {
            throw new ArgumentNullException("targetBody");
        }

        if (sourceBody == null)
        {
            throw new ArgumentNullException("sourceBody");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        int count = 0;

        foreach (DocxBlock block in sourceBody.Blocks)
        {
            targetBody.Blocks.Add(CloneBlock(block, context));
            count = count + 1;
        }

        if (count > 0)
        {
            targetBody.Xml = targetBody.WriteXml();
            targetBody.LoadBlocksFromXml();
            targetBody.LoadSectionsFromXml();
        }

        return count;
    }

    /// <summary>
    /// Inserts content from another document after the named bookmark and remaps dependent resources.
    /// </summary>
    /// <param name="target">The target used by the operation.</param>
    /// <param name="bookmarkName">The bookmark name used by the operation.</param>
    /// <param name="source">The source document or value to copy from.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int InsertAtBookmark(DocxDocument target, string bookmarkName, DocxDocument source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        ValidateBookmarkName(bookmarkName);

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (FindBookmarkBlockIndex(target.Body, bookmarkName) < 0)
        {
            return 0;
        }

        DocxMergeContext context = PrepareDocumentMerge(target, source);
        int count = InsertAtBookmark(target.Body, bookmarkName, source.Body, context);

        if (count > 0)
        {
            DocxTemplate.UpdateMainDocumentBody(target);
            WriteImportedParts(target, context);
        }

        return count;
    }

    /// <summary>
    /// Inserts content from another document after the named bookmark and remaps dependent resources.
    /// </summary>
    /// <param name="targetBody">The target body used by the operation.</param>
    /// <param name="bookmarkName">The bookmark name used by the operation.</param>
    /// <param name="sourceBody">The source body used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int InsertAtBookmark(DocxBody targetBody, string bookmarkName, DocxBody sourceBody)
    {
        if (targetBody == null)
        {
            throw new ArgumentNullException("targetBody");
        }

        ValidateBookmarkName(bookmarkName);

        if (sourceBody == null)
        {
            throw new ArgumentNullException("sourceBody");
        }

        int blockIndex = FindBookmarkBlockIndex(targetBody, bookmarkName);

        if (blockIndex < 0)
        {
            return 0;
        }

        int insertIndex = blockIndex + 1;
        int count = 0;

        foreach (DocxBlock block in sourceBody.Blocks)
        {
            targetBody.Blocks.Insert(insertIndex + count, CloneBlock(block));
            count = count + 1;
        }

        if (count > 0)
        {
            targetBody.Xml = targetBody.WriteXml();
            targetBody.LoadBlocksFromXml();
            targetBody.LoadSectionsFromXml();
        }

        return count;
    }

    internal static int InsertAtBookmark(DocxBody targetBody, string bookmarkName, DocxBody sourceBody, DocxMergeContext context)
    {
        if (targetBody == null)
        {
            throw new ArgumentNullException("targetBody");
        }

        ValidateBookmarkName(bookmarkName);

        if (sourceBody == null)
        {
            throw new ArgumentNullException("sourceBody");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        int blockIndex = FindBookmarkBlockIndex(targetBody, bookmarkName);

        if (blockIndex < 0)
        {
            return 0;
        }

        int insertIndex = blockIndex + 1;
        int count = 0;

        foreach (DocxBlock block in sourceBody.Blocks)
        {
            targetBody.Blocks.Insert(insertIndex + count, CloneBlock(block, context));
            count = count + 1;
        }

        if (count > 0)
        {
            targetBody.Xml = targetBody.WriteXml();
            targetBody.LoadBlocksFromXml();
            targetBody.LoadSectionsFromXml();
        }

        return count;
    }

    /// <summary>
    /// Finds bookmark block index in this merger.
    /// </summary>
    /// <param name="body">The body used by the operation.</param>
    /// <param name="bookmarkName">The bookmark name used by the operation.</param>
    /// <returns>The number of affected or available items.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public static int FindBookmarkBlockIndex(DocxBody body, string bookmarkName)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        ValidateBookmarkName(bookmarkName);

        int index = 0;

        foreach (DocxBlock block in body.Blocks)
        {
            if (BlockContainsBookmark(block, bookmarkName))
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static bool BlockContainsBookmark(DocxBlock block, string bookmarkName)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        ValidateBookmarkName(bookmarkName);

        if (block.Paragraph != null)
        {
            return ParagraphContainsBookmark(block.Paragraph, bookmarkName);
        }

        if (block.Table != null)
        {
            return TableContainsBookmark(block.Table, bookmarkName);
        }

        if (block.ContentControl != null)
        {
            return ContentControlContainsBookmark(block.ContentControl, bookmarkName);
        }

        return false;
    }

    internal static bool TableContainsBookmark(DocxTable table, string bookmarkName)
    {
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        ValidateBookmarkName(bookmarkName);

        foreach (DocxTableRow row in table.Rows)
        {
            foreach (DocxTableCell cell in row.Cells)
            {
                if (CellContainsBookmark(cell, bookmarkName))
                {
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool CellContainsBookmark(DocxTableCell cell, string bookmarkName)
    {
        if (cell == null)
        {
            throw new ArgumentNullException("cell");
        }

        ValidateBookmarkName(bookmarkName);

        foreach (DocxParagraph paragraph in cell.Paragraphs)
        {
            if (ParagraphContainsBookmark(paragraph, bookmarkName))
            {
                return true;
            }
        }

        foreach (DocxTable table in cell.Tables)
        {
            if (TableContainsBookmark(table, bookmarkName))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ContentControlContainsBookmark(DocxContentControl contentControl, string bookmarkName)
    {
        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        ValidateBookmarkName(bookmarkName);

        foreach (DocxBlock block in contentControl.Blocks)
        {
            if (BlockContainsBookmark(block, bookmarkName))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool ParagraphContainsBookmark(DocxParagraph paragraph, string bookmarkName)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        ValidateBookmarkName(bookmarkName);
        return paragraph.FindBookmarkStartByName(bookmarkName) != null;
    }

    internal static DocxBlock CloneBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (block.Paragraph != null)
        {
            return DocxBlock.FromParagraph(new DocxParagraph(block.Paragraph.WriteXml()));
        }

        if (block.Table != null)
        {
            return DocxBlock.FromTable(new DocxTable(block.Table.WriteXml()));
        }

        if (block.ContentControl != null)
        {
            return DocxBlock.FromContentControl(new DocxContentControl(block.ContentControl.WriteXml()));
        }

        return new DocxBlock(block.Kind, block.GetXml());
    }

    internal static DocxBlock CloneBlock(DocxBlock block, DocxMergeContext context)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string xml = RemapXml(GetBlockXml(block), context);

        if (block.Paragraph != null)
        {
            return DocxBlock.FromParagraph(new DocxParagraph(xml));
        }

        if (block.Table != null)
        {
            return DocxBlock.FromTable(new DocxTable(xml));
        }

        if (block.ContentControl != null)
        {
            return DocxBlock.FromContentControl(new DocxContentControl(xml));
        }

        return new DocxBlock(block.Kind, xml);
    }

    internal static DocxMergeContext PrepareDocumentMerge(DocxDocument target, DocxDocument source)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        DocxMergeContext context = new DocxMergeContext();
        context.SourceMainDocumentPartName = source.Package.GetMainDocumentPartName();
        context.RelationshipSourcePartName = context.SourceMainDocumentPartName;
        context.CollectBookmarkIds(target.Body.WriteXml());
        context.CollectDrawingIds(target.Body.WriteXml());
        context.AddPartName(context.SourceMainDocumentPartName, target.Package.GetMainDocumentPartName());
        ImportStyles(target, source, context);
        ImportNumbering(target, source, context);
        ImportMedia(target, source, context);
        ImportFootnotes(target, source, context);
        ImportEndnotes(target, source, context);
        ImportComments(target, source, context);
        ImportMainDocumentRelationships(target, source, context);
        return context;
    }

    internal static void ImportStyles(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        foreach (DocxStyle style in source.Styles.Styles)
        {
            if (!target.Styles.HasImportConflict(style) && target.Styles.ImportStyle(style))
            {
                context.ImportedStyleCount = context.ImportedStyleCount + 1;
            }
        }
    }

    internal static void ImportMainDocumentRelationships(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        string sourcePartName = context.SourceMainDocumentPartName;
        string targetPartName = target.Package.GetMainDocumentPartName();
        List<DocxRelationship> relationships = source.Package.GetRelationships(sourcePartName);

        foreach (DocxRelationship relationship in relationships)
        {
            if (!IsManagedMainDocumentRelationship(relationship.Type))
            {
                ImportRelationship(target.Package, source.Package, sourcePartName, targetPartName, relationship, context);
            }
        }
    }

    internal static bool IsManagedMainDocumentRelationship(string type)
    {
        return type == DocxConstants.RelationshipTypeStyles
            || type == DocxConstants.RelationshipTypeNumbering
            || type == DocxConstants.RelationshipTypeImage
            || type == DocxConstants.RelationshipTypeFootnotes
            || type == DocxConstants.RelationshipTypeEndnotes
            || type == DocxConstants.RelationshipTypeComments;
    }

    internal static void ImportRelationship(DocxPackage target, DocxPackage source, string sourcePartName, string targetPartName, DocxRelationship relationship, DocxMergeContext context)
    {
        string relationshipTarget = relationship.Target;

        if (!relationship.IsExternal)
        {
            string sourceTargetPartName = source.ResolveRelationshipTarget(sourcePartName, relationship.Id);
            string targetTargetPartName = ImportPartGraph(target, source, sourceTargetPartName, context);
            relationshipTarget = DocxCustomXmlManagement.GetRelativeTarget(targetPartName, targetTargetPartName);
        }

        DocxRelationship imported = target.CreateRelationship(targetPartName, relationship.Type, relationshipTarget, relationship.IsExternal);
        context.AddRelationshipId(sourcePartName, relationship.Id, imported.Id);
        context.ImportedRelationshipCount = context.ImportedRelationshipCount + 1;
    }

    internal static string ImportPartGraph(DocxPackage target, DocxPackage source, string sourcePartName, DocxMergeContext context)
    {
        string normalizedSourcePartName = DocxPartName.Normalize(sourcePartName);

        if (context.PartNames.ContainsKey(normalizedSourcePartName))
        {
            return context.GetPartName(normalizedSourcePartName);
        }

        DocxPart? sourcePart = source.FindPart(normalizedSourcePartName);

        if (sourcePart == null)
        {
            return normalizedSourcePartName;
        }

        string targetPartName = AllocateImportedPartName(target, normalizedSourcePartName);
        target.AddBinaryPart(targetPartName, sourcePart.ContentType, sourcePart.GetBytes());
        context.AddPartName(normalizedSourcePartName, targetPartName);
        List<DocxRelationship> relationships = source.GetRelationships(normalizedSourcePartName);

        foreach (DocxRelationship relationship in relationships)
        {
            ImportRelationship(target, source, normalizedSourcePartName, targetPartName, relationship, context);
        }

        if (DocxPackage.IsXmlPart(normalizedSourcePartName, sourcePart.ContentType))
        {
            string xml = RemapPartRelationshipXml(sourcePart.GetText(), normalizedSourcePartName, context);
            target.ReplaceXmlPart(targetPartName, sourcePart.ContentType, xml);
        }

        return targetPartName;
    }

    internal static string AllocateImportedPartName(DocxPackage package, string sourcePartName)
    {
        string normalizedPartName = DocxPartName.Normalize(sourcePartName);

        if (!package.ContainsPart(normalizedPartName))
        {
            return normalizedPartName;
        }

        int extensionIndex = normalizedPartName.LastIndexOf('.');
        string baseName = normalizedPartName;
        string extension = "";

        if (extensionIndex > normalizedPartName.LastIndexOf('/'))
        {
            baseName = normalizedPartName.Substring(0, extensionIndex);
            extension = normalizedPartName.Substring(extensionIndex);
        }

        int index = 1;

        while (true)
        {
            string candidate = baseName + "Imported" + index.ToString() + extension;

            if (!package.ContainsPart(candidate))
            {
                return candidate;
            }

            index = index + 1;
        }
    }

    internal static string RemapPartRelationshipXml(string xml, string sourcePartName, DocxMergeContext context)
    {
        string previousSourcePartName = context.RelationshipSourcePartName;
        context.RelationshipSourcePartName = sourcePartName;
        string remapped = RemapXml(xml, context);
        context.RelationshipSourcePartName = previousSourcePartName;
        return remapped;
    }

    internal static void ImportNumbering(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (source.Numbering.AbstractNumberingCount() == 0 && source.Numbering.NumberingInstanceCount() == 0)
        {
            return;
        }

        target.GetOrCreateNumbering();
        context.NumberingMap = target.ImportNumbering(source);
        context.ImportedNumbering = context.NumberingMap.AbstractNumberingCount() > 0 || context.NumberingMap.NumberingCount() > 0;
    }

    internal static void ImportMedia(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (source.Media.ImageCount() == 0)
        {
            return;
        }

        context.MediaMap = target.ImportMedia(source);
    }

    internal static void ImportFootnotes(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (source.FootnoteCount() == 0)
        {
            return;
        }

        DocxFootnotes footnotes = target.GetOrCreateFootnotes();

        foreach (DocxFootnote footnote in source.Footnotes.Footnotes)
        {
            if (footnote.Id != "" && footnote.Type == "")
            {
                string id = footnotes.AllocateFootnoteId();
                DocxFootnote imported = new DocxFootnote(footnote.WriteXml());
                imported.Id = id;
                imported.Xml = "";
                footnotes.AddFootnote(imported);
                context.AddFootnoteId(footnote.Id, id);
                context.ImportedFootnotes = true;
            }
        }
    }

    internal static void ImportEndnotes(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (source.EndnoteCount() == 0)
        {
            return;
        }

        DocxEndnotes endnotes = target.GetOrCreateEndnotes();

        foreach (DocxEndnote endnote in source.Endnotes.Endnotes)
        {
            if (endnote.Id != "" && endnote.Type == "")
            {
                string id = endnotes.AllocateEndnoteId();
                DocxEndnote imported = new DocxEndnote(endnote.WriteXml());
                imported.Id = id;
                imported.Xml = "";
                endnotes.AddEndnote(imported);
                context.AddEndnoteId(endnote.Id, id);
                context.ImportedEndnotes = true;
            }
        }
    }

    internal static void ImportComments(DocxDocument target, DocxDocument source, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (source == null)
        {
            throw new ArgumentNullException("source");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (source.CommentCount() == 0)
        {
            return;
        }

        DocxComments comments = target.GetOrCreateComments();

        foreach (DocxComment comment in source.Comments.Comments)
        {
            if (comment.Id != "")
            {
                string id = comments.AllocateCommentId();
                DocxComment imported = new DocxComment(comment.WriteXml());
                imported.Id = id;
                imported.Xml = "";
                comments.AddComment(imported);
                context.AddCommentId(comment.Id, id);
                context.ImportedComments = true;
            }
        }
    }

    internal static void WriteImportedParts(DocxDocument target, DocxMergeContext context)
    {
        if (target == null)
        {
            throw new ArgumentNullException("target");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (context.ImportedStyleCount > 0)
        {
            target.WriteStylesPart();
        }

        if (context.ImportedNumbering)
        {
            target.WriteNumberingPart();
        }

        if (context.ImportedFootnotes)
        {
            target.WriteFootnotesPart();
        }

        if (context.ImportedEndnotes)
        {
            target.WriteEndnotesPart();
        }

        if (context.ImportedComments)
        {
            target.WriteCommentsPart();
        }
    }

    internal static string RemapXml(string xml, DocxMergeContext context)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        XmlReaderSettings readerSettings = new XmlReaderSettings();
        readerSettings.ConformanceLevel = ConformanceLevel.Fragment;
        XmlWriterSettings writerSettings = new XmlWriterSettings();
        writerSettings.ConformanceLevel = ConformanceLevel.Fragment;
        writerSettings.OmitXmlDeclaration = true;

        using (StringReader stringReader = new StringReader(xml))
        {
            using (StringWriter stringWriter = new StringWriter())
            {
                using (XmlReader reader = XmlReader.Create(stringReader, readerSettings))
                {
                    using (XmlWriter writer = XmlWriter.Create(stringWriter, writerSettings))
                    {
                        while (reader.Read())
                        {
                            WriteRemappedNode(reader, writer, context);
                        }
                    }
                }

                return stringWriter.ToString();
            }
        }
    }

    internal static void WriteRemappedNode(XmlReader reader, XmlWriter writer, DocxMergeContext context)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (reader.NodeType == XmlNodeType.Element)
        {
            WriteRemappedElement(reader, writer, context);
        }
        else if (reader.NodeType == XmlNodeType.EndElement)
        {
            writer.WriteFullEndElement();
        }
        else if (reader.NodeType == XmlNodeType.Text)
        {
            writer.WriteString(reader.Value);
        }
        else if (reader.NodeType == XmlNodeType.CDATA)
        {
            writer.WriteCData(reader.Value);
        }
        else if (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace)
        {
            writer.WriteWhitespace(reader.Value);
        }
    }

    internal static void WriteRemappedElement(XmlReader reader, XmlWriter writer, DocxMergeContext context)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        string elementLocalName = reader.LocalName;
        string elementNamespaceUri = reader.NamespaceURI;
        bool isEmptyElement = reader.IsEmptyElement;
        writer.WriteStartElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);

        if (reader.HasAttributes)
        {
            while (reader.MoveToNextAttribute())
            {
                string value = RemapAttributeValue(
                    elementLocalName,
                    elementNamespaceUri,
                    reader.LocalName,
                    reader.NamespaceURI,
                    reader.Value,
                    context);
                writer.WriteAttributeString(reader.Prefix, reader.LocalName, reader.NamespaceURI, value);
            }

            reader.MoveToElement();
        }

        if (isEmptyElement)
        {
            writer.WriteEndElement();
        }
    }

    internal static string RemapAttributeValue(string elementLocalName, string elementNamespaceUri, string attributeLocalName, string attributeNamespaceUri, string value, DocxMergeContext context)
    {
        if (elementLocalName == null)
        {
            throw new ArgumentNullException("elementLocalName");
        }

        if (elementNamespaceUri == null)
        {
            throw new ArgumentNullException("elementNamespaceUri");
        }

        if (attributeLocalName == null)
        {
            throw new ArgumentNullException("attributeLocalName");
        }

        if (attributeNamespaceUri == null)
        {
            throw new ArgumentNullException("attributeNamespaceUri");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        if (attributeNamespaceUri == DocxConstants.NamespaceWordprocessingMl && attributeLocalName == "id")
        {
            if (elementLocalName == "footnoteReference")
            {
                return context.GetFootnoteId(value);
            }

            if (elementLocalName == "endnoteReference")
            {
                return context.GetEndnoteId(value);
            }

            if (elementLocalName == "commentRangeStart" || elementLocalName == "commentRangeEnd" || elementLocalName == "commentReference")
            {
                return context.GetCommentId(value);
            }

            if (elementLocalName == "bookmarkStart" || elementLocalName == "bookmarkEnd")
            {
                return context.GetBookmarkId(value);
            }
        }

        if (elementLocalName == "docPr"
            && (elementNamespaceUri == DocxConstants.NamespaceWordprocessingDrawing || elementNamespaceUri == DocxConstants.NamespaceStrictWordprocessingDrawing)
            && attributeNamespaceUri == ""
            && attributeLocalName == "id")
        {
            return context.GetDrawingId(value);
        }

        if (elementNamespaceUri == DocxConstants.NamespaceWordprocessingMl
            && elementLocalName == "numId"
            && attributeNamespaceUri == DocxConstants.NamespaceWordprocessingMl
            && attributeLocalName == "val")
        {
            return context.NumberingMap.GetNumberingId(value);
        }

        if (attributeNamespaceUri == DocxConstants.NamespaceRelationships)
        {
            string relationshipId = context.GetRelationshipId(context.RelationshipSourcePartName, value);

            if (relationshipId != value)
            {
                return relationshipId;
            }

            if (attributeLocalName == "embed" || attributeLocalName == "link")
            {
                return context.MediaMap.GetRelationshipId(context.SourceMainDocumentPartName, value);
            }
        }

        return value;
    }

    internal static void ValidateBookmarkName(string bookmarkName)
    {
        if (bookmarkName == null)
        {
            throw new ArgumentNullException("bookmarkName");
        }

        if (bookmarkName == "")
        {
            throw new ArgumentException("Bookmark name is required.", "bookmarkName");
        }
    }

    internal static string GetBlockXml(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (block.Paragraph != null)
        {
            return block.Paragraph.WriteXml();
        }

        if (block.Table != null)
        {
            return block.Table.WriteXml();
        }

        if (block.ContentControl != null)
        {
            return block.ContentControl.WriteXml();
        }

        return block.GetXml();
    }
}

internal class DocxMergeContext
{
    internal DocxNumberingImportMap NumberingMap;
    internal DocxMediaImportMap MediaMap;
    internal Dictionary<string, string> FootnoteIds;
    internal Dictionary<string, string> EndnoteIds;
    internal Dictionary<string, string> CommentIds;
    internal Dictionary<string, string> BookmarkIds;
    internal Dictionary<string, string> DrawingIds;
    internal Dictionary<string, string> RelationshipIds;
    internal Dictionary<string, string> PartNames;
    internal List<string> UsedBookmarkIds;
    internal List<string> UsedDrawingIds;
    internal string SourceMainDocumentPartName;
    internal string RelationshipSourcePartName;
    internal int ImportedStyleCount;
    internal int ImportedRelationshipCount;
    internal bool ImportedNumbering;
    internal bool ImportedFootnotes;
    internal bool ImportedEndnotes;
    internal bool ImportedComments;

    internal DocxMergeContext()
    {
        this.NumberingMap = new DocxNumberingImportMap();
        this.MediaMap = new DocxMediaImportMap();
        this.FootnoteIds = new Dictionary<string, string>();
        this.EndnoteIds = new Dictionary<string, string>();
        this.CommentIds = new Dictionary<string, string>();
        this.BookmarkIds = new Dictionary<string, string>();
        this.DrawingIds = new Dictionary<string, string>();
        this.RelationshipIds = new Dictionary<string, string>();
        this.PartNames = new Dictionary<string, string>();
        this.UsedBookmarkIds = new List<string>();
        this.UsedDrawingIds = new List<string>();
        this.SourceMainDocumentPartName = "/word/document.xml";
        this.RelationshipSourcePartName = "/word/document.xml";
        this.ImportedStyleCount = 0;
        this.ImportedRelationshipCount = 0;
        this.ImportedNumbering = false;
        this.ImportedFootnotes = false;
        this.ImportedEndnotes = false;
        this.ImportedComments = false;
    }

    internal void AddRelationshipId(string sourcePartName, string sourceId, string targetId)
    {
        AddMapValue(this.RelationshipIds, sourcePartName + "|" + sourceId, targetId);
    }

    internal string GetRelationshipId(string sourcePartName, string sourceId)
    {
        string key = sourcePartName + "|" + sourceId;

        if (this.RelationshipIds.ContainsKey(key))
        {
            return this.RelationshipIds[key];
        }

        return sourceId;
    }

    internal void AddPartName(string sourcePartName, string targetPartName)
    {
        AddMapValue(this.PartNames, sourcePartName, targetPartName);
    }

    internal string GetPartName(string sourcePartName)
    {
        return GetMapValue(this.PartNames, sourcePartName);
    }

    internal void AddFootnoteId(string sourceId, string targetId)
    {
        AddMapValue(this.FootnoteIds, sourceId, targetId);
    }

    internal string GetFootnoteId(string sourceId)
    {
        return GetMapValue(this.FootnoteIds, sourceId);
    }

    internal void AddEndnoteId(string sourceId, string targetId)
    {
        AddMapValue(this.EndnoteIds, sourceId, targetId);
    }

    internal string GetEndnoteId(string sourceId)
    {
        return GetMapValue(this.EndnoteIds, sourceId);
    }

    internal void AddCommentId(string sourceId, string targetId)
    {
        AddMapValue(this.CommentIds, sourceId, targetId);
    }

    internal string GetCommentId(string sourceId)
    {
        return GetMapValue(this.CommentIds, sourceId);
    }

    internal string GetBookmarkId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (sourceId == "")
        {
            return sourceId;
        }

        if (this.BookmarkIds.ContainsKey(sourceId))
        {
            return this.BookmarkIds[sourceId];
        }

        string targetId = AllocateBookmarkId();
        this.BookmarkIds[sourceId] = targetId;
        return targetId;
    }

    internal string GetDrawingId(string sourceId)
    {
        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (sourceId == "")
        {
            return sourceId;
        }

        if (this.DrawingIds.ContainsKey(sourceId))
        {
            return this.DrawingIds[sourceId];
        }

        int candidate = 1;

        while (this.UsedDrawingIds.Contains(candidate.ToString()))
        {
            candidate = candidate + 1;
        }

        string targetId = candidate.ToString();
        this.UsedDrawingIds.Add(targetId);
        this.DrawingIds[sourceId] = targetId;
        return targetId;
    }

    internal void CollectBookmarkIds(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ConformanceLevel = ConformanceLevel.Fragment;

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && (reader.LocalName == "bookmarkStart" || reader.LocalName == "bookmarkEnd")
                        && reader.NamespaceURI == DocxConstants.NamespaceWordprocessingMl)
                    {
                        string? id = reader.GetAttribute("id", DocxConstants.NamespaceWordprocessingMl);

                        if (id != null)
                        {
                            AddUsedBookmarkId(id);
                        }
                    }
                }
            }
        }
    }

    internal void CollectDrawingIds(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ConformanceLevel = ConformanceLevel.Fragment;

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "docPr"
                        && (reader.NamespaceURI == DocxConstants.NamespaceWordprocessingDrawing || reader.NamespaceURI == DocxConstants.NamespaceStrictWordprocessingDrawing))
                    {
                        string? id = reader.GetAttribute("id");

                        if (id != null && id != "" && !this.UsedDrawingIds.Contains(id))
                        {
                            this.UsedDrawingIds.Add(id);
                        }
                    }
                }
            }
        }
    }

    internal void AddUsedBookmarkId(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            return;
        }

        if (!this.UsedBookmarkIds.Contains(id))
        {
            this.UsedBookmarkIds.Add(id);
        }
    }

    internal string AllocateBookmarkId()
    {
        int candidate = 0;

        while (this.UsedBookmarkIds.Contains(candidate.ToString()))
        {
            candidate = candidate + 1;
        }

        string id = candidate.ToString();
        this.UsedBookmarkIds.Add(id);
        return id;
    }

    internal static void AddMapValue(Dictionary<string, string> map, string sourceId, string targetId)
    {
        if (map == null)
        {
            throw new ArgumentNullException("map");
        }

        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (targetId == null)
        {
            throw new ArgumentNullException("targetId");
        }

        map[sourceId] = targetId;
    }

    internal static string GetMapValue(Dictionary<string, string> map, string sourceId)
    {
        if (map == null)
        {
            throw new ArgumentNullException("map");
        }

        if (sourceId == null)
        {
            throw new ArgumentNullException("sourceId");
        }

        if (map.ContainsKey(sourceId))
        {
            return map[sourceId];
        }

        return sourceId;
    }
}
