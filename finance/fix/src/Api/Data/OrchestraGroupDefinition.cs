using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra repeating-group definition.</summary>
public class OrchestraGroupDefinition
{
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The group name.</summary>
    public string Name = "";
    /// <summary>The abbreviated group name.</summary>
    public string Abbreviation = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The category name.</summary>
    public string Category = "";
    /// <summary>The rendering hint.</summary>
    public string Rendering = "";
    /// <summary>The member-selection mode.</summary>
    public string Selection = "";
    /// <summary>The count-field reference when supplied.</summary>
    public OrchestraMemberDefinition? CountMember;
    /// <summary>The ordered entry members.</summary>
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
