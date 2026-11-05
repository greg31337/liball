using System;
using System.Collections.Generic;

namespace pdf;

/// <summary>
/// Builds an academic-style paper with authors, sections, citations, figures, and a bibliography.
/// </summary>
public class PdfPaper : IPdfPaper
{
    /// <summary>
    /// Stores the title used by this object.
    /// </summary>
    internal string Title = string.Empty;
    /// <summary>
    /// Stores the document used by this object.
    /// </summary>
    internal PdfDocument Document = new PdfDocument();
    /// <summary>
    /// Stores the layout used by this object.
    /// </summary>
    internal PdfLayoutDocument Layout = PdfLayoutDocument.Default(new PdfDocument());
    /// <summary>
    /// Stores the page size used by this object.
    /// </summary>
    internal PdfPageDimensions PageSize = new PdfPageSizeFactory().Letter();
    /// <summary>
    /// Stores the margins used by this object.
    /// </summary>
    internal PdfPageMargins Margins = new PdfPageMargins(72, 72, 72, 72);
    /// <summary>
    /// Specifies the content width in PDF points.
    /// </summary>
    internal double ContentWidth;
    internal List<PdfCitation> _citations = new List<PdfCitation>();
    internal List<PdfBibliographyEntry> _bibliography = new List<PdfBibliographyEntry>();
    internal List<PdfPaperAffiliation> _affiliations = new List<PdfPaperAffiliation>();
    internal List<string> _endnotes = new List<string>();
    internal List<PdfPaperPendingLink> _pendingLinks = new List<PdfPaperPendingLink>();
    internal List<PdfPaperTwoColumnSection> _twoColumnSections = new List<PdfPaperTwoColumnSection>();
    /// <summary>
    /// Exposes the citations currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfCitation> Citations = new List<PdfCitation>();
    /// <summary>
    /// Exposes the bibliography currently stored by this object.
    /// </summary>
    internal IReadOnlyList<PdfBibliographyEntry> Bibliography = new List<PdfBibliographyEntry>();
    /// <summary>
    /// Stores the title style used by this object.
    /// </summary>
    internal PdfParagraphStyle TitleStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the author style used by this object.
    /// </summary>
    internal PdfParagraphStyle AuthorStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the heading style used by this object.
    /// </summary>
    internal PdfParagraphStyle HeadingStyle = new PdfParagraphStyle();
    internal PdfParagraphStyle SecondHeadingStyle = new PdfParagraphStyle();
    internal PdfParagraphStyle ThirdHeadingStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the body style used by this object.
    /// </summary>
    internal PdfParagraphStyle BodyStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the caption style used by this object.
    /// </summary>
    internal PdfParagraphStyle CaptionStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the header style used by this object.
    /// </summary>
    internal PdfParagraphStyle HeaderStyle = new PdfParagraphStyle();
    /// <summary>
    /// Stores the footer style used by this object.
    /// </summary>
    internal PdfParagraphStyle FooterStyle = new PdfParagraphStyle();
    internal PdfParagraphStyle NoteStyle = new PdfParagraphStyle();
    internal bool CitationLinksEnabled;
    internal bool FigureListEnabled;
    internal bool TableListEnabled;
    internal bool PageLabelsEnabled;
    internal PdfPageLabelStyle PageLabelStyle;
    internal string PageLabelPrefix = string.Empty;
    internal int PageLabelStartNumber;
    internal int FootnoteNumber;
    internal bool Built;

    /// <summary>
    /// Initializes a new paper.
    /// </summary>
    /// <param name="title">The title to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public PdfPaper(string title)
    {
        if (title == null)
        {
            throw new ArgumentNullException("title");
        }

        Title = title;
        Document = new PdfDocument();
        PageSize = new PdfPageSizeFactory().Letter();
        Margins = new PdfPageMargins(72, 72, 72, 72);
        ContentWidth = PageSize.Width - Margins.Left - Margins.Right;
        Layout = new PdfLayoutDocument(Document, PageSize, Margins);
        _citations = new List<PdfCitation>();
        _bibliography = new List<PdfBibliographyEntry>();
        _affiliations = new List<PdfPaperAffiliation>();
        _endnotes = new List<string>();
        _pendingLinks = new List<PdfPaperPendingLink>();
        _twoColumnSections = new List<PdfPaperTwoColumnSection>();
        Citations = _citations;
        Bibliography = _bibliography;
        InitializeStyles();
        ApplyPageFurniture();
        AddTitlePageHeading();
        CitationLinksEnabled = false;
        FigureListEnabled = false;
        TableListEnabled = false;
        PageLabelsEnabled = false;
        PageLabelStyle = PdfPageLabelStyle.Decimal;
        PageLabelPrefix = string.Empty;
        PageLabelStartNumber = 1;
        FootnoteNumber = 0;
        Built = false;
    }

