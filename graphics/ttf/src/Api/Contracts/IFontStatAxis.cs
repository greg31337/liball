namespace ttf;

/// <summary>Defines one public <c>STAT</c> design axis.</summary>
public interface IFontStatAxis
{
    /// <summary>Gets the axis tag.</summary>
    /// <returns>The big-endian tag.</returns>
    int GetTag();

    /// <summary>Gets the axis name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    int GetNameId();

    /// <summary>Gets the selected localized axis name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    FontNameValue GetName();

    /// <summary>Gets the display ordering value.</summary>
    /// <returns>The axis ordering value.</returns>
    int GetOrdering();
}
