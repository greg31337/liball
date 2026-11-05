namespace docx;

/// <summary>
/// Enumerates, creates, edits, deletes, and references footnotes and endnotes.
/// </summary>
public class DocxNoteManagement : IDocxNoteManagement
{
    /// <summary>
    /// Creates a note management service.
    /// </summary>
    public DocxNoteManagement()
    {
    }

    /// <summary>
    /// Returns all footnote stories, including separators.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the footnotes.</returns>
    public List<DocxFootnote> GetFootnotes(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxFootnote>(document.Footnotes.Footnotes);
    }

    /// <summary>
    /// Returns all endnote stories, including separators.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the endnotes.</returns>
    public List<DocxEndnote> GetEndnotes(DocxDocument document)
    {
        ValidateDocument(document);
        return new List<DocxEndnote>(document.Endnotes.Endnotes);
    }

    /// <summary>
    /// Finds a footnote by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <returns>The matching footnote, or null.</returns>
    public DocxFootnote? FindFootnote(DocxDocument document, string id)
    {
        ValidateDocument(document);
        return document.Footnotes.FindFootnoteById(id);
    }

    /// <summary>
    /// Finds an endnote by identifier.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <returns>The matching endnote, or null.</returns>
    public DocxEndnote? FindEndnote(DocxDocument document, string id)
    {
        ValidateDocument(document);
        return document.Endnotes.FindEndnoteById(id);
    }

    /// <summary>
    /// Returns a footnote identifier.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>The identifier.</returns>
    public string GetFootnoteId(DocxFootnote footnote)
    {
        ValidateFootnote(footnote);
        return footnote.Id;
    }

    /// <summary>
    /// Returns an endnote identifier.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>The identifier.</returns>
    public string GetEndnoteId(DocxEndnote endnote)
    {
        ValidateEndnote(endnote);
        return endnote.Id;
    }

    /// <summary>
    /// Returns the footnote story type.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>The story type, or an empty string.</returns>
    public string GetFootnoteType(DocxFootnote footnote)
    {
        ValidateFootnote(footnote);
        return footnote.Type;
    }

    /// <summary>
    /// Returns the endnote story type.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>The story type, or an empty string.</returns>
    public string GetEndnoteType(DocxEndnote endnote)
    {
        ValidateEndnote(endnote);
        return endnote.Type;
    }

    /// <summary>
    /// Returns the blocks in a footnote story.
    /// </summary>
    /// <param name="footnote">The footnote to inspect.</param>
    /// <returns>A new list containing its blocks.</returns>
    public List<DocxBlock> GetFootnoteBlocks(DocxFootnote footnote)
    {
        ValidateFootnote(footnote);
        return new List<DocxBlock>(footnote.Blocks);
    }

    /// <summary>
    /// Returns the blocks in an endnote story.
    /// </summary>
    /// <param name="endnote">The endnote to inspect.</param>
    /// <returns>A new list containing its blocks.</returns>
    public List<DocxBlock> GetEndnoteBlocks(DocxEndnote endnote)
    {
        ValidateEndnote(endnote);
        return new List<DocxBlock>(endnote.Blocks);
    }

    /// <summary>
    /// Creates a footnote with an automatically allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created footnote.</returns>
    public DocxFootnote CreateFootnote(DocxDocument document, string text)
    {
        ValidateDocument(document);
        return document.CreateFootnote(text);
    }

    /// <summary>
    /// Creates a footnote with an explicit identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created footnote.</returns>
    public DocxFootnote CreateFootnoteWithId(DocxDocument document, string id, string text)
    {
        ValidateDocument(document);
        return document.CreateFootnoteWithId(id, text);
    }

    /// <summary>
    /// Creates an endnote with an automatically allocated identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created endnote.</returns>
    public DocxEndnote CreateEndnote(DocxDocument document, string text)
    {
        ValidateDocument(document);
        return document.CreateEndnote(text);
    }

