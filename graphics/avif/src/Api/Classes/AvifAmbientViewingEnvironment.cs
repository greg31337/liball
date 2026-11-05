namespace avif;

/// <summary>
/// Describes ambient illuminance and the chromaticity of ambient light.
/// </summary>
public class AvifAmbientViewingEnvironment : IAvifAmbientViewingEnvironment
{
    uint ambientIlluminance;
    int ambientLightX;
    int ambientLightY;

    internal AvifAmbientViewingEnvironment(
        uint ambientIlluminance,
        int ambientLightX,
        int ambientLightY)
    {
        this.ambientIlluminance = ambientIlluminance;
        this.ambientLightX = ambientLightX;
        this.ambientLightY = ambientLightY;
    }

    internal AvifAmbientViewingEnvironment CreateCopy()
    {
        return new AvifAmbientViewingEnvironment(
            ambientIlluminance,
            ambientLightX,
            ambientLightY);
    }

    internal bool ValuesEqual(AvifAmbientViewingEnvironment other)
    {
        return other != null
            && ambientIlluminance == other.ambientIlluminance
            && ambientLightX == other.ambientLightX
            && ambientLightY == other.ambientLightY;
    }

    /// <summary>Returns ambient illuminance in units of 0.0001 lux.</summary>
    /// <returns>The ambient illuminance.</returns>
    public uint GetAmbientIlluminance()
    {
        return ambientIlluminance;
    }

    /// <summary>Returns the ambient-light X chromaticity in units of 0.00002.</summary>
    /// <returns>The X chromaticity.</returns>
    public int GetAmbientLightX()
    {
        return ambientLightX;
    }

    /// <summary>Returns the ambient-light Y chromaticity in units of 0.00002.</summary>
    /// <returns>The Y chromaticity.</returns>
    public int GetAmbientLightY()
    {
        return ambientLightY;
    }
}
