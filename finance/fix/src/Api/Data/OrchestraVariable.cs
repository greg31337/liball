using System.Collections.Generic;

namespace fix;

/// <summary>Defines one named Orchestra behavior variable.</summary>
public class OrchestraVariable
{
    /// <summary>The variable name.</summary>
    public string Name = "";
    /// <summary>The source variable element kind.</summary>
    public string Kind = "";
    /// <summary>The source identifier in lexical form.</summary>
    public string Id = "";
    /// <summary>The referenced identifier in lexical form.</summary>
    public string ReferenceId = "";
    /// <summary>The variable scenario.</summary>
    public string Scenario = "";
    /// <summary>The datatype name.</summary>
    public string Datatype = "";
    /// <summary>The initial lexical value.</summary>
    public string InitialValue = "";
    /// <summary>Whether the value is supplied by external state.</summary>
    public bool IsExternal;
    /// <summary>The rules attached to the variable definition or reference.</summary>
    public List<OrchestraRule> Rules = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
    /// <summary>The complete source XML representation.</summary>
    public string Xml = "";
}
