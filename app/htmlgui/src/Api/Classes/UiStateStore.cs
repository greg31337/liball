using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Provides scoped observable state, transactions, snapshots, derived state, effects, and observers.</summary>
public class UiStateStore : IUiStateStore
{
    internal const int MaximumObserverDispatchCycles = 64;

    internal StateStoreCore Core;
    internal List<string> ObserverStateScopeIds;
    internal List<UiSubscription> ObserverSubscriptions;
    internal List<IUiStateObserver> Observers;

    /// <summary>Initializes an empty state store.</summary>
    public UiStateStore()
    {
        Core = new StateStoreCore();
        ObserverStateScopeIds = new List<string>();
        ObserverSubscriptions = new List<UiSubscription>();
        Observers = new List<IUiStateObserver>();
    }

    /// <summary>Creates one state entry and delivers resulting observer callbacks.</summary>
    public UiStateResult CreateState(string? scopeId, string? stateId, UiValue? value, string? origin)
    {
        return CompletePublicStateOperation(Core.CreateStateEntry(scopeId, stateId, value, origin), true);
    }

    /// <summary>Reads one copied state entry.</summary>
    public UiStateResult ReadState(string? scopeId, string? stateId)
    {
        return CompletePublicStateOperation(Core.ReadStateEntry(scopeId, stateId), false);
    }

    /// <summary>Writes one state entry and delivers resulting observer callbacks.</summary>
    public UiStateResult WriteState(string? scopeId, string? stateId, UiValue? value, string? origin)
    {
        return CompletePublicStateOperation(Core.WriteStateEntry(scopeId, stateId, value, origin), true);
    }

    /// <summary>Removes one state entry and delivers resulting observer callbacks.</summary>
    public UiStateResult RemoveState(string? scopeId, string? stateId, string? origin)
    {
        return CompletePublicStateOperation(Core.RemoveStateEntry(scopeId, stateId, origin), true);
    }

    /// <summary>Writes one state entry when its version matches and delivers resulting observer callbacks.</summary>
    public UiStateResult CompareAndSetState(string? scopeId, string? stateId, int expectedVersion, UiValue? value, string? origin)
    {
        return CompletePublicStateOperation(Core.CompareAndSetStateEntry(scopeId, stateId, expectedVersion, value, origin), true);
    }

    /// <summary>Applies bounded patches and delivers resulting observer callbacks.</summary>
    public UiStateResult PatchState(string? scopeId, string? stateId, UiStatePatch[]? patches, string? origin)
    {
        return CompletePublicStateOperation(Core.PatchStateEntry(scopeId, stateId, patches, origin), true);
    }

    /// <summary>Begins one atomic state batch.</summary>
    public UiStateBatchResult BeginStateBatch(string? batchId)
    {
        return CompletePublicStateBatch(Core.BeginStateBatch(batchId), false);
    }

    /// <summary>Commits the active batch and delivers its observer callbacks.</summary>
    public UiStateBatchResult CommitStateBatch()
    {
        return CompletePublicStateBatch(Core.CommitStateBatch(), true);
    }

    /// <summary>Rolls back the active batch without delivering callbacks.</summary>
    public UiStateBatchResult RollbackStateBatch()
    {
        return CompletePublicStateBatch(Core.RollbackStateBatch(), false);
    }

    /// <summary>Creates one named state snapshot.</summary>
    public UiStateSnapshotResult CreateStateSnapshot(string? snapshotId)
    {
        return CompletePublicStateSnapshot(Core.CreateStateSnapshot(snapshotId), false);
    }

    /// <summary>Restores one named snapshot and delivers committed observer callbacks.</summary>
    public UiStateSnapshotResult RestoreStateSnapshot(string? snapshotId, string? origin)
    {
        return CompletePublicStateSnapshot(Core.RestoreStateSnapshot(snapshotId, origin), true);
    }

    /// <summary>Registers one derived-state definition.</summary>
    public UiDerivedStateResult RegisterDerivedState(string? scopeId, UiDerivedState? derived)
    {
        return CreatePublicDerivedStateResult(Core.RegisterDerivedState(scopeId, derived));
    }

    /// <summary>Reads one copied derived-state definition.</summary>
    public UiDerivedStateResult ReadDerivedState(string? scopeId, string? derivedId)
    {
        return CreatePublicDerivedStateResult(Core.ReadDerivedState(scopeId, derivedId));
    }

    /// <summary>Updates one derived state's explicit dependencies.</summary>
    public UiDerivedStateResult UpdateDerivedStateDependencies(string? scopeId, string? derivedId, string[]? dependencyIds)
    {
        return CreatePublicDerivedStateResult(Core.UpdateDerivedStateDependencies(scopeId, derivedId, dependencyIds));
    }

