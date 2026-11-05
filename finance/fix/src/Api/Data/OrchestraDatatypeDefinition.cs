using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra datatype definition.</summary>
public class OrchestraDatatypeDefinition
{
    /// <summary>The datatype name.</summary>
    public string Name = "";
    /// <summary>The parent datatype name.</summary>
    public string BaseType = "";
    /// <summary>The external datatype mappings in source order.</summary>
    public List<OrchestraDatatypeMapping> Mappings = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
