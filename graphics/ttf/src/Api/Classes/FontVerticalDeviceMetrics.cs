namespace ttf;

/// <summary>Contains legacy VDMX vertical device extents.</summary>
public class FontVerticalDeviceMetrics : IFontVerticalDeviceMetrics
{
    SfntVdmxTable table;

    internal FontVerticalDeviceMetrics(SfntVdmxTable table)
    {
        this.table = table;
    }

    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    public int GetVersion() { return table.GetVersion(); }

    /// <summary>Gets the number of shared metric groups.</summary>
    /// <returns>The group count.</returns>
    public int GetGroupCount() { return table.GetGroupCount(); }

    /// <summary>Gets a group's starting pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The starting height.</returns>
    public int GetGroupStartingPixelHeight(int groupIndex)
    {
        return GetGroup(groupIndex).GetStartingPixelHeight();
    }

    /// <summary>Gets a group's ending pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The ending height.</returns>
    public int GetGroupEndingPixelHeight(int groupIndex)
    {
        return GetGroup(groupIndex).GetEndingPixelHeight();
    }

    /// <summary>Gets a group's metric-record count.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The record count.</returns>
    public int GetGroupRecordCount(int groupIndex)
    {
        return GetGroup(groupIndex).GetRecordCount();
    }

    /// <summary>Gets one record's pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The pixel height.</returns>
    public int GetRecordPixelHeight(int groupIndex, int recordIndex)
    {
        return GetRecord(groupIndex, recordIndex).GetPixelHeight();
    }

    /// <summary>Gets one record's maximum y extent.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The maximum y extent.</returns>
    public int GetRecordMaximumY(int groupIndex, int recordIndex)
    {
        return GetRecord(groupIndex, recordIndex).GetMaximumY();
    }

    /// <summary>Gets one record's minimum y extent.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The minimum y extent.</returns>
    public int GetRecordMinimumY(int groupIndex, int recordIndex)
    {
        return GetRecord(groupIndex, recordIndex).GetMinimumY();
    }

    /// <summary>Gets the number of device-ratio records.</summary>
    /// <returns>The ratio count.</returns>
    public int GetRatioCount() { return table.GetRatioCount(); }

    /// <summary>Reports whether a ratio is the default ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>True for the default ratio.</returns>
    public bool IsDefaultRatio(int ratioIndex)
    {
        return GetRatio(ratioIndex).IsDefaultRatio();
    }

    /// <summary>Gets a ratio's character-set selector.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The character-set selector.</returns>
    public int GetRatioCharacterSet(int ratioIndex)
    {
        return GetRatio(ratioIndex).GetCharacterSet();
    }

    /// <summary>Gets a ratio's x ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The x ratio.</returns>
    public int GetRatioX(int ratioIndex) { return GetRatio(ratioIndex).GetXRatio(); }

    /// <summary>Gets a ratio's starting y ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The starting y ratio.</returns>
    public int GetRatioStartingY(int ratioIndex)
    {
        return GetRatio(ratioIndex).GetStartingYRatio();
    }

    /// <summary>Gets a ratio's ending y ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The ending y ratio.</returns>
    public int GetRatioEndingY(int ratioIndex)
    {
        return GetRatio(ratioIndex).GetEndingYRatio();
    }

    /// <summary>Gets the group referenced by a ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The zero-based group index.</returns>
    public int GetRatioGroupIndex(int ratioIndex)
    {
        SfntVdmxGroup group = GetRatio(ratioIndex).GetGroup();
        int index = 0;

        while (index < table.GetGroupCount())
        {
            if (table.GetGroup(index) == group)
            {
                return index;
            }

            index += 1;
        }

        throw new System.InvalidOperationException(
            "A VDMX ratio references an unknown group.");
    }

    /// <summary>Finds the first matching device ratio.</summary>
    /// <param name="deviceXRatio">The positive device x ratio.</param>
    /// <param name="deviceYRatio">The positive device y ratio.</param>
    /// <returns>The ratio index, or negative one when absent.</returns>
    public int FindRatioIndex(int deviceXRatio, int deviceYRatio)
    {
        if (deviceXRatio <= 0)
        {
            throw new System.ArgumentOutOfRangeException("deviceXRatio");
        }

        if (deviceYRatio <= 0)
        {
            throw new System.ArgumentOutOfRangeException("deviceYRatio");
        }

        int index = 0;

        while (index < table.GetRatioCount())
        {
            if (table.GetRatio(index).Matches(deviceXRatio, deviceYRatio))
            {
                return index;
            }

            index += 1;
        }

        return -1;
    }

    SfntVdmxGroup GetGroup(int index)
    {
        if (index < 0 || index >= table.GetGroupCount())
        {
            throw new System.ArgumentOutOfRangeException("groupIndex");
        }

        return table.GetGroup(index);
    }

    SfntVdmxRecord GetRecord(int groupIndex, int recordIndex)
    {
        SfntVdmxGroup group = GetGroup(groupIndex);

        if (recordIndex < 0 || recordIndex >= group.GetRecordCount())
        {
            throw new System.ArgumentOutOfRangeException("recordIndex");
        }

        return group.GetRecord(recordIndex);
    }

    SfntVdmxRatio GetRatio(int index)
    {
        if (index < 0 || index >= table.GetRatioCount())
        {
            throw new System.ArgumentOutOfRangeException("ratioIndex");
        }

        return table.GetRatio(index);
    }
}
