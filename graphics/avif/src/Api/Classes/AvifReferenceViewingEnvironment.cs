namespace avif;

/// <summary>
/// Describes reference surround and periphery viewing conditions.
/// </summary>
public class AvifReferenceViewingEnvironment : IAvifReferenceViewingEnvironment
{
    uint surroundLuminance;
    int surroundLightX;
    int surroundLightY;
    uint peripheryLuminance;
    int peripheryLightX;
    int peripheryLightY;

    internal AvifReferenceViewingEnvironment(
        uint surroundLuminance,
        int surroundLightX,
        int surroundLightY,
        uint peripheryLuminance,
        int peripheryLightX,
        int peripheryLightY)
    {
        this.surroundLuminance = surroundLuminance;
        this.surroundLightX = surroundLightX;
        this.surroundLightY = surroundLightY;
        this.peripheryLuminance = peripheryLuminance;
        this.peripheryLightX = peripheryLightX;
        this.peripheryLightY = peripheryLightY;
    }

    internal AvifReferenceViewingEnvironment CreateCopy()
    {
        return new AvifReferenceViewingEnvironment(
            surroundLuminance,
            surroundLightX,
            surroundLightY,
            peripheryLuminance,
            peripheryLightX,
            peripheryLightY);
    }

    internal bool ValuesEqual(AvifReferenceViewingEnvironment other)
    {
        return other != null
            && surroundLuminance == other.surroundLuminance
            && surroundLightX == other.surroundLightX
            && surroundLightY == other.surroundLightY
            && peripheryLuminance == other.peripheryLuminance
            && peripheryLightX == other.peripheryLightX
            && peripheryLightY == other.peripheryLightY;
    }

    /// <summary>Returns surround luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The surround luminance.</returns>
    public uint GetSurroundLuminance()
    {
        return surroundLuminance;
    }

    /// <summary>Returns the surround-light X chromaticity in units of 0.0001.</summary>
    /// <returns>The X chromaticity.</returns>
    public int GetSurroundLightX()
    {
        return surroundLightX;
    }

    /// <summary>Returns the surround-light Y chromaticity in units of 0.0001.</summary>
    /// <returns>The Y chromaticity.</returns>
    public int GetSurroundLightY()
    {
        return surroundLightY;
    }

    /// <summary>Returns periphery luminance in units of 0.0001 candela per square metre.</summary>
    /// <returns>The periphery luminance.</returns>
    public uint GetPeripheryLuminance()
    {
        return peripheryLuminance;
    }

    /// <summary>Returns the periphery-light X chromaticity in units of 0.0001.</summary>
    /// <returns>The X chromaticity.</returns>
    public int GetPeripheryLightX()
    {
        return peripheryLightX;
    }

    /// <summary>Returns the periphery-light Y chromaticity in units of 0.0001.</summary>
    /// <returns>The Y chromaticity.</returns>
    public int GetPeripheryLightY()
    {
        return peripheryLightY;
    }
}
