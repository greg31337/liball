using System.Collections.Generic;

namespace gltf;

/// <summary>Provides non-mutating document inspection and dependency enumeration.</summary>
public class GltfInspector : IGltfInspector
{
    internal DocumentInspector Engine;

    /// <summary>Initializes a document inspector.</summary>
    public GltfInspector()
    {
        Engine = new DocumentInspector();
    }

    /// <summary>Inspects a document.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The inspection options.</param>
    /// <returns>The inspection result.</returns>
    public GltfInspectionResult InspectDocument(GltfDocument document, GltfInspectionOptions options)
    {
        return Engine.InspectDocumentData(document, options);
    }

    /// <summary>Enumerates forward document dependencies in deterministic order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The inspection options.</param>
    /// <returns>The dependency edges.</returns>
    public List<GltfDependency> EnumerateDependencies(GltfDocument document, GltfInspectionOptions options)
    {
        return Engine.EnumerateConfiguredDependencies(document, options);
    }
}
