namespace docx;

/// <summary>
/// Enumerates, creates, edits, deletes, and anchors document comments.
/// </summary>
public class DocxCommentManagement : IDocxCommentManagement
{
    /// <summary>
    /// Creates a comment management service.
    /// </summary>
    public DocxCommentManagement()
    {
    }

    /// <summary>
    /// Returns all comment stories in part order.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the comments.</returns>
    public List<DocxComment> GetComments(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxComment>(document.Comments.Comments);
    }

    /// <summary>
    /// Finds a comment by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The matching comment, or null.</returns>
    public DocxComment? FindComment(DocxDocument document, string id)
    {
        ValidateDocument(document);
        return document.Comments.FindCommentById(id);
    }

    /// <summary>
    /// Returns a comment identifier.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The identifier.</returns>
    public string GetCommentId(DocxComment comment)
    {
        ValidateComment(comment);
        return comment.Id;
    }

    /// <summary>
    /// Returns the review author.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The author.</returns>
    public string GetCommentAuthor(DocxComment comment)
    {
        ValidateComment(comment);
        return comment.Author;
    }

    /// <summary>
    /// Returns the review author initials.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The initials.</returns>
    public string GetCommentInitials(DocxComment comment)
    {
        ValidateComment(comment);
        return comment.Initials;
    }

    /// <summary>
    /// Returns the review timestamp text.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>The timestamp, or an empty string.</returns>
    public string GetCommentDate(DocxComment comment)
    {
        ValidateComment(comment);
        return comment.Date;
    }

    /// <summary>
    /// Returns the blocks in a comment story.
    /// </summary>
    /// <param name="comment">The comment to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    public List<DocxBlock> GetCommentBlocks(DocxComment comment)
    {
        ValidateComment(comment);
        return new List<DocxBlock>(comment.Blocks);
    }

    /// <summary>
    /// Creates a comment with automatically allocated identifier and no timestamp.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    public DocxComment CreateComment(DocxDocument document, string author, string initials, string text)
    {
        ValidateDocument(document);
        return document.CreateComment(author, initials, text);
    }

    /// <summary>
    /// Creates a comment with complete review metadata and an allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    public DocxComment CreateCommentWithMetadata(DocxDocument document, string author, string initials, string date, string text)
    {
        ValidateDocument(document);
        DocxComments comments = document.GetOrCreateComments();
        string id = comments.AllocateCommentId();
        return document.CreateCommentWithId(id, author, initials, date, text);
    }

    /// <summary>
    /// Creates a comment with an explicit identifier and review metadata.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    /// <param name="text">The initial comment text.</param>
    /// <returns>The created comment.</returns>
    public DocxComment CreateCommentWithId(DocxDocument document, string id, string author, string initials, string date, string text)
    {
        ValidateDocument(document);
        return document.CreateCommentWithId(id, author, initials, date, text);
    }

    /// <summary>
    /// Replaces all comment content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="text">The replacement text.</param>
    public void ReplaceCommentText(DocxDocument document, DocxComment comment, string text)
    {
        ValidateOwnedComment(document, comment);
        comment.ReplaceText(text);
        document.WriteCommentsPart();
    }

    /// <summary>
    /// Replaces a comment's review metadata.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="author">The review author.</param>
    /// <param name="initials">The author initials.</param>
    /// <param name="date">The timestamp text, or an empty string.</param>
    public void SetCommentReviewMetadata(DocxDocument document, DocxComment comment, string author, string initials, string date)
    {
        ValidateOwnedComment(document, comment);
        comment.SetReviewMetadata(author, initials, date);
        document.WriteCommentsPart();
    }

    /// <summary>
    /// Appends a paragraph to a comment story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    public void AppendCommentParagraph(DocxDocument document, DocxComment comment, DocxParagraph paragraph)
    {
        ValidateOwnedComment(document, comment);
        comment.AddParagraph(paragraph);
        document.WriteCommentsPart();
    }

    /// <summary>
    /// Removes a block from a comment story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="comment">The comment to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveCommentBlock(DocxDocument document, DocxComment comment, int index)
    {
        ValidateOwnedComment(document, comment);
        DocxBlock block = comment.RemoveBlockAt(index);
        document.WriteCommentsPart();
        return block;
    }

    /// <summary>
    /// Deletes a comment definition without changing range or reference markup.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>True when a comment was deleted.</returns>
    public bool DeleteComment(DocxDocument document, string id)
    {
        ValidateDocument(document);
        bool removed = document.Comments.RemoveCommentById(id);

        if (removed)
        {
            document.WriteCommentsPart();
        }

        return removed;
    }

    /// <summary>
    /// Inserts a comment range and reference around a paragraph child range.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="startIndex">The first child in the range.</param>
    /// <param name="endIndex">The exclusive end child position.</param>
    /// <param name="comment">The referenced comment.</param>
    /// <returns>The inserted reference run.</returns>
    public DocxRun InsertCommentRange(DocxParagraph paragraph, int startIndex, int endIndex, DocxComment comment)
    {
        ValidateParagraph(paragraph);
        ValidateComment(comment);

        if (startIndex < 0 || startIndex > paragraph.ChildCount())
        {
            throw new ArgumentOutOfRangeException("startIndex");
        }

        if (endIndex < startIndex || endIndex > paragraph.ChildCount())
        {
            throw new ArgumentOutOfRangeException("endIndex");
        }

        DocxParagraphChild end = new DocxParagraphChild(DocxComment.CreateRangeEndXml(comment.Id));
        DocxParagraphChild start = new DocxParagraphChild(DocxComment.CreateRangeStartXml(comment.Id));
        paragraph.InsertParagraphChild(endIndex, end);
        paragraph.InsertParagraphChild(startIndex, start);
        return InsertCommentReference(paragraph, endIndex + 2, comment);
    }