    /// <summary>Accepts a computed value for one dirty derived state.</summary>
    public UiDerivedStateResult AcceptDerivedStateValue(string? scopeId, string? derivedId, UiValue? value)
    {
        return CreatePublicDerivedStateResult(Core.AcceptDerivedStateValue(scopeId, derivedId, value));
    }

    /// <summary>Registers one post-commit state effect.</summary>
    public UiStateEffectResult RegisterStateEffect(string? scopeId, UiEffect? effect)
    {
        return CreatePublicStateEffectResult(Core.RegisterStateEffect(scopeId, effect));
    }

    /// <summary>Reads one copied post-commit state effect.</summary>
    public UiStateEffectResult ReadStateEffect(string? scopeId, string? effectId)
    {
        return CreatePublicStateEffectResult(Core.ReadStateEffect(scopeId, effectId));
    }

    /// <summary>Cancels one post-commit state effect.</summary>
    public UiStateEffectResult CancelStateEffect(string? scopeId, string? effectId)
    {
        return CreatePublicStateEffectResult(Core.CancelStateEffect(scopeId, effectId));
    }

    /// <summary>Registers one explicit observer lifetime scope.</summary>
    public UiStateSubscriptionResult RegisterStateSubscriptionLifetime(string? lifetimeId)
    {
        return CreatePublicStateSubscriptionResult(Core.RegisterStateSubscriptionLifetime(lifetimeId));
    }

    /// <summary>Disposes an observer lifetime scope and all of its callbacks.</summary>
    public UiStateSubscriptionResult DisposeStateSubscriptionLifetime(string? lifetimeId)
    {
        StateSubscriptionOperationResult disposed = Core.DisposeStateSubscriptionLifetime(lifetimeId);
        if (disposed.Success && lifetimeId != null)
        {
            RemovePublicStateObserversByLifetime(lifetimeId);
        }

        return CreatePublicStateSubscriptionResult(disposed);
    }

    /// <summary>Registers one state subscription and observer callback.</summary>
    public UiStateSubscriptionResult RegisterStateSubscription(string? stateScopeId, UiSubscription? subscription, IUiStateObserver? observer)
    {
        if (observer == null)
        {
            return CreatePublicStateSubscriptionFailure("state.null-observer", "/state/subscription/observer");
        }

        StateSubscriptionOperationResult registered = Core.RegisterStateSubscription(stateScopeId, subscription);
        if (registered.Success && subscription != null)
        {
            string checkedScope = Core.NormalizeStateScope(stateScopeId);
            ObserverStateScopeIds.Add(checkedScope);
            ObserverSubscriptions.Add(Core.SubscriptionCore.CopyStateSubscription(subscription));
            Observers.Add(observer);
        }

        return CreatePublicStateSubscriptionResult(registered);
    }

    /// <summary>Removes one state subscription and observer callback.</summary>
    public UiStateSubscriptionResult UnsubscribeState(string? stateScopeId, string? subscriptionId)
    {
        StateSubscriptionOperationResult removed = Core.UnsubscribeStateSubscription(stateScopeId, subscriptionId);
        if (removed.Success)
        {
            int index = FindPublicStateObserverIndex(Core.NormalizeStateScope(stateScopeId), subscriptionId);
            if (index >= 0)
            {
                RemovePublicStateObserver(index);
            }
        }

        return CreatePublicStateSubscriptionResult(removed);
    }