    /// <summary>
    /// Creates an endnote with an explicit identifier.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <param name="text">The initial note text.</param>
    /// <returns>The created endnote.</returns>
    public DocxEndnote CreateEndnoteWithId(DocxDocument document, string id, string text)
    {
        ValidateDocument(document);
        return document.CreateEndnoteWithId(id, text);
    }

    /// <summary>
    /// Replaces all footnote story content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="text">The replacement text.</param>
    public void ReplaceFootnoteText(DocxDocument document, DocxFootnote footnote, string text)
    {
        ValidateOwnedFootnote(document, footnote);
        footnote.ReplaceText(text);
        document.WriteFootnotesPart();
    }

    /// <summary>
    /// Replaces all endnote story content with one text paragraph.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="text">The replacement text.</param>
    public void ReplaceEndnoteText(DocxDocument document, DocxEndnote endnote, string text)
    {
        ValidateOwnedEndnote(document, endnote);
        endnote.ReplaceText(text);
        document.WriteEndnotesPart();
    }

    /// <summary>
    /// Appends a paragraph to a footnote story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    public void AppendFootnoteParagraph(DocxDocument document, DocxFootnote footnote, DocxParagraph paragraph)
    {
        ValidateOwnedFootnote(document, footnote);
        footnote.AddParagraph(paragraph);
        document.WriteFootnotesPart();
    }

    /// <summary>
    /// Appends a paragraph to an endnote story.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="paragraph">The paragraph to append.</param>
    public void AppendEndnoteParagraph(DocxDocument document, DocxEndnote endnote, DocxParagraph paragraph)
    {
        ValidateOwnedEndnote(document, endnote);
        endnote.AddParagraph(paragraph);
        document.WriteEndnotesPart();
    }

    /// <summary>
    /// Removes a footnote story block.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footnote">The footnote to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveFootnoteBlock(DocxDocument document, DocxFootnote footnote, int index)
    {
        ValidateOwnedFootnote(document, footnote);
        DocxBlock block = footnote.RemoveBlockAt(index);
        document.WriteFootnotesPart();
        return block;
    }

    /// <summary>
    /// Removes an endnote story block.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="endnote">The endnote to edit.</param>
    /// <param name="index">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveEndnoteBlock(DocxDocument document, DocxEndnote endnote, int index)
    {
        ValidateOwnedEndnote(document, endnote);
        DocxBlock block = endnote.RemoveBlockAt(index);
        document.WriteEndnotesPart();
        return block;
    }

    /// <summary>
    /// Deletes a footnote definition without changing document references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The footnote identifier.</param>
    /// <returns>True when a footnote was deleted.</returns>
    public bool DeleteFootnote(DocxDocument document, string id)
    {
        ValidateDocument(document);
        bool removed = document.Footnotes.RemoveFootnoteById(id);

        if (removed)
        {
            document.WriteFootnotesPart();
        }

        return removed;
    }

    /// <summary>
    /// Deletes an endnote definition without changing document references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="id">The endnote identifier.</param>
    /// <returns>True when an endnote was deleted.</returns>
    public bool DeleteEndnote(DocxDocument document, string id)
    {
        ValidateDocument(document);
        bool removed = document.Endnotes.RemoveEndnoteById(id);

        if (removed)
        {
            document.WriteEndnotesPart();
        }

        return removed;
    }

    /// <summary>
    /// Inserts a footnote reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="footnote">The referenced footnote.</param>
    /// <returns>The inserted run.</returns>
    public DocxRun InsertFootnoteReference(DocxParagraph paragraph, int index, DocxFootnote footnote)
    {
        ValidateParagraph(paragraph);
        ValidateFootnote(footnote);
        DocxRun run = DocxFootnote.CreateReferenceRun(footnote.Id);
        paragraph.InsertRun(index, run);
        return run;
    }

