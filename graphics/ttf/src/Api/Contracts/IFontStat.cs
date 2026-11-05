namespace ttf;

/// <summary>Defines public <c>STAT</c> style-attribute metadata.</summary>
public interface IFontStat
{
    /// <summary>Gets the major table version.</summary>
    /// <returns>The major version.</returns>
    int GetMajorVersion();

    /// <summary>Gets the minor table version.</summary>
    /// <returns>The minor version.</returns>
    int GetMinorVersion();

    /// <summary>Gets the number of design axes.</summary>
    /// <returns>The axis count.</returns>
    int GetAxisCount();

    /// <summary>Gets one design axis.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The design axis.</returns>
    FontStatAxis GetAxis(int index);

    /// <summary>Finds a design axis by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The axis, or null.</returns>
    FontStatAxis? FindAxis(int axisTag);

    /// <summary>Gets the number of axis-value tables.</summary>
    /// <returns>The axis-value count.</returns>
    int GetAxisValueCount();

    /// <summary>Gets one axis-value table.</summary>
    /// <param name="index">The zero-based table index.</param>
    /// <returns>The axis-value model.</returns>
    FontStatAxisValue GetAxisValue(int index);

    /// <summary>Reports whether an elided fallback name is present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    bool HasElidedFallbackName();

    /// <summary>Gets the elided fallback name identifier.</summary>
    /// <returns>The name identifier, or 65535 when absent.</returns>
    int GetElidedFallbackNameId();

    /// <summary>Gets the selected localized elided fallback name.</summary>
    /// <returns>The localized fallback name, or null.</returns>
    FontNameValue? GetElidedFallbackName();
}
