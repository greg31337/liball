namespace ttf;

/// <summary>Contains OpenType BASE baseline axes.</summary>
public class FontBaselines : IFontBaselines
{
    SfntBaseTable table;
    FontBaselineAxis? horizontalAxis;
    FontBaselineAxis? verticalAxis;

    internal FontBaselines(SfntBaseTable table)
    {
        this.table = table;
        SfntBaseAxis? horizontal = table.GetHorizontalAxis();
        SfntBaseAxis? vertical = table.GetVerticalAxis();
        horizontalAxis = horizontal == null ? null : new FontBaselineAxis(horizontal);
        verticalAxis = vertical == null ? null : new FontBaselineAxis(vertical);
    }

    /// <summary>Gets the BASE major version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }

    /// <summary>Gets the BASE minor version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }

    /// <summary>Gets the horizontal baseline axis.</summary>
    /// <returns>The axis, or null when absent.</returns>
    public FontBaselineAxis? GetHorizontalAxis() { return horizontalAxis; }

    /// <summary>Gets the vertical baseline axis.</summary>
    /// <returns>The axis, or null when absent.</returns>
    public FontBaselineAxis? GetVerticalAxis() { return verticalAxis; }
}
