namespace svg;

/// <summary>Defines nodes in a parsed CSS supports condition.</summary>
public enum SvgSupportsConditionKind
{
    /// <summary>A property declaration condition.</summary>
    Declaration = 0,
    /// <summary>A selector function condition.</summary>
    Selector = 1,
    /// <summary>A negated child condition.</summary>
    Not = 2,
    /// <summary>A conjunction of child conditions.</summary>
    And = 3,
    /// <summary>A disjunction of child conditions.</summary>
    Or = 4,
    /// <summary>A valid general-enclosed condition that safely evaluates false.</summary>
    Unknown = 5
}
