namespace ttf;

/// <summary>Represents one localized <c>STAT</c> axis-value table.</summary>
public class FontStatAxisValue : IFontStatAxisValue
{
    SfntStatAxisValue value;
    SfntStatTable table;
    FontNameValue? name;

    internal FontStatAxisValue(
        SfntStatAxisValue value,
        SfntStatTable table,
        FontNames names)
    {
        this.value = value;
        this.table = table;
        name = value.IsSupported() ? names.GetName(value.GetNameId()) : null;
        if (value.IsSupported() && name == null)
        {
            throw FontFormatException.Create("A STAT axis-value name cannot be resolved.");
        }
    }

    /// <summary>Gets the axis-value format.</summary>
    /// <returns>The encoded format.</returns>
    public int GetFormat() { return value.GetFormat(); }

    /// <summary>Reports whether the format is semantically supported.</summary>
    /// <returns>True when supported; otherwise false.</returns>
    public bool IsSupported() { return value.IsSupported(); }

    /// <summary>Gets the axis index for formats 1 through 3.</summary>
    /// <returns>The axis index, or -1 for other formats.</returns>
    public int GetAxisIndex() { return value.GetAxisIndex(); }

    /// <summary>Gets the raw axis-value flags.</summary>
    /// <returns>The flags.</returns>
    public int GetFlags() { return value.GetFlags(); }

    /// <summary>Reports the older-sibling-font attribute flag.</summary>
    /// <returns>True when set; otherwise false.</returns>
    public bool IsOlderSiblingFontAttribute()
    {
        return value.IsOlderSiblingFontAttribute();
    }

    /// <summary>Reports whether the value name is elidable.</summary>
    /// <returns>True when elidable; otherwise false.</returns>
    public bool IsElidable() { return value.IsElidable(); }

    /// <summary>Gets the value name identifier.</summary>
    /// <returns>The name identifier, or zero for unsupported formats.</returns>
    public int GetNameId() { return value.GetNameId(); }

    /// <summary>Gets the selected localized value name.</summary>
    /// <returns>The localized name, or null for unsupported formats.</returns>
    public FontNameValue? GetName() { return name; }

    /// <summary>Gets the value used by formats 1 and 3.</summary>
    /// <returns>The design-axis value.</returns>
    public double GetValue() { return value.GetValue(); }

    /// <summary>Gets the nominal value used by format 2.</summary>
    /// <returns>The nominal value.</returns>
    public double GetNominalValue() { return value.GetNominalValue(); }

    /// <summary>Gets the inclusive range minimum used by format 2.</summary>
    /// <returns>The range minimum.</returns>
    public double GetRangeMinimumValue() { return value.GetRangeMinimumValue(); }

    /// <summary>Gets the inclusive range maximum used by format 2.</summary>
    /// <returns>The range maximum.</returns>
    public double GetRangeMaximumValue() { return value.GetRangeMaximumValue(); }

    /// <summary>Gets the linked value used by format 3.</summary>
    /// <returns>The linked value.</returns>
    public double GetLinkedValue() { return value.GetLinkedValue(); }

    /// <summary>Gets the number of format 4 axis records.</summary>
    /// <returns>The record count.</returns>
    public int GetRecordCount() { return value.GetRecordCount(); }

    /// <summary>Gets one format 4 axis record.</summary>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The axis-value record.</returns>
    public FontStatAxisValueRecord GetRecord(int index)
    {
        return new FontStatAxisValueRecord(value.GetRecord(index), table);
    }
}
