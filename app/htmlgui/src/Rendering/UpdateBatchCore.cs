using System;
using System.Collections.Generic;

namespace htmlgui;

internal class UpdateBatchOperationResult
{
    internal bool Success;
    internal bool IsReplay;
    internal string Code = string.Empty;
    internal UiUpdateBatch? Batch;
    internal UiUpdateResult Result = new UiUpdateResult();
}

internal class UpdateBatchHistoryEntry
{
    internal UiUpdateBatch Batch = new UiUpdateBatch();
    internal DomSnapshot Before = new DomSnapshot();
    internal DomSnapshot After = new DomSnapshot();
    internal bool Acknowledged;
    internal bool Rejected;
}

internal class UpdateBatchCore
{
    internal const int DefaultMaximumHistoryCount = 32;
    internal const int MaximumHistoryCountLimit = 4096;

    internal DomTreeDiffer Differ;
    internal PreservationPlanner Preservation;
    internal List<UpdateBatchHistoryEntry> History;
    internal DomSnapshot? CurrentSnapshot;
    internal UpdateBatchHistoryEntry? Pending;
    internal int MaximumHistoryCount;

    internal UpdateBatchCore()
    {
        Differ = new DomTreeDiffer();
        Preservation = new PreservationPlanner();
        History = new List<UpdateBatchHistoryEntry>();
        MaximumHistoryCount = DefaultMaximumHistoryCount;
    }

    internal UiUpdateResult InitializeUpdateHistory(DomSnapshot? snapshot, int maximumHistoryCount)
    {
        if (snapshot == null)
        {
            return CreateUpdateFailure("update.null-snapshot", false);
        }

        if (maximumHistoryCount < 1 || maximumHistoryCount > MaximumHistoryCountLimit)
        {
            return CreateUpdateFailure("update.invalid-history-limit", false);
        }

        if (snapshot.Revision < 0)
        {
            return CreateUpdateFailure("update.invalid-revision", false);
        }

        CurrentSnapshot = CopyUpdateSnapshot(snapshot);
        Pending = null;
        History.Clear();
        MaximumHistoryCount = maximumHistoryCount;
        return CreateUpdateSuccess(CurrentSnapshot.Revision);
    }

    internal UpdateBatchOperationResult BeginUpdateTransaction(DomSnapshot? after, UiUpdatePriority priority, UiPreservationState? currentPreservation)
    {
        if (CurrentSnapshot == null)
        {
            return CreateBatchFailure("update.not-initialized", false);
        }

        if (after == null)
        {
            return CreateBatchFailure("update.null-snapshot", true);
        }

        if (!IsValidUpdatePriority(priority))
        {
            return CreateBatchFailure("update.invalid-priority", false);
        }

        if (!string.Equals(CurrentSnapshot.SessionId, after.SessionId, StringComparison.Ordinal))
        {
            return CreateBatchFailure("update.session-mismatch", true);
        }

        if (!string.Equals(CurrentSnapshot.RootSeed, after.RootSeed, StringComparison.Ordinal))
        {
            return CreateBatchFailure("update.root-seed-mismatch", true);
        }

        if (CurrentSnapshot.Revision == int.MaxValue)
        {
            return CreateBatchFailure("update.revision-exhausted", true);
        }

        if (Pending != null)
        {
            if (SnapshotsHaveSameContent(Pending.After, after))
            {
                UpdateBatchOperationResult replay = CreateBatchSuccess(Pending.Batch, true);
                return replay;
            }

            return CreateBatchFailure("update.transaction-active", false);
        }

        DomSnapshot beforeSnapshot = CopyUpdateSnapshot(CurrentSnapshot);
        DomSnapshot afterSnapshot = CopyUpdateSnapshot(after);
        afterSnapshot.Revision = beforeSnapshot.Revision + 1;
        DomMutation[] mutations = Differ.CreateMutations(beforeSnapshot, afterSnapshot);
        UiPreservationState preservation = new UiPreservationState();
        if (currentPreservation != null)
        {
            preservation = Preservation.CreatePreservationPlan(beforeSnapshot, afterSnapshot, mutations, currentPreservation);
        }

        UiUpdateBatch batch = new UiUpdateBatch();
        batch.Id = CreateUpdateBatchId(beforeSnapshot, afterSnapshot);
        batch.BaseRevision = beforeSnapshot.Revision;
        batch.Revision = afterSnapshot.Revision;
        batch.Priority = priority;
        batch.Mutations = mutations;
        batch.Preservation = preservation;
        Pending = new UpdateBatchHistoryEntry
        {
            Batch = CopyUpdateBatch(batch),
            Before = beforeSnapshot,
            After = afterSnapshot
        };
        return CreateBatchSuccess(batch, false);
    }

