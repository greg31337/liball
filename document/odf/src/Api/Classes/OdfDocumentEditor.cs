namespace odf;

/// <summary>Provides document-wide edit sessions, mutations, and resource maintenance.</summary>
public class OdfDocumentEditor : IOdfDocumentEditor
{
    /// <summary>Initializes a document editor.</summary>
    public OdfDocumentEditor()
    {
    }

    /// <summary>Begins an in-memory edit session.</summary>
    public OdfEditSession BeginEditSession(
        OdfDocument document,
        string sessionId,
        OdfValidationOptions validationOptions,
        int maximumOperations)
    {
        EditSessionState state = EditSessionModel.BeginSession(
            document,
            sessionId,
            validationOptions,
            maximumOperations);
        OdfEditSession session = new OdfEditSession();
        session.Id = state.SessionId;
        session.Document = state.Document;
        session.MaximumOperations = state.MaximumOperations;
        session.State = state;
        RefreshEditSession(session, state);
        return session;
    }

    /// <summary>Stages one operation without changing the document.</summary>
    public OdfEditResult StageEditOperation(
        OdfEditSession session,
        OdfOperation operation)
    {
        EditSessionState state = RequireEditSessionState(session);
        OdfEditResult result = EditSessionModel.StageOperation(
            state,
            operation);
        RefreshEditSession(session, state);
        return result;
    }

    /// <summary>Gets an independent snapshot of staged operations.</summary>
    public OdfOperation[] GetStagedEditOperations(OdfEditSession session)
    {
        EditSessionState state = RequireEditSessionState(session);
        OdfOperation[] result = EditSessionModel.GetStagedOperations(state);
        RefreshEditSession(session, state);
        return result;
    }

    /// <summary>Validates and atomically commits the staged operations.</summary>
    public OdfEditResult CommitEditSession(OdfEditSession session)
    {
        EditSessionState state = RequireEditSessionState(session);
        OdfEditResult result = EditSessionModel.CommitSession(state);
        RefreshEditSession(session, state);
        return result;
    }

    /// <summary>Discards staged operations without changing the document.</summary>
    public OdfEditResult DiscardEditSession(OdfEditSession session)
    {
        EditSessionState state = RequireEditSessionState(session);
        OdfEditResult result = EditSessionModel.DiscardSession(state);
        RefreshEditSession(session, state);
        return result;
    }

    /// <summary>Checks the document body family.</summary>
    public OdfEditResult CheckDocumentBodyKind(
        OdfDocument document,
        OdfDocumentKind expectedKind)
    {
        return DocumentMutationModel.CheckBodyKind(document, expectedKind);
    }

    /// <summary>Adds a materialized standard XML part.</summary>
    public OdfEditResult AddDocumentPart(
        OdfDocument document,
        string path,
        OdfNode[] nodes,
        string mediaType)
    {
        return DocumentMutationModel.AddDocumentPart(
            document,
            path,
            nodes,
            mediaType);
    }

    /// <summary>Removes a materialized optional standard XML part.</summary>
    public OdfEditResult RemoveDocumentPart(
        OdfDocument document,
        string path)
    {
        return DocumentMutationModel.RemoveDocumentPart(document, path);
    }

    /// <summary>Copies a materialized standard XML part between documents.</summary>
    public OdfEditResult CopyDocumentPart(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath)
    {
        return DocumentMutationModel.CopyDocumentPart(
            sourceDocument,
            targetDocument,
            sourcePath,
            destinationPath);
    }

    /// <summary>Adds a binary or XML package resource.</summary>
    public OdfEditResult AddDocumentResource(
        OdfDocument document,
        string path,
        byte[] data,
        string mediaType,
        OdfCompressionMethod compressionMethod)
    {
        return DocumentMutationModel.AddDocumentResource(
            document,
            path,
            data,
            mediaType,
            compressionMethod);
    }

    /// <summary>Removes a package resource.</summary>
    public OdfEditResult RemoveDocumentResource(
        OdfDocument document,
        string path)
    {
        return DocumentMutationModel.RemoveDocumentResource(document, path);
    }

