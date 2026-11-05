namespace uuid;

/// <summary>Identifies the text and binary format recognized for an identifier.</summary>
public enum IdentifierKind
{
    /// <summary>No supported identifier format was recognized.</summary>
    Unknown = 0,

    /// <summary>A UUID in canonical, compact, or UUID URN text form.</summary>
    Uuid = 1,

    /// <summary>A 26-character Universally Unique Lexicographically Sortable Identifier.</summary>
    Ulid = 2,

    /// <summary>A 27-character K-Sortable Unique Identifier.</summary>
    Ksuid = 3,

    /// <summary>A decimal 64-bit Snowflake identifier.</summary>
    Snowflake = 4,

    /// <summary>A 24-character MongoDB-style ObjectId.</summary>
    ObjectId = 5,

    /// <summary>A Nano ID that uses the configured alphabet and length.</summary>
    NanoId = 6
}
