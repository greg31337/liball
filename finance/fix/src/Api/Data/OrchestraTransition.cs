using System.Collections.Generic;

namespace fix;

/// <summary>Defines one conditional state transition.</summary>
public class OrchestraTransition
{
    /// <summary>The transition name.</summary>
    public string Name = "";
    /// <summary>The triggering message or event.</summary>
    public string Trigger = "";
    /// <summary>The Score condition expression.</summary>
    public string Condition = "";
    /// <summary>The target state name.</summary>
    public string TargetState = "";
    /// <summary>The parsed conditional rule when a guard is supplied.</summary>
    public OrchestraRule? Rule;
    /// <summary>The ordered transition responses.</summary>
    public List<OrchestraResponse> Responses = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
