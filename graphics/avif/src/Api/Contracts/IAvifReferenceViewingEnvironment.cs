namespace avif;

/// <summary>Defines the public instance operations provided by <see cref="AvifReferenceViewingEnvironment"/>.</summary>
public interface IAvifReferenceViewingEnvironment
{
    /// <summary>Returns surround luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The surround luminance.</returns>
    uint GetSurroundLuminance();

    /// <summary>Returns the surround-light X chromaticity in units of 0.0001.</summary>
    /// <returns>The X chromaticity.</returns>
    int GetSurroundLightX();

    /// <summary>Returns the surround-light Y chromaticity in units of 0.0001.</summary>
    /// <returns>The Y chromaticity.</returns>
    int GetSurroundLightY();

    /// <summary>Returns periphery luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The periphery luminance.</returns>
    uint GetPeripheryLuminance();

    /// <summary>Returns the periphery-light X chromaticity in units of 0.0001.</summary>
    /// <returns>The X chromaticity.</returns>
    int GetPeripheryLightX();

    /// <summary>Returns the periphery-light Y chromaticity in units of 0.0001.</summary>
    /// <returns>The Y chromaticity.</returns>
    int GetPeripheryLightY();
}
