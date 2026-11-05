namespace config;

/// <summary>Identifies the value conversion or container shape required by a type-validation rule.</summary>
public enum ConfigValidationValueType
{
    /// <summary>Accepts any present, non-null value.</summary>
    Any = 0,
    /// <summary>Requires a native configuration string.</summary>
    String = 1,
    /// <summary>Accepts values understood by Boolean conversion.</summary>
    Boolean = 2,
    /// <summary>Accepts values understood as a 32-bit integer.</summary>
    Integer = 3,
    /// <summary>Accepts values understood as a 64-bit integer.</summary>
    Long = 4,
    /// <summary>Accepts values understood as a decimal number.</summary>
    Decimal = 5,
    /// <summary>Accepts values understood as a double-precision number.</summary>
    Double = 6,
    /// <summary>Accepts standard or suffix-based durations.</summary>
    TimeSpan = 7,
    /// <summary>Accepts numeric or suffix-based byte sizes.</summary>
    ByteSize = 8,
    /// <summary>Accepts an array or scalar that can be represented as text items.</summary>
    StringArray = 9,
    /// <summary>Requires a native configuration array.</summary>
    Array = 10,
    /// <summary>Requires a native configuration object.</summary>
    Object = 11
}