    /// <summary>
    /// Inserts an endnote reference run at a paragraph child position.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="index">The child insertion position.</param>
    /// <param name="endnote">The referenced endnote.</param>
    /// <returns>The inserted run.</returns>
    public DocxRun InsertEndnoteReference(DocxParagraph paragraph, int index, DocxEndnote endnote)
    {
        ValidateParagraph(paragraph);
        ValidateEndnote(endnote);
        DocxRun run = DocxEndnote.CreateReferenceRun(endnote.Id);
        paragraph.InsertRun(index, run);
        return run;
    }

    /// <summary>
    /// Returns footnote reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    public List<string> GetFootnoteReferenceIds(DocxParagraph paragraph)
    {
        return GetReferenceIds(paragraph, "footnoteReference");
    }

    /// <summary>
    /// Returns endnote reference identifiers from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to inspect.</param>
    /// <returns>The identifiers in document order.</returns>
    public List<string> GetEndnoteReferenceIds(DocxParagraph paragraph)
    {
        return GetReferenceIds(paragraph, "endnoteReference");
    }

    /// <summary>
    /// Removes matching footnote references from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The referenced footnote identifier.</param>
    /// <returns>The number of removed elements.</returns>
    public int RemoveFootnoteReferences(DocxParagraph paragraph, string id)
    {
        return RemoveReferences(paragraph, "footnoteReference", id);
    }

    /// <summary>
    /// Removes matching endnote references from direct paragraph runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="id">The referenced endnote identifier.</param>
    /// <returns>The number of removed elements.</returns>
    public int RemoveEndnoteReferences(DocxParagraph paragraph, string id)
    {
        return RemoveReferences(paragraph, "endnoteReference", id);
    }

    internal static List<string> GetReferenceIds(DocxParagraph paragraph, string localName)
    {
        ValidateParagraph(paragraph);
        List<string> ids = new List<string>();

        foreach (DocxParagraphChild paragraphChild in paragraph.GetChildren())
        {
            if (paragraphChild.Run != null)
            {
                foreach (DocxRunChild runChild in paragraphChild.Run.GetChildren())
                {
                    string id = GetReferenceId(runChild, localName);

                    if (id != "")
                    {
                        ids.Add(id);
                    }
                }
            }
        }

        return ids;
    }

    internal static int RemoveReferences(DocxParagraph paragraph, string localName, string id)
    {
        ValidateParagraph(paragraph);

        if (id == null)
        {
            throw new ArgumentNullException("id");
        }

        if (id == "")
        {
            throw new ArgumentException("Note id is required.", "id");
        }

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
                DocxRunChild runChild = run.GetChild(runIndex);

                if (GetReferenceId(runChild, localName) == id)
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

    internal static string GetReferenceId(DocxRunChild child, string localName)
    {
        DocxXmlReader reader = DocxXmlReader.Create(child.Xml);

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

    internal static void ValidateOwnedFootnote(DocxDocument document, DocxFootnote footnote)
    {
        ValidateDocument(document);
        ValidateFootnote(footnote);

        if (document.Footnotes.FindFootnoteById(footnote.Id) != footnote)
        {
            throw new InvalidOperationException("Footnote does not belong to the document.");
        }
    }

    internal static void ValidateOwnedEndnote(DocxDocument document, DocxEndnote endnote)
    {
        ValidateDocument(document);
        ValidateEndnote(endnote);

        if (document.Endnotes.FindEndnoteById(endnote.Id) != endnote)
        {
            throw new InvalidOperationException("Endnote does not belong to the document.");
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

    internal static void ValidateFootnote(DocxFootnote footnote)
    {
        if (footnote == null)
        {
            throw new ArgumentNullException("footnote");
        }

        if (footnote.Id == "")
        {
            throw new ArgumentException("Footnote id is required.", "footnote");
        }
    }

    internal static void ValidateEndnote(DocxEndnote endnote)
    {
        if (endnote == null)
        {
            throw new ArgumentNullException("endnote");
        }

        if (endnote.Id == "")
        {
            throw new ArgumentException("Endnote id is required.", "endnote");
        }
    }
}
