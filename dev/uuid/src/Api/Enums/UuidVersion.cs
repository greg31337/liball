namespace uuid;

/// <summary>Identifies the standardized version encoded in a UUID.</summary>
public enum UuidVersion
{
    /// <summary>The version nibble does not identify a supported UUID version.</summary>
    Unknown = 0,

    /// <summary>A time-based UUID using the original Gregorian timestamp layout.</summary>
    Version1 = 1,

    /// <summary>A name-based UUID produced with MD5.</summary>
    Version3 = 3,

    /// <summary>A randomly generated UUID.</summary>
    Version4 = 4,

    /// <summary>A name-based UUID produced with SHA-1.</summary>
    Version5 = 5,

    /// <summary>A reordered time-based UUID designed for database locality.</summary>
    Version6 = 6,

    /// <summary>A Unix-millisecond time-ordered UUID.</summary>
    Version7 = 7,

    /// <summary>An application-defined UUID layout.</summary>
    Version8 = 8
}
