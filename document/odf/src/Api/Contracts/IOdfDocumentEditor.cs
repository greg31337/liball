namespace odf;

/// <summary>Provides document-wide edit sessions, mutations, and resource maintenance.</summary>
public interface IOdfDocumentEditor
{
    /// <summary>Begins an in-memory edit session.</summary>
    /// <param name="document">The document to edit.</param>
    /// <param name="sessionId">The non-empty session identifier.</param>
    /// <param name="validationOptions">Validation performed before commit.</param>
    /// <param name="maximumOperations">The positive staged-operation limit.</param>
    /// <returns>The new edit session.</returns>
    public OdfEditSession BeginEditSession(
        OdfDocument document,
        string sessionId,
        OdfValidationOptions validationOptions,
        int maximumOperations);
    /// <summary>Stages one operation without changing the document.</summary>
    /// <param name="session">The active session.</param>
    /// <param name="operation">The operation to stage.</param>
    /// <returns>The staging result.</returns>
    public OdfEditResult StageEditOperation(
        OdfEditSession session,
        OdfOperation operation);
    /// <summary>Gets an independent snapshot of staged operations.</summary>
    /// <param name="session">The edit session.</param>
    /// <returns>The staged operations.</returns>
    public OdfOperation[] GetStagedEditOperations(OdfEditSession session);
    /// <summary>Validates and atomically commits the staged operations.</summary>
    /// <param name="session">The active session.</param>
    /// <returns>The commit result and mutation report.</returns>
    public OdfEditResult CommitEditSession(OdfEditSession session);
    /// <summary>Discards staged operations without changing the document.</summary>
    /// <param name="session">The active session.</param>
    /// <returns>The discard result.</returns>
    public OdfEditResult DiscardEditSession(OdfEditSession session);
    /// <summary>Checks the document body family.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <param name="expectedKind">The expected document kind.</param>
    /// <returns>The body-kind check result.</returns>
    public OdfEditResult CheckDocumentBodyKind(
        OdfDocument document,
        OdfDocumentKind expectedKind);
    /// <summary>Adds a materialized standard XML part.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="path">The standard part path.</param>
    /// <param name="nodes">The part nodes.</param>
    /// <param name="mediaType">The XML media type or an empty string.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult AddDocumentPart(
        OdfDocument document,
        string path,
        OdfNode[] nodes,
        string mediaType);
    /// <summary>Removes a materialized optional standard XML part.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="path">The standard part path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemoveDocumentPart(
        OdfDocument document,
        string path);
    /// <summary>Copies a materialized standard XML part between documents.</summary>
    /// <param name="sourceDocument">The source document.</param>
    /// <param name="targetDocument">The target document.</param>
    /// <param name="sourcePath">The source part path.</param>
    /// <param name="destinationPath">The destination part path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult CopyDocumentPart(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath);
    /// <summary>Adds a binary or XML package resource.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="path">The canonical resource path.</param>
    /// <param name="data">The resource bytes.</param>
    /// <param name="mediaType">The resource media type.</param>
    /// <param name="compressionMethod">The ZIP compression method.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult AddDocumentResource(
        OdfDocument document,
        string path,
        byte[] data,
        string mediaType,
        OdfCompressionMethod compressionMethod);
    /// <summary>Removes a package resource.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="path">The resource path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RemoveDocumentResource(
        OdfDocument document,
        string path);
    /// <summary>Copies a package resource between documents.</summary>
    /// <param name="sourceDocument">The source document.</param>
    /// <param name="targetDocument">The target document.</param>
    /// <param name="sourcePath">The source resource path.</param>
    /// <param name="destinationPath">The destination resource path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult CopyDocumentResource(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath);
    /// <summary>Moves a resource and rewrites document references to it.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <param name="sourcePath">The current resource path.</param>
    /// <param name="destinationPath">The new resource path.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult MoveDocumentResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath);
    /// <summary>Finds unreferenced package resources.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The unreferenced resource paths.</returns>
    public string[] FindUnusedDocumentResources(OdfDocument document);
    /// <summary>Removes unreferenced package resources.</summary>
    /// <param name="document">The package document to modify.</param>
    /// <returns>The garbage-collection result.</returns>
    public OdfEditResult CollectUnusedDocumentResources(
        OdfDocument document);
    /// <summary>Renames one unique identifier and rewrites its references.</summary>
    /// <param name="document">The document to modify.</param>
    /// <param name="oldIdentifier">The existing identifier.</param>
    /// <param name="newIdentifier">The replacement identifier.</param>
    /// <returns>The edit result.</returns>
    public OdfEditResult RenameDocumentIdentifier(
        OdfDocument document,
        string oldIdentifier,
        string newIdentifier);
    /// <summary>Clones a subtree and resolves name and identifier collisions.</summary>
    /// <param name="document">The destination document.</param>
    /// <param name="subtree">The source subtree.</param>
    /// <returns>The collision-safe independent subtree.</returns>
    public OdfNode PrepareCopiedDocumentSubtree(
        OdfDocument document,
        OdfNode subtree);
    /// <summary>Creates a deterministic unused document name.</summary>
    /// <param name="document">The destination document.</param>
    /// <param name="requestedName">The preferred name.</param>
    /// <returns>An unused name.</returns>
    public string CreateUniqueDocumentName(
        OdfDocument document,
        string requestedName);
    /// <summary>Creates a deterministic unused XML identifier.</summary>
    /// <param name="document">The destination document.</param>
    /// <param name="requestedIdentifier">The preferred identifier.</param>
    /// <returns>An unused valid XML identifier.</returns>
    public string CreateUniqueDocumentIdentifier(
        OdfDocument document,
        string requestedIdentifier);
    /// <summary>Creates a deterministic unused package resource path.</summary>
    /// <param name="document">The destination document.</param>
    /// <param name="requestedPath">The preferred canonical path.</param>
    /// <returns>An unused package path.</returns>
    public string CreateUniqueDocumentResourcePath(
        OdfDocument document,
        string requestedPath);
    /// <summary>Converts a flat document to a package document.</summary>
    /// <param name="document">The flat document.</param>
    /// <returns>The package document, or the input if already packaged.</returns>
    public OdfDocument ConvertDocumentToPackage(OdfDocument document);
    /// <summary>Converts a resource-free package document to a flat document.</summary>
    /// <param name="document">The package document.</param>
    /// <returns>The flat document, or the input if already flat.</returns>
    public OdfDocument ConvertDocumentToFlat(OdfDocument document);
    /// <summary>Proposes explicit document repairs without applying them.</summary>
    /// <param name="document">The document to inspect.</param>
    /// <returns>The repair analysis.</returns>
    public OdfRepairAnalysis ProposeDocumentRepairs(OdfDocument document);
    /// <summary>Applies selected safe repair proposals.</summary>
    /// <param name="document">The document to repair.</param>
    /// <param name="proposals">Original proposals returned for the document.</param>
    /// <returns>The repair result.</returns>
    public OdfRepairResult ApplyDocumentRepairs(
        OdfDocument document,
        OdfRepairProposal[] proposals);
}
