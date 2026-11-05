using System.Collections.Generic;

namespace fix;

/// <summary>Retains an actor timer declaration or scheduled timer activity.</summary>
public class OrchestraTimer
{
    /// <summary>The timer name.</summary>
    public string Name = "";
    /// <summary>The owning actor name.</summary>
    public string Actor = "";
    /// <summary>The timer operation.</summary>
    public string Operation = "";
    /// <summary>The timer interval in source lexical form.</summary>
    public string Interval = "";
    /// <summary>The actions executed when the timer expires.</summary>
    public List<OrchestraAction> Actions = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
