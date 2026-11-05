namespace ttf;

/// <summary>Represents one localized <c>fvar</c> named instance.</summary>
public class FontNamedInstance : IFontNamedInstance
{
    int index;
    SfntFvarTable table;
    SfntFvarInstance instance;
    FontNameValue subfamilyName;
    FontNameValue? postScriptName;

    internal FontNamedInstance(
        int index,
        SfntFvarTable table,
        SfntFvarInstance instance,
        FontNames names)
    {
        this.index = index;
        this.table = table;
        this.instance = instance;
        FontNameValue? selected = names.GetName(instance.GetSubfamilyNameId());
        if (selected == null)
        {
            throw FontFormatException.Create("A named-instance subfamily name cannot be resolved.");
        }
        subfamilyName = selected;
        postScriptName = instance.HasPostScriptName() ?
            names.GetName(instance.GetPostScriptNameId()) : null;
        if (instance.HasPostScriptName() && postScriptName == null)
        {
            throw FontFormatException.Create("A named-instance PostScript name cannot be resolved.");
        }
    }

    /// <summary>Gets this instance's table-order index.</summary>
    /// <returns>The zero-based index.</returns>
    public int GetIndex() { return index; }

    /// <summary>Gets the raw instance flags.</summary>
    /// <returns>The flags.</returns>
    public int GetFlags() { return instance.GetFlags(); }

    /// <summary>Gets the subfamily name identifier.</summary>
    /// <returns>The OpenType name identifier.</returns>
    public int GetSubfamilyNameId() { return instance.GetSubfamilyNameId(); }

    /// <summary>Gets the selected localized subfamily name.</summary>
    /// <returns>The localized name and its selection provenance.</returns>
    public FontNameValue GetSubfamilyName() { return subfamilyName; }

    /// <summary>Gets the number of axis coordinates.</summary>
    /// <returns>The coordinate count.</returns>
    public int GetCoordinateCount() { return instance.GetCoordinateCount(); }

    /// <summary>Gets one coordinate's axis tag.</summary>
    /// <param name="coordinateIndex">The zero-based axis index.</param>
    /// <returns>The big-endian axis tag.</returns>
    public int GetAxisTag(int coordinateIndex)
    {
        return table.GetAxis(coordinateIndex).GetTag();
    }

    /// <summary>Gets one user-space coordinate.</summary>
    /// <param name="coordinateIndex">The zero-based axis index.</param>
    /// <returns>The coordinate value.</returns>
    public double GetCoordinate(int coordinateIndex)
    {
        return instance.GetCoordinate(coordinateIndex);
    }

    /// <summary>Reports whether a PostScript name is present.</summary>
    /// <returns>True when present; otherwise false.</returns>
    public bool HasPostScriptName() { return instance.HasPostScriptName(); }

    /// <summary>Gets the PostScript name identifier.</summary>
    /// <returns>The name identifier, or 65535 when absent.</returns>
    public int GetPostScriptNameId() { return instance.GetPostScriptNameId(); }

    /// <summary>Gets the selected localized PostScript name.</summary>
    /// <returns>The localized name, or null when absent.</returns>
    public FontNameValue? GetPostScriptName() { return postScriptName; }
}
