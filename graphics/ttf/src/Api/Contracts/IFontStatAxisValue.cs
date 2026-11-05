namespace ttf;

/// <summary>Defines one public <c>STAT</c> axis-value table.</summary>
public interface IFontStatAxisValue
{
    /// <summary>Gets the axis-value format.</summary>
    /// <returns>The encoded format.</returns>
    int GetFormat();

    /// <summary>Reports whether the format is semantically supported.</summary>
    /// <returns>True when supported; otherwise false.</returns>
    bool IsSupported();

    /// <summary>Gets the axis index for formats 1 through 3.</summary>
    /// <returns>The axis index, or -1 for other formats.</returns>
    int GetAxisIndex();

    /// <summary>Gets the raw axis-value flags.</summary>
    /// <returns>The flags.</returns>
    int GetFlags();

    /// <summary>Reports the older-sibling-font attribute flag.</summary>
    /// <returns>True when set; otherwise false.</returns>
    bool IsOlderSiblingFontAttribute();

    /// <summary>Reports whether the value name is elidable.</summary>
    /// <returns>True when elidable; otherwise false.</returns>
    bool IsElidable();

    /// <summary>Gets the value name identifier.</summary>
    /// <returns>The name identifier, or zero for unsupported formats.</returns>
    int GetNameId();

    /// <summary>Gets the selected localized value name.</summary>
    /// <returns>The localized name, or null for unsupported formats.</returns>
    FontNameValue? GetName();

    /// <summary>Gets the value used by formats 1 and 3.</summary>
    /// <returns>The design-axis value.</returns>
    double GetValue();

    /// <summary>Gets the nominal value used by format 2.</summary>
    /// <returns>The nominal value.</returns>
    double GetNominalValue();

    /// <summary>Gets the inclusive range minimum used by format 2.</summary>
    /// <returns>The range minimum.</returns>
    double GetRangeMinimumValue();

    /// <summary>Gets the inclusive range maximum used by format 2.</summary>
    /// <returns>The range maximum.</returns>
    double GetRangeMaximumValue();

    /// <summary>Gets the linked value used by format 3.</summary>
    /// <returns>The linked value.</returns>
    double GetLinkedValue();

    /// <summary>Gets the number of format 4 axis records.</summary>
    /// <returns>The record count.</returns>
    int GetRecordCount();

    /// <summary>Gets one format 4 axis record.</summary>
    /// <param name="index">The zero-based record index.</param>
    /// <returns>The axis-value record.</returns>
    FontStatAxisValueRecord GetRecord(int index);
}
