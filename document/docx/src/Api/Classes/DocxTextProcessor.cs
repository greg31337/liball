namespace docx;

/// <summary>
/// Extracts, traverses, searches, replaces, and imports document text.
/// </summary>
public class DocxTextProcessor : IDocxTextProcessor
{
    /// <summary>
    /// Creates a text processor.
    /// </summary>
    public DocxTextProcessor()
    {
    }

    /// <summary>
    /// Extracts text from one text-bearing model with explicit options.
    /// </summary>
    /// <param name="content">The content to extract.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The extracted text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string ExtractContent(IDocxTextContent content, DocxTextOptions options)
    {
        return DocxTextExtractor.Extract(content, options);
    }

    /// <summary>
    /// Extracts selected document stories with explicit options.
    /// </summary>
    /// <param name="document">The document to extract.</param>
    /// <param name="options">The extraction and story-selection options.</param>
    /// <returns>The extracted text in story order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public string ExtractDocument(DocxDocument document, DocxTextOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = "";
        bool hasStory = false;

        foreach (DocxStory story in document.GetStories())
        {
            if (ShouldIncludeStory(story.Type, options))
            {
                if (hasStory)
                {
                    text = text + options.ParagraphSeparator;
                }

                text = text + story.Content.GetTextWithOptions(options);
                hasStory = true;
            }
        }

