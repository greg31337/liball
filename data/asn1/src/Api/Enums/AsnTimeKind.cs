namespace asn1;

/// <summary>
/// Identifies an ASN.1 useful or time type.
/// </summary>
public enum AsnTimeKind
{
    /// <summary>
    /// Represents utc time.
    /// </summary>
    UtcTime = 0,

    /// <summary>
    /// Represents generalized time.
    /// </summary>
    GeneralizedTime = 1,

    /// <summary>
    /// Represents time.
    /// </summary>
    Time = 2,

    /// <summary>
    /// Represents date.
    /// </summary>
    Date = 3,

    /// <summary>
    /// Represents time of day.
    /// </summary>
    TimeOfDay = 4,

    /// <summary>
    /// Represents date time.
    /// </summary>
    DateTime = 5,

    /// <summary>
    /// Represents duration.
    /// </summary>
    Duration = 6
}
