namespace tiff;

/// <summary>Specifies how a configured writer image is divided into stored segments.</summary>
public enum TiffWriterLayoutKind
{
    /// <summary>Stores the image in row-oriented strips.</summary>
    Strips = 1,
    /// <summary>Stores the image in rectangular or volumetric tiles.</summary>
    Tiles = 2
}
