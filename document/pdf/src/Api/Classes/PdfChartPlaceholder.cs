namespace pdf;

/// <summary>
/// Draws a reusable labeled chart placeholder with figure semantics.
/// </summary>
public class PdfChartPlaceholder : IPdfLayoutElement, IPdfSemanticLayoutElement, IPdfChartPlaceholder
{
    internal PdfChartPlaceholderContent Placeholder;
    internal PdfSemanticLayoutElement Semantic;

    /// <summary>
    /// Initializes a labeled fixed-size chart placeholder.
    /// </summary>
    /// <param name="label">The label drawn inside the placeholder.</param>
    /// <param name="width">The positive finite placeholder width in PDF points.</param>
    /// <param name="height">The positive finite placeholder height in PDF points.</param>
    public PdfChartPlaceholder(string label, double width, double height)
    {
        Placeholder = new PdfChartPlaceholderContent(label, width, height);
        Semantic = new PdfSemanticLayoutElement(Placeholder, "Figure", label);
    }

    /// <summary>
    /// Applies a defensive copy of the text style used for the placeholder label.
    /// </summary>
    /// <param name="style">The label text style.</param>
    public void SetTextStyle(PdfTextStyle style)
    {
        Placeholder.SetTextStyle(style);
    }

    /// <summary>Applies a defensive copy of a reusable element style.</summary>
    /// <param name="style">The placeholder container style.</param>
    public void SetStyle(PdfElementStyle style) { Semantic.SetStyle(style); }
    /// <summary>Requests that the complete placeholder start in a fresh flow area when needed.</summary>
    public void SetKeepTogether() { Semantic.SetKeepTogether(); }
    /// <summary>Clears the explicit keep-together request.</summary>
    public void ClearKeepTogether() { Semantic.ClearKeepTogether(); }
    /// <summary>Determines whether explicit keep-together behavior is enabled.</summary>
    /// <returns>True when keep-together behavior is enabled; otherwise, false.</returns>
    public bool IsKeepTogether() { return Semantic.IsKeepTogether(); }
    /// <summary>Sets the description stored with the placeholder accessibility role.</summary>
    /// <param name="description">The accessibility description.</param>
    public void SetAccessibilityDescription(string description) { Semantic.SetAccessibilityDescription(description); }
    /// <summary>Returns the placeholder accessibility role.</summary>
    /// <returns>The accessibility role name.</returns>
    public string GetAccessibilityRole() { return Semantic.GetAccessibilityRole(); }
    /// <summary>Returns the current accessibility description.</summary>
    /// <returns>The accessibility description.</returns>
    public string GetAccessibilityDescription() { return Semantic.GetAccessibilityDescription(); }
    /// <summary>Measures the styled placeholder.</summary>
    /// <param name="context">The available layout area.</param>
    /// <returns>The measured layout result.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context) { return Semantic.Measure(context); }
    /// <summary>Draws the styled chart placeholder.</summary>
    /// <param name="context">The target page area.</param>
    /// <returns>The drawn layout result.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context) { return Semantic.Draw(context); }
    /// <summary>Flows the placeholder and registers its figure accessibility role.</summary>
    /// <param name="cursor">The cursor that controls pagination.</param>
    /// <returns>The flowed layout result.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor) { return Semantic.Flow(cursor, this); }
}
