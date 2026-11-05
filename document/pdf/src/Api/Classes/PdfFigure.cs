namespace pdf;

/// <summary>
/// Groups figure content with a semantic caption, reusable styling, and pagination behavior.
/// </summary>
public class PdfFigure : IPdfLayoutElement, IPdfSemanticLayoutElement, IPdfFigure
{
    internal IPdfLayoutElement Content;
    internal PdfCaption Caption;
    internal PdfStackBlock Stack;
    internal PdfSemanticLayoutElement Semantic;

    /// <summary>
    /// Initializes a figure with content followed by its caption.
    /// </summary>
    /// <param name="content">The primary figure content.</param>
    /// <param name="caption">The semantic figure caption.</param>
    public PdfFigure(IPdfLayoutElement content, PdfCaption caption)
    {
        if (content == null)
        {
            throw new global::System.ArgumentNullException("content");
        }

        if (caption == null)
        {
            throw new global::System.ArgumentNullException("caption");
        }

        Content = content;
        Caption = caption;
        Stack = new PdfStackBlock();
        Stack.AddElement(content);
        Stack.AddElement(caption);
        Semantic = new PdfSemanticLayoutElement(Stack, "Figure", string.Empty);
    }

    /// <summary>
    /// Sets the spacing between figure content and its caption.
    /// </summary>
    /// <param name="spacing">The non-negative finite spacing in PDF points.</param>
    public void SetCaptionSpacing(double spacing)
    {
        Stack.SetSpacing(spacing);
    }

    /// <summary>
    /// Applies a defensive copy of a reusable element style and its item spacing.
    /// </summary>
    /// <param name="style">The figure style.</param>
    public void SetStyle(PdfElementStyle style)
    {
        Semantic.SetStyle(style);
        Stack.SetSpacing(style.ItemSpacing);
    }

    /// <summary>Requests that the complete figure start in a fresh flow area when needed.</summary>
    public void SetKeepTogether() { Semantic.SetKeepTogether(); }
    /// <summary>Clears the explicit keep-together request.</summary>
    public void ClearKeepTogether() { Semantic.ClearKeepTogether(); }
    /// <summary>Determines whether explicit keep-together behavior is enabled.</summary>
    /// <returns>True when keep-together behavior is enabled; otherwise, false.</returns>
    public bool IsKeepTogether() { return Semantic.IsKeepTogether(); }
    /// <summary>Sets the description stored with the figure accessibility role.</summary>
    /// <param name="description">The accessibility description.</param>
    public void SetAccessibilityDescription(string description) { Semantic.SetAccessibilityDescription(description); }
    /// <summary>Returns the figure accessibility role.</summary>
    /// <returns>The accessibility role name.</returns>
    public string GetAccessibilityRole() { return Semantic.GetAccessibilityRole(); }
    /// <summary>Returns the current accessibility description.</summary>
    /// <returns>The accessibility description.</returns>
    public string GetAccessibilityDescription() { return Semantic.GetAccessibilityDescription(); }
    /// <summary>Measures the figure content and caption as one element.</summary>
    /// <param name="context">The available layout area.</param>
    /// <returns>The measured layout result.</returns>
    public PdfLayoutResult Measure(PdfLayoutContext context) { return Semantic.Measure(context); }
    /// <summary>Draws the figure content and caption.</summary>
    /// <param name="context">The target page area.</param>
    /// <returns>The drawn layout result.</returns>
    public PdfLayoutResult Draw(PdfLayoutDrawContext context) { return Semantic.Draw(context); }

    /// <summary>
    /// Flows the figure and registers both figure and caption accessibility roles.
    /// </summary>
    /// <param name="cursor">The cursor that controls pagination.</param>
    /// <returns>The flowed layout result.</returns>
    public PdfLayoutResult Flow(PdfPageCursor cursor)
    {
        return Semantic.Flow(cursor, this);
    }
}
