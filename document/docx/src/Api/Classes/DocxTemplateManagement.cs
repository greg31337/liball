namespace docx;

/// <summary>
/// Discovers and applies document template operations.
/// </summary>
public class DocxTemplateManagement : IDocxTemplateManagement
{
    /// <summary>Creates a document template management service.</summary>
    public DocxTemplateManagement()
    {
    }

    /// <summary>Returns distinct placeholders from all supported document stories.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The placeholders in first-occurrence order.</returns>
    public List<DocxTemplatePlaceholder> GetDocumentPlaceholders(DocxDocument document, string prefix, string suffix)
    {
        ValidateDocument(document);
        ValidateDelimiters(prefix, suffix);
        List<DocxTemplatePlaceholder> placeholders = new List<DocxTemplatePlaceholder>();

        foreach (DocxStory story in document.GetStories())
        {
            AddPlaceholdersFromText(placeholders, story.Content.GetText(), prefix, suffix);
        }

        return placeholders;
    }

    /// <summary>Replaces named placeholders across all supported document stories.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">The replacement values keyed by placeholder name.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The replacement count.</returns>
    public int ReplaceDocumentPlaceholders(DocxDocument document, Dictionary<string, string> values, string prefix, string suffix)
    {
        ValidateDocument(document);
        DocxTemplate.ValidatePlaceholderArguments(values, prefix, suffix);
        int count = DocxTemplate.ReplacePlaceholders(document.Body, values, prefix, suffix);

        foreach (DocxHeader header in document.Headers)
        {
            count = count + ReplaceBlockListPlaceholders(header.Blocks, values, prefix, suffix);
        }

        foreach (DocxFooter footer in document.Footers)
        {
            count = count + ReplaceBlockListPlaceholders(footer.Blocks, values, prefix, suffix);
        }

        foreach (DocxFootnote footnote in document.Footnotes.Footnotes)
        {
            count = count + ReplaceBlockListPlaceholders(footnote.Blocks, values, prefix, suffix);
        }

        foreach (DocxEndnote endnote in document.Endnotes.Endnotes)
        {
            count = count + ReplaceBlockListPlaceholders(endnote.Blocks, values, prefix, suffix);
        }

        foreach (DocxComment comment in document.Comments.Comments)
        {
            count = count + ReplaceBlockListPlaceholders(comment.Blocks, values, prefix, suffix);
        }

        if (count > 0)
        {
            WriteDocumentStories(document);
        }

        return count;
    }

    /// <summary>Expands a body block range delimited by start and end markers.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="key">The repeating block key.</param>
    /// <param name="rows">The values for each repetition.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of repetitions inserted.</returns>
    public int ExpandRepeatingBlock(DocxDocument document, string key, List<Dictionary<string, string>> rows, string prefix, string suffix)
    {
        ValidateDocument(document);
        ValidateTemplateKey(key);
        ValidateRows(rows, prefix, suffix);
        string startMarker = prefix + "#" + key + suffix;
        string endMarker = prefix + "/" + key + suffix;
        int startIndex = FindMarkerBlockIndex(document.Body.Blocks, startMarker, 0);

        if (startIndex < 0)
        {
            return 0;
        }

        int endIndex = FindMarkerBlockIndex(document.Body.Blocks, endMarker, startIndex + 1);

        if (endIndex < 0)
        {
            throw new InvalidOperationException("Repeating block end marker is missing.");
        }

        List<DocxBlock> templates = new List<DocxBlock>();
        int index = startIndex + 1;

        while (index < endIndex)
        {
            templates.Add(DocxMerger.CloneBlock(document.Body.Blocks[index]));
            index = index + 1;
        }

        RemoveBlockRange(document.Body.Blocks, startIndex, endIndex);
        int insertionIndex = startIndex;

        foreach (Dictionary<string, string> row in rows)
        {
            foreach (DocxBlock template in templates)
            {
                DocxBlock block = DocxMerger.CloneBlock(template);
                DocxTemplate.ReplacePlaceholders(block, row, prefix, suffix);
                document.Body.Blocks.Insert(insertionIndex, block);
                insertionIndex = insertionIndex + 1;
            }
        }

        RefreshBodyAndWrite(document);
        return rows.Count;
    }

