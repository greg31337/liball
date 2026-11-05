namespace docx;

/// <summary>
/// Extracts, traverses, searches, replaces, and imports document text.
/// </summary>
public interface IDocxTextProcessor
{
    /// <summary>
    /// Extracts text from one text-bearing model with explicit options.
    /// </summary>
    /// <param name="content">The content to extract.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The extracted text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    string ExtractContent(IDocxTextContent content, DocxTextOptions options);

    /// <summary>
    /// Extracts selected document stories with explicit options.
    /// </summary>
    /// <param name="document">The document to extract.</param>
    /// <param name="options">The extraction and story-selection options.</param>
    /// <returns>The extracted text in story order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    string ExtractDocument(DocxDocument document, DocxTextOptions options);

    /// <summary>
    /// Traverses the direct text-producing children of a run.
    /// </summary>
    /// <param name="run">The run to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    List<DocxTextToken> GetRunTokens(DocxRun run, DocxTextOptions options);

    /// <summary>
    /// Traverses the text-producing children of a paragraph.
    /// </summary>
    /// <param name="paragraph">The paragraph to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    List<DocxTextToken> GetParagraphTokens(DocxParagraph paragraph, DocxTextOptions options);

    /// <summary>
    /// Traverses text in body order.
    /// </summary>
    /// <param name="body">The body to traverse.</param>
    /// <param name="options">The extraction options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    List<DocxTextToken> GetBodyTokens(DocxBody body, DocxTextOptions options);

    /// <summary>
    /// Traverses selected document stories in story order.
    /// </summary>
    /// <param name="document">The document to traverse.</param>
    /// <param name="options">The extraction and story-selection options.</param>
    /// <returns>The ordered text tokens.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    List<DocxTextToken> GetDocumentTokens(DocxDocument document, DocxTextOptions options);

    /// <summary>
    /// Finds body text, including matches spanning adjacent runs.
    /// </summary>
    /// <param name="body">The body to search.</param>
    /// <param name="searchText">The text to find.</param>
    /// <param name="options">The search extraction options.</param>
    /// <returns>The matching ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    List<DocxTextMatch> FindInBody(DocxBody body, string searchText, DocxTextOptions options);

    /// <summary>
    /// Finds paragraph text, including matches spanning adjacent runs.
    /// </summary>
    /// <param name="paragraph">The paragraph to search.</param>
    /// <param name="searchText">The text to find.</param>
    /// <param name="options">The search extraction options.</param>
    /// <returns>The matching ranges.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    List<DocxTextMatch> FindInParagraph(DocxParagraph paragraph, string searchText, DocxTextOptions options);

    /// <summary>
    /// Replaces paragraph text across text nodes and adjacent runs while retaining run formatting.
    /// </summary>
    /// <param name="paragraph">The paragraph to edit.</param>
    /// <param name="searchText">The text to replace.</param>
    /// <param name="replacementText">The replacement text.</param>
    /// <returns>The number of replacements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    int ReplaceInParagraph(DocxParagraph paragraph, string searchText, string replacementText);

    /// <summary>
    /// Replaces run text across adjacent text nodes.
    /// </summary>
    /// <param name="run">The run to edit.</param>
    /// <param name="searchText">The text to replace.</param>
    /// <param name="replacementText">The replacement text.</param>
    /// <returns>The number of replacements.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <c>searchText</c> is empty.</exception>
    int ReplaceInRun(DocxRun run, string searchText, string replacementText);

    /// <summary>
    /// Imports plain text as a body, using line endings for paragraphs and tab characters for tab controls.
    /// </summary>
    /// <param name="text">The plain text to import.</param>
    /// <returns>The imported body.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>text</c> is null.</exception>
    DocxBody ImportPlainText(string text);
}
