namespace odf;

/// <summary>Describes a label range and the data range it identifies.</summary>
public class OdfLabelRange
{
    /// <summary>The cell range containing labels.</summary>
    public string LabelRangeAddress = "";
    /// <summary>The cell range containing the labelled data.</summary>
    public string DataRangeAddress = "";
    /// <summary>The preserved row or column orientation token.</summary>
    public string Orientation = "";
    /// <summary>The underlying label-range element.</summary>
    public OdfNode? Node;
}
