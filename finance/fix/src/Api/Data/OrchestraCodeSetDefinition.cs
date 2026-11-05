using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra code-set definition.</summary>
public class OrchestraCodeSetDefinition
{
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The code-set name.</summary>
    public string Name = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The underlying datatype name.</summary>
    public string Type = "";
    /// <summary>The default lexical value.</summary>
    public string DefaultValue = "";
    /// <summary>The external specification URI.</summary>
    public string SpecificationUri = "";
    /// <summary>The union datatype name.</summary>
    public string UnionDatatype = "";
    /// <summary>The code values in source order.</summary>
    public List<OrchestraCodeDefinition> Codes = [];
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
