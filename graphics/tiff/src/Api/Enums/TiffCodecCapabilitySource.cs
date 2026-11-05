namespace tiff;

/// <summary>Specifies the source that provides one direction of TIFF compression support.</summary>
public enum TiffCodecCapabilitySource
{
    /// <summary>Specifies that no codec is available for the direction.</summary>
    Unavailable = 0,
    /// <summary>Specifies that the direction is provided by the library's built-in codec.</summary>
    BuiltIn = 1,
    /// <summary>Specifies that the direction is provided by an application-supplied codec.</summary>
    ApplicationProvided = 2
}
