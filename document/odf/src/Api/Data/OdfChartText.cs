namespace odf;

/// <summary>Describes a chart title, subtitle, footer, or axis title.</summary>
public class OdfChartText
{
    /// <summary>The displayed text.</summary>
    public string Text = "";
    /// <summary>The source cell range for the text.</summary>
    public string CellRange = "";
    /// <summary>The parsed source cell ranges.</summary>
    public OdfCellRange[] SourceRanges = [];
    /// <summary>Whether the source range list is absent or valid.</summary>
    public bool IsSourceRangeValid = true;
    /// <summary>The horizontal position.</summary>
    public string X = "";
    /// <summary>The vertical position.</summary>
    public string Y = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The underlying chart text element.</summary>
    public OdfNode? Node;
    internal string OriginalText = "";
    internal string OriginalCellRange = "";
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalStyleName = "";
}
