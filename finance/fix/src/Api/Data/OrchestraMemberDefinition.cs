using System.Collections.Generic;

namespace fix;

/// <summary>Retains one ordered Orchestra field, component, or group reference.</summary>
public class OrchestraMemberDefinition
{
    /// <summary>The source reference kind.</summary>
    public string Kind = "";
    /// <summary>The referenced identifier in source lexical form.</summary>
    public string ReferenceId = "";
    /// <summary>The member scenario.</summary>
    public string Scenario = "";
    /// <summary>The source-defined instance name.</summary>
    public string InstanceName = "";
    /// <summary>The zero-based position.</summary>
    public int Position;
    /// <summary>The member presence.</summary>
    public FixPresence Presence;
    /// <summary>The presence in source lexical form.</summary>
    public string PresenceText = "";
    /// <summary>The default or constant lexical value.</summary>
    public string Value = "";
    /// <summary>The implementation minimum occurrence count.</summary>
    public string ImplementationMinimum = "";
    /// <summary>The implementation maximum occurrence count.</summary>
    public string ImplementationMaximum = "";
    /// <summary>The associated length-field identifier.</summary>
    public string LengthId = "";
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
