namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifMasteringDisplay"/>.</summary>
public interface IAvifMasteringDisplay
{
    /// <summary>Returns one primary chromaticity X coordinate in units of 0.00002.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The X coordinate, or zero for an invalid index.</returns>
    int GetPrimaryChromaticityX(int index);

    /// <summary>Returns one primary chromaticity Y coordinate in units of 0.00002.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The Y coordinate, or zero for an invalid index.</returns>
    int GetPrimaryChromaticityY(int index);

    /// <summary>Returns the white-point X coordinate in units of 0.00002.</summary>
    /// <returns>The white-point X coordinate.</returns>
    int GetWhitePointChromaticityX();

    /// <summary>Returns the white-point Y coordinate in units of 0.00002.</summary>
    /// <returns>The white-point Y coordinate.</returns>
    int GetWhitePointChromaticityY();

    /// <summary>Returns the mastering-display maximum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The maximum luminance.</returns>
    uint GetMaximumLuminance();

    /// <summary>Returns the mastering-display minimum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The minimum luminance.</returns>
    uint GetMinimumLuminance();
}
