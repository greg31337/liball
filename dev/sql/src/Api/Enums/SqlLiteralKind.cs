namespace sql;

/// <summary>Specifies the supported literal kind.</summary>
public enum SqlLiteralKind
{
    /// <summary>No recognized value is available.</summary>
    Unknown = 0,
    /// <summary>Represents null.</summary>
    Null = 1,
    /// <summary>Represents integer.</summary>
    Integer = 2,
    /// <summary>Represents decimal.</summary>
    Decimal = 3,
    /// <summary>Represents string.</summary>
    String = 4,
    /// <summary>Represents blob.</summary>
    Blob = 5,
    /// <summary>Represents boolean.</summary>
    Boolean = 6,
    /// <summary>Represents current date.</summary>
    CurrentDate = 7,
    /// <summary>Represents current time.</summary>
    CurrentTime = 8,
    /// <summary>Represents current timestamp.</summary>
    CurrentTimestamp = 9
}
