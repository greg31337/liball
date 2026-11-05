using System;

namespace htmlgui;

/// <summary>Creates canonical snapshots and coordinates deterministic transactional DOM updates.</summary>
public class UiUpdateEngine : IUiUpdateEngine
{
    internal const int MaximumSnapshotDiagnostics = 100;

    internal UpdateBatchCore Core;
    internal RenderTreeAnnotator Annotator;

    /// <summary>Initializes an update engine with bounded revision history.</summary>
    public UiUpdateEngine()
    {
        Core = new UpdateBatchCore();
        Annotator = new RenderTreeAnnotator();
    }

    /// <summary>Creates a canonical detached DOM snapshot.</summary>
    public DomSnapshot CreateDomSnapshot(DomNode[]? roots, string? sessionId, string? rootSeed, int revision)
    {
        if (roots == null)
        {
            throw new ArgumentNullException("roots");
        }

        string checkedSessionId = Guard.ArgumentNotWhiteSpace(sessionId, "sessionId");
        string checkedRootSeed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        Guard.ArgumentNonNegative(revision, "revision");
        RenderTreeAnnotationResult annotated = Annotator.AnnotateTree(roots, checkedRootSeed, MaximumSnapshotDiagnostics);
        if (!annotated.Success)
        {
            UiDiagnostic diagnostic = annotated.Diagnostics[0];
            throw new ArgumentException(diagnostic.Code + ": " + diagnostic.Message, "roots");
        }

        return new DomSnapshot
        {
            SessionId = checkedSessionId,
            RootSeed = checkedRootSeed,
            Revision = revision,
            Roots = annotated.Roots,
            ContentHash = annotated.ContentHash
        };
    }

    /// <summary>Computes one revision transaction from the currently accepted snapshot.</summary>
    public UiUpdateBatch ComputeDomUpdates(DomSnapshot? before, DomSnapshot? after, UiUpdatePriority priority, UiPreservationState? currentPreservation)
    {
        if (before == null)
        {
            throw new ArgumentNullException("before");
        }

        if (after == null)
        {
            throw new ArgumentNullException("after");
        }

        ValidateUpdateSnapshot(before, "before");
        ValidateUpdateSnapshot(after, "after");
        if (!Core.IsValidUpdatePriority(priority))
        {
            throw new ArgumentOutOfRangeException("priority", priority, "The update priority is invalid.");
        }

        if (Core.CurrentSnapshot == null)
        {
            UiUpdateResult initialized = Core.InitializeUpdateHistory(before, UpdateBatchCore.DefaultMaximumHistoryCount);
            if (!initialized.Accepted)
            {
                throw new ArgumentException(initialized.Diagnostics[0].Message, "before");
            }
        }
        else if (!IsAcceptedUpdateBase(before))
        {
            throw new ArgumentException("The base snapshot does not match the currently accepted revision.", "before");
        }

        UpdateBatchOperationResult operation = Core.BeginUpdateTransaction(after, priority, currentPreservation);
        if (!operation.Success || operation.Batch == null)
        {
            ThrowUpdateOperationFailure(operation.Code);
        }

        return operation.Batch ?? throw new InvalidOperationException("The update batch could not be created.");
    }

    /// <summary>Accepts a pending update batch and commits its resulting snapshot.</summary>
    public UiUpdateResult AcceptDomUpdates(string? batchId, int revision)
    {
        return Core.AcknowledgeUpdateTransaction(batchId, revision);
    }

    /// <summary>Rejects a pending update batch and retains its accepted base snapshot.</summary>
    public UiUpdateResult RejectDomUpdates(string? batchId, int revision)
    {
        return Core.RejectUpdateTransaction(batchId, revision);
    }

    internal void ValidateUpdateSnapshot(DomSnapshot snapshot, string argumentName)
    {
        if (string.IsNullOrWhiteSpace(snapshot.SessionId))
        {
            throw new ArgumentException("The snapshot session identifier is required.", argumentName);
        }

        if (string.IsNullOrWhiteSpace(snapshot.RootSeed))
        {
            throw new ArgumentException("The snapshot root seed is required.", argumentName);
        }

        if (snapshot.Revision < 0)
        {
            throw new ArgumentException("The snapshot revision cannot be negative.", argumentName);
        }

        if (snapshot.Roots == null)
        {
            throw new ArgumentException("The snapshot roots are required.", argumentName);
        }

        if (string.IsNullOrWhiteSpace(snapshot.ContentHash))
        {
            throw new ArgumentException("The snapshot content hash is required.", argumentName);
        }
    }

    internal bool IsAcceptedUpdateBase(DomSnapshot before)
    {
        DomSnapshot? accepted = Core.CurrentSnapshot;
        if (accepted == null)
        {
            return false;
        }

        return accepted.Revision == before.Revision
            && string.Equals(accepted.SessionId, before.SessionId, StringComparison.Ordinal)
            && string.Equals(accepted.RootSeed, before.RootSeed, StringComparison.Ordinal)
            && string.Equals(accepted.ContentHash, before.ContentHash, StringComparison.Ordinal);
    }

    internal void ThrowUpdateOperationFailure(string code)
    {
        string message = Core.GetUpdateDiagnosticMessage(code);
        if (string.Equals(code, "update.session-mismatch", StringComparison.Ordinal)
            || string.Equals(code, "update.root-seed-mismatch", StringComparison.Ordinal))
        {
            throw new ArgumentException(message, "after");
        }

        throw new InvalidOperationException(message);
    }
}