    /// <summary>
    /// Adds an author to this paper.
    /// </summary>
    /// <param name="name">The name used to identify the value in the document.</param>
    /// <param name="affiliation">The affiliation to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddAuthor(string name, string affiliation)
    {
        EnsureNotBuilt();
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (affiliation == null)
        {
            throw new ArgumentNullException("affiliation");
        }

        Layout.AddTextBlock(new PdfTextBlock(name + ", " + affiliation, AuthorStyle));
    }

    /// <summary>
    /// Adds a reusable affiliation line to this paper.
    /// </summary>
    /// <param name="affiliation">The affiliation to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the identifier has already been added.</exception>
    public void AddAffiliation(PdfPaperAffiliation affiliation)
    {
        EnsureNotBuilt();
        if (affiliation == null)
        {
            throw new ArgumentNullException("affiliation");
        }

        for (int index = 0; index < _affiliations.Count; index++)
        {
            if (_affiliations[index].Identifier == affiliation.Identifier)
            {
                throw new ArgumentException("Affiliation identifier has already been added.", "affiliation");
            }
        }

        _affiliations.Add(affiliation);
        Layout.AddTextBlock(new PdfTextBlock(affiliation.Identifier + "  " + affiliation.Name, AuthorStyle));
    }

    /// <summary>
    /// Adds an author associated with previously added affiliation identifiers.
    /// </summary>
    /// <param name="name">The author name.</param>
    /// <param name="affiliationIdentifiers">The affiliation identifiers to display and validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when an identifier is empty or has not been added.</exception>
    public void AddAuthorWithAffiliations(string name, string[] affiliationIdentifiers)
    {
        EnsureNotBuilt();
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (affiliationIdentifiers == null)
        {
            throw new ArgumentNullException("affiliationIdentifiers");
        }

        if (affiliationIdentifiers.Length == 0)
        {
            throw new ArgumentException("At least one affiliation identifier is required.", "affiliationIdentifiers");
        }

        for (int identifierIndex = 0; identifierIndex < affiliationIdentifiers.Length; identifierIndex++)
        {
            string identifier = affiliationIdentifiers[identifierIndex];
            if (identifier == null)
            {
                throw new ArgumentException("Affiliation identifiers cannot contain null.", "affiliationIdentifiers");
            }

            bool found = false;
            for (int affiliationIndex = 0; affiliationIndex < _affiliations.Count; affiliationIndex++)
            {
                if (_affiliations[affiliationIndex].Identifier == identifier)
                {
                    found = true;
                }
            }

            if (!found)
            {
                throw new ArgumentException("Affiliation identifier has not been added: " + identifier, "affiliationIdentifiers");
            }
        }

        Layout.AddTextBlock(new PdfTextBlock(name + " [" + string.Join(", ", affiliationIdentifiers) + "]", AuthorStyle));
    }

