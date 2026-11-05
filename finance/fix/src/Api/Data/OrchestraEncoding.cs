using System.Collections.Generic;

namespace fix;

/// <summary>Retains physical encoding information for one standard.</summary>
public class OrchestraEncoding
{
    /// <summary>The encoding standard name, or empty when supplied by the containing context.</summary>
    public string Standard = "";
    /// <summary>The attached documentation and application information.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>External schemas referenced by encoding-specific content.</summary>
    public List<OrchestraExternalSchemaReference> SchemaReferences = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Namespace-qualified encoding-specific content retained losslessly.</summary>
    public List<OrchestraExtension> Extensions = [];
}
