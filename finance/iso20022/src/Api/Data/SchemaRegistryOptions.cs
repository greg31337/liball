using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Controls bounded local schema import, registration, selection, compilation, and caching.</summary>
public class SchemaRegistryOptions
{
    /// <summary>The caller-selected absolute root beneath which schema dependencies may be resolved.</summary>
    public string LocalRootPath = string.Empty;
    /// <summary>The parsing, closure, collection, and diagnostic resource bounds.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>The caller-supplied local schemas eligible for offline include or import resolution.</summary>
    public List<SchemaArtifact> LocalSchemas = new List<SchemaArtifact>();
    /// <summary>Whether local XSD include declarations are resolved.</summary>
    public bool ResolveIncludes = true;
    /// <summary>Whether local XSD import declarations are resolved.</summary>
    public bool ResolveImports = true;
    /// <summary>Whether repository metadata may fill semantic details absent from an XSD.</summary>
    public bool AllowRepositoryFallback = true;
    /// <summary>Whether a higher-precedence schema may replace an existing exact registry key.</summary>
    public bool AllowReplacement;
    /// <summary>Whether only approved schema artifacts may be registered or compiled.</summary>
    public bool RequireApprovedArtifacts;
    /// <summary>Whether an accepted registration immediately compiles its dependency closure.</summary>
    public bool CompileOnRegistration;
    /// <summary>Whether successful compiled schema closures are cached by their exact registry key.</summary>
    public bool CacheCompiledSchemas = true;
    /// <summary>The cancellation request observed between bounded schema operations.</summary>
    public CancellationToken CancellationToken;
}
