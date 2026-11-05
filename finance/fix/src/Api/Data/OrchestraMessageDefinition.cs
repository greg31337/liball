using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra message and scenario definition.</summary>
public class OrchestraMessageDefinition
{
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The message name.</summary>
    public string Name = "";
    /// <summary>The abbreviated message name.</summary>
    public string Abbreviation = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The exact MsgType value.</summary>
    public string MessageType = "";
    /// <summary>The category name.</summary>
    public string Category = "";
    /// <summary>The flow name.</summary>
    public string Flow = "";
    /// <summary>The member-selection mode.</summary>
    public string Selection = "";
    /// <summary>The ordered structure members.</summary>
    public List<OrchestraMemberDefinition> Members = [];
    /// <summary>The physical encodings in source order.</summary>
    public List<OrchestraEncoding> Encodings = [];
    /// <summary>The scenario-selection and assignment rules.</summary>
    public List<OrchestraRule> Rules = [];
    /// <summary>The ordered message responses.</summary>
    public List<OrchestraResponse> Responses = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
