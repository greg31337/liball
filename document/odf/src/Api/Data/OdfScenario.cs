namespace odf;

/// <summary>Describes a spreadsheet scenario attached to a table.</summary>
public class OdfScenario
{
    /// <summary>The table containing the scenario.</summary>
    public OdfTable? Table;
    /// <summary>The scenario cell range list.</summary>
    public string RangeAddresses = "";
    /// <summary>The scenario comment.</summary>
    public string Comment = "";
    /// <summary>Whether scenario values are copied back when selected.</summary>
    public bool CopyBack;
    /// <summary>Whether scenario styles are copied.</summary>
    public bool CopyStyles;
    /// <summary>Whether scenario formulas are copied.</summary>
    public bool CopyFormulas;
    /// <summary>Whether this scenario is active.</summary>
    public bool IsActive;
    /// <summary>Whether a border is displayed around the scenario range.</summary>
    public bool DisplayBorder = true;
    /// <summary>The lexical color of the displayed scenario border.</summary>
    public string BorderColor = "";
    /// <summary>Whether the scenario is protected.</summary>
    public bool IsProtected;
    /// <summary>The preserved protection key.</summary>
    public string ProtectionKey = "";
    /// <summary>The underlying scenario element.</summary>
    public OdfNode? Node;
}
