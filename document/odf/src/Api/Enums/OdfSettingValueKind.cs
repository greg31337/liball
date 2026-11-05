namespace odf;

/// <summary>
/// Identifies a configuration item's declared value type.
/// </summary>
public enum OdfSettingValueKind
{
    /// <summary>The value type is absent or unrecognized.</summary>
    Unknown = 0,
    /// <summary>An XML Schema Boolean value.</summary>
    Boolean = 1,
    /// <summary>A signed 16-bit integer value.</summary>
    Short = 2,
    /// <summary>A signed 32-bit integer value.</summary>
    Integer = 3,
    /// <summary>A signed 64-bit integer value.</summary>
    Long = 4,
    /// <summary>An XML Schema double value.</summary>
    Double = 5,
    /// <summary>A string value.</summary>
    String = 6,
    /// <summary>An XML Schema date-time value.</summary>
    DateTime = 7,
    /// <summary>An XML Schema base64 binary value.</summary>
    Base64Binary = 8
}
