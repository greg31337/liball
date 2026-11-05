namespace chart;
/// <summary>Stores options that control retained-scene SVG serialization.</summary>
public class ChartSvgOptions
{
    /// <summary>When <c>true</c>, repeated presentation styles are emitted as deterministic CSS classes.</summary>
    public bool UseCssClasses;
    /// <summary>When <c>true</c>, accessible image, series, point, and group roles are emitted.</summary>
    public bool IncludeAccessibilityRoles;
    /// <summary>An optional stable prefix applied to generated and retained SVG identifiers.</summary>
    public string IdPrefix;
    /// <summary>Initializes SVG options with inline styles, accessibility roles, and no identifier prefix.</summary>
    public ChartSvgOptions()
    {
        UseCssClasses = false;
        IncludeAccessibilityRoles = true;
        IdPrefix = "";
    }
}
