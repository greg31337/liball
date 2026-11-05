namespace ttf;

/// <summary>Represents localized public <c>STAT</c> style-attribute metadata.</summary>
public class FontStat : IFontStat
{
    SfntStatTable table;
    FontNames names;
    FontNameValue? fallbackName;

    internal FontStat(SfntStatTable table, FontNames names)
    {
        this.table = table;
        this.names = names;
        fallbackName = table.HasElidedFallbackName() ?
            names.GetName(table.GetElidedFallbackNameId()) : null;
        if (table.HasElidedFallbackName() && fallbackName == null)
        {
            throw FontFormatException.Create("The STAT elided fallback name cannot be resolved.");
        }
    }

    /// <summary>Gets the major table version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }

    /// <summary>Gets the minor table version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }

    /// <summary>Gets the number of design axes.</summary>
    /// <returns>The axis count.</returns>
    public int GetAxisCount() { return table.GetAxisCount(); }

    /// <summary>Gets one design axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The design axis.</returns>
    public FontStatAxis GetAxis(int index)
    {
        return new FontStatAxis(table.GetAxis(index), names);
    }

    /// <summary>Finds a design axis by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The axis, or null.</returns>
    public FontStatAxis? FindAxis(int axisTag)
    {
        SfntStatAxis? axis = table.FindAxis(axisTag);
        return axis == null ? null : new FontStatAxis(axis, names);
    }

    /// <summary>Gets the number of axis-value tables.</summary>
    /// <returns>The axis-value count.</returns>
    public int GetAxisValueCount() { return table.GetAxisValueCount(); }

    /// <summary>Gets one axis-value table.</summary>
    /// <param name="index">The zero-based table index.</param>
    /// <returns>The axis-value model.</returns>
    public FontStatAxisValue GetAxisValue(int index)
    {
        return new FontStatAxisValue(table.GetAxisValue(index), table, names);
    }

    /// <summary>Reports whether an elided fallback name is present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    public bool HasElidedFallbackName() { return table.HasElidedFallbackName(); }

    /// <summary>Gets the elided fallback name identifier.</summary>
    /// <returns>The name identifier, or 65535 when absent.</returns>
    public int GetElidedFallbackNameId() { return table.GetElidedFallbackNameId(); }

    /// <summary>Gets the selected localized elided fallback name.</summary>
    /// <returns>The localized fallback name, or null.</returns>
    public FontNameValue? GetElidedFallbackName() { return fallbackName; }
}
