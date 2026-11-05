using System.IO;
using System.Xml;

namespace docx;

/// <summary>
/// Appends, splits, and imports document content and dependent resources.
/// </summary>
public class DocxAssemblyManagement : IDocxAssemblyManagement
{
    /// <summary>Creates a document assembly management service.</summary>
    public DocxAssemblyManagement()
    {
    }

    /// <summary>Reports non-fatal conflicts between two documents.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The conflicts in discovery order.</returns>
    public List<DocxAssemblyConflict> GetAssemblyConflicts(DocxDocument target, DocxDocument source)
    {
        ValidateDocuments(target, source);
        List<DocxAssemblyConflict> conflicts = new List<DocxAssemblyConflict>();

        foreach (DocxStyle style in source.Styles.Styles)
        {
            if (target.Styles.HasImportConflict(style))
            {
                conflicts.Add(new DocxAssemblyConflict(
                    DocxAssemblyDiagnosticCodes.StyleConflict,
                    "Source style conflicts with a target style using the same identifier.",
                    style.StyleId));
            }
        }

        List<string> targetBookmarks = new List<string>();
        List<string> sourceBookmarks = new List<string>();
        CollectBookmarkNames(target.Body.WriteXml(), targetBookmarks);
        CollectBookmarkNames(source.Body.WriteXml(), sourceBookmarks);

        foreach (string bookmarkName in sourceBookmarks)
        {
            if (targetBookmarks.Contains(bookmarkName)
                && !ContainsConflict(conflicts, DocxAssemblyDiagnosticCodes.BookmarkNameConflict, bookmarkName))
            {
                conflicts.Add(new DocxAssemblyConflict(
                    DocxAssemblyDiagnosticCodes.BookmarkNameConflict,
                    "Source bookmark name is already present in the target document.",
                    bookmarkName));
            }
        }

        return conflicts;
    }

    /// <summary>Appends source body content and imports its dependent resources.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    public DocxAssemblyResult AppendDocument(DocxDocument target, DocxDocument source)
    {
        List<DocxAssemblyConflict> conflicts = GetAssemblyConflicts(target, source);
        DocxMergeContext context = DocxMerger.PrepareDocumentMerge(target, source);
        int count = DocxMerger.AppendPreparedDocument(target, source, context);
        return CreateResult(context, count, 0, conflicts);
    }

    /// <summary>Splits a document before one body block and returns the trailing document.</summary>
    /// <param name="document">The document whose leading blocks remain in place.</param>
    /// <param name="blockIndex">The first block moved to the returned document.</param>
    /// <returns>A document containing the trailing blocks and preserved package resources.</returns>
    public DocxDocument SplitDocumentAtBlock(DocxDocument document, int blockIndex)
    {
        ValidateDocument(document, "document");
        int blockCount = document.Body.BlockCount();

        if (blockIndex < 0 || blockIndex > blockCount)
        {
            throw new ArgumentOutOfRangeException("blockIndex");
        }

        DocxTemplate.UpdateMainDocumentBody(document);
        DocxDocument trailing;

        using (MemoryStream stream = new MemoryStream())
        {
            document.Package.Save(stream);
            stream.Position = 0;
            trailing = DocxDocument.OpenPackage(DocxPackage.Open(stream));
        }

        int index = document.Body.BlockCount() - 1;

        while (index >= blockIndex)
        {
            document.Body.RemoveBlockAt(index);
            index = index - 1;
        }

        index = blockIndex - 1;

        while (index >= 0)
        {
            trailing.Body.RemoveBlockAt(index);
            index = index - 1;
        }

        RefreshDocumentBody(document);
        RefreshDocumentBody(trailing);
        return trailing;
    }

    /// <summary>Appends one source section and imports its dependent resources.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <param name="sectionIndex">The zero-based source section index.</param>
    /// <returns>The assembly result.</returns>
    public DocxAssemblyResult ImportDocumentSection(DocxDocument target, DocxDocument source, int sectionIndex)
    {
        List<DocxAssemblyConflict> conflicts = GetAssemblyConflicts(target, source);
        List<int> range = GetSectionBlockRange(source.Body, sectionIndex);
        DocxMergeContext context = DocxMerger.PrepareDocumentMerge(target, source);
        int count = 0;
        int index = range[0];

        while (index <= range[1])
        {
            target.Body.Blocks.Add(DocxMerger.CloneBlock(source.Body.Blocks[index], context));
            count = count + 1;
            index = index + 1;
        }

        if (range[2] == 1)
        {
            DocxSection sourceSection = source.Body.GetSection(sectionIndex);
            string sectionXml = DocxMerger.RemapPartRelationshipXml(sourceSection.WriteXml(), context.SourceMainDocumentPartName, context);
            DocxParagraph sectionBreak = new DocxParagraph("");
            sectionBreak.SetSection(new DocxSection(sectionXml));
            target.Body.Blocks.Add(DocxBlock.FromParagraph(sectionBreak));
            count = count + 1;
        }

        RefreshDocumentBody(target);
        DocxMerger.WriteImportedParts(target, context);
        target.ParseHeaders();
        target.ParseFooters();
        target.ParseMedia();
        return CreateResult(context, count, 1, conflicts);
    }

