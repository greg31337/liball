using System.Collections.Generic;

namespace fix;

/// <summary>Defines a directed message flow between actors.</summary>
public class OrchestraFlow
{
    /// <summary>The flow name.</summary>
    public string Name = "";
    /// <summary>The source actor name.</summary>
    public string SourceActor = "";
    /// <summary>The destination actor name.</summary>
    public string DestinationActor = "";
    /// <summary>The message delivery reliability.</summary>
    public string Reliability = "";
    /// <summary>The permitted message names in source order.</summary>
    public List<string> Messages = [];
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
