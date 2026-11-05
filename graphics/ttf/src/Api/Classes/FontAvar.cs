namespace ttf;

/// <summary>Represents public <c>avar</c> axis-mapping metadata.</summary>
public class FontAvar : IFontAvar
{
    SfntAvarTable table;

    internal FontAvar(SfntAvarTable table)
    {
        this.table = table;
    }

    /// <summary>Gets the major table version.</summary>
    /// <returns>The major version.</returns>
    public int GetMajorVersion() { return table.GetMajorVersion(); }

    /// <summary>Gets the minor table version.</summary>
    /// <returns>The minor version.</returns>
    public int GetMinorVersion() { return table.GetMinorVersion(); }

    /// <summary>Gets the number of axis segment maps.</summary>
    /// <returns>The map count.</returns>
    public int GetAxisMapCount() { return table.GetSegmentMapCount(); }

    /// <summary>Gets one axis segment map.</summary>
    /// <param name="index">The zero-based axis index.</param>
    /// <returns>The axis map.</returns>
    public FontAvarAxisMap GetAxisMap(int index)
    {
        return new FontAvarAxisMap(table.GetSegmentMap(index));
    }

    /// <summary>Finds an axis segment map by tag.</summary>
    /// <param name="axisTag">The big-endian axis tag.</param>
    /// <returns>The map, or null.</returns>
    public FontAvarAxisMap? FindAxisMap(int axisTag)
    {
        int index = 0;
        while (index < table.GetSegmentMapCount())
        {
            SfntAvarSegmentMap map = table.GetSegmentMap(index);
            if (map.GetAxisTag() == axisTag)
            {
                return new FontAvarAxisMap(map);
            }
            index += 1;
        }
        return null;
    }

    /// <summary>Reports whether version-two coordinate-dependent mappings are present.</summary>
    /// <returns>True when a variation store is present; otherwise false.</returns>
    public bool HasVariableMappings() { return table.GetVariationStore() != null; }
}
