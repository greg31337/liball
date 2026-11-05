using System.Collections.Generic;

namespace fix;

/// <summary>Declares one physical encoding standard available to a repository.</summary>
public class OrchestraEncodingStandard
{
    /// <summary>The stable encoding standard name.</summary>
    public string Name = "";
    /// <summary>The human-readable encoding standard name.</summary>
    public string DisplayName = "";
    /// <summary>The standard-wide encoding information when supplied.</summary>
    public OrchestraEncoding? Encoding;
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
