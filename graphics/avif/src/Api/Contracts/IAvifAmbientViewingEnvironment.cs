namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifAmbientViewingEnvironment"/>.</summary>
public interface IAvifAmbientViewingEnvironment
{
    /// <summary>Returns ambient illuminance in units of 0.0001 lux.</summary>
    /// <returns>The ambient illuminance.</returns>
    uint GetAmbientIlluminance();

    /// <summary>Returns the ambient-light X chromaticity in units of 0.00002.</summary>
    /// <returns>The X chromaticity.</returns>
    int GetAmbientLightX();

    /// <summary>Returns the ambient-light Y chromaticity in units of 0.00002.</summary>
    /// <returns>The Y chromaticity.</returns>
    int GetAmbientLightY();
}
