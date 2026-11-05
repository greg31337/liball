namespace tiff;

/// <summary>Identifies the JSON value represented by an ImageDescription node.</summary>
public enum TiffJsonValueKind
{
    /// <summary>Specifies no JSON value.</summary>
    None = 0,
    /// <summary>Specifies a JSON object.</summary>
    Object = 1,
    /// <summary>Specifies a JSON array.</summary>
    Array = 2,
    /// <summary>Specifies a JSON string.</summary>
    String = 3,
    /// <summary>Specifies a JSON number.</summary>
    Number = 4,
    /// <summary>Specifies a JSON Boolean value.</summary>
    Boolean = 5,
    /// <summary>Specifies a JSON null value.</summary>
    Null = 6
}
