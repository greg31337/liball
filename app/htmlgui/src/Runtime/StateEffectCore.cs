using System;
using System.Collections.Generic;

namespace htmlgui;

internal class StateScopedEffect
{
    internal string ScopeId = string.Empty;
    internal UiEffect Effect = new UiEffect();
    internal bool Dirty;
    internal bool Running;
    internal bool HasActiveCleanup;
}

internal class StateEffectInvocation
{
    internal string ScopeId = string.Empty;
    internal string EffectId = string.Empty;
    internal string HandlerId = string.Empty;
    internal string Kind = string.Empty;
    internal int Generation;
    internal UiValueEntry[] Dependencies = Array.Empty<UiValueEntry>();
}

internal class StateEffectError
{
    internal string ScopeId = string.Empty;
    internal string EffectId = string.Empty;
    internal string HandlerId = string.Empty;
    internal int Generation;
    internal string Code = string.Empty;
}

internal class StateEffectOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiEffect? Effect;
}

internal class StateRenderResult
{
    internal bool Success;
    internal string Code = string.Empty;
}

internal class StateEffectCore
{
    internal const string ExecuteInvocation = "execute";
    internal const string CleanupInvocation = "cleanup";

    internal List<StateScopedEffect> Effects = new List<StateScopedEffect>();
    internal List<StateEffectInvocation> PendingInvocations = new List<StateEffectInvocation>();
    internal List<StateEffectError> Errors = new List<StateEffectError>();

    internal StateEffectOperationResult RegisterStateEffect(StateStoreCore store, string? scopeId, UiEffect? effect)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (store.RenderActive)
        {
            return CreateStateEffectFailure("state.render-write", checkedScope);
        }

