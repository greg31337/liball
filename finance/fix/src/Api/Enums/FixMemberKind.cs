namespace fix;

/// <summary>Identifies the kind of member in a FIX message or component.</summary>
public enum FixMemberKind
{
    /// <summary>The member kind is not known.</summary>
    Unknown = 0,
    /// <summary>The member is a field.</summary>
    Field = 1,
    /// <summary>The member is a component reference.</summary>
    Component = 2,
    /// <summary>The member is a repeating group.</summary>
    Group = 3
}
