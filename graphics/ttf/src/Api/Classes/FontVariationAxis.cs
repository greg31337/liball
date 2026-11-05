namespace ttf;

/// <summary>Represents one localized <c>fvar</c> variation axis.</summary>
public class FontVariationAxis : IFontVariationAxis
{
    SfntFvarAxis axis;
    FontNameValue name;

    internal FontVariationAxis(SfntFvarAxis axis, FontNames names)
    {
        this.axis = axis;
        FontNameValue? selected = names.GetName(axis.GetNameId());
        if (selected == null)
        {
            throw FontFormatException.Create("A variation-axis name cannot be resolved.");
        }
        name = selected;
    }

    /// <summary>Gets the axis tag.</summary>
    /// <returns>The big-endian tag.</returns>
    public int GetTag() { return axis.GetTag(); }

    /// <summary>Gets the minimum user-space value.</summary>
    /// <returns>The minimum value.</returns>
    public double GetMinimumValue() { return axis.GetMinimumValue(); }

    /// <summary>Gets the default user-space value.</summary>
    /// <returns>The default value.</returns>
    public double GetDefaultValue() { return axis.GetDefaultValue(); }

    /// <summary>Gets the maximum user-space value.</summary>
    /// <returns>The maximum value.</returns>
    public double GetMaximumValue() { return axis.GetMaximumValue(); }

    /// <summary>Gets the raw axis flags.</summary>
    /// <returns>The flags.</returns>
    public int GetFlags() { return axis.GetFlags(); }

    /// <summary>Reports whether the axis should be hidden from user interfaces.</summary>
    /// <returns>True when hidden; otherwise false.</returns>
    public bool IsHidden() { return axis.IsHidden(); }

    /// <summary>Gets the axis name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    public int GetNameId() { return axis.GetNameId(); }

    /// <summary>Gets the selected localized axis name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    public FontNameValue GetName() { return name; }
}
