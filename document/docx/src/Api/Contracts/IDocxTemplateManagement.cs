namespace docx;

/// <summary>
/// Discovers and applies document template operations.
/// </summary>
public interface IDocxTemplateManagement
{
    /// <summary>Returns distinct placeholders from all supported document stories.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The placeholders in first-occurrence order.</returns>
    List<DocxTemplatePlaceholder> GetDocumentPlaceholders(DocxDocument document, string prefix, string suffix);

    /// <summary>Replaces named placeholders across all supported document stories.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">The replacement values keyed by placeholder name.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The replacement count.</returns>
    int ReplaceDocumentPlaceholders(DocxDocument document, Dictionary<string, string> values, string prefix, string suffix);

    /// <summary>Expands a body block range delimited by start and end markers.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="key">The repeating block key.</param>
    /// <param name="rows">The values for each repetition.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of repetitions inserted.</returns>
    int ExpandRepeatingBlock(DocxDocument document, string key, List<Dictionary<string, string>> rows, string prefix, string suffix);

    /// <summary>Includes or removes body blocks delimited by conditional markers.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="key">The conditional block key.</param>
    /// <param name="include">Whether delimited content is retained.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of conditional ranges applied.</returns>
    int ApplyConditionalBlock(DocxDocument document, string key, bool include, string prefix, string suffix);

    /// <summary>Expands a direct body table row for supplied values.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="tableIndex">The zero-based direct body table index.</param>
    /// <param name="templateRowIndex">The zero-based template row index.</param>
    /// <param name="rows">The values for each row.</param>
    /// <param name="prefix">The opening placeholder delimiter.</param>
    /// <param name="suffix">The closing placeholder delimiter.</param>
    /// <returns>The number of rows inserted.</returns>
    int ExpandTableRow(DocxDocument document, int tableIndex, int templateRowIndex, List<Dictionary<string, string>> rows, string prefix, string suffix);

    /// <summary>Fills tagged or aliased content controls from named values.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="values">Values keyed by tag, falling back to alias.</param>
    /// <returns>The number of controls filled.</returns>
    int MergeContentControls(DocxDocument document, Dictionary<string, string> values);

    /// <summary>Inserts another document after a named bookmark.</summary>
    /// <param name="document">The target document.</param>
    /// <param name="bookmarkName">The target bookmark name.</param>
    /// <param name="source">The document whose body blocks are inserted.</param>
    /// <returns>The number of blocks inserted.</returns>
    int InsertDocumentAtBookmark(DocxDocument document, string bookmarkName, DocxDocument source);
}
