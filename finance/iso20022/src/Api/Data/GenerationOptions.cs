using System.Collections.Generic;
using System.Threading;

namespace iso20022;

/// <summary>Controls deterministic schema, model, metadata, and manifest generation.</summary>
public class GenerationOptions
{
    /// <summary>The root C# namespace beneath which generated symbols are partitioned.</summary>
    public string RootNamespace = "Iso20022.Generated";
    /// <summary>The caller-selected output root used to resolve generated relative paths.</summary>
    public string OutputRootPath = string.Empty;
    /// <summary>The exact repository or artifact edition recorded in generated paths and manifests.</summary>
    public string ArtifactEdition = string.Empty;
    /// <summary>The exact market-practice or processing profile applied during generation.</summary>
    public string Profile = string.Empty;
    /// <summary>The exact message identifiers selected for generation, or an empty list for every available message.</summary>
    public List<MessageIdentifier> MessageIdentifiers = new List<MessageIdentifier>();
    /// <summary>The exact schema namespace URIs selected for generation, or an empty list for every available namespace.</summary>
    public List<string> NamespaceUris = new List<string>();
    /// <summary>The collection, output-size, schema-count, and diagnostic resource bounds.</summary>
    public ResourceLimits Limits = new ResourceLimits();
    /// <summary>Whether deprecated repository concepts remain eligible for generated output.</summary>
    public bool IncludeDeprecated;
    /// <summary>Whether semantic definitions and examples are emitted as generated documentation.</summary>
    public bool IncludeDocumentation = true;
    /// <summary>Whether exact repository, XML, and C# source mappings are emitted.</summary>
    public bool EmitSourceMappings = true;
    /// <summary>Whether a deterministic generation manifest is included in the output.</summary>
    public bool EmitManifest = true;
    /// <summary>The indentation text used by textual emitters.</summary>
    public string Indentation = "    ";
    /// <summary>The line-ending text used by textual emitters.</summary>
    public string NewLine = "\n";
    /// <summary>The cancellation request observed between bounded generation operations.</summary>
    public CancellationToken CancellationToken;
}
