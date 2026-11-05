using System.Collections.Generic;

namespace fix;

/// <summary>Represents a presence, conditional, or assignment rule.</summary>
public class OrchestraRule
{
    /// <summary>The stable source identifier.</summary>
    public int Id;
    /// <summary>The rule name.</summary>
    public string Name = "";
    /// <summary>The source rule element kind.</summary>
    public string SourceKind = "";
    /// <summary>The rule kind.</summary>
    public OrchestraRuleKind Kind;
    /// <summary>The Score expression in source lexical form.</summary>
    public string Expression = "";
    /// <summary>The presence selected by a presence rule.</summary>
    public FixPresence Presence;
    /// <summary>The presence in source lexical form.</summary>
    public string PresenceText = "";
    /// <summary>The datatype override.</summary>
    public string Type = "";
    /// <summary>The target variable or field for an assignment.</summary>
    public string AssignmentTarget = "";
    /// <summary>The field identifiers that scope a uniqueness rule.</summary>
    public List<string> UniqueFieldIds = [];
    /// <summary>Assignments evaluated by this conditional rule.</summary>
    public List<OrchestraRule> Assignments = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
