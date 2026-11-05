namespace pdf;

/// <summary>
/// Wraps reusable layout content as a styled callout or note.
/// </summary>
public class PdfCallout : IPdfLayoutElement, IPdfSemanticLayoutElement
{
    internal PdfSemanticLayoutElement Semantic;

    /// <summary>
    /// Initializes a callout from the supplied content.
    /// </summary>
    /// <param name="content">The callout content.</param>
    public PdfCallout(IPdfLayoutElement content)
    {
        Semantic = new PdfSemanticLayoutElement(content, "Note", string.Empty);
    }

    /// <summary>Applies a defensive copy of a reusable element style.</summary>
    /// <param name="style">The callout style.</param>
    public void SetStyle(PdfElementStyle style) { Semantic.SetStyle(style); }
    /// <summary>Requests that the complete callout start in a fresh flow area when needed.</summary>
    public void SetKeepTogether() { Semantic.SetKeepTogether(); }
    /// <summary>Clears the explicit keep-together request.</summary>
    public void ClearKeepTogether() { Semantic.ClearKeepTogether(); }
    /// <summary>Determines whether explicit keep-together behavior is enabled.</summary>
    /// <returns>True when keep-together behavior is enabled; otherwise, false.</returns>
    public bool IsKeepTogether() { return Semantic.IsKeepTogether(); }
    /// <summary>Sets the description stored with the callout accessibility role.</summary>
    /// <param name="description">The accessibility description.</param>
    public void SetAccessibilityDescription(string description) { Semantic.SetAccessibilityDescription(description); }
    /// <summary>Returns the callout accessibility role.</summary>
    /// <returns>The accessibility role name.</returns>
    public string GetAccessibilityRole() { return Semantic.GetAccessibilityRole(); }
    /// <summary>Returns the current accessibility description.</summary>
    /// <returns>The accessibility description.</returns>
    public string GetAccessibilityDescription() { return Semantic.GetAccessibilityDescription(); }
    /// <summary>Measures the styled callout.</summary>
    /// <param name="context">The available layout area.</param>
    /// <returns>The measured layout result.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context) { return Semantic.Measure(context); }
    /// <summary>Draws the styled callout.</summary>
    /// <param name="context">The target page area.</param>
    /// <returns>The drawn layout result.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context) { return Semantic.Draw(context); }
    /// <summary>Flows the callout and registers its accessibility role.</summary>
    /// <param name="cursor">The cursor that controls pagination.</param>
    /// <returns>The flowed layout result.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor) { return Semantic.Flow(cursor, this); }
}