    internal UiUpdateResult AcknowledgeUpdateTransaction(string? batchId, int revision)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            return CreateUpdateFailure("update.invalid-batch", false);
        }

        if (revision < 0)
        {
            return CreateUpdateFailure("update.invalid-revision", false);
        }

        if (Pending != null && string.Equals(Pending.Batch.Id, batchId, StringComparison.Ordinal))
        {
            if (Pending.Batch.Revision != revision)
            {
                return CreateUpdateFailure("update.revision-mismatch", true);
            }

            Pending.Acknowledged = true;
            CurrentSnapshot = CopyUpdateSnapshot(Pending.After);
            AddUpdateHistory(Pending);
            Pending = null;
            return CreateUpdateSuccess(revision);
        }

        UpdateBatchHistoryEntry? historical = FindUpdateHistory(batchId);
        if (historical != null && historical.Batch.Revision == revision && historical.Acknowledged)
        {
            return CreateUpdateSuccess(revision);
        }

        if (historical != null && historical.Batch.Revision != revision)
        {
            return CreateUpdateFailure("update.revision-mismatch", true);
        }

        if (historical != null && historical.Rejected)
        {
            return CreateUpdateFailure("update.batch-rejected", true);
        }

        return CreateUpdateFailure("update.unknown-batch", true);
    }

    internal UiUpdateResult RejectUpdateTransaction(string? batchId, int revision)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            return CreateUpdateFailure("update.invalid-batch", false);
        }

        if (revision < 0)
        {
            return CreateUpdateFailure("update.invalid-revision", false);
        }

        if (Pending != null && string.Equals(Pending.Batch.Id, batchId, StringComparison.Ordinal))
        {
            if (Pending.Batch.Revision != revision)
            {
                return CreateUpdateFailure("update.revision-mismatch", true);
            }

            Pending.Rejected = true;
            AddUpdateHistory(Pending);
            Pending = null;
            return CreateUpdateFailure("update.batch-rejected", true);
        }

        UpdateBatchHistoryEntry? historical = FindUpdateHistory(batchId);
        if (historical != null && historical.Batch.Revision != revision)
        {
            return CreateUpdateFailure("update.revision-mismatch", true);
        }

        if (historical != null && historical.Rejected)
        {
            return CreateUpdateFailure("update.batch-rejected", true);
        }

        if (historical != null && historical.Acknowledged)
        {
            return CreateUpdateFailure("update.batch-acknowledged", true);
        }

        return CreateUpdateFailure("update.unknown-batch", true);
    }

    internal UpdateBatchOperationResult ReplayUpdateTransaction(string? batchId, int baseRevision)
    {
        if (string.IsNullOrWhiteSpace(batchId))
        {
            return CreateBatchFailure("update.invalid-batch", false);
        }

        if (baseRevision < 0)
        {
            return CreateBatchFailure("update.invalid-revision", false);
        }

        if (Pending != null && string.Equals(Pending.Batch.Id, batchId, StringComparison.Ordinal))
        {
            if (Pending.Batch.BaseRevision != baseRevision)
            {
                return CreateBatchFailure("update.stale-base", true);
            }

            return CreateBatchSuccess(Pending.Batch, true);
        }

        UpdateBatchHistoryEntry? historical = FindUpdateHistory(batchId);
        if (historical == null)
        {
            return CreateBatchFailure("update.unknown-batch", true);
        }

        if (historical.Batch.BaseRevision != baseRevision)
        {
            return CreateBatchFailure("update.stale-base", true);
        }

        if (historical.Rejected)
        {
            return CreateBatchFailure("update.batch-rejected", true);
        }

        return CreateBatchSuccess(historical.Batch, true);
    }

    internal UiUpdateResult CreateResyncUpdateResult(string code)
    {
        return CreateUpdateFailure(code, true);
    }

    internal DomSnapshot? ReadCurrentUpdateSnapshot()
    {
        if (CurrentSnapshot == null)
        {
            return null;
        }

        return CopyUpdateSnapshot(CurrentSnapshot);
    }

    internal bool IsValidUpdatePriority(UiUpdatePriority priority)
    {
        return priority == UiUpdatePriority.Immediate
            || priority == UiUpdatePriority.UserInteraction
            || priority == UiUpdatePriority.Normal
            || priority == UiUpdatePriority.Background;
    }

    internal bool SnapshotsHaveSameContent(DomSnapshot left, DomSnapshot right)
    {
        return left.ContentHash.Length > 0
            && string.Equals(left.SessionId, right.SessionId, StringComparison.Ordinal)
            && string.Equals(left.RootSeed, right.RootSeed, StringComparison.Ordinal)
            && string.Equals(left.ContentHash, right.ContentHash, StringComparison.Ordinal);
    }

    internal string CreateUpdateBatchId(DomSnapshot before, DomSnapshot after)
    {
        string contentIdentity = CreateUpdateContentIdentity(after);
        string hash = StableHash.HashParts(new string[]
        {
            before.SessionId,
            before.RootSeed,
            InvariantFormatter.FormatInt32(before.Revision),
            InvariantFormatter.FormatInt32(after.Revision),
            contentIdentity
        });
        return "update-" + hash.Substring(0, 24);
    }

    internal string CreateUpdateContentIdentity(DomSnapshot snapshot)
    {
        if (!string.IsNullOrEmpty(snapshot.ContentHash))
        {
            return snapshot.ContentHash;
        }

        List<string> parts = new List<string>();
        DomNode[] roots = snapshot.Roots ?? Array.Empty<DomNode>();
        parts.Add(InvariantFormatter.FormatInt32(roots.Length));
        int index = 0;
        while (index < roots.Length)
        {
            DomNode root = roots[index];
            if (root == null)
            {
                parts.Add("null");
            }
            else
            {
                parts.Add(root.Id);
                parts.Add(root.Key);
                parts.Add(root.SubtreeHash ?? string.Empty);
            }

            index++;
        }

        return StableHash.HashParts(parts.ToArray());
    }

    internal void AddUpdateHistory(UpdateBatchHistoryEntry entry)
    {
        History.Add(CopyUpdateHistoryEntry(entry));
        while (History.Count > MaximumHistoryCount)
        {
            History.RemoveAt(0);
        }
    }

    internal UpdateBatchHistoryEntry? FindUpdateHistory(string batchId)
    {
        int index = History.Count - 1;
        while (index >= 0)
        {
            if (string.Equals(History[index].Batch.Id, batchId, StringComparison.Ordinal))
            {
                return History[index];
            }

            index--;
        }

        return null;
    }

    internal UpdateBatchOperationResult CreateBatchSuccess(UiUpdateBatch batch, bool isReplay)
    {
        UpdateBatchOperationResult result = new UpdateBatchOperationResult();
        result.Success = true;
        result.IsReplay = isReplay;
        result.Batch = CopyUpdateBatch(batch);
        int revision = CurrentSnapshot == null ? 0 : CurrentSnapshot.Revision;
        result.Result = CreateUpdateSuccess(revision);
        return result;
    }

    internal UpdateBatchOperationResult CreateBatchFailure(string code, bool requiresResync)
    {
        UpdateBatchOperationResult result = new UpdateBatchOperationResult();
        result.Code = code;
        result.Result = CreateUpdateFailure(code, requiresResync);
        return result;
    }

    internal UiUpdateResult CreateUpdateSuccess(int revision)
    {
        UiUpdateResult result = new UiUpdateResult();
        result.Accepted = true;
        result.Revision = revision;
        result.Result.Success = true;
        return result;
    }

    internal UiUpdateResult CreateUpdateFailure(string code, bool requiresResync)
    {
        UiDiagnostic diagnostic = CreateUpdateDiagnostic(code);
        UiUpdateResult result = new UiUpdateResult();
        result.Accepted = false;
        result.Revision = CurrentSnapshot == null ? 0 : CurrentSnapshot.Revision;
        result.RequiresResync = requiresResync && CurrentSnapshot != null;
        if (result.RequiresResync && CurrentSnapshot != null)
        {
            result.Snapshot = CopyUpdateSnapshot(CurrentSnapshot);
        }

        result.Diagnostics = new UiDiagnostic[] { diagnostic };
        result.Result.Success = false;
        result.Result.Diagnostics = new UiDiagnostic[] { CopyUpdateDiagnostic(diagnostic) };
        return result;
    }

    internal UiDiagnostic CreateUpdateDiagnostic(string code)
    {
        UiDiagnostic diagnostic = new UiDiagnostic();
        diagnostic.Severity = UiDiagnosticSeverity.Error;
        diagnostic.Code = code;
        diagnostic.Message = GetUpdateDiagnosticMessage(code);
        diagnostic.Location = "/update";
        return diagnostic;
    }

    internal string GetUpdateDiagnosticMessage(string code)
    {
        switch (code)
        {
            case "update.null-snapshot":
                return "An update snapshot is required.";
            case "update.invalid-history-limit":
                return "The update history limit is outside the supported range.";
            case "update.invalid-revision":
                return "The update revision is invalid.";
            case "update.not-initialized":
                return "The update history has not been initialized.";
            case "update.invalid-priority":
                return "The update priority is invalid.";
            case "update.session-mismatch":
                return "The update snapshot belongs to another session.";
            case "update.root-seed-mismatch":
                return "The update snapshot uses another deterministic root seed.";
            case "update.revision-exhausted":
                return "The update revision cannot advance further.";
            case "update.transaction-active":
                return "Another update transaction is awaiting acknowledgement.";
            case "update.invalid-batch":
                return "A valid update batch identifier is required.";
            case "update.revision-mismatch":
                return "The acknowledged update revision does not match the batch.";
            case "update.stale-base":
                return "The update batch base revision is stale.";
            case "update.batch-rejected":
                return "The update batch was rejected.";
            case "update.batch-acknowledged":
                return "The update batch was already acknowledged.";
            case "update.unknown-batch":
                return "The update batch is unknown or no longer retained.";
            default:
                return "The update operation failed.";
        }
    }

    internal DomSnapshot CopyUpdateSnapshot(DomSnapshot source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The update snapshot copy could not be created.");
        }

        return (DomSnapshot)copied;
    }

    internal UiUpdateBatch CopyUpdateBatch(UiUpdateBatch source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        if (copied == null)
        {
            throw new InvalidOperationException("The update batch copy could not be created.");
        }

        return (UiUpdateBatch)copied;
    }

    internal UpdateBatchHistoryEntry CopyUpdateHistoryEntry(UpdateBatchHistoryEntry source)
    {
        return new UpdateBatchHistoryEntry
        {
            Batch = CopyUpdateBatch(source.Batch),
            Before = CopyUpdateSnapshot(source.Before),
            After = CopyUpdateSnapshot(source.After),
            Acknowledged = source.Acknowledged,
            Rejected = source.Rejected
        };
    }

    internal UiDiagnostic CopyUpdateDiagnostic(UiDiagnostic source)
    {
        return new UiDiagnostic
        {
            Severity = source.Severity,
            Code = source.Code,
            Message = source.Message,
            Location = source.Location
        };
    }
}
