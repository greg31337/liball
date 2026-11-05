using System.Collections.Generic;

namespace fix;

/// <summary>Maps one logical Orchestra datatype to an external encoding standard.</summary>
public class OrchestraDatatypeMapping
{
    /// <summary>The target encoding standard name.</summary>
    public string Standard = "";
    /// <summary>Whether the target datatype is built into the encoding standard.</summary>
    public bool Builtin;
    /// <summary>Whether the source supplied the built-in flag.</summary>
    public bool HasBuiltin;
    /// <summary>The built-in flag in source lexical form.</summary>
    public string BuiltinText = "";
    /// <summary>The external base datatype name.</summary>
    public string Base = "";
    /// <summary>The lexical restriction pattern.</summary>
    public string Pattern = "";
    /// <summary>The element datatype for an aggregate mapping.</summary>
    public string Element = "";
    /// <summary>The aggregate size in source lexical form.</summary>
    public string Size = "";
    /// <summary>The external datatype parameter.</summary>
    public string Parameter = "";
    /// <summary>The inclusive lower bound in source lexical form.</summary>
    public string MinimumInclusive = "";
    /// <summary>The inclusive upper bound in source lexical form.</summary>
    public string MaximumInclusive = "";
    /// <summary>The mapping-specific encoding information when supplied.</summary>
    public OrchestraEncoding? Encoding;
    /// <summary>The attached annotations.</summary>
    public List<OrchestraAnnotation> Annotations = [];
    /// <summary>All source attributes keyed by qualified name.</summary>
    public Dictionary<string, string> Attributes = [];
    /// <summary>Unknown namespace-qualified extensions.</summary>
    public List<OrchestraExtension> Extensions = [];
}
