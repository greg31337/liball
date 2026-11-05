namespace cbor;

/// <summary>Identifies an occurrence constraint on a CDDL group entry.</summary>
public enum CddlOccurrenceKind
{
    /// <summary>No occurrence constraint has been assigned.</summary>
    None = 0,
    /// <summary>The entry occurs exactly once.</summary>
    ExactlyOne = 1,
    /// <summary>The entry is optional.</summary>
    Optional = 2,
    /// <summary>The entry occurs zero or more times.</summary>
    ZeroOrMore = 3,
    /// <summary>The entry occurs one or more times.</summary>
    OneOrMore = 4,
    /// <summary>The entry has explicit lower and upper bounds.</summary>
    Bounded = 5
}
