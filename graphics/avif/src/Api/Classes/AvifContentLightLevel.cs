namespace avif;

/// <summary>
/// Describes static HDR content-light-level metadata.
/// </summary>
public class AvifContentLightLevel : IAvifContentLightLevel
{
    int maximumContentLightLevel;
    int maximumFrameAverageLightLevel;

    internal AvifContentLightLevel(
        int maximumContentLightLevel,
        int maximumFrameAverageLightLevel)
    {
        this.maximumContentLightLevel = maximumContentLightLevel;
        this.maximumFrameAverageLightLevel = maximumFrameAverageLightLevel;
    }

    internal AvifContentLightLevel CreateCopy()
    {
        return new AvifContentLightLevel(
            maximumContentLightLevel,
            maximumFrameAverageLightLevel);
    }

    internal bool ValuesEqual(AvifContentLightLevel other)
    {
        return other != null
            && maximumContentLightLevel == other.maximumContentLightLevel
            && maximumFrameAverageLightLevel == other.maximumFrameAverageLightLevel;
    }

    /// <summary>Returns the maximum content light level in candelas per square metre.</summary>
    /// <returns>The maximum content light level.</returns>
    public int GetMaximumContentLightLevel()
    {
        return maximumContentLightLevel;
    }

    /// <summary>Returns the maximum frame-average light level in candelas per square metre.</summary>
    /// <returns>The maximum frame-average light level.</returns>
    public int GetMaximumFrameAverageLightLevel()
    {
        return maximumFrameAverageLightLevel;
    }
}
