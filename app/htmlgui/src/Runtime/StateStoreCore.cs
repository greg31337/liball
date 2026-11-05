using System;
using System.Collections.Generic;

namespace htmlgui;

internal class StateScopedEntry
{
    internal string ScopeId = string.Empty;
    internal UiStateEntry Entry = new UiStateEntry();
}

internal class StateVersionClock
{
    internal string ScopeId = string.Empty;
    internal string StateId = string.Empty;
    internal int Version;
}

internal class StateValueValidationState
{
    internal int CollectionCount;
    internal List<UiValue> Ancestors = new List<UiValue>();
}

internal class StateOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal int OperationIndex = -1;
    internal string ScopeId = string.Empty;
    internal UiStateEntry? Entry;
    internal UiStateChange? Change;
}

internal class StateScopedChange
{
    internal string ScopeId = string.Empty;
    internal UiStateChange Change = new UiStateChange();
}

internal class StateStoreSnapshot
{
    internal string Id = string.Empty;
    internal List<StateScopedEntry> Entries = new List<StateScopedEntry>();
}

internal class StateBatchResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string BatchId = string.Empty;
    internal StateScopedChange[] Changes = Array.Empty<StateScopedChange>();
}

internal class StateSnapshotResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal StateStoreSnapshot? Snapshot;
    internal StateScopedChange[] Changes = Array.Empty<StateScopedChange>();
}

internal class StateStoreCore
{
    internal const int MaximumValueDepth = 64;
    internal const int MaximumValueCollectionCount = 10000;
    internal const int MaximumValueStringLength = 1048576;
    internal const int MaximumValueBinaryLength = 1048576;

    internal List<StateScopedEntry> Entries = new List<StateScopedEntry>();
    internal List<StateVersionClock> Versions = new List<StateVersionClock>();
    internal StateDerivedCore DerivedCore = new StateDerivedCore();
    internal StateEffectCore EffectCore = new StateEffectCore();
    internal StateSubscriptionCore SubscriptionCore = new StateSubscriptionCore();
    internal List<StateScopedChange> CommittedChanges = new List<StateScopedChange>();
    internal List<StateScopedChange> PendingChanges = new List<StateScopedChange>();
    internal List<StateStoreSnapshot> Snapshots = new List<StateStoreSnapshot>();
    internal List<StateScopedEntry> BatchEntrySnapshot = new List<StateScopedEntry>();
    internal List<StateVersionClock> BatchVersionSnapshot = new List<StateVersionClock>();
    internal bool BatchActive;
    internal bool BatchFailed;
    internal string BatchId = string.Empty;
    internal string BatchFailureCode = string.Empty;
    internal bool RenderActive;

    internal StateOperationResult CreateStateEntry(string? scopeId, string? stateId, UiValue? value, string? origin)
    {
        if (RenderActive)
        {
            return CreateStateMutationFailure("state.render-write", NormalizeStateScope(scopeId));
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateFailure("state.batch-failed", NormalizeStateScope(scopeId));
        }

        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0)
        {
            return CreateStateMutationFailure(addressCode, NormalizeStateScope(scopeId));
        }

        if (IsInvalidStateOrigin(origin))
        {
            return CreateStateMutationFailure("state.invalid-origin", NormalizeStateScope(scopeId));
        }

