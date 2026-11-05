using System.Collections.Generic;

namespace fix;

/// <summary>Defines one state and its outgoing transitions.</summary>
public class OrchestraState
{
    /// <summary>The state name.</summary>
    public string Name = "";
    /// <summary>The outgoing transitions in source order.</summary>
    public List<OrchestraTransition> Transitions = [];
    /// <summary>The actions executed when entering the state.</summary>
    public List<OrchestraAction> EntryActions = [];
    /// <summary>The actions active while in the state.</summary>
    public List<OrchestraAction> ActivityActions = [];
    /// <summary>The actions executed when exiting the state.</summary>
    public List<OrchestraAction> ExitActions = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