        if (store.BatchActive)
        {
            return CreateStateEffectFailure("state.batch-active", checkedScope);
        }

        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateEffectFailure("state.invalid-scope", checkedScope);
        }

        string validationCode = ValidateStateEffect(store, checkedScope, effect);
        if (validationCode.Length > 0 || effect == null)
        {
            return CreateStateEffectFailure(validationCode, checkedScope);
        }

        if (FindStateEffectIndex(checkedScope, effect.Id) >= 0)
        {
            return CreateStateEffectFailure("state.duplicate-effect", checkedScope);
        }

        StateScopedEffect scoped = new StateScopedEffect();
        scoped.ScopeId = checkedScope;
        scoped.Effect = CopyStateEffect(effect);
        InsertStateEffect(scoped);
        RefreshStateEffectViews(store, checkedScope);
        return CreateStateEffectSuccess(checkedScope, scoped.Effect, true);
    }

    internal StateEffectOperationResult ReadStateEffect(StateStoreCore store, string? scopeId, string? effectId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateEffectFailure("state.invalid-scope", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(effectId))
        {
            return CreateStateEffectFailure("state.invalid-effect-id", checkedScope);
        }

        int index = FindStateEffectIndex(checkedScope, effectId);
        if (index < 0)
        {
            return CreateStateEffectFailure("state.unknown-effect", checkedScope);
        }

        return CreateStateEffectSuccess(checkedScope, Effects[index].Effect, false);
    }

    internal StateEffectOperationResult CancelStateEffect(StateStoreCore store, string? scopeId, string? effectId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (store.RenderActive)
        {
            return CreateStateEffectFailure("state.render-write", checkedScope);
        }

        if (store.BatchActive)
        {
            return CreateStateEffectFailure("state.batch-active", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(effectId))
        {
            return CreateStateEffectFailure("state.invalid-effect-id", checkedScope);
        }

        int index = FindStateEffectIndex(checkedScope, effectId);
        if (index < 0)
        {
            return CreateStateEffectFailure("state.unknown-effect", checkedScope);
        }

        StateScopedEffect scoped = Effects[index];
        if (scoped.Effect.IsCancelled)
        {
            return CreateStateEffectSuccess(checkedScope, scoped.Effect, false);
        }

        if (scoped.Effect.Generation == int.MaxValue)
        {
            return CreateStateEffectFailure("state.effect-generation-exhausted", checkedScope);
        }

        int previousGeneration = scoped.Effect.Generation;
        RemovePendingStateEffectExecutions(checkedScope, effectId);
        if (scoped.HasActiveCleanup && scoped.Effect.CleanupHandlerId != null)
        {
            PendingInvocations.Add(CreateStateEffectInvocation(store, scoped, CleanupInvocation, scoped.Effect.CleanupHandlerId, previousGeneration));
        }

        scoped.Effect.Generation++;
        scoped.Effect.IsCancelled = true;
        scoped.Dirty = false;
        scoped.Running = false;
        scoped.HasActiveCleanup = false;
        RefreshStateEffectViews(store, checkedScope);
        return CreateStateEffectSuccess(checkedScope, scoped.Effect, true);
    }

    internal StateEffectOperationResult CompleteStateEffectExecution(StateStoreCore store, string? scopeId, string? effectId, int generation, bool success, string? errorCode)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (string.IsNullOrWhiteSpace(effectId))
        {
            return CreateStateEffectFailure("state.invalid-effect-id", checkedScope);
        }

        int index = FindStateEffectIndex(checkedScope, effectId);
        if (index < 0)
        {
            return CreateStateEffectFailure("state.unknown-effect", checkedScope);
        }

        StateScopedEffect scoped = Effects[index];
        if (generation != scoped.Effect.Generation || scoped.Effect.IsCancelled)
        {
            StateEffectOperationResult stale = CreateStateEffectSuccess(checkedScope, scoped.Effect, false);
            stale.Code = "state.stale-effect-generation";
            return stale;
        }

        scoped.Running = false;
        if (!success)
        {
            RouteStateEffectError(checkedScope, effectId, scoped.Effect.HandlerId, generation, errorCode);
            RefreshStateEffectViews(store, checkedScope);
            return CreateStateEffectSuccess(checkedScope, scoped.Effect, true);
        }

        scoped.HasActiveCleanup = scoped.Effect.CleanupHandlerId != null;
        RefreshStateEffectViews(store, checkedScope);
        return CreateStateEffectSuccess(checkedScope, scoped.Effect, true);
    }

    internal StateEffectOperationResult CompleteStateEffectCleanup(StateStoreCore store, string? scopeId, string? effectId, int generation, bool success, string? errorCode)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (string.IsNullOrWhiteSpace(effectId))
        {
            return CreateStateEffectFailure("state.invalid-effect-id", checkedScope);
        }

        int index = FindStateEffectIndex(checkedScope, effectId);
        if (index < 0)
        {
            return CreateStateEffectFailure("state.unknown-effect", checkedScope);
        }

        StateScopedEffect scoped = Effects[index];
        if (!success)
        {
            string handlerId = scoped.Effect.CleanupHandlerId ?? string.Empty;
            RouteStateEffectError(checkedScope, effectId, handlerId, generation, errorCode);
        }

        return CreateStateEffectSuccess(checkedScope, scoped.Effect, true);
    }

    internal void ScheduleStateEffects(StateStoreCore store, List<StateScopedChange> changes)
    {
        int changeIndex = 0;
        while (changeIndex < changes.Count)
        {
            StateScopedChange change = changes[changeIndex];
            MarkStateEffectDependency(change.ScopeId, change.Change.StateId);
            changeIndex++;
        }

        QueueReadyStateEffects(store);
    }

    internal void MarkStateEffectDependency(string scopeId, string dependencyId)
    {
        int index = 0;
        while (index < Effects.Count)
        {
            StateScopedEffect scoped = Effects[index];
            if (!scoped.Effect.IsCancelled && ValueComparer.TextEquals(scoped.ScopeId, scopeId) && ContainsStateEffectText(scoped.Effect.DependencyIds, dependencyId))
            {
                scoped.Dirty = true;
            }

            index++;
        }
    }

    internal void QueueReadyStateEffects(StateStoreCore store)
    {
        int index = 0;
        while (index < Effects.Count)
        {
            StateScopedEffect scoped = Effects[index];
            if (scoped.Dirty && !scoped.Effect.IsCancelled && AreStateEffectDependenciesReady(store, scoped))
            {
                QueueStateEffectExecution(store, scoped);
            }

            index++;
        }

        RefreshAllStateEffectViews(store);
    }

    internal void QueueStateEffectExecution(StateStoreCore store, StateScopedEffect scoped)
    {
        if (scoped.Effect.Generation == int.MaxValue)
        {
            RouteStateEffectError(scoped.ScopeId, scoped.Effect.Id, scoped.Effect.HandlerId, scoped.Effect.Generation, "state.effect-generation-exhausted");
            scoped.Dirty = false;
            return;
        }

        int previousGeneration = scoped.Effect.Generation;
        RemovePendingStateEffectExecutions(scoped.ScopeId, scoped.Effect.Id);
        if (scoped.HasActiveCleanup && scoped.Effect.CleanupHandlerId != null)
        {
            PendingInvocations.Add(CreateStateEffectInvocation(store, scoped, CleanupInvocation, scoped.Effect.CleanupHandlerId, previousGeneration));
            scoped.HasActiveCleanup = false;
        }

        scoped.Effect.Generation++;
        scoped.Dirty = false;
        scoped.Running = true;
        PendingInvocations.Add(CreateStateEffectInvocation(store, scoped, ExecuteInvocation, scoped.Effect.HandlerId, scoped.Effect.Generation));
    }

    internal bool AreStateEffectDependenciesReady(StateStoreCore store, StateScopedEffect scoped)
    {
        int index = 0;
        while (index < scoped.Effect.DependencyIds.Length)
        {
            int derivedIndex = store.DerivedCore.FindStateDerivedIndex(scoped.ScopeId, scoped.Effect.DependencyIds[index]);
            if (derivedIndex >= 0 && store.DerivedCore.DerivedStates[derivedIndex].Derived.IsDirty)
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal StateEffectInvocation CreateStateEffectInvocation(StateStoreCore store, StateScopedEffect scoped, string kind, string handlerId, int generation)
    {
        StateEffectInvocation result = new StateEffectInvocation();
        result.ScopeId = scoped.ScopeId;
        result.EffectId = scoped.Effect.Id;
        result.HandlerId = handlerId;
        result.Kind = kind;
        result.Generation = generation;
        result.Dependencies = CreateStateEffectDependencyValues(store, scoped);
        return result;
    }

    internal UiValueEntry[] CreateStateEffectDependencyValues(StateStoreCore store, StateScopedEffect scoped)
    {
        UiValueEntry[] result = new UiValueEntry[scoped.Effect.DependencyIds.Length];
        int index = 0;
        while (index < scoped.Effect.DependencyIds.Length)
        {
            string dependencyId = scoped.Effect.DependencyIds[index];
            UiValue value = new UiValue { Kind = UiValueKind.Null };
            int stateIndex = store.FindStateEntryIndex(scoped.ScopeId, dependencyId);
            if (stateIndex >= 0)
            {
                value = store.CopyStateValue(store.Entries[stateIndex].Entry.Value);
            }
            else
            {
                int derivedIndex = store.DerivedCore.FindStateDerivedIndex(scoped.ScopeId, dependencyId);
                if (derivedIndex >= 0)
                {
                    value = store.CopyStateValue(store.DerivedCore.DerivedStates[derivedIndex].Derived.Value);
                }
            }

            result[index] = new UiValueEntry { Key = dependencyId, Value = value };
            index++;
        }

        return result;
    }

    internal void RemovePendingStateEffectExecutions(string scopeId, string effectId)
    {
        int index = PendingInvocations.Count - 1;
        while (index >= 0)
        {
            StateEffectInvocation invocation = PendingInvocations[index];
            if (invocation.Kind == ExecuteInvocation && ValueComparer.TextEquals(invocation.ScopeId, scopeId) && ValueComparer.TextEquals(invocation.EffectId, effectId))
            {
                PendingInvocations.RemoveAt(index);
            }

            index--;
        }
    }

    internal StateEffectInvocation[] DrainStateEffectInvocations(StateStoreCore store)
    {
        StateEffectInvocation[] result = new StateEffectInvocation[PendingInvocations.Count];
        int index = 0;
        while (index < PendingInvocations.Count)
        {
            result[index] = CopyStateEffectInvocation(store, PendingInvocations[index]);
            index++;
        }

        PendingInvocations.Clear();
        return result;
    }

    internal StateEffectError[] DrainStateEffectErrors()
    {
        StateEffectError[] result = new StateEffectError[Errors.Count];
        int index = 0;
        while (index < Errors.Count)
        {
            result[index] = CopyStateEffectError(Errors[index]);
            index++;
        }

        Errors.Clear();
        return result;
    }

    internal void RouteStateEffectError(string scopeId, string effectId, string handlerId, int generation, string? errorCode)
    {
        StateEffectError error = new StateEffectError();
        error.ScopeId = scopeId;
        error.EffectId = effectId;
        error.HandlerId = handlerId;
        error.Generation = generation;
        error.Code = string.IsNullOrWhiteSpace(errorCode) ? "state.effect-handler-failed" : errorCode;
        Errors.Add(error);
    }

    internal string ValidateStateEffect(StateStoreCore store, string scopeId, UiEffect? effect)
    {
        if (effect == null)
        {
            return "state.null-effect";
        }

        if (string.IsNullOrWhiteSpace(effect.Id))
        {
            return "state.invalid-effect-id";
        }

        if (string.IsNullOrWhiteSpace(effect.HandlerId))
        {
            return "state.invalid-effect-handler";
        }

        if (effect.CleanupHandlerId != null && string.IsNullOrWhiteSpace(effect.CleanupHandlerId))
        {
            return "state.invalid-effect-cleanup";
        }

        if (effect.Generation < 0)
        {
            return "state.invalid-effect-generation";
        }

        if (effect.DependencyIds == null)
        {
            return "state.null-effect-dependencies";
        }

        int index = 0;
        while (index < effect.DependencyIds.Length)
        {
            string? dependencyId = effect.DependencyIds[index];
            if (string.IsNullOrWhiteSpace(dependencyId))
            {
                return "state.invalid-effect-dependency";
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (ValueComparer.TextEquals(effect.DependencyIds[priorIndex], dependencyId))
                {
                    return "state.duplicate-effect-dependency";
                }

                priorIndex++;
            }

            if (store.FindStateEntryIndex(scopeId, dependencyId) < 0 && store.DerivedCore.FindStateDerivedIndex(scopeId, dependencyId) < 0)
            {
                return "state.unknown-effect-dependency";
            }

            index++;
        }

        return string.Empty;
    }

    internal int FindStateEffectIndex(string scopeId, string effectId)
    {
        int index = 0;
        while (index < Effects.Count)
        {
            StateScopedEffect candidate = Effects[index];
            int comparison = CompareStateEffectIdentity(candidate.ScopeId, candidate.Effect.Id, scopeId, effectId);
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

    internal int CompareStateEffectIdentity(string leftScopeId, string leftEffectId, string rightScopeId, string rightEffectId)
    {
        int scopeComparison = ValueComparer.CompareText(leftScopeId, rightScopeId);
        if (scopeComparison != 0)
        {
            return scopeComparison;
        }

        return ValueComparer.CompareText(leftEffectId, rightEffectId);
    }

    internal void InsertStateEffect(StateScopedEffect scoped)
    {
        int index = 0;
        while (index < Effects.Count && CompareStateEffectIdentity(Effects[index].ScopeId, Effects[index].Effect.Id, scoped.ScopeId, scoped.Effect.Id) < 0)
        {
            index++;
        }

        Effects.Insert(index, scoped);
    }

    internal void RefreshStateEffectViews(StateStoreCore store, string scopeId)
    {
        int entryIndex = 0;
        while (entryIndex < store.Entries.Count)
        {
            StateScopedEntry entry = store.Entries[entryIndex];
            if (ValueComparer.TextEquals(entry.ScopeId, scopeId))
            {
                List<UiEffect> dependents = new List<UiEffect>();
                int effectIndex = 0;
                while (effectIndex < Effects.Count)
                {
                    StateScopedEffect candidate = Effects[effectIndex];
                    if (ValueComparer.TextEquals(candidate.ScopeId, scopeId) && ContainsStateEffectText(candidate.Effect.DependencyIds, entry.Entry.Id))
                    {
                        dependents.Add(CopyStateEffect(candidate.Effect));
                    }

                    effectIndex++;
                }

                entry.Entry.Effects = dependents.ToArray();
            }

            entryIndex++;
        }
    }

    internal void RefreshAllStateEffectViews(StateStoreCore store)
    {
        int index = 0;
        string previousScope = string.Empty;
        bool hasPreviousScope = false;
        while (index < Effects.Count)
        {
            string scopeId = Effects[index].ScopeId;
            if (!hasPreviousScope || !ValueComparer.TextEquals(previousScope, scopeId))
            {
                RefreshStateEffectViews(store, scopeId);
                previousScope = scopeId;
                hasPreviousScope = true;
            }

            index++;
        }
    }

    internal UiEffect CopyStateEffect(UiEffect source)
    {
        UiEffect result = new UiEffect();
        result.Id = source.Id;
        result.DependencyIds = ValueComparer.SortText(source.DependencyIds ?? Array.Empty<string>());
        result.HandlerId = source.HandlerId;
        result.CleanupHandlerId = source.CleanupHandlerId;
        result.Generation = source.Generation;
        result.IsCancelled = source.IsCancelled;
        return result;
    }

    internal StateEffectInvocation CopyStateEffectInvocation(StateStoreCore store, StateEffectInvocation source)
    {
        StateEffectInvocation result = new StateEffectInvocation();
        result.ScopeId = source.ScopeId;
        result.EffectId = source.EffectId;
        result.HandlerId = source.HandlerId;
        result.Kind = source.Kind;
        result.Generation = source.Generation;
        result.Dependencies = new UiValueEntry[source.Dependencies.Length];
        int index = 0;
        while (index < source.Dependencies.Length)
        {
            result.Dependencies[index] = new UiValueEntry
            {
                Key = source.Dependencies[index].Key,
                Value = store.CopyStateValue(source.Dependencies[index].Value)
            };
            index++;
        }

        return result;
    }

    internal StateEffectError CopyStateEffectError(StateEffectError source)
    {
        StateEffectError result = new StateEffectError();
        result.ScopeId = source.ScopeId;
        result.EffectId = source.EffectId;
        result.HandlerId = source.HandlerId;
        result.Generation = source.Generation;
        result.Code = source.Code;
        return result;
    }

    internal StateEffectOperationResult CreateStateEffectSuccess(string scopeId, UiEffect effect, bool applied)
    {
        StateEffectOperationResult result = new StateEffectOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.ScopeId = scopeId;
        result.Effect = CopyStateEffect(effect);
        return result;
    }

    internal StateEffectOperationResult CreateStateEffectFailure(string code, string scopeId)
    {
        return new StateEffectOperationResult { Success = false, Code = code, ScopeId = scopeId };
    }

    internal bool ContainsStateEffectText(string[] values, string value)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (ValueComparer.TextEquals(values[index], value))
            {
                return true;
            }

            index++;
        }

        return false;
    }
}
