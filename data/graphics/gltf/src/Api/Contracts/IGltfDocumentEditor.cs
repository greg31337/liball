namespace gltf;

/// <summary>Creates, clones, merges, compacts, and repairs glTF document graphs.</summary>
public interface IGltfDocumentEditor
{
    /// <summary>Creates an empty valid document.</summary>
    /// <returns>The created document.</returns>
    GltfDocument CreateDocument();

    /// <summary>Creates an independently owned clone of a document.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The cloned document.</returns>
    GltfDocument CloneDocument(GltfDocument document, GltfEditOptions options);

    /// <summary>Merges source documents into one independently owned document.</summary>
    /// <param name="documents">The source documents.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The merge result.</returns>
    GltfMergeResult MergeDocument(GltfDocument[] documents, GltfEditOptions options);

    /// <summary>Compacts unreachable objects and rebuilds indices.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The edit result and remaps.</returns>
    GltfMergeResult CompactDocument(GltfDocument document, GltfEditOptions options);

    /// <summary>Rebuilds document references and index mappings.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The edit result and remaps.</returns>
    GltfMergeResult RebuildReferences(GltfDocument document, GltfEditOptions options);
}