    internal UiStateResult CompletePublicStateOperation(StateOperationResult operation, bool dispatchObservers)
    {
        UiStateResult result = new UiStateResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        result.OperationIndex = operation.OperationIndex;
        result.ScopeId = operation.ScopeId;
        if (operation.Entry != null)
        {
            result.Entry = Core.CopyStateEntry(operation.Entry);
        }

        if (operation.Change != null)
        {
            result.Change = Core.CopyStateChange(operation.Change);
        }

        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        AddStateOperationCode(diagnostics, operation.Code, "/state");
        if (dispatchObservers && operation.Success)
        {
            UiResult observed = DispatchStateObservers();
            AddPublicStateDiagnostics(diagnostics, observed.Diagnostics);
            if (!observed.Success)
            {
                result.Success = false;
            }
        }

        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal UiStateBatchResult CompletePublicStateBatch(StateBatchResult operation, bool dispatchObservers)
    {
        UiStateBatchResult result = new UiStateBatchResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        result.BatchId = operation.BatchId;
        result.Changes = CopyPublicScopedStateChanges(operation.Changes);
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        AddStateOperationCode(diagnostics, operation.Code, "/state/batch");
        if (dispatchObservers && operation.Success)
        {
            UiResult observed = DispatchStateObservers();
            AddPublicStateDiagnostics(diagnostics, observed.Diagnostics);
            if (!observed.Success)
            {
                result.Success = false;
            }
        }

        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal UiStateSnapshotResult CompletePublicStateSnapshot(StateSnapshotResult operation, bool dispatchObservers)
    {
        UiStateSnapshotResult result = new UiStateSnapshotResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        if (operation.Snapshot != null)
        {
            result.SnapshotId = operation.Snapshot.Id;
            result.Entries = CopyPublicScopedStateEntries(operation.Snapshot.Entries);
        }

        result.Changes = CopyPublicScopedStateChanges(operation.Changes);
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        AddStateOperationCode(diagnostics, operation.Code, "/state/snapshot");
        if (dispatchObservers && operation.Success)
        {
            UiResult observed = DispatchStateObservers();
            AddPublicStateDiagnostics(diagnostics, observed.Diagnostics);
            if (!observed.Success)
            {
                result.Success = false;
            }
        }

        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal UiDerivedStateResult CreatePublicDerivedStateResult(StateDerivedOperationResult operation)
    {
        UiDerivedStateResult result = new UiDerivedStateResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        result.ScopeId = operation.ScopeId;
        if (operation.Derived != null)
        {
            result.Derived = Core.DerivedCore.CopyStateDerived(Core, operation.Derived);
        }

        if (operation.Code.Length > 0)
        {
            result.Diagnostics = new UiDiagnostic[] { CreateStateDiagnostic(operation.Code, "/state/derived") };
        }

        return result;
    }

    internal UiStateEffectResult CreatePublicStateEffectResult(StateEffectOperationResult operation)
    {
        UiStateEffectResult result = new UiStateEffectResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        result.ScopeId = operation.ScopeId;
        if (operation.Effect != null)
        {
            result.Effect = Core.EffectCore.CopyStateEffect(operation.Effect);
        }

        if (operation.Code.Length > 0)
        {
            result.Diagnostics = new UiDiagnostic[] { CreateStateDiagnostic(operation.Code, "/state/effect") };
        }

        return result;
    }

    internal UiStateSubscriptionResult CreatePublicStateSubscriptionResult(StateSubscriptionOperationResult operation)
    {
        UiStateSubscriptionResult result = new UiStateSubscriptionResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        if (operation.Subscription != null)
        {
            result.Subscription = Core.SubscriptionCore.CopyStateSubscription(operation.Subscription);
        }

        if (operation.Code.Length > 0)
        {
            result.Diagnostics = new UiDiagnostic[] { CreateStateDiagnostic(operation.Code, "/state/subscription") };
        }

        return result;
    }

    internal UiStateSubscriptionResult CreatePublicStateSubscriptionFailure(string code, string location)
    {
        UiStateSubscriptionResult result = new UiStateSubscriptionResult();
        result.Diagnostics = new UiDiagnostic[] { CreateStateDiagnostic(code, location) };
        return result;
    }

    internal UiResult DispatchStateObservers()
    {
        UiResult result = new UiResult { Success = true };
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        int cycle = 0;
        while (cycle < MaximumObserverDispatchCycles)
        {
            StateSubscriptionDispatchResult dispatch = Core.BeginStateSubscriptionDispatch();
            if (!dispatch.Success)
            {
                if (dispatch.Code == "state.subscription-dispatch-active")
                {
                    result.Diagnostics = diagnostics.ToArray();
                    return result;
                }

                result.Success = false;
                diagnostics.Add(CreateStateDiagnostic(dispatch.Code, "/state/subscription/dispatch"));
                result.Diagnostics = diagnostics.ToArray();
                return result;
            }

            int invocationIndex = 0;
            while (invocationIndex < dispatch.Invocations.Length)
            {
                StateSubscriptionInvocation invocation = dispatch.Invocations[invocationIndex];
                int observerIndex = FindPublicStateObserverIndex(invocation.StateScopeId, invocation.SubscriptionId);
                if (observerIndex >= 0)
                {
                    UiResult? callback = Observers[observerIndex].ObserveStateChange(CreatePublicStateObserverContext(invocation));
                    if (callback == null)
                    {
                        result.Success = false;
                        diagnostics.Add(CreateStateDiagnostic("state.null-observer-result", "/state/subscription/observer"));
                    }
                    else
                    {
                        if (!callback.Success)
                        {
                            result.Success = false;
                            if (callback.Diagnostics.Length == 0)
                            {
                                diagnostics.Add(CreateStateDiagnostic("state.observer-failed", "/state/subscription/observer"));
                            }
                        }

                        AddPublicStateDiagnostics(diagnostics, callback.Diagnostics);
                    }
                }

                invocationIndex++;
            }

            StateSubscriptionDispatchResult ended = Core.EndStateSubscriptionDispatch();
            if (!ended.Success)
            {
                result.Success = false;
                diagnostics.Add(CreateStateDiagnostic(ended.Code, "/state/subscription/dispatch"));
                result.Diagnostics = diagnostics.ToArray();
                return result;
            }

            if (dispatch.Invocations.Length == 0)
            {
                result.Diagnostics = diagnostics.ToArray();
                return result;
            }

            cycle++;
        }

        result.Success = false;
        diagnostics.Add(CreateStateDiagnostic("state.observer-reentrancy-limit", "/state/subscription/dispatch"));
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal UiStateObserverContext CreatePublicStateObserverContext(StateSubscriptionInvocation invocation)
    {
        UiStateObserverContext result = new UiStateObserverContext();
        result.StateScopeId = invocation.StateScopeId;
        result.SubscriptionId = invocation.SubscriptionId;
        result.HandlerId = invocation.HandlerId;
        result.Change = Core.CopyStateChange(invocation.Change);
        if (invocation.PreviousValue != null)
        {
            result.PreviousValue = Core.CopyStateValue(invocation.PreviousValue);
        }

        if (invocation.CurrentValue != null)
        {
            result.CurrentValue = Core.CopyStateValue(invocation.CurrentValue);
        }

        return result;
    }

    internal int FindPublicStateObserverIndex(string stateScopeId, string? subscriptionId)
    {
        if (subscriptionId == null)
        {
            return -1;
        }

        int index = 0;
        while (index < ObserverSubscriptions.Count)
        {
            if (ValueComparer.TextEquals(ObserverStateScopeIds[index], stateScopeId) && ValueComparer.TextEquals(ObserverSubscriptions[index].Id, subscriptionId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void RemovePublicStateObserversByLifetime(string lifetimeId)
    {
        int index = ObserverSubscriptions.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(ObserverSubscriptions[index].ScopeId, lifetimeId))
            {
                RemovePublicStateObserver(index);
            }

            index--;
        }
    }

    internal void RemovePublicStateObserver(int index)
    {
        ObserverStateScopeIds.RemoveAt(index);
        ObserverSubscriptions.RemoveAt(index);
        Observers.RemoveAt(index);
    }

    internal UiScopedStateChange[] CopyPublicScopedStateChanges(StateScopedChange[] source)
    {
        UiScopedStateChange[] result = new UiScopedStateChange[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            UiScopedStateChange change = new UiScopedStateChange();
            change.ScopeId = source[index].ScopeId;
            change.Change = Core.CopyStateChange(source[index].Change);
            result[index] = change;
            index++;
        }

        return result;
    }

    internal UiScopedStateEntry[] CopyPublicScopedStateEntries(List<StateScopedEntry> source)
    {
        UiScopedStateEntry[] result = new UiScopedStateEntry[source.Count];
        int index = 0;
        while (index < source.Count)
        {
            UiScopedStateEntry entry = new UiScopedStateEntry();
            entry.ScopeId = source[index].ScopeId;
            entry.Entry = Core.CopyStateEntry(source[index].Entry);
            result[index] = entry;
            index++;
        }

        return result;
    }

    internal void AddStateOperationCode(List<UiDiagnostic> diagnostics, string code, string location)
    {
        if (code.Length > 0)
        {
            diagnostics.Add(CreateStateDiagnostic(code, location));
        }
    }

    internal void AddPublicStateDiagnostics(List<UiDiagnostic> target, UiDiagnostic[]? source)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length)
        {
            target.Add(CopyPublicStateDiagnostic(diagnostics[index]));
            index++;
        }
    }

    internal UiDiagnostic CopyPublicStateDiagnostic(UiDiagnostic source)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = source.Severity;
        result.Code = source.Code;
        result.Message = source.Message;
        result.Location = source.Location;
        return result;
    }

    internal UiDiagnostic CreateStateDiagnostic(string code, string location)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = UiDiagnosticSeverity.Error;
        result.Code = code;
        result.Message = GetStateDiagnosticMessage(code);
        result.Location = location;
        return result;
    }

    internal string GetStateDiagnosticMessage(string code)
    {
        switch (code)
        {
            case "state.null-observer":
                return "A state observer callback is required.";
            case "state.null-observer-result":
                return "A state observer returned no callback result.";
            case "state.observer-failed":
                return "A state observer reported failure.";
            case "state.observer-reentrancy-limit":
                return "State observer reentrancy exceeded the bounded dispatch cycle limit.";
            case "state.version-mismatch":
                return "The current state version does not match the expected version.";
            case "state.unknown":
                return "The requested state entry does not exist.";
            case "state.batch-active":
                return "The operation is unavailable while a state batch is active.";
            case "state.no-batch":
                return "No state batch is active.";
            default:
                return "The state operation reported code '" + code + "'.";
        }
    }
}
