using System.Collections.Generic;

namespace gltf;

/// <summary>Inspects document statistics and reference dependencies without mutation.</summary>
public interface IGltfInspector
{
    /// <summary>Inspects a document.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The inspection options.</param>
    /// <returns>The inspection result.</returns>
    GltfInspectionResult InspectDocument(GltfDocument document, GltfInspectionOptions options);

    /// <summary>Enumerates forward document dependencies in deterministic order.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="options">The inspection options.</param>
    /// <returns>The dependency edges.</returns>
    List<GltfDependency> EnumerateDependencies(GltfDocument document, GltfInspectionOptions options);
}