    /// <summary>Copies a package resource between documents.</summary>
    public OdfEditResult CopyDocumentResource(
        OdfDocument sourceDocument,
        OdfDocument targetDocument,
        string sourcePath,
        string destinationPath)
    {
        return DocumentMutationModel.CopyDocumentResource(
            sourceDocument,
            targetDocument,
            sourcePath,
            destinationPath);
    }

    /// <summary>Moves a resource and rewrites document references to it.</summary>
    public OdfEditResult MoveDocumentResource(
        OdfDocument document,
        string sourcePath,
        string destinationPath)
    {
        return ReferenceUpdater.MoveResource(
            document,
            sourcePath,
            destinationPath);
    }

    /// <summary>Finds unreferenced package resources.</summary>
    public string[] FindUnusedDocumentResources(OdfDocument document)
    {
        return ResourceGarbageCollector.FindUnusedResources(document);
    }

    /// <summary>Removes unreferenced package resources.</summary>
    public OdfEditResult CollectUnusedDocumentResources(
        OdfDocument document)
    {
        return ResourceGarbageCollector.CollectUnusedResources(document);
    }

    /// <summary>Renames one unique identifier and rewrites its references.</summary>
    public OdfEditResult RenameDocumentIdentifier(
        OdfDocument document,
        string oldIdentifier,
        string newIdentifier)
    {
        return ReferenceUpdater.RenameIdentifier(
            document,
            oldIdentifier,
            newIdentifier);
    }

    /// <summary>Clones a subtree and resolves name and identifier collisions.</summary>
    public OdfNode PrepareCopiedDocumentSubtree(
        OdfDocument document,
        OdfNode subtree)
    {
        if (subtree == null)
        {
            throw new OdfEditException("A subtree is required for copying.");
        }
        OdfNode result = new OdfTreeEditor([]).CloneSubtree(subtree);
        ReferenceUpdater.PrepareCopiedSubtree(document, result);
        return result;
    }

    /// <summary>Creates a deterministic unused document name.</summary>
    public string CreateUniqueDocumentName(
        OdfDocument document,
        string requestedName)
    {
        return DeterministicNameModel.CreateUniqueName(
            document,
            requestedName);
    }

    /// <summary>Creates a deterministic unused XML identifier.</summary>
    public string CreateUniqueDocumentIdentifier(
        OdfDocument document,
        string requestedIdentifier)
    {
        return DeterministicNameModel.CreateUniqueIdentifier(
            document,
            requestedIdentifier);
    }

    /// <summary>Creates a deterministic unused package resource path.</summary>
    public string CreateUniqueDocumentResourcePath(
        OdfDocument document,
        string requestedPath)
    {
        return DeterministicNameModel.CreateUniquePackagePath(
            document,
            requestedPath);
    }

    /// <summary>Converts a flat document to a package document.</summary>
    public OdfDocument ConvertDocumentToPackage(OdfDocument document)
    {
        return DocumentMutationModel.ConvertToPackageDocument(document);
    }

    /// <summary>Converts a resource-free package document to a flat document.</summary>
    public OdfDocument ConvertDocumentToFlat(OdfDocument document)
    {
        return DocumentMutationModel.ConvertToFlatDocument(document);
    }

    /// <summary>Proposes explicit document repairs without applying them.</summary>
    public OdfRepairAnalysis ProposeDocumentRepairs(OdfDocument document)
    {
        return DocumentMutationModel.ProposeDocumentRepairs(document);
    }

    /// <summary>Applies selected safe repair proposals.</summary>
    public OdfRepairResult ApplyDocumentRepairs(
        OdfDocument document,
        OdfRepairProposal[] proposals)
    {
        return DocumentMutationModel.ApplyDocumentRepairs(
            document,
            proposals);
    }

    internal EditSessionState RequireEditSessionState(
        OdfEditSession session)
    {
        if (session == null || session.State == null)
        {
            throw new OdfEditException(
                "An edit session created by this API is required.");
        }
        return session.State;
    }

    internal void RefreshEditSession(
        OdfEditSession session,
        EditSessionState state)
    {
        session.StagedOperations = EditSessionModel.GetStagedOperations(state);
        session.IsFinished = state.Finished;
        session.IsDiscarded = state.Discarded;
    }
}
