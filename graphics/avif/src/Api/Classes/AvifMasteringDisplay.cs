namespace avif;

/// <summary>
/// Describes mastering-display chromaticities and luminance bounds.
/// </summary>
public class AvifMasteringDisplay : IAvifMasteringDisplay
{
    int[] primaryChromaticityX;
    int[] primaryChromaticityY;
    int whitePointChromaticityX;
    int whitePointChromaticityY;
    uint maximumLuminance;
    uint minimumLuminance;

    internal AvifMasteringDisplay(
        int[] primaryChromaticityX,
        int[] primaryChromaticityY,
        int whitePointChromaticityX,
        int whitePointChromaticityY,
        uint maximumLuminance,
        uint minimumLuminance)
    {
        this.primaryChromaticityX = (int[])primaryChromaticityX.Clone();
        this.primaryChromaticityY = (int[])primaryChromaticityY.Clone();
        this.whitePointChromaticityX = whitePointChromaticityX;
        this.whitePointChromaticityY = whitePointChromaticityY;
        this.maximumLuminance = maximumLuminance;
        this.minimumLuminance = minimumLuminance;
    }

    internal AvifMasteringDisplay CreateCopy()
    {
        return new AvifMasteringDisplay(
            primaryChromaticityX,
            primaryChromaticityY,
            whitePointChromaticityX,
            whitePointChromaticityY,
            maximumLuminance,
            minimumLuminance);
    }

    internal bool ValuesEqual(AvifMasteringDisplay other)
    {
        if (other == null
            || whitePointChromaticityX != other.whitePointChromaticityX
            || whitePointChromaticityY != other.whitePointChromaticityY
            || maximumLuminance != other.maximumLuminance
            || minimumLuminance != other.minimumLuminance)
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

    /// <summary>Returns one primary chromaticity X coordinate in units of 0.00002.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The X coordinate, or zero for an invalid index.</returns>
    public int GetPrimaryChromaticityX(int index)
    {
        if (index < 0 || index >= primaryChromaticityX.Length)
        {
            return 0;
        }

        return primaryChromaticityX[index];
    }

    /// <summary>Returns one primary chromaticity Y coordinate in units of 0.00002.</summary>
    /// <param name="index">The primary index from zero through two.</param>
    /// <returns>The Y coordinate, or zero for an invalid index.</returns>
    public int GetPrimaryChromaticityY(int index)
    {
        if (index < 0 || index >= primaryChromaticityY.Length)
        {
            return 0;
        }

        return primaryChromaticityY[index];
    }

    /// <summary>Returns the white-point X coordinate in units of 0.00002.</summary>
    /// <returns>The white-point X coordinate.</returns>
    public int GetWhitePointChromaticityX()
    {
        return whitePointChromaticityX;
    }

    /// <summary>Returns the white-point Y coordinate in units of 0.00002.</summary>
    /// <returns>The white-point Y coordinate.</returns>
    public int GetWhitePointChromaticityY()
    {
        return whitePointChromaticityY;
    }

    /// <summary>Returns the mastering-display maximum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The maximum luminance.</returns>
    public uint GetMaximumLuminance()
    {
        return maximumLuminance;
    }

    /// <summary>Returns the mastering-display minimum luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The minimum luminance.</returns>
    public uint GetMinimumLuminance()
    {
        return minimumLuminance;
    }
}