    /// <summary>Imports all non-conflicting source styles.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    public DocxAssemblyResult ImportDocumentStyles(DocxDocument target, DocxDocument source)
    {
        List<DocxAssemblyConflict> conflicts = GetAssemblyConflicts(target, source);
        DocxMergeContext context = new DocxMergeContext();
        DocxMerger.ImportStyles(target, source, context);

        if (context.ImportedStyleCount > 0)
        {
            target.WriteStylesPart();
        }

        return CreateResult(context, 0, 0, conflicts);
    }

    /// <summary>Imports and remaps source numbering definitions and instances.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    public DocxAssemblyResult ImportDocumentNumbering(DocxDocument target, DocxDocument source)
    {
        ValidateDocuments(target, source);
        DocxMergeContext context = new DocxMergeContext();
        DocxMerger.ImportNumbering(target, source, context);

        if (context.ImportedNumbering)
        {
            target.WriteNumberingPart();
        }

        return CreateResult(context, 0, 0, new List<DocxAssemblyConflict>());
    }

    /// <summary>Imports source media and creates target main-document relationships.</summary>
    /// <param name="target">The target document.</param>
    /// <param name="source">The source document.</param>
    /// <returns>The assembly result.</returns>
    public DocxAssemblyResult ImportDocumentMedia(DocxDocument target, DocxDocument source)
    {
        ValidateDocuments(target, source);
        DocxMergeContext context = new DocxMergeContext();
        context.SourceMainDocumentPartName = source.Package.GetMainDocumentPartName();
        DocxMerger.ImportMedia(target, source, context);
        target.ParseMedia();
        return CreateResult(context, 0, 0, new List<DocxAssemblyConflict>());
    }

    internal static DocxAssemblyResult CreateResult(DocxMergeContext context, int blockCount, int sectionCount, List<DocxAssemblyConflict> conflicts)
    {
        DocxAssemblyResult result = new DocxAssemblyResult();
        result.AffectedBlockCount = blockCount;
        result.ImportedSectionCount = sectionCount;
        result.ImportedStyleCount = context.ImportedStyleCount;
        result.ImportedAbstractNumberingCount = context.NumberingMap.AbstractNumberingCount();
        result.ImportedNumberingCount = context.NumberingMap.NumberingCount();
        result.ImportedMediaCount = context.MediaMap.Count();
        result.RemappedRelationshipCount = context.ImportedRelationshipCount;
        result.RemappedBookmarkCount = context.BookmarkIds.Count;
        result.RemappedCommentCount = context.CommentIds.Count;
        result.RemappedFootnoteCount = context.FootnoteIds.Count;
        result.RemappedEndnoteCount = context.EndnoteIds.Count;
        result.RemappedDrawingCount = context.DrawingIds.Count;
        result.Conflicts = conflicts;
        return result;
    }

    internal static List<int> GetSectionBlockRange(DocxBody body, int sectionIndex)
    {
        if (sectionIndex < 0 || sectionIndex >= body.SectionCount())
        {
            throw new ArgumentOutOfRangeException("sectionIndex");
        }

        int currentSectionIndex = 0;
        int startIndex = 0;
        int blockIndex = 0;

        while (blockIndex < body.BlockCount())
        {
            DocxParagraph? paragraph = body.GetBlock(blockIndex).GetParagraph();

            if (paragraph != null && paragraph.Section != null)
            {
                if (currentSectionIndex == sectionIndex)
                {
                    return new List<int> { startIndex, blockIndex, 0 };
                }

                currentSectionIndex = currentSectionIndex + 1;
                startIndex = blockIndex + 1;
            }

            blockIndex = blockIndex + 1;
        }

        if (currentSectionIndex == sectionIndex && body.GetFinalSection() != null)
        {
            return new List<int> { startIndex, body.BlockCount() - 1, 1 };
        }

        throw new ArgumentOutOfRangeException("sectionIndex");
    }

    internal static void CollectBookmarkNames(string xml, List<string> names)
    {
        XmlReaderSettings settings = new XmlReaderSettings();
        settings.ConformanceLevel = ConformanceLevel.Fragment;

        using (StringReader stringReader = new StringReader(xml))
        {
            using (XmlReader reader = XmlReader.Create(stringReader, settings))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element
                        && reader.LocalName == "bookmarkStart"
                        && reader.NamespaceURI == DocxConstants.NamespaceWordprocessingMl)
                    {
                        string? name = reader.GetAttribute("name", DocxConstants.NamespaceWordprocessingMl);

                        if (name != null && name != "" && !names.Contains(name))
                        {
                            names.Add(name);
                        }
                    }
                }
            }
        }
    }

    internal static bool ContainsConflict(List<DocxAssemblyConflict> conflicts, string code, string sourceIdentifier)
    {
        foreach (DocxAssemblyConflict conflict in conflicts)
        {
            if (conflict.Code == code && conflict.SourceIdentifier == sourceIdentifier)
            {
                return true;
            }
        }

        return false;
    }

    internal static void RefreshDocumentBody(DocxDocument document)
    {
        document.Body.Xml = document.Body.WriteXml();
        document.Body.LoadBlocksFromXml();
        document.Body.LoadSectionsFromXml();
        DocxTemplate.UpdateMainDocumentBody(document);
    }

    internal static void ValidateDocuments(DocxDocument target, DocxDocument source)
    {
        ValidateDocument(target, "target");
        ValidateDocument(source, "source");
    }

    internal static void ValidateDocument(DocxDocument document, string name)
    {
        if (document == null)
        {
            throw new ArgumentNullException(name);
        }
    }
}
