namespace pdf;

/// <summary>
/// Configures styling, pagination grouping, and accessibility metadata for a semantic layout element.
/// </summary>
public interface IPdfSemanticLayoutElement
{
    /// <summary>
    /// Applies a defensive copy of a reusable element style.
    /// </summary>
    /// <param name="style">The style applied around the semantic content.</param>
    void SetStyle(PdfElementStyle style);

    /// <summary>
    /// Requests that the complete semantic element start in a fresh flow area when it cannot fit in the current one.
    /// </summary>
    void SetKeepTogether();

    /// <summary>
    /// Clears the explicit keep-together request.
    /// </summary>
    void ClearKeepTogether();

    /// <summary>
    /// Determines whether explicit keep-together behavior is enabled.
    /// </summary>
    /// <returns>True when keep-together behavior is enabled; otherwise, false.</returns>
    bool IsKeepTogether();

    /// <summary>
    /// Sets the description stored with the element's accessibility role during document flow.
    /// </summary>
    /// <param name="description">The accessibility description; an empty value is permitted.</param>
    void SetAccessibilityDescription(string description);

    /// <summary>
    /// Returns the semantic accessibility role registered during document flow.
    /// </summary>
    /// <returns>The accessibility role name.</returns>
    string GetAccessibilityRole();

    /// <summary>
    /// Returns the current accessibility description.
    /// </summary>
    /// <returns>The accessibility description.</returns>
    string GetAccessibilityDescription();
}