    /// <summary>
    /// Inserts a comment reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="comment">The referenced comment.</param>
    /// <returns>The inserted reference run.</returns>
    public DocxRun InsertCommentReference(DocxParagraph paragraph, int index, DocxComment comment)
    {
        ValidateParagraph(paragraph);
        ValidateComment(comment);
        DocxRun run = DocxComment.CreateReferenceRun(comment.Id);
        paragraph.InsertRun(index, run);
        return run;
    }

    /// <summary>
    /// Returns comment range-start identifiers from direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    public List<string> GetCommentRangeStartIds(DocxParagraph paragraph)
    {
        return GetRangeMarkerIds(paragraph, "commentRangeStart");
    }

    /// <summary>
    /// Returns comment range-end identifiers from direct paragraph children.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    public List<string> GetCommentRangeEndIds(DocxParagraph paragraph)
    {
        return GetRangeMarkerIds(paragraph, "commentRangeEnd");
    }

    /// <summary>
    /// Returns comment reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    public List<string> GetCommentReferenceIds(DocxParagraph paragraph)
    {
        ValidateParagraph(paragraph);
        List<string> ids = new List<string>();

        foreach (DocxParagraphChild paragraphChild in paragraph.GetChildren())
        {
            if (paragraphChild.Run != null)
            {
                foreach (DocxRunChild runChild in paragraphChild.Run.GetChildren())
                {
                    string id = GetElementId(runChild.Xml, "commentReference");

                    if (id != "")
                    {
                        ids.Add(id);
                    }
                }
            }
        }

        return ids;
    }

    /// <summary>
    /// Removes matching comment range-start and range-end markers.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The number of removed markers.</returns>
    public int RemoveCommentRangeMarkers(DocxParagraph paragraph, string id)
    {
        ValidateParagraph(paragraph);
        ValidateId(id);
        int removed = 0;

        for (int index = paragraph.ChildCount() - 1; index >= 0; index = index - 1)
        {
            DocxParagraphChild child = paragraph.GetChild(index);
            string startId = GetElementId(child.Xml, "commentRangeStart");
            string endId = GetElementId(child.Xml, "commentRangeEnd");

            if (startId == id || endId == id)
            {
                paragraph.RemoveParagraphChildAt(index);
                removed = removed + 1;
            }
        }

        return removed;
    }

    /// <summary>
    /// Removes matching comment references while preserving other run content.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The comment identifier.</param>
    /// <returns>The number of removed reference elements.</returns>
    public int RemoveCommentReferences(DocxParagraph paragraph, string id)
    {
        ValidateParagraph(paragraph);
        ValidateId(id);
        int removed = 0;

        for (int paragraphIndex = paragraph.ChildCount() - 1; paragraphIndex >= 0; paragraphIndex = paragraphIndex - 1)
        {
            DocxParagraphChild paragraphChild = paragraph.GetChild(paragraphIndex);
            DocxRun? run = paragraphChild.Run;

            if (run == null)
            {
                continue;
            }

            bool changed = false;

            for (int runIndex = run.ChildCount() - 1; runIndex >= 0; runIndex = runIndex - 1)
            {
                DocxRunChild child = run.GetChild(runIndex);

                if (GetElementId(child.Xml, "commentReference") == id)
                {
                    run.RemoveRunChildAt(runIndex);
                    removed = removed + 1;
                    changed = true;
                }
            }

            if (changed)
            {
                if (run.ChildCount() == 0)
                {
                    paragraph.RemoveParagraphChildAt(paragraphIndex);
                }
                else
                {
                    paragraph.ReplaceChildWithRun(paragraphIndex, run);
                }
            }
        }

        return removed;
    }

    internal static List<string> GetRangeMarkerIds(DocxParagraph paragraph, string localName)
    {
        ValidateParagraph(paragraph);
        List<string> ids = new List<string>();

        foreach (DocxParagraphChild child in paragraph.GetChildren())
        {
            string id = GetElementId(child.Xml, localName);

            if (id != "")
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    internal static string GetElementId(string xml, string localName)
    {
        DocxXmlReader reader = DocxXmlReader.Create(xml);

        if (!reader.MoveToContent())
        {
            return "";
        }

        if (!reader.IsElement(localName, DocxConstants.NamespaceWordprocessingMl))
        {
            return "";
        }

        return reader.GetAttribute("id", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static void ValidateOwnedComment(DocxDocument document, DocxComment comment)
    {
        ValidateDocument(document);
        ValidateComment(comment);

        if (document.Comments.FindCommentById(comment.Id) != comment)
        {
            throw new InvalidOperationException("Comment does not belong to the document.");
        }
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

    internal static void ValidateComment(DocxComment comment)
    {
        if (comment == null)
        {
            throw new ArgumentNullException("comment");
        }

        if (comment.Id == "")
        {
            throw new ArgumentException("Comment id is required.", "comment");
        }
    }

    internal static void ValidateId(string id)
    {
        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            throw new ArgumentException("Comment id is required.", "id");
        }
    }
}
