namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifContentLightLevel"/>.</summary>
public interface IAvifContentLightLevel
{
    /// <summary>Returns the maximum content light level in candelas per square metre.</summary>
    /// <returns>The maximum content light level.</returns>
    int GetMaximumContentLightLevel();

    /// <summary>Returns the maximum frame-average light level in candelas per square metre.</summary>
    /// <returns>The maximum frame-average light level.</returns>
    int GetMaximumFrameAverageLightLevel();
}
