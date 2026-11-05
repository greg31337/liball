namespace odf;

/// <summary>Describes a displayed regression equation.</summary>
public class OdfChartEquation
{
    /// <summary>The equation text.</summary>
    public string Text = "";
    /// <summary>The preserved automatic-content value.</summary>
    public string AutomaticContentValue = "";
    /// <summary>Whether equation content is generated automatically.</summary>
    public bool IsAutomaticContent;
    /// <summary>Whether the automatic-content value is absent or valid.</summary>
    public bool IsAutomaticContentValid = true;
    /// <summary>The preserved display-r-square value.</summary>
    public string DisplayRSquareValue = "";
    /// <summary>Whether the coefficient of determination is displayed.</summary>
    public bool DisplaysRSquare;
    /// <summary>Whether the display-r-square value is absent or valid.</summary>
    public bool IsDisplayRSquareValid = true;
    /// <summary>The preserved display-equation value.</summary>
    public string DisplayEquationValue = "";
    /// <summary>Whether the regression equation is displayed.</summary>
    public bool DisplaysEquation;
    /// <summary>Whether the display-equation value is absent or valid.</summary>
    public bool IsDisplayEquationValid = true;
    /// <summary>The horizontal equation position.</summary>
    public string X = "";
    /// <summary>The vertical equation position.</summary>
    public string Y = "";
    /// <summary>The referenced chart style name.</summary>
    public string StyleName = "";
    /// <summary>The underlying equation element.</summary>
    public OdfNode? Node;
    internal string OriginalText = "";
    internal string OriginalAutomaticContentValue = "";
    internal bool OriginalIsAutomaticContent;
    internal string OriginalDisplayRSquareValue = "";
    internal bool OriginalDisplaysRSquare;
    internal string OriginalDisplayEquationValue = "";
    internal bool OriginalDisplaysEquation;
    internal string OriginalX = "";
    internal string OriginalY = "";
    internal string OriginalStyleName = "";
}
