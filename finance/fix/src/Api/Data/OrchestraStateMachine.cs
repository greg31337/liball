using System.Collections.Generic;

namespace fix;

/// <summary>Defines an Orchestra state machine.</summary>
public class OrchestraStateMachine
{
    /// <summary>The state-machine name.</summary>
    public string Name = "";
    /// <summary>The actor that owns the state machine.</summary>
    public string Actor = "";
    /// <summary>The initial state name.</summary>
    public string InitialState = "";
    /// <summary>The states in source order.</summary>
    public List<OrchestraState> States = [];
    /// <summary>The machine-scoped variables.</summary>
    public List<OrchestraVariable> Variables = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
