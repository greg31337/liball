using System.Collections.Generic;

namespace fix;

/// <summary>Retains one Orchestra scenario discovered from source definitions.</summary>
public class OrchestraScenarioDefinition
{
    /// <summary>The scenario name.</summary>
    public string Name = "";
    /// <summary>The inherited scenario name when explicitly defined.</summary>
    public string ParentName = "";
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