    /// <summary>Includes or removes body blocks delimited by conditional markers.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="key">The conditional block key.</param>
    /// <param name="include">Whether delimited content is retained.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of conditional ranges applied.</returns>
    public int ApplyConditionalBlock(DocxDocument document, string key, bool include, string prefix, string suffix)
    {
        ValidateDocument(document);
        ValidateTemplateKey(key);
        ValidateDelimiters(prefix, suffix);
        string startMarker = prefix + "?" + key + suffix;
        string endMarker = prefix + "/" + key + suffix;
        int count = 0;
        int searchIndex = 0;
        int startIndex = FindMarkerBlockIndex(document.Body.Blocks, startMarker, searchIndex);

        while (startIndex >= 0)
        {
            int endIndex = FindMarkerBlockIndex(document.Body.Blocks, endMarker, startIndex + 1);

            if (endIndex < 0)
            {
                throw new InvalidOperationException("Conditional block end marker is missing.");
            }

            if (include)
            {
                document.Body.Blocks.RemoveAt(endIndex);
                document.Body.Blocks.RemoveAt(startIndex);
                searchIndex = endIndex - 1;
            }
            else
            {
                RemoveBlockRange(document.Body.Blocks, startIndex, endIndex);
                searchIndex = startIndex;
            }

            count = count + 1;
            startIndex = FindMarkerBlockIndex(document.Body.Blocks, startMarker, searchIndex);
        }

        if (count > 0)
        {
            RefreshBodyAndWrite(document);
        }

        return count;
    }

    /// <summary>Expands a direct body table row for supplied values.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="tableIndex">The zero-based direct body table index.</param>
    /// <param name="templateRowIndex">The zero-based template row index.</param>
    /// <param name="rows">The values for each row.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of rows inserted.</returns>
    public int ExpandTableRow(DocxDocument document, int tableIndex, int templateRowIndex, List<Dictionary<string, string>> rows, string prefix, string suffix)
    {
        ValidateDocument(document);
        ValidateRows(rows, prefix, suffix);
        List<DocxTable> tables = document.Body.GetTables();

        if (tableIndex < 0 || tableIndex >= tables.Count)
        {
            throw new ArgumentOutOfRangeException("tableIndex");
        }

        DocxTableData data = DocxTableData.Create(rows);
        int count = DocxTemplate.RepeatTableRow(tables[tableIndex], templateRowIndex, data, prefix, suffix);
        RefreshBodyAndWrite(document);
        return count;
    }

    /// <summary>Fills tagged or aliased content controls from named values.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">Values keyed by tag, falling back to alias.</param>
    /// <returns>The number of controls filled.</returns>
    public int MergeContentControls(DocxDocument document, Dictionary<string, string> values)
    {
        DocxContentControlManagement management = new DocxContentControlManagement();
        return management.FillDocumentContentControls(document, values);
    }

    /// <summary>Inserts another document after a named bookmark.</summary>
    /// <param name="document">The target document.</param>
    /// <param name="bookmarkName">The target bookmark name.</param>
    /// <param name="source">The document whose body blocks are inserted.</param>
    /// <returns>The number of blocks inserted.</returns>
    public int InsertDocumentAtBookmark(DocxDocument document, string bookmarkName, DocxDocument source)
    {
        return DocxMerger.InsertAtBookmark(document, bookmarkName, source);
    }

