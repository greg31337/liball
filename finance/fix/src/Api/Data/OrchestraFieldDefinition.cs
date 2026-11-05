using System.Collections.Generic;

namespace fix;

/// <summary>Retains one source Orchestra field definition.</summary>
public class OrchestraFieldDefinition
{
    /// <summary>The FIX tag in source lexical form.</summary>
    public string Id = "";
    /// <summary>The field name.</summary>
    public string Name = "";
    /// <summary>The abbreviated field name.</summary>
    public string Abbreviation = "";
    /// <summary>The definition scenario.</summary>
    public string Scenario = "";
    /// <summary>The referenced logical datatype name.</summary>
    public string Type = "";
    /// <summary>The referenced code-set name.</summary>
    public string CodeSet = "";
    /// <summary>The union datatype name.</summary>
    public string UnionDatatype = "";
    /// <summary>The member presence.</summary>
    public FixPresence Presence;
    /// <summary>The presence in source lexical form.</summary>
    public string PresenceText = "";
    /// <summary>The default or constant lexical value.</summary>
    public string Value = "";
    /// <summary>The associated length-field identifier.</summary>
    public string LengthId = "";
    /// <summary>The discriminator-field identifier.</summary>
    public string DiscriminatorId = "";
    /// <summary>The physical encodings in source order.</summary>
    public List<OrchestraEncoding> Encodings = [];
    /// <summary>The conditional and assignment rules in source order.</summary>
    public List<OrchestraRule> Rules = [];
    /// <summary>The lifecycle pedigree.</summary>
    public OrchestraPedigree Pedigree = new OrchestraPedigree();
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
