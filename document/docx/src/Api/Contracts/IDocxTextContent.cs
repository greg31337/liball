namespace docx;

/// <summary>
/// Provides a common plain-text view of a document or document content element.
/// </summary>
public interface IDocxTextContent
{
    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <returns>The resulting text.</returns>
    string GetText();

    /// <summary>
    /// Builds plain text from the current content in document order.
    /// </summary>
    /// <param name="options">The options that control text extraction.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>options</c> is null.</exception>
    string GetTextWithOptions(DocxTextOptions options);
}
