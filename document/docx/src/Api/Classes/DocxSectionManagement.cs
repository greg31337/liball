namespace docx;

/// <summary>
/// Manages document sections and their default, even-page, and first-page header and footer stories.
/// </summary>
public class DocxSectionManagement : IDocxSectionManagement
{
    /// <summary>
    /// Creates a section management service.
    /// </summary>
    public DocxSectionManagement()
    {
    }

    /// <summary>
    /// Returns all sections in document order.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the sections.</returns>
    public List<DocxSection> GetSections(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetSections();
    }

    /// <summary>
    /// Creates an unattached section.
    /// </summary>
    /// <returns>The created section.</returns>
    public DocxSection CreateSection()
    {
        return new DocxSection("");
    }

    /// <summary>
    /// Appends a section-break paragraph to the body.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section properties stored on the break.</param>
    /// <returns>The created section-break paragraph.</returns>
    public DocxParagraph AppendSectionBreak(DocxDocument document, DocxSection section)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.Body.AddSectionBreak(section);
    }

    /// <summary>
    /// Inserts a section-break paragraph at a body block position.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="blockIndex">The zero-based body block insertion position.</param>
    /// <param name="section">The section properties stored on the break.</param>
    /// <returns>The created section-break paragraph.</returns>
    public DocxParagraph InsertSectionBreak(DocxDocument document, int blockIndex, DocxSection section)
    {
        ValidateDocument(document);
        ValidateSection(section);
        DocxParagraph paragraph = new DocxParagraph("");
        paragraph.SetSection(section);
        document.Body.InsertParagraph(blockIndex, paragraph);
        return paragraph;
    }

    /// <summary>
    /// Sets the final section properties for a document.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The final section properties.</param>
    public void SetFinalSection(DocxDocument document, DocxSection section)
    {
        ValidateDocument(document);
        ValidateSection(section);
        document.Body.SetFinalSection(section);
    }

    /// <summary>
    /// Removes an intermediate or final section and its owning break markup.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to remove.</param>
    /// <returns>True when the section was found and removed.</returns>
    public bool RemoveSection(DocxDocument document, DocxSection section)
    {
        ValidateDocument(document);
        ValidateSection(section);
        DocxSection? finalSection = document.Body.GetFinalSection();

        if (finalSection == section)
        {
            return document.Body.RemoveFinalSection();
        }

        for (int index = 0; index < document.Body.BlockCount(); index = index + 1)
        {
            DocxParagraph? paragraph = document.Body.GetBlock(index).GetParagraph();

            if (paragraph != null && paragraph.Section == section)
            {
                document.Body.RemoveBlockAt(index);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Enables or disables distinct first-page header and footer stories for a section.
    /// </summary>
    /// <param name="section">The section to edit.</param>
    /// <param name="enabled">Whether first-page stories are enabled.</param>
    public void SetFirstPageStories(DocxSection section, bool enabled)
    {
        ValidateSection(section);
        section.Format.SetTitlePage(enabled);
        section.Xml = "";
    }

    /// <summary>
    /// Enables or disables distinct even-page header and footer stories for the document.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="enabled">Whether even-page stories are enabled.</param>
    public void SetEvenPageStories(DocxDocument document, bool enabled)
    {
        ValidateDocument(document);
        DocxSettings settings = document.GetOrCreateSettings();
        settings.SetEvenAndOddHeaders(enabled);
        document.WriteSettingsPart();
    }

    /// <summary>
    /// Returns all header parts discovered by the document.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the headers.</returns>
    public List<DocxHeader> GetHeaders(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetHeaders();
    }

    /// <summary>
    /// Returns all footer parts discovered by the document.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>A new list containing the footers.</returns>
    public List<DocxFooter> GetFooters(DocxDocument document)
    {
        ValidateDocument(document);
        return document.GetFooters();
    }

    /// <summary>
    /// Creates a header part and assigns it to a section reference.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The created header.</returns>
    public DocxHeader CreateHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        DocxHeader header = document.CreateHeaderForSection(section, GetReferenceType(kind));
        document.MarkSectionChanged(section);
        return header;
    }

    /// <summary>
    /// Creates a footer part and assigns it to a section reference.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The created footer.</returns>
    public DocxFooter CreateFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        DocxFooter footer = document.CreateFooterForSection(section, GetReferenceType(kind));
        document.MarkSectionChanged(section);
        return footer;
    }

    /// <summary>
    /// Assigns an existing document header to a section reference.
    /// </summary>
    /// <param name="document">The document containing the header.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <param name="header">The header to assign.</param>
    public void AssignHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, DocxHeader header)
    {
        ValidateDocument(document);
        ValidateSection(section);
        ValidateHeader(header);
        ValidateDocumentHeader(document, header);
        section.Format.SetHeaderReference(GetReferenceType(kind), header.RelationshipId);
        section.Xml = "";
        document.MarkSectionChanged(section);
    }

    /// <summary>
    /// Assigns an existing document footer to a section reference.
    /// </summary>
    /// <param name="document">The document containing the footer.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <param name="footer">The footer to assign.</param>
    public void AssignFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind, DocxFooter footer)
    {
        ValidateDocument(document);
        ValidateSection(section);
        ValidateFooter(footer);
        ValidateDocumentFooter(document, footer);
        section.Format.SetFooterReference(GetReferenceType(kind), footer.RelationshipId);
        section.Xml = "";
        document.MarkSectionChanged(section);
    }

    /// <summary>
    /// Finds the header directly assigned to a section reference.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The directly assigned header, or null.</returns>
    public DocxHeader? FindHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.FindHeaderByRelationshipId(section.Format.GetHeaderReference(GetReferenceType(kind)));
    }

    /// <summary>
    /// Finds the footer directly assigned to a section reference.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The directly assigned footer, or null.</returns>
    public DocxFooter? FindFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.FindFooterByRelationshipId(section.Format.GetFooterReference(GetReferenceType(kind)));
    }

    /// <summary>
    /// Finds the effective header after following section inheritance.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>The effective header, or null.</returns>
    public DocxHeader? FindEffectiveHeader(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.FindEffectiveHeader(section, GetReferenceType(kind));
    }

    /// <summary>
    /// Finds the effective footer after following section inheritance.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>The effective footer, or null.</returns>
    public DocxFooter? FindEffectiveFooter(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.FindEffectiveFooter(section, GetReferenceType(kind));
    }

    /// <summary>
    /// Returns whether a header reference inherits from the previous section.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The header reference kind.</param>
    /// <returns>True when the header is linked to the previous section.</returns>
    public bool IsHeaderLinkedToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.IsHeaderLinkedToPrevious(section, GetReferenceType(kind));
    }

    /// <summary>
    /// Returns whether a footer reference inherits from the previous section.
    /// </summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="section">The section to inspect.</param>
    /// <param name="kind">The footer reference kind.</param>
    /// <returns>True when the footer is linked to the previous section.</returns>
    public bool IsFooterLinkedToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        return document.IsFooterLinkedToPrevious(section, GetReferenceType(kind));
    }

    /// <summary>
    /// Links a header reference to the previous section.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The header reference kind.</param>
    public void LinkHeaderToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        document.LinkHeaderToPrevious(section, GetReferenceType(kind));
        document.MarkSectionChanged(section);
    }

    /// <summary>
    /// Links a footer reference to the previous section.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="section">The section to edit.</param>
    /// <param name="kind">The footer reference kind.</param>
    public void LinkFooterToPrevious(DocxDocument document, DocxSection section, DocxHeaderFooterKind kind)
    {
        ValidateDocument(document);
        ValidateSection(section);
        document.LinkFooterToPrevious(section, GetReferenceType(kind));
        document.MarkSectionChanged(section);
    }

    /// <summary>
    /// Removes a header part, its relationship, and direct section references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="header">The header to remove.</param>
    /// <returns>True when the header belonged to the document and was removed.</returns>
    public bool RemoveHeader(DocxDocument document, DocxHeader header)
    {
        ValidateDocument(document);
        ValidateHeader(header);
        int headerIndex = FindHeaderIndex(document, header);

        if (headerIndex < 0)
        {
            return false;
        }

        ClearHeaderReferences(document, header.RelationshipId);
        string mainPartName = document.Package.GetMainDocumentPartName();

        if (mainPartName != "")
        {
            document.Package.RemoveRelationship(mainPartName, header.RelationshipId);
        }

        document.Package.RemovePart(header.PartName);
        document.Headers.RemoveAt(headerIndex);
        return true;
    }

    /// <summary>
    /// Removes a footer part, its relationship, and direct section references.
    /// </summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="footer">The footer to remove.</param>
    /// <returns>True when the footer belonged to the document and was removed.</returns>
    public bool RemoveFooter(DocxDocument document, DocxFooter footer)
    {
        ValidateDocument(document);
        ValidateFooter(footer);
        int footerIndex = FindFooterIndex(document, footer);

        if (footerIndex < 0)
        {
            return false;
        }

        ClearFooterReferences(document, footer.RelationshipId);
        string mainPartName = document.Package.GetMainDocumentPartName();

        if (mainPartName != "")
        {
            document.Package.RemoveRelationship(mainPartName, footer.RelationshipId);
        }

        document.Package.RemovePart(footer.PartName);
        document.Footers.RemoveAt(footerIndex);
        return true;
    }

    /// <summary>
    /// Returns paragraph and table blocks in header order.
    /// </summary>
    /// <param name="header">The header to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    public List<DocxBlock> GetHeaderBlocks(DocxHeader header)
    {
        ValidateHeader(header);
        return new List<DocxBlock>(header.Blocks);
    }

    /// <summary>
    /// Appends a block to a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="block">The block to append.</param>
    public void AppendHeaderBlock(DocxHeader header, DocxBlock block)
    {
        ValidateHeader(header);
        ValidateBlock(block);
        header.AddBlock(block);
    }

    /// <summary>
    /// Inserts a block into a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="blockIndex">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    public void InsertHeaderBlock(DocxHeader header, int blockIndex, DocxBlock block)
    {
        ValidateHeader(header);
        ValidateBlock(block);
        header.InsertBlock(blockIndex, block);
    }

    /// <summary>
    /// Moves a block within a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    public void MoveHeaderBlock(DocxHeader header, int sourceIndex, int destinationIndex)
    {
        ValidateHeader(header);
        ValidateMoveIndexes(sourceIndex, destinationIndex, header.BlockCount());
        header.MoveBlock(sourceIndex, destinationIndex);
    }

    /// <summary>
    /// Removes a block from a header.
    /// </summary>
    /// <param name="header">The header to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveHeaderBlock(DocxHeader header, int blockIndex)
    {
        ValidateHeader(header);
        return header.RemoveBlockAt(blockIndex);
    }

    /// <summary>
    /// Returns paragraph and table blocks in footer order.
    /// </summary>
    /// <param name="footer">The footer to inspect.</param>
    /// <returns>A new list containing the blocks.</returns>
    public List<DocxBlock> GetFooterBlocks(DocxFooter footer)
    {
        ValidateFooter(footer);
        return new List<DocxBlock>(footer.Blocks);
    }

    /// <summary>
    /// Appends a block to a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="block">The block to append.</param>
    public void AppendFooterBlock(DocxFooter footer, DocxBlock block)
    {
        ValidateFooter(footer);
        ValidateBlock(block);
        footer.AddBlock(block);
    }

    /// <summary>
    /// Inserts a block into a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="blockIndex">The zero-based insertion position.</param>
    /// <param name="block">The block to insert.</param>
    public void InsertFooterBlock(DocxFooter footer, int blockIndex, DocxBlock block)
    {
        ValidateFooter(footer);
        ValidateBlock(block);
        footer.InsertBlock(blockIndex, block);
    }

    /// <summary>
    /// Moves a block within a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="sourceIndex">The current zero-based position.</param>
    /// <param name="destinationIndex">The resulting zero-based position.</param>
    public void MoveFooterBlock(DocxFooter footer, int sourceIndex, int destinationIndex)
    {
        ValidateFooter(footer);
        ValidateMoveIndexes(sourceIndex, destinationIndex, footer.BlockCount());
        footer.MoveBlock(sourceIndex, destinationIndex);
    }

    /// <summary>
    /// Removes a block from a footer.
    /// </summary>
    /// <param name="footer">The footer to edit.</param>
    /// <param name="blockIndex">The zero-based block position.</param>
    /// <returns>The removed block.</returns>
    public DocxBlock RemoveFooterBlock(DocxFooter footer, int blockIndex)
    {
        ValidateFooter(footer);
        return footer.RemoveBlockAt(blockIndex);
    }

    internal static string GetReferenceType(DocxHeaderFooterKind kind)
    {
        switch (kind)
        {
            case DocxHeaderFooterKind.Default:
                return DocxSectionFormat.HeaderFooterReferenceDefault;
            case DocxHeaderFooterKind.Even:
                return DocxSectionFormat.HeaderFooterReferenceEven;
            case DocxHeaderFooterKind.First:
                return DocxSectionFormat.HeaderFooterReferenceFirst;
            default:
                throw new ArgumentOutOfRangeException("kind");
        }
    }

    internal static void ClearHeaderReferences(DocxDocument document, string relationshipId)
    {
        foreach (DocxSection section in document.Body.Sections)
        {
            ClearMatchingHeaderReference(document, section, DocxSectionFormat.HeaderFooterReferenceDefault, relationshipId);
            ClearMatchingHeaderReference(document, section, DocxSectionFormat.HeaderFooterReferenceEven, relationshipId);
            ClearMatchingHeaderReference(document, section, DocxSectionFormat.HeaderFooterReferenceFirst, relationshipId);
        }
    }

    internal static void ClearMatchingHeaderReference(DocxDocument document, DocxSection section, string referenceType, string relationshipId)
    {
        if (section.Format.GetHeaderReference(referenceType) == relationshipId)
        {
            section.Format.ClearHeaderReference(referenceType);
            section.Xml = "";
            document.MarkSectionChanged(section);
        }
    }

    internal static void ClearFooterReferences(DocxDocument document, string relationshipId)
    {
        foreach (DocxSection section in document.Body.Sections)
        {
            ClearMatchingFooterReference(document, section, DocxSectionFormat.HeaderFooterReferenceDefault, relationshipId);
            ClearMatchingFooterReference(document, section, DocxSectionFormat.HeaderFooterReferenceEven, relationshipId);
            ClearMatchingFooterReference(document, section, DocxSectionFormat.HeaderFooterReferenceFirst, relationshipId);
        }
    }

    internal static void ClearMatchingFooterReference(DocxDocument document, DocxSection section, string referenceType, string relationshipId)
    {
        if (section.Format.GetFooterReference(referenceType) == relationshipId)
        {
            section.Format.ClearFooterReference(referenceType);
            section.Xml = "";
            document.MarkSectionChanged(section);
        }
    }

    internal static int FindHeaderIndex(DocxDocument document, DocxHeader header)
    {
        for (int index = 0; index < document.Headers.Count; index = index + 1)
        {
            if (document.Headers[index] == header)
            {
                return index;
            }
        }

        return -1;
    }

    internal static int FindFooterIndex(DocxDocument document, DocxFooter footer)
    {
        for (int index = 0; index < document.Footers.Count; index = index + 1)
        {
            if (document.Footers[index] == footer)
            {
                return index;
            }
        }

        return -1;
    }

    internal static void ValidateDocumentHeader(DocxDocument document, DocxHeader header)
    {
        if (FindHeaderIndex(document, header) < 0)
        {
            throw new ArgumentException("The header does not belong to the document.", "header");
        }

        string mainPartName = document.Package.GetMainDocumentPartName();
        DocxRelationship? relationship = document.Package.FindRelationship(mainPartName, header.RelationshipId);

        if (relationship == null || relationship.Type != DocxConstants.RelationshipTypeHeader)
        {
            throw new ArgumentException("The header is not related from the main document part.", "header");
        }
    }

    internal static void ValidateDocumentFooter(DocxDocument document, DocxFooter footer)
    {
        if (FindFooterIndex(document, footer) < 0)
        {
            throw new ArgumentException("The footer does not belong to the document.", "footer");
        }

        string mainPartName = document.Package.GetMainDocumentPartName();
        DocxRelationship? relationship = document.Package.FindRelationship(mainPartName, footer.RelationshipId);

        if (relationship == null || relationship.Type != DocxConstants.RelationshipTypeFooter)
        {
            throw new ArgumentException("The footer is not related from the main document part.", "footer");
        }
    }

    internal static void ValidateMoveIndexes(int sourceIndex, int destinationIndex, int count)
    {
        if (sourceIndex < 0 || sourceIndex >= count)
        {
            throw new ArgumentOutOfRangeException("sourceIndex");
        }

        if (destinationIndex < 0 || destinationIndex >= count)
        {
            throw new ArgumentOutOfRangeException("destinationIndex");
        }
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateSection(DocxSection section)
    {
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }
    }

    internal static void ValidateHeader(DocxHeader header)
    {
        if (header == null)
        {
            throw new ArgumentNullException("header");
        }
    }

    internal static void ValidateFooter(DocxFooter footer)
    {
        if (footer == null)
        {
            throw new ArgumentNullException("footer");
        }
    }

    internal static void ValidateBlock(DocxBlock block)
    {
        if (block == null)
        {
            throw new ArgumentNullException("block");
        }
    }
}