        string valueCode = ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null || stateId == null)
        {
            return CreateStateMutationFailure(valueCode, NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        if (FindStateEntryIndex(checkedScope, stateId) >= 0)
        {
            return CreateStateMutationFailure("state.duplicate", checkedScope);
        }

        if (DerivedCore.FindStateDerivedIndex(checkedScope, stateId) >= 0)
        {
            return CreateStateMutationFailure("state.state-id-conflict", checkedScope);
        }

        int priorVersion = GetStateVersion(checkedScope, stateId);
        if (priorVersion == int.MaxValue)
        {
            return CreateStateMutationFailure("state.version-exhausted", checkedScope);
        }

        int version = AdvanceStateVersion(checkedScope, stateId);
        string checkedOrigin = NormalizeStateOrigin(origin);
        UiStateEntry entry = new UiStateEntry();
        entry.Id = stateId;
        entry.Path = string.Empty;
        entry.Value = CopyStateValue(value);
        entry.Version = version;
        entry.Origin = checkedOrigin;
        entry.Patches = Array.Empty<UiStatePatch>();
        InsertStateEntry(new StateScopedEntry { ScopeId = checkedScope, Entry = entry });
        UiStateChange change = CreateStateChange(stateId, null, entry.Value, version, checkedOrigin);
        RecordStateChange(checkedScope, change);
        return CreateStateSuccess(checkedScope, entry, change, true);
    }

    internal StateOperationResult ReadStateEntry(string? scopeId, string? stateId)
    {
        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0 || stateId == null)
        {
            return CreateStateFailure(addressCode, NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        int index = FindStateEntryIndex(checkedScope, stateId);
        if (index < 0)
        {
            return CreateStateFailure("state.unknown", checkedScope);
        }

        return CreateStateSuccess(checkedScope, Entries[index].Entry, null, false);
    }

    internal StateOperationResult WriteStateEntry(string? scopeId, string? stateId, UiValue? value, string? origin)
    {
        if (RenderActive)
        {
            return CreateStateMutationFailure("state.render-write", NormalizeStateScope(scopeId));
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateFailure("state.batch-failed", NormalizeStateScope(scopeId));
        }

        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0)
        {
            return CreateStateMutationFailure(addressCode, NormalizeStateScope(scopeId));
        }

        if (IsInvalidStateOrigin(origin))
        {
            return CreateStateMutationFailure("state.invalid-origin", NormalizeStateScope(scopeId));
        }

        string valueCode = ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null || stateId == null)
        {
            return CreateStateMutationFailure(valueCode, NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        int index = FindStateEntryIndex(checkedScope, stateId);
        if (index < 0)
        {
            return CreateStateMutationFailure("state.unknown", checkedScope);
        }

        int priorVersion = GetStateVersion(checkedScope, stateId);
        if (priorVersion == int.MaxValue)
        {
            return CreateStateMutationFailure("state.version-exhausted", checkedScope);
        }

        UiStateEntry entry = Entries[index].Entry;
        UiValue previousValue = CopyStateValue(entry.Value);
        int version = AdvanceStateVersion(checkedScope, stateId);
        string checkedOrigin = NormalizeStateOrigin(origin);
        entry.Value = CopyStateValue(value);
        entry.Version = version;
        entry.Origin = checkedOrigin;
        entry.Patches = Array.Empty<UiStatePatch>();
        UiStateChange change = CreateStateChange(stateId, previousValue, entry.Value, version, checkedOrigin);
        RecordStateChange(checkedScope, change);
        return CreateStateSuccess(checkedScope, entry, change, true);
    }

    internal StateOperationResult RemoveStateEntry(string? scopeId, string? stateId, string? origin)
    {
        if (RenderActive)
        {
            return CreateStateMutationFailure("state.render-write", NormalizeStateScope(scopeId));
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateFailure("state.batch-failed", NormalizeStateScope(scopeId));
        }

        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0 || stateId == null)
        {
            return CreateStateMutationFailure(addressCode, NormalizeStateScope(scopeId));
        }

        if (IsInvalidStateOrigin(origin))
        {
            return CreateStateMutationFailure("state.invalid-origin", NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        int index = FindStateEntryIndex(checkedScope, stateId);
        if (index < 0)
        {
            return CreateStateMutationFailure("state.unknown", checkedScope);
        }

        int priorVersion = GetStateVersion(checkedScope, stateId);
        if (priorVersion == int.MaxValue)
        {
            return CreateStateMutationFailure("state.version-exhausted", checkedScope);
        }

        UiStateEntry removed = Entries[index].Entry;
        UiValue previousValue = CopyStateValue(removed.Value);
        Entries.RemoveAt(index);
        int version = AdvanceStateVersion(checkedScope, stateId);
        string checkedOrigin = NormalizeStateOrigin(origin);
        UiStateChange change = CreateStateChange(stateId, previousValue, null, version, checkedOrigin);
        RecordStateChange(checkedScope, change);
        return CreateStateSuccess(checkedScope, null, change, true);
    }

    internal StateOperationResult CompareAndSetStateEntry(string? scopeId, string? stateId, int expectedVersion, UiValue? value, string? origin)
    {
        if (RenderActive)
        {
            return CreateStateMutationFailure("state.render-write", NormalizeStateScope(scopeId));
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateFailure("state.batch-failed", NormalizeStateScope(scopeId));
        }

        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0)
        {
            return CreateStateMutationFailure(addressCode, NormalizeStateScope(scopeId));
        }

        if (expectedVersion < 0)
        {
            return CreateStateMutationFailure("state.invalid-version", NormalizeStateScope(scopeId));
        }

        if (IsInvalidStateOrigin(origin))
        {
            return CreateStateMutationFailure("state.invalid-origin", NormalizeStateScope(scopeId));
        }

        string valueCode = ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null || stateId == null)
        {
            return CreateStateMutationFailure(valueCode, NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        int index = FindStateEntryIndex(checkedScope, stateId);
        if (index < 0)
        {
            return CreateStateMutationFailure("state.unknown", checkedScope);
        }

        UiStateEntry current = Entries[index].Entry;
        if (current.Version != expectedVersion)
        {
            StateOperationResult mismatch = CreateStateSuccess(checkedScope, current, null, false);
            mismatch.Code = "state.version-mismatch";
            FailActiveStateBatch(mismatch.Code);
            return mismatch;
        }

        return WriteStateEntry(checkedScope, stateId, value, origin);
    }

    internal StateOperationResult PatchStateEntry(string? scopeId, string? stateId, UiStatePatch[]? patches, string? origin)
    {
        if (RenderActive)
        {
            return CreateStateMutationFailure("state.render-write", NormalizeStateScope(scopeId));
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateFailure("state.batch-failed", NormalizeStateScope(scopeId));
        }

        string addressCode = ValidateStateAddress(scopeId, stateId);
        if (addressCode.Length > 0 || stateId == null)
        {
            return CreateStateMutationFailure(addressCode, NormalizeStateScope(scopeId));
        }

        if (IsInvalidStateOrigin(origin))
        {
            return CreateStateMutationFailure("state.invalid-origin", NormalizeStateScope(scopeId));
        }

        string checkedScope = NormalizeStateScope(scopeId);
        int index = FindStateEntryIndex(checkedScope, stateId);
        if (index < 0)
        {
            return CreateStateMutationFailure("state.unknown", checkedScope);
        }

        StatePatchApplicationResult applied = new StatePatchCore().ApplyStatePatches(this, Entries[index].Entry.Value, patches);
        if (!applied.Success || applied.Value == null)
        {
            StateOperationResult failure = CreateStateMutationFailure(applied.Code, checkedScope);
            failure.OperationIndex = applied.OperationIndex;
            return failure;
        }

        if (!applied.Applied)
        {
            return CreateStateSuccess(checkedScope, Entries[index].Entry, null, false);
        }

        StateOperationResult written = WriteStateEntry(checkedScope, stateId, applied.Value, origin);
        if (!written.Success)
        {
            return written;
        }

        int updatedIndex = FindStateEntryIndex(checkedScope, stateId);
        Entries[updatedIndex].Entry.Patches = CopyStatePatchArray(applied.Patches);
        written.Entry = CopyStateEntry(Entries[updatedIndex].Entry);
        return written;
    }

    internal StateScopedEntry[] GetScopedStateEntries()
    {
        StateScopedEntry[] result = new StateScopedEntry[Entries.Count];
        int index = 0;
        while (index < Entries.Count)
        {
            StateScopedEntry copy = new StateScopedEntry();
            copy.ScopeId = Entries[index].ScopeId;
            copy.Entry = CopyStateEntry(Entries[index].Entry);
            result[index] = copy;
            index++;
        }

        return result;
    }

    internal StateDerivedOperationResult RegisterDerivedState(string? scopeId, UiDerivedState? derived)
    {
        return DerivedCore.RegisterStateDerived(this, scopeId, derived);
    }

    internal StateDerivedOperationResult ReadDerivedState(string? scopeId, string? derivedId)
    {
        return DerivedCore.ReadStateDerived(this, scopeId, derivedId);
    }

    internal StateDerivedOperationResult UpdateDerivedStateDependencies(string? scopeId, string? derivedId, string[]? dependencyIds)
    {
        return DerivedCore.UpdateStateDerivedDependencies(this, scopeId, derivedId, dependencyIds);
    }

    internal StateDerivedOrderResult GetDirtyDerivedStateOrder(string? scopeId)
    {
        return DerivedCore.GetDirtyStateDerivedOrder(this, scopeId);
    }

    internal StateDerivedOperationResult AcceptDerivedStateValue(string? scopeId, string? derivedId, UiValue? value)
    {
        StateDerivedOperationResult result = DerivedCore.AcceptStateDerivedValue(this, scopeId, derivedId, value);
        if (result.Success && derivedId != null)
        {
            string checkedScope = NormalizeStateScope(scopeId);
            if (result.Applied)
            {
                EffectCore.MarkStateEffectDependency(checkedScope, derivedId);
            }

            EffectCore.QueueReadyStateEffects(this);
        }

        return result;
    }

    internal StateEffectOperationResult RegisterStateEffect(string? scopeId, UiEffect? effect)
    {
        return EffectCore.RegisterStateEffect(this, scopeId, effect);
    }

    internal StateEffectOperationResult ReadStateEffect(string? scopeId, string? effectId)
    {
        return EffectCore.ReadStateEffect(this, scopeId, effectId);
    }

    internal StateEffectOperationResult CancelStateEffect(string? scopeId, string? effectId)
    {
        return EffectCore.CancelStateEffect(this, scopeId, effectId);
    }

    internal StateEffectOperationResult CompleteStateEffectExecution(string? scopeId, string? effectId, int generation, bool success, string? errorCode)
    {
        return EffectCore.CompleteStateEffectExecution(this, scopeId, effectId, generation, success, errorCode);
    }

    internal StateEffectOperationResult CompleteStateEffectCleanup(string? scopeId, string? effectId, int generation, bool success, string? errorCode)
    {
        return EffectCore.CompleteStateEffectCleanup(this, scopeId, effectId, generation, success, errorCode);
    }

    internal StateEffectInvocation[] DrainStateEffectInvocations()
    {
        return EffectCore.DrainStateEffectInvocations(this);
    }

    internal StateEffectError[] DrainStateEffectErrors()
    {
        return EffectCore.DrainStateEffectErrors();
    }

    internal StateSubscriptionOperationResult RegisterStateSubscriptionLifetime(string? lifetimeId)
    {
        return SubscriptionCore.RegisterStateSubscriptionLifetime(this, lifetimeId);
    }

    internal StateSubscriptionOperationResult DisposeStateSubscriptionLifetime(string? lifetimeId)
    {
        return SubscriptionCore.DisposeStateSubscriptionLifetime(this, lifetimeId);
    }

    internal StateSubscriptionOperationResult RegisterStateSubscription(string? stateScopeId, UiSubscription? subscription)
    {
        return SubscriptionCore.RegisterStateSubscription(this, stateScopeId, subscription);
    }

    internal StateSubscriptionOperationResult UnsubscribeStateSubscription(string? stateScopeId, string? subscriptionId)
    {
        return SubscriptionCore.UnsubscribeStateSubscription(this, stateScopeId, subscriptionId);
    }

    internal StateSubscriptionDispatchResult BeginStateSubscriptionDispatch()
    {
        return SubscriptionCore.BeginStateSubscriptionDispatch(this);
    }

    internal StateSubscriptionDispatchResult EndStateSubscriptionDispatch()
    {
        return SubscriptionCore.EndStateSubscriptionDispatch(this);
    }

    internal StateRenderResult BeginStateRender()
    {
        if (RenderActive)
        {
            return new StateRenderResult { Success = false, Code = "state.render-active" };
        }

        RenderActive = true;
        return new StateRenderResult { Success = true };
    }

    internal StateRenderResult EndStateRender()
    {
        if (!RenderActive)
        {
            return new StateRenderResult { Success = false, Code = "state.render-not-active" };
        }

        RenderActive = false;
        return new StateRenderResult { Success = true };
    }

    internal StateBatchResult BeginStateBatch(string? batchId)
    {
        if (RenderActive)
        {
            return CreateStateBatchFailure("state.render-write", batchId ?? string.Empty);
        }

        if (BatchActive)
        {
            return CreateStateBatchFailure("state.batch-active", batchId ?? string.Empty);
        }

        if (string.IsNullOrWhiteSpace(batchId))
        {
            return CreateStateBatchFailure("state.invalid-batch", batchId ?? string.Empty);
        }

        BatchActive = true;
        BatchFailed = false;
        BatchId = batchId;
        BatchFailureCode = string.Empty;
        BatchEntrySnapshot = CopyStateEntryList(Entries);
        BatchVersionSnapshot = CopyStateVersionList(Versions);
        PendingChanges.Clear();
        return new StateBatchResult { Success = true, BatchId = batchId };
    }

    internal StateBatchResult CommitStateBatch()
    {
        if (!BatchActive)
        {
            return CreateStateBatchFailure("state.no-batch", string.Empty);
        }

        string completedBatchId = BatchId;
        if (BatchFailed)
        {
            string failureCode = BatchFailureCode;
            ClearStateBatch();
            return CreateStateBatchFailure(failureCode, completedBatchId);
        }

        StateScopedChange[] changes = CopyStateChangeArray(PendingChanges);
        int index = 0;
        while (index < PendingChanges.Count)
        {
            CommittedChanges.Add(CopyScopedStateChange(PendingChanges[index]));
            DerivedCore.MarkStateDerivedDependents(this, PendingChanges[index].ScopeId, PendingChanges[index].Change.StateId);
            index++;
        }

        EffectCore.ScheduleStateEffects(this, PendingChanges);
        SubscriptionCore.ScheduleStateSubscriptions(this, PendingChanges);

        ClearStateBatch();
        StateBatchResult result = new StateBatchResult();
        result.Success = true;
        result.Applied = true;
        result.BatchId = completedBatchId;
        result.Changes = changes;
        return result;
    }

    internal StateBatchResult RollbackStateBatch()
    {
        if (!BatchActive)
        {
            return CreateStateBatchFailure("state.no-batch", string.Empty);
        }

        string rolledBackBatchId = BatchId;
        Entries = CopyStateEntryList(BatchEntrySnapshot);
        Versions = CopyStateVersionList(BatchVersionSnapshot);
        ClearStateBatch();
        return new StateBatchResult { Success = true, BatchId = rolledBackBatchId };
    }

    internal StateSnapshotResult CreateStateSnapshot(string? snapshotId)
    {
        if (BatchActive)
        {
            return CreateStateSnapshotFailure("state.batch-active");
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            return CreateStateSnapshotFailure("state.invalid-snapshot");
        }

        if (FindStateSnapshotIndex(snapshotId) >= 0)
        {
            return CreateStateSnapshotFailure("state.duplicate-snapshot");
        }

        StateStoreSnapshot snapshot = new StateStoreSnapshot();
        snapshot.Id = snapshotId;
        snapshot.Entries = CopyStateEntryList(Entries);
        InsertStateSnapshot(snapshot);
        StateSnapshotResult result = new StateSnapshotResult();
        result.Success = true;
        result.Snapshot = CopyStateSnapshot(snapshot);
        return result;
    }

    internal StateSnapshotResult RestoreStateSnapshot(string? snapshotId, string? origin)
    {
        if (RenderActive)
        {
            FailActiveStateBatch("state.render-write");
            return CreateStateSnapshotFailure("state.render-write");
        }

        if (BatchActive && BatchFailed)
        {
            return CreateStateSnapshotFailure("state.batch-failed");
        }

        if (string.IsNullOrWhiteSpace(snapshotId))
        {
            FailActiveStateBatch("state.invalid-snapshot");
            return CreateStateSnapshotFailure("state.invalid-snapshot");
        }

        if (IsInvalidStateOrigin(origin))
        {
            FailActiveStateBatch("state.invalid-origin");
            return CreateStateSnapshotFailure("state.invalid-origin");
        }

        int snapshotIndex = FindStateSnapshotIndex(snapshotId);
        if (snapshotIndex < 0)
        {
            FailActiveStateBatch("state.unknown-snapshot");
            return CreateStateSnapshotFailure("state.unknown-snapshot");
        }

        bool ownsBatch = !BatchActive;
        if (ownsBatch)
        {
            BeginStateBatch("snapshot-restore:" + snapshotId);
        }

        List<StateScopedEntry> currentEntries = CopyStateEntryList(Entries);
        List<StateScopedEntry> desiredEntries = CopyStateEntryList(Snapshots[snapshotIndex].Entries);
        int currentIndex = 0;
        int desiredIndex = 0;
        string checkedOrigin = NormalizeStateOrigin(origin);
        while (currentIndex < currentEntries.Count || desiredIndex < desiredEntries.Count)
        {
            StateScopedEntry? current = currentIndex < currentEntries.Count ? currentEntries[currentIndex] : null;
            StateScopedEntry? desired = desiredIndex < desiredEntries.Count ? desiredEntries[desiredIndex] : null;
            int comparison = current == null ? 1 : desired == null ? -1 : CompareStateIdentity(current.ScopeId, current.Entry.Id, desired.ScopeId, desired.Entry.Id);
            StateOperationResult operation;
            if (comparison < 0 && current != null)
            {
                operation = RemoveStateEntry(current.ScopeId, current.Entry.Id, checkedOrigin);
                currentIndex++;
            }
            else if (comparison > 0 && desired != null)
            {
                operation = CreateStateEntry(desired.ScopeId, desired.Entry.Id, desired.Entry.Value, checkedOrigin);
                desiredIndex++;
            }
            else if (current != null && desired != null)
            {
                if (StateValuesEqual(current.Entry.Value, desired.Entry.Value))
                {
                    operation = new StateOperationResult { Success = true };
                }
                else
                {
                    operation = WriteStateEntry(current.ScopeId, current.Entry.Id, desired.Entry.Value, checkedOrigin);
                }

                currentIndex++;
                desiredIndex++;
            }
            else
            {
                operation = CreateStateFailure("state.restore-failed", string.Empty);
            }

            if (!operation.Success)
            {
                if (ownsBatch)
                {
                    CommitStateBatch();
                }

                return CreateStateSnapshotFailure(operation.Code);
            }
        }

        StateSnapshotResult result = new StateSnapshotResult();
        result.Success = true;
        result.Applied = true;
        result.Snapshot = CopyStateSnapshot(Snapshots[snapshotIndex]);
        if (ownsBatch)
        {
            StateBatchResult committed = CommitStateBatch();
            if (!committed.Success)
            {
                return CreateStateSnapshotFailure(committed.Code);
            }

            result.Changes = committed.Changes;
        }

        return result;
    }

    internal StateScopedChange[] DrainStateChanges()
    {
        StateScopedChange[] result = CopyStateChangeArray(CommittedChanges);
        CommittedChanges.Clear();
        return result;
    }

    internal string ValidateStateAddress(string? scopeId, string? stateId)
    {
        if (string.IsNullOrWhiteSpace(stateId))
        {
            return "state.invalid-id";
        }

        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return "state.invalid-scope";
        }

        return string.Empty;
    }

    internal bool IsInvalidStateOrigin(string? origin)
    {
        return origin != null && origin.Length > 0 && origin.Trim().Length == 0;
    }

    internal string NormalizeStateScope(string? scopeId)
    {
        return scopeId ?? string.Empty;
    }

    internal string NormalizeStateOrigin(string? origin)
    {
        return origin ?? string.Empty;
    }

    internal int FindStateEntryIndex(string scopeId, string stateId)
    {
        int index = 0;
        while (index < Entries.Count)
        {
            StateScopedEntry candidate = Entries[index];
            int comparison = CompareStateIdentity(candidate.ScopeId, candidate.Entry.Id, scopeId, stateId);
            if (comparison == 0)
            {
                return index;
            }

            if (comparison > 0)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal int FindStateVersionIndex(string scopeId, string stateId)
    {
        int index = 0;
        while (index < Versions.Count)
        {
            StateVersionClock candidate = Versions[index];
            int comparison = CompareStateIdentity(candidate.ScopeId, candidate.StateId, scopeId, stateId);
            if (comparison == 0)
            {
                return index;
            }

            if (comparison > 0)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal int CompareStateIdentity(string leftScopeId, string leftStateId, string rightScopeId, string rightStateId)
    {
        int scopeComparison = ValueComparer.CompareText(leftScopeId, rightScopeId);
        if (scopeComparison != 0)
        {
            return scopeComparison;
        }

        return ValueComparer.CompareText(leftStateId, rightStateId);
    }

    internal void InsertStateEntry(StateScopedEntry scopedEntry)
    {
        int index = 0;
        while (index < Entries.Count && CompareStateIdentity(Entries[index].ScopeId, Entries[index].Entry.Id, scopedEntry.ScopeId, scopedEntry.Entry.Id) < 0)
        {
            index++;
        }

        Entries.Insert(index, scopedEntry);
    }

    internal int GetStateVersion(string scopeId, string stateId)
    {
        int index = FindStateVersionIndex(scopeId, stateId);
        return index < 0 ? 0 : Versions[index].Version;
    }

    internal int AdvanceStateVersion(string scopeId, string stateId)
    {
        int index = FindStateVersionIndex(scopeId, stateId);
        if (index >= 0)
        {
            Versions[index].Version++;
            return Versions[index].Version;
        }

        StateVersionClock clock = new StateVersionClock();
        clock.ScopeId = scopeId;
        clock.StateId = stateId;
        clock.Version = 1;
        int insertionIndex = 0;
        while (insertionIndex < Versions.Count && CompareStateIdentity(Versions[insertionIndex].ScopeId, Versions[insertionIndex].StateId, scopeId, stateId) < 0)
        {
            insertionIndex++;
        }

        Versions.Insert(insertionIndex, clock);
        return clock.Version;
    }

    internal StateOperationResult CreateStateSuccess(string scopeId, UiStateEntry? entry, UiStateChange? change, bool applied)
    {
        StateOperationResult result = new StateOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.ScopeId = scopeId;
        if (entry != null)
        {
            result.Entry = CopyStateEntry(entry);
        }

        if (change != null)
        {
            result.Change = CopyStateChange(change);
        }

        return result;
    }

    internal StateOperationResult CreateStateFailure(string code, string scopeId)
    {
        return new StateOperationResult { Success = false, Code = code, ScopeId = scopeId };
    }

    internal StateOperationResult CreateStateMutationFailure(string code, string scopeId)
    {
        FailActiveStateBatch(code);
        return CreateStateFailure(code, scopeId);
    }

    internal StateBatchResult CreateStateBatchFailure(string code, string batchId)
    {
        return new StateBatchResult { Success = false, Code = code, BatchId = batchId };
    }

    internal StateSnapshotResult CreateStateSnapshotFailure(string code)
    {
        return new StateSnapshotResult { Success = false, Code = code };
    }

    internal void RecordStateChange(string scopeId, UiStateChange change)
    {
        StateScopedChange scopedChange = new StateScopedChange();
        scopedChange.ScopeId = scopeId;
        scopedChange.Change = CopyStateChange(change);
        if (!BatchActive)
        {
            CommittedChanges.Add(scopedChange);
            DerivedCore.MarkStateDerivedDependents(this, scopeId, change.StateId);
            List<StateScopedChange> changes = new List<StateScopedChange>();
            changes.Add(scopedChange);
            EffectCore.ScheduleStateEffects(this, changes);
            SubscriptionCore.ScheduleStateSubscriptions(this, changes);
            return;
        }

        int index = FindPendingStateChangeIndex(scopeId, change.StateId);
        if (index < 0)
        {
            PendingChanges.Add(scopedChange);
            return;
        }

        UiStateChange pending = PendingChanges[index].Change;
        pending.CurrentValue = change.CurrentValue == null ? null : CopyStateValue(change.CurrentValue);
        pending.Version = change.Version;
        pending.Origin = change.Origin;
        if (pending.PreviousValue == null && pending.CurrentValue == null)
        {
            PendingChanges.RemoveAt(index);
        }
    }

    internal int FindPendingStateChangeIndex(string scopeId, string stateId)
    {
        int index = 0;
        while (index < PendingChanges.Count)
        {
            StateScopedChange candidate = PendingChanges[index];
            if (ValueComparer.TextEquals(candidate.ScopeId, scopeId) && ValueComparer.TextEquals(candidate.Change.StateId, stateId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void FailActiveStateBatch(string code)
    {
        if (!BatchActive || BatchFailed)
        {
            return;
        }

        Entries = CopyStateEntryList(BatchEntrySnapshot);
        Versions = CopyStateVersionList(BatchVersionSnapshot);
        PendingChanges.Clear();
        BatchFailed = true;
        BatchFailureCode = code;
    }

    internal void ClearStateBatch()
    {
        BatchActive = false;
        BatchFailed = false;
        BatchId = string.Empty;
        BatchFailureCode = string.Empty;
        BatchEntrySnapshot.Clear();
        BatchVersionSnapshot.Clear();
        PendingChanges.Clear();
    }

    internal List<StateScopedEntry> CopyStateEntryList(List<StateScopedEntry> source)
    {
        List<StateScopedEntry> result = new List<StateScopedEntry>();
        int index = 0;
        while (index < source.Count)
        {
            StateScopedEntry entry = new StateScopedEntry();
            entry.ScopeId = source[index].ScopeId;
            entry.Entry = CopyStateEntry(source[index].Entry);
            result.Add(entry);
            index++;
        }

        return result;
    }

    internal List<StateVersionClock> CopyStateVersionList(List<StateVersionClock> source)
    {
        List<StateVersionClock> result = new List<StateVersionClock>();
        int index = 0;
        while (index < source.Count)
        {
            StateVersionClock clock = new StateVersionClock();
            clock.ScopeId = source[index].ScopeId;
            clock.StateId = source[index].StateId;
            clock.Version = source[index].Version;
            result.Add(clock);
            index++;
        }

        return result;
    }

    internal StateScopedChange CopyScopedStateChange(StateScopedChange source)
    {
        StateScopedChange result = new StateScopedChange();
        result.ScopeId = source.ScopeId;
        result.Change = CopyStateChange(source.Change);
        return result;
    }

    internal StateScopedChange[] CopyStateChangeArray(List<StateScopedChange> source)
    {
        StateScopedChange[] result = new StateScopedChange[source.Count];
        int index = 0;
        while (index < source.Count)
        {
            result[index] = CopyScopedStateChange(source[index]);
            index++;
        }

        return result;
    }

    internal int FindStateSnapshotIndex(string snapshotId)
    {
        int index = 0;
        while (index < Snapshots.Count)
        {
            int comparison = ValueComparer.CompareText(Snapshots[index].Id, snapshotId);
            if (comparison == 0)
            {
                return index;
            }

            if (comparison > 0)
            {
                return -1;
            }

            index++;
        }

        return -1;
    }

    internal void InsertStateSnapshot(StateStoreSnapshot snapshot)
    {
        int index = 0;
        while (index < Snapshots.Count && ValueComparer.CompareText(Snapshots[index].Id, snapshot.Id) < 0)
        {
            index++;
        }

        Snapshots.Insert(index, snapshot);
    }

    internal StateStoreSnapshot CopyStateSnapshot(StateStoreSnapshot source)
    {
        StateStoreSnapshot result = new StateStoreSnapshot();
        result.Id = source.Id;
        result.Entries = CopyStateEntryList(source.Entries);
        return result;
    }

    internal bool StateValuesEqual(UiValue left, UiValue right)
    {
        return ValueJsonCodec.ValuesEqual(left, right, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
    }

    internal UiStateEntry CopyStateEntry(UiStateEntry source)
    {
        UiStateEntry result = new UiStateEntry();
        result.Id = source.Id;
        result.Path = source.Path;
        result.Value = CopyStateValue(source.Value);
        result.Version = source.Version;
        result.Origin = source.Origin;
        result.Patches = CopyStatePatchArray(source.Patches);
        result.Derived = CopyStateDerivedArray(source.Derived);
        result.Effects = CopyStateEffectArray(source.Effects);
        result.Subscriptions = CopyStateSubscriptionArray(source.Subscriptions);
        return result;
    }

    internal UiStatePatch[] CopyStatePatchArray(UiStatePatch[]? source)
    {
        UiStatePatch[] patches = source ?? Array.Empty<UiStatePatch>();
        UiStatePatch[] result = new UiStatePatch[patches.Length];
        int index = 0;
        while (index < patches.Length)
        {
            UiStatePatch patch = patches[index];
            UiStatePatch copy = new UiStatePatch();
            copy.Operation = patch.Operation;
            copy.Path = patch.Path;
            copy.FromPath = patch.FromPath;
            if (patch.Value != null)
            {
                copy.Value = CopyStateValue(patch.Value);
            }

            result[index] = copy;
            index++;
        }

        return result;
    }

    internal UiDerivedState[] CopyStateDerivedArray(UiDerivedState[]? source)
    {
        UiDerivedState[] derivedStates = source ?? Array.Empty<UiDerivedState>();
        UiDerivedState[] result = new UiDerivedState[derivedStates.Length];
        int index = 0;
        while (index < derivedStates.Length)
        {
            result[index] = DerivedCore.CopyStateDerived(this, derivedStates[index]);
            index++;
        }

        return result;
    }

    internal UiEffect[] CopyStateEffectArray(UiEffect[]? source)
    {
        UiEffect[] effects = source ?? Array.Empty<UiEffect>();
        UiEffect[] result = new UiEffect[effects.Length];
        int index = 0;
        while (index < effects.Length)
        {
            result[index] = EffectCore.CopyStateEffect(effects[index]);
            index++;
        }

        return result;
    }

    internal UiSubscription[] CopyStateSubscriptionArray(UiSubscription[]? source)
    {
        UiSubscription[] subscriptions = source ?? Array.Empty<UiSubscription>();
        UiSubscription[] result = new UiSubscription[subscriptions.Length];
        int index = 0;
        while (index < subscriptions.Length)
        {
            result[index] = SubscriptionCore.CopyStateSubscription(subscriptions[index]);
            index++;
        }

        return result;
    }

    internal UiStateChange CreateStateChange(string stateId, UiValue? previousValue, UiValue? currentValue, int version, string origin)
    {
        UiStateChange result = new UiStateChange();
        result.StateId = stateId;
        result.Path = string.Empty;
        if (previousValue != null)
        {
            result.PreviousValue = CopyStateValue(previousValue);
        }

        if (currentValue != null)
        {
            result.CurrentValue = CopyStateValue(currentValue);
        }

        result.Version = version;
        result.Origin = origin;
        return result;
    }

    internal UiStateChange CopyStateChange(UiStateChange source)
    {
        return CreateStateChange(source.StateId, source.PreviousValue, source.CurrentValue, source.Version, source.Origin);
    }

    internal UiValue CopyStateValue(UiValue source)
    {
        return ValueJsonCodec.CloneValue(source, MaximumValueDepth, MaximumValueCollectionCount, MaximumValueStringLength, MaximumValueBinaryLength);
    }

    internal string ValidateStateValue(UiValue? value)
    {
        if (value == null)
        {
            return "state.null-value";
        }

        StateValueValidationState state = new StateValueValidationState();
        return ValidateStateValueNode(value, 0, state);
    }

    internal string ValidateStateValueNode(UiValue? value, int depth, StateValueValidationState state)
    {
        if (value == null)
        {
            return "state.invalid-value";
        }

        if (depth > MaximumValueDepth)
        {
            return "state.value-too-deep";
        }

        int ancestorIndex = 0;
        while (ancestorIndex < state.Ancestors.Count)
        {
            if (object.ReferenceEquals(state.Ancestors[ancestorIndex], value))
            {
                return "state.value-cycle";
            }

            ancestorIndex++;
        }

        state.Ancestors.Add(value);
        string shapeCode = ValidateStateValueShape(value);
        if (shapeCode.Length > 0)
        {
            return shapeCode;
        }

        if (value.Kind == UiValueKind.Array)
        {
            UiValue[] items = value.ArrayValue ?? Array.Empty<UiValue>();
            if (!AddStateCollectionCount(items.Length, state))
            {
                return "state.value-too-large";
            }

            int itemIndex = 0;
            while (itemIndex < items.Length)
            {
                string itemCode = ValidateStateValueNode(items[itemIndex], depth + 1, state);
                if (itemCode.Length > 0)
                {
                    return itemCode;
                }

                itemIndex++;
            }
        }
        else if (value.Kind == UiValueKind.Object)
        {
            UiValueEntry[] entries = value.ObjectValue ?? Array.Empty<UiValueEntry>();
            if (!AddStateCollectionCount(entries.Length, state))
            {
                return "state.value-too-large";
            }

            int entryIndex = 0;
            while (entryIndex < entries.Length)
            {
                UiValueEntry? entry = entries[entryIndex];
                if (entry == null || entry.Key == null || entry.Key.Length > MaximumValueStringLength)
                {
                    return "state.invalid-value";
                }

                int priorIndex = 0;
                while (priorIndex < entryIndex)
                {
                    if (ValueComparer.TextEquals(entries[priorIndex].Key, entry.Key))
                    {
                        return "state.invalid-value";
                    }

                    priorIndex++;
                }

                string entryCode = ValidateStateValueNode(entry.Value, depth + 1, state);
                if (entryCode.Length > 0)
                {
                    return entryCode;
                }

                entryIndex++;
            }
        }

        state.Ancestors.RemoveAt(state.Ancestors.Count - 1);
        return string.Empty;
    }

    internal string ValidateStateValueShape(UiValue value)
    {
        bool hasReference = value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null;
        bool hasPrimitive = value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m;
        switch (value.Kind)
        {
            case UiValueKind.Null:
                return hasReference || hasPrimitive ? "state.invalid-value" : string.Empty;
            case UiValueKind.Boolean:
                return hasReference || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m ? "state.invalid-value" : string.Empty;
            case UiValueKind.Integer:
                return hasReference || value.BooleanValue || value.FloatingPointValue != 0.0 || value.DecimalValue != 0m ? "state.invalid-value" : string.Empty;
            case UiValueKind.FloatingPoint:
                if (hasReference || value.BooleanValue || value.IntegerValue != 0L || value.DecimalValue != 0m || double.IsNaN(value.FloatingPointValue) || double.IsInfinity(value.FloatingPointValue))
                {
                    return "state.invalid-value";
                }

                return string.Empty;
            case UiValueKind.Decimal:
                return hasReference || value.BooleanValue || value.IntegerValue != 0L || value.FloatingPointValue != 0.0 ? "state.invalid-value" : string.Empty;
            case UiValueKind.String:
                if (value.StringValue == null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "state.invalid-value";
                }

                return value.StringValue.Length > MaximumValueStringLength ? "state.value-too-large" : string.Empty;
            case UiValueKind.DateTime:
                if (value.StringValue != null || value.DateTimeValue == null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "state.invalid-value";
                }

                return value.DateTimeValue.Length > MaximumValueStringLength ? "state.value-too-large" : string.Empty;
            case UiValueKind.Binary:
                if (value.StringValue != null || value.DateTimeValue != null || value.BinaryValue == null || value.ArrayValue != null || value.ObjectValue != null || hasPrimitive)
                {
                    return "state.invalid-value";
                }

                return value.BinaryValue.Length > MaximumValueBinaryLength ? "state.value-too-large" : string.Empty;
            case UiValueKind.Array:
                return value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue == null || value.ObjectValue != null || hasPrimitive ? "state.invalid-value" : string.Empty;
            case UiValueKind.Object:
                return value.StringValue != null || value.DateTimeValue != null || value.BinaryValue != null || value.ArrayValue != null || value.ObjectValue == null || hasPrimitive ? "state.invalid-value" : string.Empty;
            default:
                return "state.invalid-value";
        }
    }

    internal bool AddStateCollectionCount(int count, StateValueValidationState state)
    {
        if (count > MaximumValueCollectionCount - state.CollectionCount)
        {
            return false;
        }

        state.CollectionCount += count;
        return true;
    }
}
