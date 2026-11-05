using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra component definition.</summary>
public class OrchestraComponentDefinition
{
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The component name.</summary>
    public string Name = "";
    /// <summary>The abbreviated component name.</summary>
    public string Abbreviation = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The category name.</summary>
    public string Category = "";
    /// <summary>The rendering hint.</summary>
    public string Rendering = "";
    /// <summary>The member-selection mode.</summary>
    public string Selection = "";
    /// <summary>The ordered members.</summary>
    public List<OrchestraMemberDefinition> Members = [];
    /// <summary>The physical encodings in source order.</summary>
    public List<OrchestraEncoding> Encodings = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
