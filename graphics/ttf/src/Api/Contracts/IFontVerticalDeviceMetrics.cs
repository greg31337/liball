namespace ttf;

/// <summary>Defines inspection of the legacy VDMX table.</summary>
public interface IFontVerticalDeviceMetrics
{
    /// <summary>Gets the table version.</summary>
    /// <returns>The version.</returns>
    int GetVersion();

    /// <summary>Gets the number of shared metric groups.</summary>
    /// <returns>The group count.</returns>
    int GetGroupCount();

    /// <summary>Gets a group's starting pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The starting height.</returns>
    int GetGroupStartingPixelHeight(int groupIndex);

    /// <summary>Gets a group's ending pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The ending height.</returns>
    int GetGroupEndingPixelHeight(int groupIndex);

    /// <summary>Gets a group's metric-record count.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <returns>The record count.</returns>
    int GetGroupRecordCount(int groupIndex);

    /// <summary>Gets one record's pixel height.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The pixel height.</returns>
    int GetRecordPixelHeight(int groupIndex, int recordIndex);

    /// <summary>Gets one record's maximum y extent.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The maximum y extent.</returns>
    int GetRecordMaximumY(int groupIndex, int recordIndex);

    /// <summary>Gets one record's minimum y extent.</summary>
    /// <param name="groupIndex">The zero-based group index.</param>
    /// <param name="recordIndex">The zero-based record index.</param>
    /// <returns>The minimum y extent.</returns>
    int GetRecordMinimumY(int groupIndex, int recordIndex);

    /// <summary>Gets the number of device-ratio records.</summary>
    /// <returns>The ratio count.</returns>
    int GetRatioCount();

    /// <summary>Reports whether a ratio is the default ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>True for the default ratio.</returns>
    bool IsDefaultRatio(int ratioIndex);

    /// <summary>Gets a ratio's character-set selector.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The character-set selector.</returns>
    int GetRatioCharacterSet(int ratioIndex);

    /// <summary>Gets a ratio's x ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The x ratio.</returns>
    int GetRatioX(int ratioIndex);

    /// <summary>Gets a ratio's starting y ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The starting y ratio.</returns>
    int GetRatioStartingY(int ratioIndex);

    /// <summary>Gets a ratio's ending y ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The ending y ratio.</returns>
    int GetRatioEndingY(int ratioIndex);

    /// <summary>Gets the group referenced by a ratio.</summary>
    /// <param name="ratioIndex">The zero-based ratio index.</param>
    /// <returns>The zero-based group index.</returns>
    int GetRatioGroupIndex(int ratioIndex);

    /// <summary>Finds the first matching device ratio.</summary>
    /// <param name="deviceXRatio">The positive device x ratio.</param>
    /// <param name="deviceYRatio">The positive device y ratio.</param>
    /// <returns>The ratio index, or negative one when absent.</returns>
    int FindRatioIndex(int deviceXRatio, int deviceYRatio);
}
