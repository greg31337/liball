namespace ttf;

/// <summary>Defines one public <c>fvar</c> named instance.</summary>
public interface IFontNamedInstance
{
    /// <summary>Gets this instance's table-order index.</summary>
    /// <returns>The zero-based index.</returns>
    int GetIndex();

    /// <summary>Gets the raw instance flags.</summary>
    /// <returns>The flags.</returns>
    int GetFlags();

    /// <summary>Gets the subfamily name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    int GetSubfamilyNameId();

    /// <summary>Gets the selected localized subfamily name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    FontNameValue GetSubfamilyName();

    /// <summary>Gets the number of axis coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    int GetCoordinateCount();

    /// <summary>Gets one coordinate's axis tag.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The big-endian axis tag.</returns>
    int GetAxisTag(int index);

    /// <summary>Gets one user-space coordinate.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The coordinate value.</returns>
    double GetCoordinate(int index);

    /// <summary>Reports whether a PostScript name is present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    bool HasPostScriptName();

    /// <summary>Gets the PostScript name identifier.</summary>
    /// <returns>The name identifier, or 65535 when absent.</returns>
    int GetPostScriptNameId();

    /// <summary>Gets the selected localized PostScript name.</summary>
    /// <returns>The localized name, or null when absent.</returns>
    FontNameValue? GetPostScriptName();
}
