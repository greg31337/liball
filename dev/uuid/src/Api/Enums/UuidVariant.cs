namespace uuid;

/// <summary>Describes the bit-layout family encoded in a UUID variant field.</summary>
public enum UuidVariant
{
    /// <summary>The variant bits do not map to a recognized category.</summary>
    Unknown = 0,

    /// <summary>The legacy Network Computing System variant whose leading bit is zero.</summary>
    Ncs = 1,

    /// <summary>The variant used by RFC 9562 and its predecessor RFC 4122.</summary>
    Rfc9562 = 2,

    /// <summary>The reserved Microsoft compatibility variant.</summary>
    Microsoft = 3,

    /// <summary>The variant range reserved for future definitions.</summary>
    Future = 4
}
