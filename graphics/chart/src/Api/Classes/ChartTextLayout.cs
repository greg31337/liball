namespace chart;
/// <summary>Represents a deterministic multiline text layout.</summary>
public class ChartTextLayout : IChartTextLayout
{
    internal ChartTextLayoutLine[] Lines;
    internal ChartRect Bounds;
    internal bool HasClip;
    internal ChartRect ClipBounds;

    /// <summary>Initializes a text layout from positioned lines and bounds.</summary>
    /// <param name = "lines">The positioned lines.</param>
    /// <param name = "x">The layout left edge.</param>
    /// <param name = "y">The layout top edge.</param>
    /// <param name = "width">The nonnegative layout width.</param>
    /// <param name = "height">The positive layout height.</param>
    public ChartTextLayout(ChartTextLayoutLine[] lines, int x, int y, int width, int height)
    {
        if (lines == null)
        {
            throw new System.ArgumentNullException("lines");
        }

        if (width < 0 || height <= 0)
        {
            throw new System.ArgumentOutOfRangeException("width");
        }

        Lines = CopyLines(lines);
        Bounds = new ChartRect(x, y, width, height);
        HasClip = false;
        ClipBounds = new ChartRect(0.0, 0.0, 1.0, 1.0);
    }

    /// <summary>Returns the number of positioned lines.</summary>
    /// <returns>The line count.</returns>
    public int GetLineCount()
    {
        return Lines.Length;
    }

    /// <summary>Returns one positioned line.</summary>
    /// <param name = "index">The zero-based line index.</param>
    /// <returns>The requested line.</returns>
    public ChartTextLayoutLine LineAt(int index)
    {
        if (index < 0 || index >= Lines.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return Lines[index];
    }

    /// <summary>Returns the unrotated layout bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    public ChartRect GetBounds()
    {
        return ChartPlot.CopyRect(Bounds);
    }

    /// <summary>Returns whether a clipping rectangle is attached.</summary>
    /// <returns>The clipping state.</returns>
    public bool GetHasClip()
    {
        return HasClip;
    }

    /// <summary>Returns the clipping rectangle.</summary>
    /// <returns>An independent clipping rectangle copy.</returns>
    public ChartRect GetClipBounds()
    {
        return ChartPlot.CopyRect(ClipBounds);
    }

    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied text layout.</returns>
    public ChartTextLayout Copy()
    {
        ChartTextLayout layout = new ChartTextLayout(Lines, ChartRenderScene.RoundToInt(Bounds.X), ChartRenderScene.RoundToInt(Bounds.Y), ChartRenderScene.RoundToInt(Bounds.Width), ChartRenderScene.RoundToInt(Bounds.Height));
        layout.HasClip = HasClip;
        layout.ClipBounds = ChartPlot.CopyRect(ClipBounds);
        return layout;
    }

    internal static ChartTextLayoutLine[] CopyLines(ChartTextLayoutLine[] lines)
    {
        ChartTextLayoutLine[] copy = new ChartTextLayoutLine[lines.Length];
        int index = 0;
        while (index < lines.Length)
        {
            if (lines[index] == null)
            {
                throw new System.ArgumentException("Text layout lines cannot contain null values.");
            }

            copy[index] = lines[index].Copy();
            index = index + 1;
        }

        return copy;
    }
}
