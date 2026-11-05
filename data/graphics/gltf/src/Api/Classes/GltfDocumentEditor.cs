namespace gltf;

/// <summary>Provides document creation, cloning, merging, compaction, and reference rebuilding.</summary>
public class GltfDocumentEditor : IGltfDocumentEditor
{
    internal RequiredExtensionCapabilityChecker RequiredExtensions;
    internal ExtensionUsageCollector ExtensionUsage;
    internal DocumentCloner Cloner;
    internal DocumentMerger Merger;
    internal DocumentCompactor Compactor;
    internal IGltfValidator Validator;

    /// <summary>Initializes a document editor.</summary>
    public GltfDocumentEditor()
    {
        RequiredExtensions = new RequiredExtensionCapabilityChecker();
        ExtensionUsage = new ExtensionUsageCollector();
        Cloner = new DocumentCloner();
        Merger = new DocumentMerger();
        Compactor = new DocumentCompactor();
        Validator = new GltfValidator();
    }

    /// <summary>Creates an empty valid document.</summary>
    /// <returns>The created document.</returns>
    public GltfDocument CreateDocument()
    {
        return new GltfDocument();
    }

    /// <summary>Creates an independently owned clone of a document.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The cloned document.</returns>
    public GltfDocument CloneDocument(GltfDocument document, GltfEditOptions options)
    {
        GltfDocument result = Cloner.CloneDocumentData(document, options);
        if (options.ValidateResult)
        {
            GltfValidationReport validation = Validator.ValidateDocument(
                result,
                new GltfValidationOptions());
            if (!validation.IsValid)
            {
                string message = "The cloned document is invalid.";
                if (validation.Issues.Count > 0)
                {
                    message = validation.Issues[0].Message;
                }

                throw new GltfValidationException(message);
            }
        }

        return result;
    }

    /// <summary>Merges source documents into one independently owned document.</summary>
    /// <param name="documents">The source documents.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The merge result.</returns>
    public GltfMergeResult MergeDocument(GltfDocument[] documents, GltfEditOptions options)
    {
        if (documents == null)
        {
            throw new System.ArgumentNullException("documents");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        ValidateSourceDocuments(documents);
        if (options.RequireSafeUnknownExtensionRemap)
        {
            int index = 0;
            while (index < documents.Length)
            {
                ThrowForUnsafeExtensions(documents[index]);
                index++;
            }
        }

        return Merger.MergeDocumentData(documents, options);
    }

    /// <summary>Compacts unreachable objects and rebuilds indices.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The edit result and remaps.</returns>
    public GltfMergeResult CompactDocument(GltfDocument document, GltfEditOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.RequireSafeUnknownExtensionRemap)
        {
            ThrowForUnsafeExtensions(document);
        }

        return Compactor.CompactDocumentData(document, options);
    }

    /// <summary>Rebuilds document references and index mappings.</summary>
    /// <param name="document">The source document.</param>
    /// <param name="options">The edit options.</param>
    /// <returns>The edit result and remaps.</returns>
    public GltfMergeResult RebuildReferences(GltfDocument document, GltfEditOptions options)
    {
        if (document == null)
        {
            throw new System.ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (options.RequireSafeUnknownExtensionRemap)
        {
            ThrowForUnsafeExtensions(document);
        }

        return Compactor.RebuildDocumentData(document, options);
    }

    internal void ValidateSourceDocuments(GltfDocument[] documents)
    {
        int index = 0;
        while (index < documents.Length)
        {
            if (documents[index] == null)
            {
                throw new System.ArgumentException(
                    $"Source document {index} must not be null.",
                    "documents");
            }

            index++;
        }
    }

    internal void ThrowForUnsafeExtensions(GltfDocument document)
    {
        System.Collections.Generic.List<GltfValidationIssue> issues =
            RequiredExtensions.CheckMutationCapabilities(document);
        if (issues.Count > 0)
        {
            throw new GltfUnsupportedFeatureException(issues[0].Message);
        }

        System.Collections.Generic.List<string> names =
            ExtensionUsage.CollectObjectGraphExtensions(document);
        int index = 0;
        while (index < names.Count)
        {
            string name = names[index];
            GltfExtensionDescriptor? descriptor =
                RequiredExtensions.Registry.FindDescriptor(name);
            if (descriptor == null || !descriptor.CanRemapReferences)
            {
                throw new GltfUnsupportedFeatureException(
                    $"Extension '{name}' cannot safely remap references during document editing.");
            }

            index++;
        }
    }
}
