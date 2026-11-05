namespace iso20022;

/// <summary>Identifies one normalized repository, message, profile, or mapping difference.</summary>
public enum DifferenceKind
{
    /// <summary>The difference kind is absent or not recognized.</summary>
    Unknown = 0,
    /// <summary>The compared content is unchanged and was explicitly retained.</summary>
    Unchanged = 1,
    /// <summary>Content exists only in the target.</summary>
    Added = 2,
    /// <summary>Content exists only in the source.</summary>
    Removed = 3,
    /// <summary>Content changed without a more specific classification.</summary>
    Modified = 4,
    /// <summary>Matched content has a different name.</summary>
    Renamed = 5,
    /// <summary>Matched content has a different containing location.</summary>
    Moved = 6,
    /// <summary>A data or component type changed.</summary>
    TypeChanged = 7,
    /// <summary>A minimum or maximum occurrence changed.</summary>
    CardinalityChanged = 8,
    /// <summary>A primitive or schema facet changed.</summary>
    FacetChanged = 9,
    /// <summary>A code exists only in the target code set.</summary>
    CodeAdded = 10,
    /// <summary>A code exists only in the source code set.</summary>
    CodeRemoved = 11,
    /// <summary>A matched code value or definition changed.</summary>
    CodeChanged = 12,
    /// <summary>A constraint exists only in the target.</summary>
    ConstraintAdded = 13,
    /// <summary>A constraint exists only in the source.</summary>
    ConstraintRemoved = 14,
    /// <summary>A matched constraint changed.</summary>
    ConstraintChanged = 15,
    /// <summary>Registration or lifecycle metadata changed.</summary>
    RegistrationChanged = 16,
    /// <summary>A repository relationship exists only in the target.</summary>
    RelationshipAdded = 17,
    /// <summary>A repository relationship exists only in the source.</summary>
    RelationshipRemoved = 18,
    /// <summary>A matched repository relationship changed.</summary>
    RelationshipChanged = 19,
    /// <summary>Syntax or encoding metadata changed.</summary>
    SyntaxChanged = 20,
    /// <summary>Transport or endpoint metadata changed.</summary>
    TransportChanged = 21,
    /// <summary>Semantic fallback produced more than one plausible match.</summary>
    AmbiguousMatch = 22,
    /// <summary>Concurrent or profile changes conflict.</summary>
    Conflict = 23
}
