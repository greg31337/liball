namespace docx;

internal class DocxTextFinder
{
    internal static List<DocxTextMatch> Find(DocxDocument document, string searchText)
    {
        DocxTextOptions options = new DocxTextOptions();
        return Find(document, searchText, options);
    }

    internal static List<DocxTextMatch> Find(DocxDocument document, string searchText, DocxTextOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        return Find(document.Body, searchText, options);
    }

    internal static List<DocxTextMatch> Find(DocxBody body, string searchText)
    {
        DocxTextOptions options = new DocxTextOptions();
        return Find(body, searchText, options);
    }

    internal static List<DocxTextMatch> Find(DocxBody body, string searchText, DocxTextOptions options)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        ValidateSearchText(searchText);

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxParagraph> paragraphs = GetParagraphs(body);
        List<DocxTextMatch> matches = new List<DocxTextMatch>();
        int paragraphIndex = 0;

        foreach (DocxParagraph paragraph in paragraphs)
        {
            AddParagraphMatches(matches, paragraph, paragraphIndex, searchText, options);
            paragraphIndex = paragraphIndex + 1;
        }

        return matches;
    }

    internal static List<DocxTextMatch> Find(DocxParagraph paragraph, string searchText)
    {
        DocxTextOptions options = new DocxTextOptions();
        return Find(paragraph, searchText, options);
    }

    internal static List<DocxTextMatch> Find(DocxParagraph paragraph, string searchText, DocxTextOptions options)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        ValidateSearchText(searchText);

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxTextMatch> matches = new List<DocxTextMatch>();
        AddParagraphMatches(matches, paragraph, 0, searchText, options);
        return matches;
    }

    internal static bool Contains(DocxParagraph paragraph, string searchText)
    {
        return Find(paragraph, searchText).Count > 0;
    }

    internal static bool Contains(DocxBody body, string searchText)
    {
        return Find(body, searchText).Count > 0;
    }

    internal static bool Contains(DocxDocument document, string searchText)
    {
        return Find(document, searchText).Count > 0;
    }

    internal static void AddParagraphMatches(List<DocxTextMatch> matches, DocxParagraph paragraph, int paragraphIndex, string searchText, DocxTextOptions options)
    {
        if (matches == null)
        {
            throw new ArgumentNullException("matches");
        }

        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        ValidateSearchText(searchText);

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<DocxRun> runs = new List<DocxRun>();
        List<string> runTexts = new List<string>();
        List<int> runStartIndexes = new List<int>();
        string paragraphText = BuildParagraphSearchText(paragraph, options, runs, runTexts, runStartIndexes);
        int matchIndex = paragraphText.IndexOf(searchText, 0, StringComparison.Ordinal);

        while (matchIndex >= 0)
        {
            int matchEndIndex = matchIndex + searchText.Length - 1;
            int startRunIndex = FindRunIndex(runStartIndexes, runTexts, matchIndex);
            int endRunIndex = FindRunIndex(runStartIndexes, runTexts, matchEndIndex);

            if (startRunIndex >= 0 && endRunIndex >= 0)
            {
                DocxTextMatch match = new DocxTextMatch(
                    paragraph,
                    paragraphIndex,
                    matchIndex,
                    searchText.Length,
                    startRunIndex,
                    matchIndex - runStartIndexes[startRunIndex],
                    endRunIndex,
                    matchEndIndex - runStartIndexes[endRunIndex],
                    searchText,
                    runs[startRunIndex],
                    runs[endRunIndex]);
                matches.Add(match);
            }

            matchIndex = paragraphText.IndexOf(searchText, matchIndex + 1, StringComparison.Ordinal);
        }
    }

    internal static string BuildParagraphSearchText(DocxParagraph paragraph, DocxTextOptions options, List<DocxRun> runs, List<string> runTexts, List<int> runStartIndexes)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        if (runs == null)
        {
            throw new ArgumentNullException("runs");
        }

        if (runTexts == null)
        {
            throw new ArgumentNullException("runTexts");
        }

        if (runStartIndexes == null)
        {
            throw new ArgumentNullException("runStartIndexes");
        }

        string text = "";

        foreach (DocxXmlBuffer child in paragraph.Children)
        {
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                text = text + AddRunText(runs, runTexts, runStartIndexes, DocxParagraph.DocxRunFromBuffer(child), options, text.Length);
            }
            else if (reader.IsElement("hyperlink", DocxConstants.NamespaceWordprocessingMl))
            {
                DocxHyperlink hyperlink = DocxParagraph.DocxHyperlinkFromBuffer(child);
                text = text + AddRunListText(runs, runTexts, runStartIndexes, hyperlink.Runs, options, text.Length);
            }
            else if (DocxField.IsSupported(reader) && options.IncludeFieldResults)
            {
                DocxField field = DocxParagraph.DocxFieldFromBuffer(child);
                text = text + AddRunListText(runs, runTexts, runStartIndexes, field.Runs, options, text.Length);
            }
            else if (DocxContentControl.IsSupported(reader))
            {
                DocxContentControl contentControl = DocxParagraph.DocxContentControlFromBuffer(child);
                text = text + AddRunListText(runs, runTexts, runStartIndexes, contentControl.Runs, options, text.Length);
            }
        }

        return text;
    }

    internal static string AddRunListText(List<DocxRun> runs, List<string> runTexts, List<int> runStartIndexes, List<DocxRun> values, DocxTextOptions options, int startIndex)
    {
        if (values == null)
        {
            throw new ArgumentNullException("values");
        }

        string text = "";

        foreach (DocxRun run in values)
        {
            text = text + AddRunText(runs, runTexts, runStartIndexes, run, options, startIndex + text.Length);
        }

        return text;
    }

    internal static string AddRunText(List<DocxRun> runs, List<string> runTexts, List<int> runStartIndexes, DocxRun run, DocxTextOptions options, int startIndex)
    {
        if (runs == null)
        {
            throw new ArgumentNullException("runs");
        }

        if (runTexts == null)
        {
            throw new ArgumentNullException("runTexts");
        }

        if (runStartIndexes == null)
        {
            throw new ArgumentNullException("runStartIndexes");
        }

        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        string text = run.GetTextWithOptions(options);

        if (text != "")
        {
            runs.Add(run);
            runTexts.Add(text);
            runStartIndexes.Add(startIndex);
        }

        return text;
    }

    internal static int FindRunIndex(List<int> runStartIndexes, List<string> runTexts, int characterIndex)
    {
        if (runStartIndexes == null)
        {
            throw new ArgumentNullException("runStartIndexes");
        }

        if (runTexts == null)
        {
            throw new ArgumentNullException("runTexts");
        }

        int index = 0;

        while (index < runTexts.Count)
        {
            int runStartIndex = runStartIndexes[index];
            if (characterIndex >= runStartIndex
                && characterIndex - runStartIndex < runTexts[index].Length)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static List<DocxParagraph> GetParagraphs(DocxBody body)
    {
        if (body == null)
        {
            throw new ArgumentNullException("body");
        }

        List<DocxParagraph> paragraphs = new List<DocxParagraph>();

        foreach (DocxBlock block in body.Blocks)
        {
            AddBlockParagraphs(paragraphs, block);
        }

        return paragraphs;
    }

    internal static void AddBlockParagraphs(List<DocxParagraph> paragraphs, DocxBlock block)
    {
        if (paragraphs == null)
        {
            throw new ArgumentNullException("paragraphs");
        }

        if (block == null)
        {
            throw new ArgumentNullException("block");
        }

        if (block.Paragraph != null)
        {
            paragraphs.Add(block.Paragraph);
        }
        else if (block.Table != null)
        {
            AddTableParagraphs(paragraphs, block.Table);
        }
        else if (block.ContentControl != null)
        {
            AddContentControlParagraphs(paragraphs, block.ContentControl);
        }
    }

    internal static void AddTableParagraphs(List<DocxParagraph> paragraphs, DocxTable table)
    {
        if (paragraphs == null)
        {
            throw new ArgumentNullException("paragraphs");
        }

        if (table == null)
        {
            throw new ArgumentNullException("table");
        }

        foreach (DocxTableRow row in table.Rows)
        {
            foreach (DocxTableCell cell in row.Cells)
            {
                foreach (DocxParagraph paragraph in cell.Paragraphs)
                {
                    paragraphs.Add(paragraph);
                }

                foreach (DocxTable nestedTable in cell.Tables)
                {
                    AddTableParagraphs(paragraphs, nestedTable);
                }
            }
        }
    }

    internal static void AddContentControlParagraphs(List<DocxParagraph> paragraphs, DocxContentControl contentControl)
    {
        if (paragraphs == null)
        {
            throw new ArgumentNullException("paragraphs");
        }

        if (contentControl == null)
        {
            throw new ArgumentNullException("contentControl");
        }

        foreach (DocxBlock block in contentControl.Blocks)
        {
            AddBlockParagraphs(paragraphs, block);
        }
    }

    internal static void ValidateSearchText(string searchText)
    {
        if (searchText == null)
        {
            throw new ArgumentNullException("searchText");
        }

        if (searchText == "")
        {
            throw new ArgumentException("Search text is required.", "searchText");
        }
    }
}
