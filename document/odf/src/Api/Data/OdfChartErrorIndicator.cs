namespace odf;

/// <summary>Describes an error indicator or error-bar set for one series dimension.</summary>
public class OdfChartErrorIndicator
{
    /// <summary>The error-indicator dimension.</summary>
    public string Dimension = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective error-bar style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The underlying error-indicator element.</summary>
    public OdfNode? Node;
    internal string OriginalDimension = "";
    internal string OriginalStyleName = "";
}
