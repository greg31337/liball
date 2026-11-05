namespace avif;

/// <summary>
/// Describes optional content-colour-volume primaries and luminance statistics.
/// </summary>
public class AvifContentColorVolume : IAvifContentColorVolume
{
    bool primariesPresent;
    bool minimumLuminancePresent;
    bool maximumLuminancePresent;
    bool averageLuminancePresent;
    int[] primaryChromaticityX;
    int[] primaryChromaticityY;
    uint minimumLuminance;
    uint maximumLuminance;
    uint averageLuminance;

    internal AvifContentColorVolume(
        bool primariesPresent,
        bool minimumLuminancePresent,
        bool maximumLuminancePresent,
        bool averageLuminancePresent,
        int[] primaryChromaticityX,
        int[] primaryChromaticityY,
        uint minimumLuminance,
        uint maximumLuminance,
        uint averageLuminance)
    {
        this.primariesPresent = primariesPresent;
        this.minimumLuminancePresent = minimumLuminancePresent;
        this.maximumLuminancePresent = maximumLuminancePresent;
        this.averageLuminancePresent = averageLuminancePresent;
        this.primaryChromaticityX = (int[])primaryChromaticityX.Clone();
        this.primaryChromaticityY = (int[])primaryChromaticityY.Clone();
        this.minimumLuminance = minimumLuminance;
        this.maximumLuminance = maximumLuminance;
        this.averageLuminance = averageLuminance;
    }

    internal AvifContentColorVolume CreateCopy()
    {
        return new AvifContentColorVolume(
            primariesPresent,
            minimumLuminancePresent,
            maximumLuminancePresent,
            averageLuminancePresent,
            primaryChromaticityX,
            primaryChromaticityY,
            minimumLuminance,
            maximumLuminance,
            averageLuminance);
    }

    internal bool ValuesEqual(AvifContentColorVolume other)
    {
        if (other == null
            || primariesPresent != other.primariesPresent
            || minimumLuminancePresent != other.minimumLuminancePresent
            || maximumLuminancePresent != other.maximumLuminancePresent
            || averageLuminancePresent != other.averageLuminancePresent
            || minimumLuminance != other.minimumLuminance
            || maximumLuminance != other.maximumLuminance
            || averageLuminance != other.averageLuminance)
        {
            return false;
        }

        int index = 0;
        while (index < 3)
        {
            if (primaryChromaticityX[index] != other.primaryChromaticityX[index]
                || primaryChromaticityY[index] != other.primaryChromaticityY[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    /// <summary>Reports whether content primary chromaticities are present.</summary>
    /// <returns><c>true</c> when primaries are present.</returns>
    public bool HasPrimaries()
    {
        return primariesPresent;
    }

    /// <summary>Reports whether minimum luminance is present.</summary>
    /// <returns><c>true</c> when minimum luminance is present.</returns>
    public bool HasMinimumLuminance()
    {
        return minimumLuminancePresent;
    }

    /// <summary>Reports whether maximum luminance is present.</summary>
    /// <returns><c>true</c> when maximum luminance is present.</returns>
    public bool HasMaximumLuminance()
    {
        return maximumLuminancePresent;
    }

    /// <summary>Reports whether average luminance is present.</summary>
    /// <returns><c>true</c> when average luminance is present.</returns>
    public bool HasAverageLuminance()
    {
        return averageLuminancePresent;
    }

    /// <summary>Returns one primary X coordinate in units of 0.0000001.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The coordinate, or zero when absent or invalid.</returns>
    public int GetPrimaryChromaticityX(int index)
    {
        if (!primariesPresent || index < 0 || index >= primaryChromaticityX.Length)
        {
            return 0;
        }

        return primaryChromaticityX[index];
    }

    /// <summary>Returns one primary Y coordinate in units of 0.0000001.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The coordinate, or zero when absent or invalid.</returns>
    public int GetPrimaryChromaticityY(int index)
    {
        if (!primariesPresent || index < 0 || index >= primaryChromaticityY.Length)
        {
            return 0;
        }

        return primaryChromaticityY[index];
    }

    /// <summary>Returns minimum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    public uint GetMinimumLuminance()
    {
        return minimumLuminance;
    }

    /// <summary>Returns maximum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    public uint GetMaximumLuminance()
    {
        return maximumLuminance;
    }

    /// <summary>Returns average luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The value, or zero when absent.</returns>
    public uint GetAverageLuminance()
    {
        return averageLuminance;
    }
}
