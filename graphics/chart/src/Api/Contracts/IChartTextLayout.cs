namespace chart;
/// <summary>Defines a deterministic multiline text layout.</summary>
public interface IChartTextLayout
{
    /// <summary>Returns the number of positioned lines.</summary>
    /// <returns>The line count.</returns>
    int GetLineCount();
    /// <summary>Returns one positioned line.</summary>
    /// <param name = "index">The zero-based line index.</param>
    /// <returns>The requested line.</returns>
    ChartTextLayoutLine LineAt(int index);
    /// <summary>Returns the unrotated layout bounds.</summary>
    /// <returns>An independent bounds copy.</returns>
    ChartRect GetBounds();
    /// <summary>Returns whether a clipping rectangle is attached.</summary>
    /// <returns>The clipping state.</returns>
    bool GetHasClip();
    /// <summary>Returns the clipping rectangle.</summary>
    /// <returns>An independent clipping rectangle copy.</returns>
    ChartRect GetClipBounds();
    /// <summary>Creates an independent copy.</summary>
    /// <returns>A copied text layout.</returns>
    ChartTextLayout Copy();
}
