namespace chart;
/// <summary>Represents one styled span in rich text.</summary>
public class ChartTextSpan : IChartTextSpan
{
    internal string Text;
    internal ChartRenderStyle Style;

    /// <summary>Initializes a rich-text span.</summary>
    /// <param name = "text">The span text.</param>
    /// <param name = "style">The span rendering style.</param>
    public ChartTextSpan(string text, ChartRenderStyle style)
    {
        ChartTextBlock.ValidateText(text, "text");
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        Text = text;
        Style = style.Copy();
    }

    /// <summary>Returns the span text.</summary>
    /// <returns>The span text.</returns>
    public string GetText()
    {
        return Text;
    }

    /// <summary>Sets the span text.</summary>
    /// <param name = "text">The replacement text.</param>
    public void SetText(string text)
    {
        ChartTextBlock.ValidateText(text, "text");
        Text = text;
    }

    /// <summary>Returns an independent rendering style.</summary>
    /// <returns>A copied rendering style.</returns>
    public ChartRenderStyle GetStyle()
    {
        return Style.Copy();
    }

    /// <summary>Sets the span rendering style.</summary>
    /// <param name = "style">The replacement style.</param>
    public void SetStyle(ChartRenderStyle style)
    {
        if (style == null)
        {
            throw new System.ArgumentNullException("style");
        }

        Style = style.Copy();
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied rich-text span.</returns>
    public ChartTextSpan Copy()
    {
        return new ChartTextSpan(Text, Style);
    }
}
