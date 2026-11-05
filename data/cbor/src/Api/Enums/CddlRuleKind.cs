namespace cbor;

/// <summary>Identifies how a CDDL rule defines or extends a type or group.</summary>
public enum CddlRuleKind
{
    /// <summary>No rule kind has been assigned.</summary>
    None = 0,
    /// <summary>A base type rule defined with <c>=</c>.</summary>
    Type = 1,
    /// <summary>A base group rule defined with <c>=</c>.</summary>
    Group = 2,
    /// <summary>A type extension defined with <c>/=</c>.</summary>
    TypeExtension = 3,
    /// <summary>A group extension defined with <c>//=</c>.</summary>
    GroupExtension = 4,
    /// <summary>A type socket rule.</summary>
    TypeSocket = 5,
    /// <summary>A group socket rule.</summary>
    GroupSocket = 6
}
