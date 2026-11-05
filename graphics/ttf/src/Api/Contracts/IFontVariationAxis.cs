namespace ttf;

/// <summary>Defines one public <c>fvar</c> variation axis.</summary>
public interface IFontVariationAxis
{
    /// <summary>Gets the axis tag.</summary>
    /// <returns>The big-endian tag.</returns>
    int GetTag();

    /// <summary>Gets the minimum user-space value.</summary>
    /// <returns>The minimum value.</returns>
    double GetMinimumValue();

    /// <summary>Gets the default user-space value.</summary>
    /// <returns>The default value.</returns>
    double GetDefaultValue();

    /// <summary>Gets the maximum user-space value.</summary>
    /// <returns>The maximum value.</returns>
    double GetMaximumValue();

    /// <summary>Gets the raw axis flags.</summary>
    /// <returns>The flags.</returns>
    int GetFlags();

    /// <summary>Reports whether the axis should be hidden from user interfaces.</summary>
    /// <returns>True when hidden; otherwise false.</returns>
    bool IsHidden();

    /// <summary>Gets the axis name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    int GetNameId();

    /// <summary>Gets the selected localized axis name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    FontNameValue GetName();
}
