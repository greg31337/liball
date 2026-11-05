namespace fix;

/// <summary>Defines an ordered field, component, or group reference.</summary>
public class FixMemberDefinition
{
    /// <summary>The member's name in its containing definition.</summary>
    public string Name = "";
    /// <summary>The kind of referenced definition.</summary>
    public FixMemberKind Kind;
    /// <summary>The referenced tag, component identifier, or group identifier.</summary>
    public int ReferenceId;
    /// <summary>The zero-based member position.</summary>
    public int Position;
    /// <summary>The member presence requirement.</summary>
    public FixPresence Presence;
    /// <summary>The scenario that specializes this member.</summary>
    public string Scenario = "";
    /// <summary>The conditional presence expression, or an empty string when unconditional.</summary>
    public string PresenceRule = "";
    /// <summary>The presence selected when the conditional expression is true.</summary>
    public FixPresence RulePresence = FixPresence.Required;
    /// <summary>The source repository or overlay that supplied the member.</summary>
    public string Source = "";
    /// <summary>Whether this overlay member removes the matching base member.</summary>
    public bool IsRemoved;
}
