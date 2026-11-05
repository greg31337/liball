namespace fix;

/// <summary>Describes when a dictionary member may appear.</summary>
public enum FixPresence
{
    /// <summary>The member may be omitted.</summary>
    Optional = 0,
    /// <summary>The member must be present.</summary>
    Required = 1,
    /// <summary>The member must not be present.</summary>
    Forbidden = 2,
    /// <summary>The member is ignored by the scenario.</summary>
    Ignored = 3,
    /// <summary>The member has a constant value.</summary>
    Constant = 4,
    /// <summary>The member's presence is determined by a rule.</summary>
    Conditional = 5
}
