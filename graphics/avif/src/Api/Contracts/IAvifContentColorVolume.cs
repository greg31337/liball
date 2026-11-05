namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifContentColorVolume"/>.</summary>
public interface IAvifContentColorVolume
{
    /// <summary>Reports whether content primary chromaticities are present.</summary>
    /// <returns><c>true</c> when primaries are present.</returns>
    bool HasPrimaries();

    /// <summary>Reports whether minimum luminance is present.</summary>
    /// <returns><c>true</c> when minimum luminance is present.</returns>
    bool HasMinimumLuminance();

    /// <summary>Reports whether maximum luminance is present.</summary>
    /// <returns><c>true</c> when maximum luminance is present.</returns>
    bool HasMaximumLuminance();

    /// <summary>Reports whether average luminance is present.</summary>
    /// <returns><c>true</c> when average luminance is present.</returns>
    bool HasAverageLuminance();

    /// <summary>Returns one primary X coordinate in units of 0.0000001.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The coordinate, or zero when absent or invalid.</returns>
    int GetPrimaryChromaticityX(int index);

    /// <summary>Returns one primary Y coordinate in units of 0.0000001.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The coordinate, or zero when absent or invalid.</returns>
    int GetPrimaryChromaticityY(int index);

    /// <summary>Returns minimum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    uint GetMinimumLuminance();

    /// <summary>Returns maximum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    uint GetMaximumLuminance();

    /// <summary>Returns average luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    uint GetAverageLuminance();
}
