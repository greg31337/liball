using System.Collections.Generic;

namespace fix;

/// <summary>Retains one named Orchestra session identifier.</summary>
public class OrchestraIdentifier
{
    /// <summary>The identifier name.</summary>
    public string Name = "";
    /// <summary>The identifier value.</summary>
    public string Value = "";
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
