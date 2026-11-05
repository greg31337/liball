namespace odf;

/// <summary>Describes a chart regression curve.</summary>
public class OdfChartRegressionCurve
{
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The effective regression style metadata.</summary>
    public OdfChartStyleMetadata StyleMetadata = new OdfChartStyleMetadata();
    /// <summary>The displayed regression equation, when present.</summary>
    public OdfChartEquation? Equation;
    /// <summary>The underlying regression-curve element.</summary>
    public OdfNode? Node;
    internal string OriginalStyleName = "";
}
