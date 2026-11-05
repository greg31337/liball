using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra code value.</summary>
public class OrchestraCodeDefinition
{
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The symbolic code name.</summary>
    public string Name = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The exact lexical code value.</summary>
    public string Value = "";
    /// <summary>The display order in source lexical form.</summary>
    public string Sort = "";
    /// <summary>The source grouping label.</summary>
    public string Group = "";
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
