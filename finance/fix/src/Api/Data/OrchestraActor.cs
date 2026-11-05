using System.Collections.Generic;

namespace fix;

/// <summary>Defines one actor participating in Orchestra flows.</summary>
public class OrchestraActor
{
    /// <summary>The actor name.</summary>
    public string Name = "";
    /// <summary>The state-machine names owned by the actor.</summary>
    public List<string> StateMachines = [];
    /// <summary>The actor-scoped state variables.</summary>
    public List<OrchestraVariable> Variables = [];
    /// <summary>The actor-owned timer declarations.</summary>
    public List<OrchestraTimer> Timers = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