        return text;
    }

    /// <summary>
    /// Traverses the direct text-producing children of a run.
    /// </summary>
    /// <param name="run">The run to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public List<DocxTextToken> GetRunTokens(DocxRun run, DocxTextOptions options)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxTextToken> tokens = new List<DocxTextToken>();
        AddRunTokens(tokens, run, null, DocxStoryType.MainDocument, options);
        return tokens;
    }

    /// <summary>
    /// Traverses the text-producing children of a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public List<DocxTextToken> GetParagraphTokens(DocxParagraph paragraph, DocxTextOptions options)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxTextToken> tokens = new List<DocxTextToken>();
        AddParagraphTokens(tokens, paragraph, DocxStoryType.MainDocument, options);
        return tokens;
    }

    /// <summary>
    /// Traverses text in body order.
    /// </summary>
    /// <param name="body">The body to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public List<DocxTextToken> GetBodyTokens(DocxBody body, DocxTextOptions options)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxTextToken> tokens = new List<DocxTextToken>();
        AddBodyTokens(tokens, body, DocxStoryType.MainDocument, options);
        return tokens;
    }

    /// <summary>
    /// Traverses selected document stories in story order.
    /// </summary>
    /// <param name="document">The document to traverse.</param>
    /// <param name="options">The extraction and story-selection options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public List<DocxTextToken> GetDocumentTokens(DocxDocument document, DocxTextOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxTextToken> tokens = new List<DocxTextToken>();
        bool hasStory = false;

        foreach (DocxStory story in document.GetStories())
        {
            if (ShouldIncludeStory(story.Type, options))
            {
                if (hasStory)
                {
                    tokens.Add(CreateToken(DocxTextTokenKind.StorySeparator, DocxBreak.TextOrEmpty(options.ParagraphSeparator), false, story.Type, null, null, null));
                }

                if (story.Type == DocxStoryType.MainDocument)
                {
                    AddBodyTokens(tokens, document.Body, story.Type, options);
                }
                else
                {
                    tokens.Add(CreateToken(DocxTextTokenKind.CompositeText, story.Content.GetTextWithOptions(options), false, story.Type, null, null, null));
                }

                hasStory = true;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Finds body text, including matches spanning adjacent runs.
    /// </summary>
    /// <param name="body">The body to search.</param>
    /// <param name="searchText">The text to find.</param>
    /// <param name="options">The search extraction options.</param>
    /// <returns>The matching ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    public List<DocxTextMatch> FindInBody(DocxBody body, string searchText, DocxTextOptions options)
    {
        return DocxTextFinder.Find(body, searchText, options);
    }

    /// <summary>
    /// Finds paragraph text, including matches spanning adjacent runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to search.</param>
    /// <param name="searchText">The text to find.</param>
    /// <param name="options">The search extraction options.</param>
    /// <returns>The matching ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    public List<DocxTextMatch> FindInParagraph(DocxParagraph paragraph, string searchText, DocxTextOptions options)
    {
        return DocxTextFinder.Find(paragraph, searchText, options);
    }

    /// <summary>
    /// Replaces paragraph text across text nodes and adjacent runs while retaining run formatting.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="searchText">The text to replace.</param>
    /// <param name="replacementText">The replacement text.</param>
    /// <returns>The number of replacements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    public int ReplaceInParagraph(DocxParagraph paragraph, string searchText, string replacementText)
    {
        return DocxTextReplacer.Replace(paragraph, searchText, replacementText);
    }

    /// <summary>
    /// Replaces run text across adjacent text nodes.
    /// </summary>
    /// <param name="run">The run to edit.</param>
    /// <param name="searchText">The text to replace.</param>
    /// <param name="replacementText">The replacement text.</param>
    /// <returns>The number of replacements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    public int ReplaceInRun(DocxRun run, string searchText, string replacementText)
    {
        return DocxTextReplacer.Replace(run, searchText, replacementText);
    }

    /// <summary>
    /// Imports plain text as a body, using line endings for paragraphs and tab characters for tab controls.
    /// </summary>
    /// <param name="text">The plain text to import.</param>
    /// <returns>The imported body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    public DocxBody ImportPlainText(string text)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        DocxContentFactory factory = new DocxContentFactory();
        DocxBody body = factory.CreateBody();
        string normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        string[] lines = normalized.Split(new string[] { "\n" }, StringSplitOptions.None);

        foreach (string line in lines)
        {
            DocxParagraph paragraph = CreatePlainTextParagraph(line, factory);
            body.AddBlock(factory.CreateParagraphBlock(paragraph));
        }

        return body;
    }

    internal static bool ShouldIncludeStory(DocxStoryType storyType, DocxTextOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (storyType == DocxStoryType.MainDocument)
        {
            return true;
        }

        if (storyType == DocxStoryType.Header || storyType == DocxStoryType.Footer)
        {
            return options.IncludeHeadersAndFooters;
        }

        if (storyType == DocxStoryType.Footnotes || storyType == DocxStoryType.Endnotes)
        {
            return options.IncludeFootnotesAndEndnotes;
        }

        if (storyType == DocxStoryType.Comments)
        {
            return options.IncludeComments;
        }

        return false;
    }

    internal static DocxTextToken CreateToken(DocxTextTokenKind kind, string text, bool preserveSpace, DocxStoryType storyType, DocxParagraph? paragraph, DocxRun? run, DocxText? textNode)
    {
        return new DocxTextToken(kind, text, preserveSpace, storyType, paragraph, run, textNode);
    }

    internal static void AddRunTokens(List<DocxTextToken> tokens, DocxRun run, DocxParagraph? paragraph, DocxStoryType storyType, DocxTextOptions options)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        foreach (DocxXmlBuffer buffer in run.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(buffer.GetXml());

            if (!reader.MoveToContent())
            {
                continue;
            }

            if (reader.IsElement("delText", DocxConstants.NamespaceWordprocessingMl))
            {
                if (options.IncludeDeletedText)
                {
                    string deletedText = reader.Reader.ReadElementContentAsString();
                    tokens.Add(CreateToken(DocxTextTokenKind.RevisionText, deletedText, false, storyType, paragraph, run, null));
                }

                continue;
            }

            DocxRunChild child = new DocxRunChild(buffer.GetXml());

            if (child.Kind == DocxRunChildKind.Text && child.Text != null)
            {
                tokens.Add(CreateToken(DocxTextTokenKind.Text, child.Text.Value, child.Text.PreserveSpace, storyType, paragraph, run, child.Text));
            }
            else if (child.Kind == DocxRunChildKind.Tab && child.Break != null)
            {
                AddBreakToken(tokens, DocxTextTokenKind.Tab, child.Break, paragraph, run, storyType, options);
            }
            else if (child.Kind == DocxRunChildKind.CarriageReturn && child.Break != null)
            {
                AddBreakToken(tokens, DocxTextTokenKind.CarriageReturn, child.Break, paragraph, run, storyType, options);
            }
            else if (child.Kind == DocxRunChildKind.Break && child.Break != null)
            {
                AddBreakElementToken(tokens, child.Break, paragraph, run, storyType, options);
            }
            else if (child.Kind == DocxRunChildKind.Hyphen && child.Break != null)
            {
                AddHyphenToken(tokens, child.Break, paragraph, run, storyType, options);
            }
            else if (child.Kind == DocxRunChildKind.Symbol && child.Break != null)
            {
                AddBreakToken(tokens, DocxTextTokenKind.Symbol, child.Break, paragraph, run, storyType, options);
            }
            else if (child.Kind == DocxRunChildKind.Revision)
            {
                DocxRevision revision = DocxRevision.ParseXml(child.Xml);
                string revisionText = revision.GetTextWithOptions(options);

                if (revisionText != "")
                {
                    tokens.Add(CreateToken(DocxTextTokenKind.RevisionText, revisionText, false, storyType, paragraph, run, null));
                }
            }
        }
    }

    internal static void AddBreakToken(List<DocxTextToken> tokens, DocxTextTokenKind kind, DocxBreak value, DocxParagraph? paragraph, DocxRun run, DocxStoryType storyType, DocxTextOptions options)
    {
        tokens.Add(CreateToken(kind, value.GetTextWithOptions(options), false, storyType, paragraph, run, null));
    }

    internal static void AddBreakElementToken(List<DocxTextToken> tokens, DocxBreak value, DocxParagraph? paragraph, DocxRun run, DocxStoryType storyType, DocxTextOptions options)
    {
        DocxTextTokenKind kind = DocxTextTokenKind.LineBreak;

        if (value.Kind == DocxBreak.KindPageBreak)
        {
            kind = DocxTextTokenKind.PageBreak;
        }
        else if (value.Kind == DocxBreak.KindColumnBreak)
        {
            kind = DocxTextTokenKind.ColumnBreak;
        }

        AddBreakToken(tokens, kind, value, paragraph, run, storyType, options);
    }

    internal static void AddHyphenToken(List<DocxTextToken> tokens, DocxBreak value, DocxParagraph? paragraph, DocxRun run, DocxStoryType storyType, DocxTextOptions options)
    {
        DocxTextTokenKind kind = DocxTextTokenKind.SoftHyphen;

        if (value.Kind == DocxBreak.KindNoBreakHyphen)
        {
            kind = DocxTextTokenKind.NoBreakHyphen;
        }

        AddBreakToken(tokens, kind, value, paragraph, run, storyType, options);
    }

    internal static void AddParagraphTokens(List<DocxTextToken> tokens, DocxParagraph paragraph, DocxStoryType storyType, DocxTextOptions options)
    {
        if (tokens == null)
        {
            throw new ArgumentNullException("tokens");
        }

        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        foreach (DocxXmlBuffer child in paragraph.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (!reader.MoveToContent())
            {
                continue;
            }

            if (reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                AddRunTokens(tokens, DocxParagraph.DocxRunFromBuffer(child), paragraph, storyType, options);
            }
            else if (reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxHyperlink hyperlink = DocxParagraph.DocxHyperlinkFromBuffer(child);

                foreach (DocxRun run in hyperlink.Runs)
                {
                    AddRunTokens(tokens, run, paragraph, storyType, options);
                }
            }
            else if (DocxField.IsSupported(reader) && options.IncludeFieldResults)
            {
                int tokenStart = tokens.Count;
                DocxField field = DocxParagraph.DocxFieldFromBuffer(child);

                foreach (DocxRun run in field.Runs)
                {
                    AddRunTokens(tokens, run, paragraph, storyType, options);
                }

                SetTokenKinds(tokens, tokenStart, DocxTextTokenKind.FieldResult);
            }
            else if (DocxRevision.IsSupported(reader))
            {
                string revisionText = DocxParagraph.DocxRevisionFromBuffer(child).GetTextWithOptions(options);

                if (revisionText != "")
                {
                    tokens.Add(CreateToken(DocxTextTokenKind.RevisionText, revisionText, false, storyType, paragraph, null, null));
                }
            }
            else if (DocxContentControl.IsSupported(reader))
            {
                string contentText = DocxParagraph.DocxContentControlFromBuffer(child).GetTextWithOptions(options);
                tokens.Add(CreateToken(DocxTextTokenKind.CompositeText, contentText, false, storyType, paragraph, null, null));
            }
        }
    }

    internal static void SetTokenKinds(List<DocxTextToken> tokens, int startIndex, DocxTextTokenKind kind)
    {
        int index = startIndex;

        while (index < tokens.Count)
        {
            tokens[index].Kind = kind;
            index = index + 1;
        }
    }

    internal static void AddBodyTokens(List<DocxTextToken> tokens, DocxBody body, DocxStoryType storyType, DocxTextOptions options)
    {
        bool hasBlock = false;

        foreach (DocxBlock block in body.Blocks)
        {
            if (block.IsPreserved())
            {
                continue;
            }

            if (hasBlock)
            {
                tokens.Add(CreateToken(DocxTextTokenKind.ParagraphSeparator, DocxBreak.TextOrEmpty(options.ParagraphSeparator), false, storyType, null, null, null));
            }

            if (block.Paragraph != null)
            {
                AddParagraphTokens(tokens, block.Paragraph, storyType, options);
            }
            else
            {
                tokens.Add(CreateToken(DocxTextTokenKind.CompositeText, block.GetTextWithOptions(options), false, storyType, null, null, null));
            }

            hasBlock = true;
        }
    }

    internal static DocxParagraph CreatePlainTextParagraph(string line, DocxContentFactory factory)
    {
        DocxParagraph paragraph = factory.CreateParagraph();
        DocxRun run = factory.CreateRun();
        int startIndex = 0;
        int tabIndex = line.IndexOf("\t", startIndex, StringComparison.Ordinal);

        while (tabIndex >= 0)
        {
            if (tabIndex > startIndex)
            {
                run.InsertTextNode(run.ChildCount(), factory.CreateText(line.Substring(startIndex, tabIndex - startIndex)));
            }

            run.InsertBreak(run.ChildCount(), factory.CreateTab());
            startIndex = tabIndex + 1;
            tabIndex = line.IndexOf("\t", startIndex, StringComparison.Ordinal);
        }

        if (startIndex < line.Length || run.ChildCount() == 0)
        {
            run.InsertTextNode(run.ChildCount(), factory.CreateText(line.Substring(startIndex)));
        }

        paragraph.InsertRun(0, run);
        return paragraph;
    }
}
