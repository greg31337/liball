namespace ttf;

/// <summary>Represents one localized <c>STAT</c> design axis.</summary>
public class FontStatAxis : IFontStatAxis
{
    SfntStatAxis axis;
    FontNameValue name;

    internal FontStatAxis(SfntStatAxis axis, FontNames names)
    {
        this.axis = axis;
        FontNameValue? selected = names.GetName(axis.GetNameId());
        if (selected == null)
        {
            throw FontFormatException.Create("A STAT design-axis name cannot be resolved.");
        }
        name = selected;
    }

    /// <summary>Gets the axis tag.</summary>
    /// <returns>The big-endian tag.</returns>
    public int GetTag() { return axis.GetTag(); }

    /// <summary>Gets the axis name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    public int GetNameId() { return axis.GetNameId(); }

    /// <summary>Gets the selected localized axis name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    public FontNameValue GetName() { return name; }

    /// <summary>Gets the display ordering value.</summary>
    /// <returns>The axis ordering value.</returns>
    public int GetOrdering() { return axis.GetOrdering(); }
}