    /// <summary>
    /// Sets the abstract used for subsequent PDF generation.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetAbstract(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Layout.AddBlock(PdfBlock.WithSize(0, 16));
        AddSection("Abstract");
        AddParagraph(text);
    }

    /// <summary>
    /// Adds a keyword to this paper.
    /// </summary>
    /// <param name="keyword">The keyword to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddKeyword(string keyword)
    {
        EnsureNotBuilt();
        if (keyword == null)
        {
            throw new ArgumentNullException("keyword");
        }

        Layout.AddTextBlock(new PdfTextBlock("Keywords: " + keyword, BodyStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 8));
    }

    /// <summary>
    /// Adds a section to this paper.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    public void AddSection(string heading)
    {
        EnsureNotBuilt();
        PdfPaperSection section = new PdfPaperSection(heading);
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTextBlock(section.CreateBlock(HeadingStyle)));
        Layout.AddBlock(PdfBlock.WithSize(0, 4));
    }

    /// <summary>
    /// Adds a heading at an explicit hierarchy level.
    /// </summary>
    /// <param name="heading">The heading text.</param>
    /// <param name="level">The heading hierarchy level.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the heading level is unsupported.</exception>
    public void AddHeading(string heading, PdfPaperHeadingLevel level)
    {
        EnsureNotBuilt();
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        PdfParagraphStyle style = GetHeadingStyle(level);
        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTextBlock(new PdfTextBlock(heading, style)));
        Layout.AddBlock(PdfBlock.WithSize(0, 4));
    }

    /// <summary>
    /// Adds a heading that can be targeted by paper cross-references.
    /// </summary>
    /// <param name="referenceName">The unique cross-reference name.</param>
    /// <param name="heading">The heading text.</param>
    /// <param name="level">The heading hierarchy level.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when text is empty or the heading level is unsupported.</exception>
    public void AddReferencedHeading(string referenceName, string heading, PdfPaperHeadingLevel level)
    {
        EnsureNotBuilt();
        ValidateRequiredText(referenceName, "referenceName");
        ValidateRequiredText(heading, "heading");
        PdfTextBlock block = new PdfTextBlock(heading, GetHeadingStyle(level));
        Layout.AddKeepTogetherItem(new PdfPaperAnchoredTextBlock(this, block, referenceName, heading));
        Layout.AddBlock(PdfBlock.WithSize(0, 4));
    }

    /// <summary>
    /// Adds linked text that targets a referenced heading.
    /// </summary>
    /// <param name="referenceName">The target cross-reference name.</param>
    /// <param name="text">The linked display text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when text is empty.</exception>
    public void AddCrossReference(string referenceName, string text)
    {
        EnsureNotBuilt();
        ValidateRequiredText(referenceName, "referenceName");
        ValidateRequiredText(text, "text");
        Layout.AddItem(new PdfPaperLinkedTextBlock(this, new PdfTextBlock(text, BodyStyle), referenceName));
        Layout.AddBlock(PdfBlock.WithSize(0, 8));
    }

    /// <summary>
    /// Adds a section to this paper.
    /// </summary>
    /// <param name="section">The section to add to the paper.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddPaperSection(PdfPaperSection section)
    {
        EnsureNotBuilt();
        if (section == null)
        {
            throw new ArgumentNullException("section");
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromTextBlock(section.CreateBlock(GetHeadingStyle(section.Level))));
        Layout.AddBlock(PdfBlock.WithSize(0, 4));
    }

    /// <summary>
    /// Adds a paragraph to this paper.
    /// </summary>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddParagraph(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Layout.AddTextBlock(new PdfTextBlock(text, BodyStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 8));
    }

    /// <summary>
    /// Adds a numbered footnote at the current content position.
    /// </summary>
    /// <param name="text">The footnote text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddFootnote(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        FootnoteNumber++;
        Layout.AddTextBlock(new PdfTextBlock("[" + FootnoteNumber + "] " + text, NoteStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 4));
    }

    /// <summary>
    /// Collects a numbered endnote for the generated endnotes section.
    /// </summary>
    /// <param name="text">The endnote text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddEndnote(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        _endnotes.Add(text);
    }

    /// <summary>
    /// Adds a figure to this paper.
    /// </summary>
    /// <param name="image">The image resource to place or store.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddFigure(PdfImage image, string caption)
    {
        EnsureNotBuilt();
        if (image == null)
        {
            throw new ArgumentNullException("image");
        }

        if (caption == null)
        {
            throw new ArgumentNullException("caption");
        }

        double imageWidth = image.Width;
        double imageHeight = image.Height;
        if (imageWidth > ContentWidth)
        {
            double scale = ContentWidth / imageWidth;
            imageWidth = ContentWidth;
            imageHeight = imageHeight * scale;
        }

        Layout.AddKeepTogetherItem(PdfLayoutItem.FromImageBlock(new PdfImageBlock(image, imageWidth, imageHeight)));
        Layout.AddItem(new PdfPaperPublishingTextBlock(this, new PdfTextBlock(caption, CaptionStyle), caption, PdfPublishingMarkerKind.Figure));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    /// <summary>
    /// Adds a table to this paper.
    /// </summary>
    /// <param name="table">The table to use for this operation.</param>
    /// <param name="caption">The caption to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddTable(PdfReportTable table, string caption)
    {
        EnsureNotBuilt();
        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        if (caption == null)
        {
            throw new ArgumentNullException("caption");
        }

        Layout.AddTableBlock(table.CreateBlock(ContentWidth, HeadingStyle, BodyStyle));
        Layout.AddItem(new PdfPaperPublishingTextBlock(this, new PdfTextBlock(caption, CaptionStyle), caption, PdfPublishingMarkerKind.Table));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    /// <summary>
    /// Adds a citation to this paper.
    /// </summary>
    /// <param name="citation">The citation to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddCitation(PdfCitation citation)
    {
        EnsureNotBuilt();
        if (citation == null)
        {
            throw new ArgumentNullException("citation");
        }

        _citations.Add(citation);
    }

    /// <summary>
    /// Adds a bibliography entry to this paper.
    /// </summary>
    /// <param name="entry">The entry to use for this operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddBibliographyEntry(PdfBibliographyEntry entry)
    {
        EnsureNotBuilt();
        if (entry == null)
        {
            throw new ArgumentNullException("entry");
        }

        _bibliography.Add(entry);
    }

    /// <summary>
    /// Enables internal links from collected citations to matching bibliography entries.
    /// </summary>
    public void EnableCitationLinks()
    {
        EnsureNotBuilt();
        CitationLinksEnabled = true;
    }

    /// <summary>
    /// Includes a generated linked list of figures in the built document.
    /// </summary>
    public void IncludeListOfFigures()
    {
        EnsureNotBuilt();
        FigureListEnabled = true;
    }

    /// <summary>
    /// Includes a generated linked list of tables in the built document.
    /// </summary>
    public void IncludeListOfTables()
    {
        EnsureNotBuilt();
        TableListEnabled = true;
    }

    /// <summary>
    /// Adds an appendix to this paper.
    /// </summary>
    /// <param name="heading">The heading to use for this operation.</param>
    /// <param name="text">The text to measure, draw, or store.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void AddAppendix(string heading, string text)
    {
        EnsureNotBuilt();
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        AddSection("Appendix: " + heading);
        AddParagraph(text);
    }

    /// <summary>
    /// Adds an independently paginated two-column paper section.
    /// </summary>
    /// <param name="heading">The section heading.</param>
    /// <param name="paragraphs">The paragraphs flowed through the columns.</param>
    /// <param name="columnGap">The non-negative gap between columns in PDF points.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the column gap is invalid.</exception>
    /// <exception cref="ArgumentException">Thrown when a paragraph is null.</exception>
    public void AddTwoColumnSection(string heading, string[] paragraphs, double columnGap)
    {
        EnsureNotBuilt();
        if (heading == null)
        {
            throw new ArgumentNullException("heading");
        }

        if (paragraphs == null)
        {
            throw new ArgumentNullException("paragraphs");
        }

        if (double.IsNaN(columnGap) || double.IsInfinity(columnGap) || columnGap < 0 || columnGap >= ContentWidth)
        {
            throw new ArgumentOutOfRangeException("columnGap", "Column gap must be finite, non-negative, and smaller than the content width.");
        }

        string[] copiedParagraphs = new string[paragraphs.Length];
        for (int index = 0; index < paragraphs.Length; index++)
        {
            if (paragraphs[index] == null)
            {
                throw new ArgumentException("Paragraphs cannot contain null.", "paragraphs");
            }

            copiedParagraphs[index] = paragraphs[index];
        }

        _twoColumnSections.Add(new PdfPaperTwoColumnSection(heading, copiedParagraphs, columnGap));
    }

    /// <summary>
    /// Sets the running head rendered by the paper header template.
    /// </summary>
    /// <param name="text">The running-head text.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetRunningHead(string text)
    {
        EnsureNotBuilt();
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        Layout.RunningHead = text;
        Layout.SetHeaderTemplate(PdfPageTemplate.FromPageText("{running}", HeaderStyle, 12));
    }

    /// <summary>
    /// Configures logical labels for all generated paper pages.
    /// </summary>
    /// <param name="style">The page-label numbering style.</param>
    /// <param name="prefix">The text placed before each generated number.</param>
    /// <param name="startNumber">The positive first generated number.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the first number is not positive.</exception>
    /// <exception cref="ArgumentException">Thrown when the page-label configuration is unsupported.</exception>
    public void SetPageLabels(PdfPageLabelStyle style, string prefix, int startNumber)
    {
        EnsureNotBuilt();
        ValidatePageLabelStyle(style);
        if (prefix == null)
        {
            throw new ArgumentNullException("prefix");
        }

        if (startNumber <= 0)
        {
            throw new ArgumentOutOfRangeException("startNumber", "Page-label start number must be positive.");
        }

        if (style == PdfPageLabelStyle.None && prefix.Length == 0)
        {
            throw new ArgumentException("A prefix-only page label requires a non-empty prefix.", "prefix");
        }

        PageLabelsEnabled = true;
        PageLabelStyle = style;
        PageLabelPrefix = prefix;
        PageLabelStartNumber = startNumber;
    }

    /// <summary>
    /// Builds a paper from the values collected so far and returns the result.
    /// </summary>
    /// <returns>The newly built document.</returns>
    public PdfDocument Build()
    {
        if (!Built)
        {
            if (_twoColumnSections.Count == 0)
            {
                AddCollectedReferences();
            }
            else
            {
                AddStoredTwoColumnSections();
                AddCollectedReferenceSection();
            }

            Layout.Render();
            ApplyConfiguredPageLabels();
            ResolvePendingLinks();
            AddGeneratedPublishingLists();
            Built = true;
        }

        return Document;
    }

    internal void AddTitlePageHeading()
    {
        Layout.AddTextBlock(new PdfTextBlock(Title, TitleStyle));
        Layout.AddBlock(PdfBlock.WithSize(0, 10));
    }

    internal void AddCollectedReferences()
    {
        if (_endnotes.Count > 0)
        {
            AddSection("Endnotes");
            for (int index = 0; index < _endnotes.Count; index++)
            {
                AddParagraph("[" + (index + 1) + "] " + _endnotes[index]);
            }
        }

        if (_citations.Count > 0)
        {
            AddSection("Citations");
            for (int index = 0; index < _citations.Count; index++)
            {
                if (CitationLinksEnabled)
                {
                    string markerName = GetBibliographyMarkerName(_citations[index].Key);
                    Layout.AddItem(new PdfPaperLinkedTextBlock(this, new PdfTextBlock(_citations[index].Format(), BodyStyle), markerName));
                    Layout.AddBlock(PdfBlock.WithSize(0, 8));
                }
                else
                {
                    AddParagraph(_citations[index].Format());
                }
            }
        }

        if (_bibliography.Count > 0)
        {
            AddSection("Bibliography");
            for (int index = 0; index < _bibliography.Count; index++)
            {
                if (CitationLinksEnabled)
                {
                    string markerName = GetBibliographyMarkerName(_bibliography[index].Key);
                    PdfTextBlock block = new PdfTextBlock(_bibliography[index].Format(), BodyStyle);
                    Layout.AddItem(new PdfPaperAnchoredTextBlock(this, block, markerName, _bibliography[index].Key));
                    Layout.AddBlock(PdfBlock.WithSize(0, 8));
                }
                else
                {
                    AddParagraph(_bibliography[index].Format());
                }
            }
        }
    }

    internal void AddStoredTwoColumnSections()
    {
        for (int sectionIndex = 0; sectionIndex < _twoColumnSections.Count; sectionIndex++)
        {
            PdfPaperTwoColumnSection stored = _twoColumnSections[sectionIndex];
            PdfDocumentSection section = Layout.AddSection(PageSize, Margins);
            section.SetContentColumns(2, stored.ColumnGap);
            section.SetRunningHead(stored.Heading);
            section.SetHeaderTemplate(PdfPageTemplate.FromPageText("{running}", HeaderStyle, 12));
            section.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", FooterStyle, 12));
            section.AddTextBlock(new PdfTextBlock(stored.Heading, HeadingStyle));
            section.AddBlock(PdfBlock.WithSize(0, 4));
            for (int paragraphIndex = 0; paragraphIndex < stored.Paragraphs.Length; paragraphIndex++)
            {
                section.AddTextBlock(new PdfTextBlock(stored.Paragraphs[paragraphIndex], BodyStyle));
                section.AddBlock(PdfBlock.WithSize(0, 8));
            }
        }
    }

    internal void AddCollectedReferenceSection()
    {
        if (_endnotes.Count == 0 && _citations.Count == 0 && _bibliography.Count == 0)
        {
            return;
        }

        PdfDocumentSection section = Layout.AddSection(PageSize, Margins);
        section.SetRunningHead(Title);
        section.SetHeaderTemplate(PdfPageTemplate.FromPageText("{running}", HeaderStyle, 12));
        section.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", FooterStyle, 12));
        if (_endnotes.Count > 0)
        {
            AddSectionHeadingToReferenceSection(section, "Endnotes");
            for (int index = 0; index < _endnotes.Count; index++)
            {
                AddParagraphToReferenceSection(section, "[" + (index + 1) + "] " + _endnotes[index]);
            }
        }

        if (_citations.Count > 0)
        {
            AddSectionHeadingToReferenceSection(section, "Citations");
            for (int index = 0; index < _citations.Count; index++)
            {
                PdfTextBlock citationBlock = new PdfTextBlock(_citations[index].Format(), BodyStyle);
                if (CitationLinksEnabled)
                {
                    section.AddItem(new PdfPaperLinkedTextBlock(this, citationBlock, GetBibliographyMarkerName(_citations[index].Key)));
                }
                else
                {
                    section.AddTextBlock(citationBlock);
                }

                section.AddBlock(PdfBlock.WithSize(0, 8));
            }
        }

        if (_bibliography.Count > 0)
        {
            AddSectionHeadingToReferenceSection(section, "Bibliography");
            for (int index = 0; index < _bibliography.Count; index++)
            {
                PdfTextBlock bibliographyBlock = new PdfTextBlock(_bibliography[index].Format(), BodyStyle);
                if (CitationLinksEnabled)
                {
                    section.AddItem(new PdfPaperAnchoredTextBlock(this, bibliographyBlock, GetBibliographyMarkerName(_bibliography[index].Key), _bibliography[index].Key));
                }
                else
                {
                    section.AddTextBlock(bibliographyBlock);
                }

                section.AddBlock(PdfBlock.WithSize(0, 8));
            }
        }
    }

    internal void AddSectionHeadingToReferenceSection(PdfDocumentSection section, string heading)
    {
        section.AddTextBlock(new PdfTextBlock(heading, HeadingStyle));
        section.AddBlock(PdfBlock.WithSize(0, 4));
    }

    internal void AddParagraphToReferenceSection(PdfDocumentSection section, string text)
    {
        section.AddTextBlock(new PdfTextBlock(text, BodyStyle));
        section.AddBlock(PdfBlock.WithSize(0, 8));
    }

    internal void RegisterCrossReferenceTarget(string markerName, string markerText, PdfPage page, double x, double y)
    {
        Document.AddCrossReferenceMarker(markerName, markerText, page, x, y);
    }

    internal void RegisterPendingLink(string markerName, PdfPage page, double x, double y, double width, double height)
    {
        double linkHeight = height;
        if (linkHeight <= 0)
        {
            linkHeight = 1;
        }

        _pendingLinks.Add(new PdfPaperPendingLink(markerName, page, new PdfRectangle(x, y, width, linkHeight)));
    }

    internal void RegisterPublishingTarget(PdfPublishingMarkerKind kind, string title, PdfPage page, double x, double y)
    {
        if (kind == PdfPublishingMarkerKind.Figure && FigureListEnabled)
        {
            Document.AddFigureMarker(title, page, x, y);
        }

        if (kind == PdfPublishingMarkerKind.Table && TableListEnabled)
        {
            Document.AddTableMarker(title, page, x, y);
        }
    }

    internal void ResolvePendingLinks()
    {
        for (int index = 0; index < _pendingLinks.Count; index++)
        {
            PdfPaperPendingLink pending = _pendingLinks[index];
            Document.AddCrossReferenceLink(pending.Page, pending.MarkerName, pending.Rectangle);
        }
    }

    internal void ApplyConfiguredPageLabels()
    {
        if (PageLabelsEnabled)
        {
            Document.AddPageLabelRange(0, PageLabelStyle, PageLabelPrefix, PageLabelStartNumber);
        }
    }

    internal void AddGeneratedPublishingLists()
    {
        if (FigureListEnabled)
        {
            PdfGeneratedList figures = new PdfGeneratedList(PdfPublishingMarkerKind.Figure);
            figures.InsertPageWithLayout(Document, Document.Pages.Count, PageSize, Margins);
        }

        if (TableListEnabled)
        {
            PdfGeneratedList tables = new PdfGeneratedList(PdfPublishingMarkerKind.Table);
            tables.InsertPageWithLayout(Document, Document.Pages.Count, PageSize, Margins);
        }
    }

    internal string GetBibliographyMarkerName(string key)
    {
        return "paper-bibliography-" + key;
    }

    internal void InitializeStyles()
    {
        TitleStyle = PdfParagraphStyle.Create(PdfTextStyle.WithFont(22, PdfFont.Bold), 27, 0, 0, 0, PdfTextAlignment.Center);
        AuthorStyle = PdfParagraphStyle.Create(PdfTextStyle.WithFont(10, PdfFont.Regular), 13, 0, 0, 0, PdfTextAlignment.Center);
        HeadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(13, PdfFont.Bold), 16, 0, 0, 0);
        SecondHeadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(11, PdfFont.Bold), 14, 0, 0, 0);
        ThirdHeadingStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Bold), 13, 0, 0, 0);
        BodyStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(10, PdfFont.Regular), 14, 0, 0, 0);
        CaptionStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.35)), 11, 0, 0, 0);
        NoteStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithFont(8, PdfFont.Regular), 11, 12, 0, 0);
        HeaderStyle = PdfParagraphStyle.WithMetrics(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.35)), 10, 0, 0, 0);
        FooterStyle = PdfParagraphStyle.Create(PdfTextStyle.WithColor(8, PdfFont.Regular, PdfColor.Gray(0.35)), 10, 0, 0, 0, PdfTextAlignment.Right);
    }

    internal void ApplyPageFurniture()
    {
        Layout.SetHeaderTemplate(PdfPageTemplate.FromTextBlock(new PdfTextBlock(Title, HeaderStyle), 12));
        Layout.SetFooterTemplate(PdfPageTemplate.FromPageText("Page {page} of {total}", FooterStyle, 12));
    }

    internal PdfParagraphStyle GetHeadingStyle(PdfPaperHeadingLevel level)
    {
        if (level == PdfPaperHeadingLevel.Level1)
        {
            return HeadingStyle;
        }

        if (level == PdfPaperHeadingLevel.Level2)
        {
            return SecondHeadingStyle;
        }

        if (level == PdfPaperHeadingLevel.Level3)
        {
            return ThirdHeadingStyle;
        }

        throw new ArgumentException("Paper heading level is not supported.", "level");
    }

    internal void ValidateRequiredText(string text, string name)
    {
        if (text == null)
        {
            throw new ArgumentNullException(name);
        }

        if (text.Length == 0)
        {
            throw new ArgumentException("Text cannot be empty.", name);
        }
    }

    internal void ValidatePageLabelStyle(PdfPageLabelStyle style)
    {
        switch (style)
        {
            case PdfPageLabelStyle.None:
            case PdfPageLabelStyle.Decimal:
            case PdfPageLabelStyle.UpperRoman:
            case PdfPageLabelStyle.LowerRoman:
            case PdfPageLabelStyle.UpperLetters:
            case PdfPageLabelStyle.LowerLetters:
                return;
            default:
                throw new ArgumentException("Page-label style is not supported.", "style");
        }
    }

    internal void EnsureNotBuilt()
    {
        if (Built)
        {
            throw new InvalidOperationException("Paper has already been built.");
        }
    }
}
