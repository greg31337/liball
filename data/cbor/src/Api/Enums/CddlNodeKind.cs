namespace cbor;

/// <summary>Identifies the syntactic role of a CDDL AST node.</summary>
public enum CddlNodeKind
{
    /// <summary>No node kind has been assigned.</summary>
    None = 0,
    /// <summary>A choice between type alternatives.</summary>
    TypeChoice = 1,
    /// <summary>A choice between group alternatives.</summary>
    GroupChoice = 2,
    /// <summary>An ordered group of entries.</summary>
    Group = 3,
    /// <summary>One entry in an array, map, or group.</summary>
    GroupEntry = 4,
    /// <summary>An array type.</summary>
    Array = 5,
    /// <summary>A map type.</summary>
    Map = 6,
    /// <summary>A literal value.</summary>
    Literal = 7,
    /// <summary>A reference to a named rule or generic parameter.</summary>
    RuleReference = 8,
    /// <summary>A control-operator expression.</summary>
    Control = 9,
    /// <summary>An inclusive or exclusive-upper range.</summary>
    Range = 10,
    /// <summary>An unwrapping expression.</summary>
    Unwrap = 11,
    /// <summary>A group-to-choice enumeration.</summary>
    GroupEnumeration = 12,
    /// <summary>A semantic tag expression.</summary>
    Tag = 13,
    /// <summary>A CBOR major-type representation expression.</summary>
    Representation = 14,
    /// <summary>A parenthesized type expression.</summary>
    ParenthesizedType = 15,
    /// <summary>A parenthesized group expression.</summary>
    ParenthesizedGroup = 16
}