    internal static void AddPlaceholdersFromText(List<DocxTemplatePlaceholder> placeholders, string text, string prefix, string suffix)
    {
        int searchIndex = 0;
        int prefixIndex = text.IndexOf(prefix, searchIndex, StringComparison.Ordinal);

        while (prefixIndex >= 0)
        {
            int keyIndex = prefixIndex + prefix.Length;
            int suffixIndex = text.IndexOf(suffix, keyIndex, StringComparison.Ordinal);

            if (suffixIndex < 0)
            {
                return;
            }

            string key = text.Substring(keyIndex, suffixIndex - keyIndex);

            if (IsValuePlaceholderKey(key))
            {
                DocxTemplatePlaceholder? placeholder = FindPlaceholder(placeholders, key);

                if (placeholder == null)
                {
                    placeholders.Add(new DocxTemplatePlaceholder(key, prefix + key + suffix, 1));
                }
                else
                {
                    placeholder.Count = placeholder.Count + 1;
                }
            }

            searchIndex = suffixIndex + suffix.Length;
            prefixIndex = text.IndexOf(prefix, searchIndex, StringComparison.Ordinal);
        }
    }

    internal static DocxTemplatePlaceholder? FindPlaceholder(List<DocxTemplatePlaceholder> placeholders, string key)
    {
        foreach (DocxTemplatePlaceholder placeholder in placeholders)
        {
            if (placeholder.Key == key)
            {
                return placeholder;
            }
        }

        return null;
    }

    internal static bool IsValuePlaceholderKey(string key)
    {
        if (key == "")
        {
            return false;
        }

        char first = key[0];
        return first != '#' && first != '?' && first != '/';
    }

    internal static int ReplaceBlockListPlaceholders(List<DocxBlock> blocks, Dictionary<string, string> values, string prefix, string suffix)
    {
        int count = 0;

        foreach (DocxBlock block in blocks)
        {
            count = count + DocxTemplate.ReplacePlaceholders(block, values, prefix, suffix);
        }

        return count;
    }

    internal static int FindMarkerBlockIndex(List<DocxBlock> blocks, string marker, int startIndex)
    {
        int index = startIndex;

        while (index < blocks.Count)
        {
            if (blocks[index].GetText().IndexOf(marker, 0, StringComparison.Ordinal) >= 0)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal static void RemoveBlockRange(List<DocxBlock> blocks, int startIndex, int endIndex)
    {
        int count = endIndex - startIndex + 1;

        while (count > 0)
        {
            blocks.RemoveAt(startIndex);
            count = count - 1;
        }
    }

    internal static void ValidateDocument(DocxDocument document)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }
    }

    internal static void ValidateTemplateKey(string key)
    {
        if (key == null)
        {
            throw new ArgumentNullException("key");
        }

        if (key == "")
        {
            throw new ArgumentException("Template key is required.", "key");
        }
    }

    internal static void ValidateDelimiters(string prefix, string suffix)
    {
        DocxTemplate.ValidateDelimiter("prefix", prefix);
        DocxTemplate.ValidateDelimiter("suffix", suffix);
    }

    internal static void ValidateRows(List<Dictionary<string, string>> rows, string prefix, string suffix)
    {
        if (rows == null)
        {
            throw new ArgumentNullException("rows");
        }

        ValidateDelimiters(prefix, suffix);

        foreach (Dictionary<string, string> row in rows)
        {
            DocxTemplate.ValidatePlaceholderArguments(row, prefix, suffix);
        }
    }

    internal static void RefreshBodyAndWrite(DocxDocument document)
    {
        document.Body.Xml = document.Body.WriteXml();
        document.Body.LoadBlocksFromXml();
        document.Body.LoadSectionsFromXml();
        WriteDocumentStories(document);
    }

    internal static void WriteDocumentStories(DocxDocument document)
    {
        DocxTemplate.UpdateMainDocumentBody(document);

        if (document.FootnotesPart != null)
        {
            document.WriteFootnotesPart();
        }

        if (document.EndnotesPart != null)
        {
            document.WriteEndnotesPart();
        }

        if (document.CommentsPart != null)
        {
            document.WriteCommentsPart();
        }
    }
}
