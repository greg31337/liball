namespace docx;

internal class DocxTextReplacer
{
    internal static int Replace(DocxParagraph paragraph, string searchText, string replacementText)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        ValidateSearchText(searchText);

        if (replacementText == null)
        {
            throw new ArgumentNullException("replacementText");
        }

        List<string> runTexts = new List<string>();
        List<int> runStartIndexes = new List<int>();
        string text = BuildParagraphRunText(paragraph, runTexts, runStartIndexes);
        List<int> matchIndexes = FindMatchIndexes(text, searchText);

        if (matchIndexes.Count == 0)
        {
            return 0;
        }

        int matchIndex = matchIndexes.Count - 1;

        while (matchIndex >= 0)
        {
            ReplaceParagraphMatch(paragraph, runTexts, runStartIndexes, matchIndexes[matchIndex], searchText.Length, replacementText);
            matchIndex = matchIndex - 1;
        }

        RefreshParagraphRunChildren(paragraph);
        paragraph.Xml = paragraph.WriteXml();
        return matchIndexes.Count;
    }

    internal static int Replace(DocxRun run, string searchText, string replacementText)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        ValidateSearchText(searchText);

        if (replacementText == null)
        {
            throw new ArgumentNullException("replacementText");
        }

        List<int> textStartIndexes = new List<int>();
        string text = BuildRunText(run, textStartIndexes);
        List<int> matchIndexes = FindMatchIndexes(text, searchText);

        if (matchIndexes.Count == 0)
        {
            return 0;
        }

        int matchIndex = matchIndexes.Count - 1;

        while (matchIndex >= 0)
        {
            ReplaceMatch(run, textStartIndexes, matchIndexes[matchIndex], searchText.Length, replacementText);
            matchIndex = matchIndex - 1;
        }

        RefreshRunTextChildren(run);
        run.Xml = run.WriteXml();
        run.NotifyParentParagraph();
        return matchIndexes.Count;
    }

    internal static string BuildParagraphRunText(DocxParagraph paragraph, List<string> runTexts, List<int> runStartIndexes)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
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

        foreach (DocxRun run in paragraph.Runs)
        {
            string runText = BuildRunText(run, new List<int>());

            if (runText != "")
            {
                runTexts.Add(runText);
                runStartIndexes.Add(text.Length);
            }
            else
            {
                runTexts.Add("");
                runStartIndexes.Add(text.Length);
            }

            text = text + runText;
        }

        return text;
    }

    internal static void ReplaceParagraphMatch(DocxParagraph paragraph, List<string> runTexts, List<int> runStartIndexes, int matchIndex, int length, string replacementText)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        if (runTexts == null)
        {
            throw new ArgumentNullException("runTexts");
        }

        if (runStartIndexes == null)
        {
            throw new ArgumentNullException("runStartIndexes");
        }

        if (replacementText == null)
        {
            throw new ArgumentNullException("replacementText");
        }

        if (matchIndex < 0)
        {
            throw new ArgumentOutOfRangeException("matchIndex");
        }

        if (length <= 0)
        {
            throw new ArgumentException("Length must be positive.", "length");
        }

        long matchEnd = (long)matchIndex + length - 1;

        if (matchEnd > int.MaxValue)
        {
            return;
        }

        int matchEndIndex = (int)matchEnd;
        int startRunIndex = FindRunIndex(runStartIndexes, runTexts, matchIndex);
        int endRunIndex = FindRunIndex(runStartIndexes, runTexts, matchEndIndex);

        if (startRunIndex < 0 || endRunIndex < 0)
        {
            return;
        }

        int startRunOffset = matchIndex - runStartIndexes[startRunIndex];
        int endRunOffset = matchEndIndex - runStartIndexes[endRunIndex];

        if (startRunIndex == endRunIndex)
        {
            ReplaceRunRange(paragraph.Runs[startRunIndex], startRunOffset, length, replacementText);
            return;
        }

        ReplaceRunRange(paragraph.Runs[startRunIndex], startRunOffset, runTexts[startRunIndex].Length - startRunOffset, replacementText);
        ReplaceRunRange(paragraph.Runs[endRunIndex], 0, endRunOffset + 1, "");

        int runIndex = startRunIndex + 1;

        while (runIndex < endRunIndex)
        {
            ReplaceRunRange(paragraph.Runs[runIndex], 0, runTexts[runIndex].Length, "");
            runIndex = runIndex + 1;
        }
    }

    internal static void ReplaceRunRange(DocxRun run, int matchIndex, int length, string replacementText)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (replacementText == null)
        {
            throw new ArgumentNullException("replacementText");
        }

        if (matchIndex < 0)
        {
            throw new ArgumentOutOfRangeException("matchIndex");
        }

        if (length < 0)
        {
            throw new ArgumentException("Length cannot be negative.", "length");
        }

        if (length == 0)
        {
            return;
        }

        List<int> textStartIndexes = new List<int>();
        BuildRunText(run, textStartIndexes);
        ReplaceMatch(run, textStartIndexes, matchIndex, length, replacementText);
        RefreshRunTextChildren(run);
        run.Xml = run.WriteXml();
    }

    internal static string BuildRunText(DocxRun run, List<int> textStartIndexes)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (textStartIndexes == null)
        {
            throw new ArgumentNullException("textStartIndexes");
        }

        string text = "";

        foreach (DocxText textNode in run.Texts)
        {
            textStartIndexes.Add(text.Length);
            text = text + textNode.GetText();
        }

        return text;
    }

    internal static List<int> FindMatchIndexes(string text, string searchText)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        ValidateSearchText(searchText);

        List<int> matchIndexes = new List<int>();
        int matchIndex = text.IndexOf(searchText, 0, StringComparison.Ordinal);

        while (matchIndex >= 0)
        {
            matchIndexes.Add(matchIndex);
            matchIndex = text.IndexOf(searchText, matchIndex + searchText.Length, StringComparison.Ordinal);
        }

        return matchIndexes;
    }

    internal static void ReplaceMatch(DocxRun run, List<int> textStartIndexes, int matchIndex, int length, string replacementText)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (textStartIndexes == null)
        {
            throw new ArgumentNullException("textStartIndexes");
        }

        if (replacementText == null)
        {
            throw new ArgumentNullException("replacementText");
        }

        if (matchIndex < 0)
        {
            throw new ArgumentOutOfRangeException("matchIndex");
        }

        if (length <= 0)
        {
            throw new ArgumentException("Length must be positive.", "length");
        }

        long matchEnd = (long)matchIndex + length - 1;

        if (matchEnd > int.MaxValue)
        {
            return;
        }

        int matchEndIndex = (int)matchEnd;
        int startTextIndex = FindTextNodeIndex(run, textStartIndexes, matchIndex);
        int endTextIndex = FindTextNodeIndex(run, textStartIndexes, matchEndIndex);

        if (startTextIndex < 0 || endTextIndex < 0)
        {
            return;
        }

        DocxText startText = run.Texts[startTextIndex];
        DocxText endText = run.Texts[endTextIndex];
        int startOffset = matchIndex - textStartIndexes[startTextIndex];
        int endOffset = matchEndIndex - textStartIndexes[endTextIndex];
        string prefix = startText.Value.Substring(0, startOffset);
        string suffix = endText.Value.Substring(endOffset + 1);

        SetTextValue(startText, prefix + replacementText + suffix);

        if (endTextIndex > startTextIndex)
        {
            int textIndex = startTextIndex + 1;

            while (textIndex <= endTextIndex)
            {
                SetTextValue(run.Texts[textIndex], "");
                textIndex = textIndex + 1;
            }
        }
    }

    internal static int FindTextNodeIndex(DocxRun run, List<int> textStartIndexes, int characterIndex)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        if (textStartIndexes == null)
        {
            throw new ArgumentNullException("textStartIndexes");
        }

        int index = 0;

        while (index < run.Texts.Count)
        {
            int startIndex = textStartIndexes[index];
            if (characterIndex >= startIndex
                && characterIndex - startIndex < run.Texts[index].Value.Length)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
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
            int startIndex = runStartIndexes[index];
            if (characterIndex >= startIndex
                && characterIndex - startIndex < runTexts[index].Length)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void SetTextValue(DocxText text, string value)
    {
        if (text == null)
        {
            throw new ArgumentNullException("text");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        text.SetText(value);
        text.SetPreserveSpace(DocxText.ShouldPreserveSpace(value));
    }

    internal static void RefreshRunTextChildren(DocxRun run)
    {
        if (run == null)
        {
            throw new ArgumentNullException("run");
        }

        int childIndex = 0;
        int textIndex = 0;

        while (childIndex < run.Children.Count)
        {
            DocxXmlBuffer child = run.Children[childIndex];
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("t", DocxConstants.NamespaceWordprocessingMl))
            {
                run.Children[childIndex] = new DocxXmlBuffer(run.Texts[textIndex].WriteXml());
                textIndex = textIndex + 1;
            }

            childIndex = childIndex + 1;
        }
    }

    internal static void RefreshParagraphRunChildren(DocxParagraph paragraph)
    {
        if (paragraph == null)
        {
            throw new ArgumentNullException("paragraph");
        }

        int childIndex = 0;
        int runIndex = 0;

        while (childIndex < paragraph.Children.Count)
        {
            DocxXmlBuffer child = paragraph.Children[childIndex];
            DocxXmlReader reader = DocxXmlReader.Create(child.GetXml());

            if (reader.MoveToContent() && reader.IsElement("r", DocxConstants.NamespaceWordprocessingMl))
            {
                paragraph.Children[childIndex] = new DocxXmlBuffer(paragraph.Runs[runIndex].WriteXml());
                runIndex = runIndex + 1;
            }

            childIndex = childIndex + 1;
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
