namespace icc;

/// <summary>
/// Identifies the semantic class of an ICC profile.
/// </summary>
public enum IccProfileClass
{
    /// <summary>
    /// Indicates an unknown profile class.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Identifies an input-device profile.
    /// </summary>
    Input = 1,

    /// <summary>
    /// Identifies a display-device profile.
    /// </summary>
    Display = 2,

    /// <summary>
    /// Identifies an output-device profile.
    /// </summary>
    Output = 3,

    /// <summary>
    /// Identifies a device-link profile.
    /// </summary>
    DeviceLink = 4,

    /// <summary>
    /// Identifies a colour-space conversion profile.
    /// </summary>
    ColorSpace = 5,

    /// <summary>
    /// Identifies an abstract profile.
    /// </summary>
    Abstract = 6,

    /// <summary>
    /// Identifies a named-colour profile.
    /// </summary>
    NamedColor = 7,

    /// <summary>
    /// Identifies an ICC.2 colour-encoding-space profile.
    /// </summary>
    ColorEncodingSpace = 8,

    /// <summary>
    /// Identifies an ICC.2 multiplex-identification profile.
    /// </summary>
    MultiplexIdentification = 9,

    /// <summary>
    /// Identifies an ICC.2 multiplex-link profile.
    /// </summary>
    MultiplexLink = 10,

    /// <summary>
    /// Identifies an ICC.2 multiplex-visualization profile.
    /// </summary>
    MultiplexVisualization = 11
}
