using System.Collections.Generic;

namespace fix;

/// <summary>Defines a transition response and its message or assignments.</summary>
public class OrchestraResponse
{
    /// <summary>The response name.</summary>
    public string Name = "";
    /// <summary>The referenced response message name.</summary>
    public string MessageName = "";
    /// <summary>The response scenario.</summary>
    public string Scenario = "";
    /// <summary>The synchronization mode.</summary>
    public string Synchronization = "";
    /// <summary>The condition guarding this response.</summary>
    public string Condition = "";
    /// <summary>The parsed conditional rule when a condition is supplied.</summary>
    public OrchestraRule? Rule;
    /// <summary>The assignment rules executed by the response.</summary>
    public List<OrchestraRule> Assignments = [];
    /// <summary>The ordered parameters and actions.</summary>
    public List<OrchestraAction> Actions = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
