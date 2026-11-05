using System;
using System.Collections.Generic;

namespace htmlgui;

internal class StateScopedDerived
{
    internal string ScopeId = string.Empty;
    internal UiDerivedState Derived = new UiDerivedState();
}

internal class StateDerivedOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string ScopeId = string.Empty;
    internal UiDerivedState? Derived;
}

internal class StateDerivedOrderResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiDerivedState[] States = Array.Empty<UiDerivedState>();
}

internal class StateDerivedCore
{
    internal List<StateScopedDerived> DerivedStates = new List<StateScopedDerived>();

    internal StateDerivedOperationResult RegisterStateDerived(StateStoreCore store, string? scopeId, UiDerivedState? derived)
    {
        if (store.RenderActive)
        {
            return CreateStateDerivedFailure("state.render-write", store.NormalizeStateScope(scopeId));
        }

        if (store.BatchActive)
        {
            return CreateStateDerivedFailure("state.batch-active", store.NormalizeStateScope(scopeId));
        }

        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateDerivedFailure("state.invalid-scope", checkedScope);
        }

        string validationCode = ValidateStateDerived(store, derived);
        if (validationCode.Length > 0 || derived == null)
        {
            return CreateStateDerivedFailure(validationCode, checkedScope);
        }

        if (FindStateDerivedIndex(checkedScope, derived.Id) >= 0)
        {
            return CreateStateDerivedFailure("state.duplicate-derived", checkedScope);
        }

        if (store.FindStateEntryIndex(checkedScope, derived.Id) >= 0)
        {
            return CreateStateDerivedFailure("state.derived-id-conflict", checkedScope);
        }

        string dependencyCode = ValidateStateDerivedDependencies(store, checkedScope, derived.Id, derived.DependencyIds);
        if (dependencyCode.Length > 0)
        {
            return CreateStateDerivedFailure(dependencyCode, checkedScope);
        }

        StateScopedDerived scoped = new StateScopedDerived();
        scoped.ScopeId = checkedScope;
        scoped.Derived = CopyStateDerived(store, derived);
        InsertStateDerived(scoped);
        RefreshStateDerivedViews(store, checkedScope);
        return CreateStateDerivedSuccess(store, checkedScope, scoped.Derived, true);
    }

    internal StateDerivedOperationResult ReadStateDerived(StateStoreCore store, string? scopeId, string? derivedId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (string.IsNullOrWhiteSpace(derivedId))
        {
            return CreateStateDerivedFailure("state.invalid-derived-id", checkedScope);
        }

        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateDerivedFailure("state.invalid-scope", checkedScope);
        }

        int index = FindStateDerivedIndex(checkedScope, derivedId);
        if (index < 0)
        {
            return CreateStateDerivedFailure("state.unknown-derived", checkedScope);
        }

        return CreateStateDerivedSuccess(store, checkedScope, DerivedStates[index].Derived, false);
    }

    internal StateDerivedOperationResult UpdateStateDerivedDependencies(StateStoreCore store, string? scopeId, string? derivedId, string[]? dependencyIds)
    {
        if (store.RenderActive)
        {
            return CreateStateDerivedFailure("state.render-write", store.NormalizeStateScope(scopeId));
        }

        if (store.BatchActive)
        {
            return CreateStateDerivedFailure("state.batch-active", store.NormalizeStateScope(scopeId));
        }

        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateDerivedFailure("state.invalid-scope", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(derivedId))
        {
            return CreateStateDerivedFailure("state.invalid-derived-id", checkedScope);
        }

        int index = FindStateDerivedIndex(checkedScope, derivedId);
        if (index < 0)
        {
            return CreateStateDerivedFailure("state.unknown-derived", checkedScope);
        }

        string dependencyCode = ValidateStateDerivedDependencies(store, checkedScope, derivedId, dependencyIds);
        if (dependencyCode.Length > 0 || dependencyIds == null)
        {
            return CreateStateDerivedFailure(dependencyCode, checkedScope);
        }

        if (WouldCreateStateDerivedCycle(checkedScope, derivedId, dependencyIds))
        {
            return CreateStateDerivedFailure("state.derived-cycle", checkedScope);
        }

        UiDerivedState derived = DerivedStates[index].Derived;
        derived.DependencyIds = ValueComparer.SortText(dependencyIds);
        derived.IsDirty = true;
        MarkStateDerivedDependents(store, checkedScope, derivedId);
        RefreshStateDerivedViews(store, checkedScope);
        return CreateStateDerivedSuccess(store, checkedScope, derived, true);
    }

    internal StateDerivedOrderResult GetDirtyStateDerivedOrder(StateStoreCore store, string? scopeId)
    {
        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return new StateDerivedOrderResult { Success = false, Code = "state.invalid-scope" };
        }

        bool[] processed = new bool[DerivedStates.Count];
        List<UiDerivedState> ordered = new List<UiDerivedState>();
        int dirtyCount = CountDirtyStateDerived(checkedScope);
        while (ordered.Count < dirtyCount)
        {
            int candidateIndex = FindNextDirtyStateDerived(checkedScope, processed);
            if (candidateIndex < 0)
            {
                return new StateDerivedOrderResult { Success = false, Code = "state.derived-cycle" };
            }

            processed[candidateIndex] = true;
            ordered.Add(CopyStateDerived(store, DerivedStates[candidateIndex].Derived));
        }

        return new StateDerivedOrderResult { Success = true, States = ordered.ToArray() };
    }

    internal StateDerivedOperationResult AcceptStateDerivedValue(StateStoreCore store, string? scopeId, string? derivedId, UiValue? value)
    {
        if (store.BatchActive)
        {
            return CreateStateDerivedFailure("state.batch-active", store.NormalizeStateScope(scopeId));
        }

        string checkedScope = store.NormalizeStateScope(scopeId);
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreateStateDerivedFailure("state.invalid-scope", checkedScope);
        }

        if (string.IsNullOrWhiteSpace(derivedId))
        {
            return CreateStateDerivedFailure("state.invalid-derived-id", checkedScope);
        }

        int index = FindStateDerivedIndex(checkedScope, derivedId);
        if (index < 0)
        {
            return CreateStateDerivedFailure("state.unknown-derived", checkedScope);
        }

        string valueCode = store.ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null)
        {
            return CreateStateDerivedFailure(valueCode, checkedScope);
        }

        UiDerivedState derived = DerivedStates[index].Derived;
        if (!derived.IsDirty)
        {
            return CreateStateDerivedSuccess(store, checkedScope, derived, false);
        }

        if (store.StateValuesEqual(derived.Value, value))
        {
            derived.IsDirty = false;
            RefreshStateDerivedViews(store, checkedScope);
            return CreateStateDerivedSuccess(store, checkedScope, derived, false);
        }

        if (derived.Version == int.MaxValue)
        {
            return CreateStateDerivedFailure("state.derived-version-exhausted", checkedScope);
        }

        derived.Value = store.CopyStateValue(value);
        derived.Version++;
        derived.IsDirty = false;
        MarkStateDerivedDependents(store, checkedScope, derivedId);
        RefreshStateDerivedViews(store, checkedScope);
        return CreateStateDerivedSuccess(store, checkedScope, derived, true);
    }

    internal string ValidateStateDerived(StateStoreCore store, UiDerivedState? derived)
    {
        if (derived == null)
        {
            return "state.null-derived";
        }

        if (string.IsNullOrWhiteSpace(derived.Id))
        {
            return "state.invalid-derived-id";
        }

        if (string.IsNullOrWhiteSpace(derived.HandlerId))
        {
            return "state.invalid-derived-handler";
        }

        if (derived.Version < 0)
        {
            return "state.invalid-derived-version";
        }

        string valueCode = store.ValidateStateValue(derived.Value);
        if (valueCode.Length > 0)
        {
            return valueCode;
        }

        return string.Empty;
    }

    internal string ValidateStateDerivedDependencies(StateStoreCore store, string scopeId, string derivedId, string[]? dependencyIds)
    {
        if (dependencyIds == null)
        {
            return "state.null-derived-dependencies";
        }

        int index = 0;
        while (index < dependencyIds.Length)
        {
            string? dependencyId = dependencyIds[index];
            if (string.IsNullOrWhiteSpace(dependencyId))
            {
                return "state.invalid-derived-dependency";
            }

            if (ValueComparer.TextEquals(dependencyId, derivedId))
            {
                return "state.derived-cycle";
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (ValueComparer.TextEquals(dependencyIds[priorIndex], dependencyId))
                {
                    return "state.duplicate-derived-dependency";
                }

                priorIndex++;
            }

            if (store.FindStateEntryIndex(scopeId, dependencyId) < 0 && FindStateDerivedIndex(scopeId, dependencyId) < 0)
            {
                return "state.unknown-derived-dependency";
            }

            index++;
        }

        return string.Empty;
    }

    internal bool WouldCreateStateDerivedCycle(string scopeId, string derivedId, string[] dependencyIds)
    {
        int index = 0;
        while (index < dependencyIds.Length)
        {
            if (HasStateDerivedDependencyPath(scopeId, dependencyIds[index], derivedId))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool HasStateDerivedDependencyPath(string scopeId, string startId, string targetId)
    {
        List<string> pending = new List<string>();
        List<string> visited = new List<string>();
        pending.Add(startId);
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            string currentId = pending[pendingIndex];
            pendingIndex++;
            if (ValueComparer.TextEquals(currentId, targetId))
            {
                return true;
            }

            if (ContainsVisitedStateDerivedText(visited, currentId))
            {
                continue;
            }

            visited.Add(currentId);
            int derivedIndex = FindStateDerivedIndex(scopeId, currentId);
            if (derivedIndex >= 0)
            {
                string[] dependencies = DerivedStates[derivedIndex].Derived.DependencyIds;
                int dependencyIndex = 0;
                while (dependencyIndex < dependencies.Length)
                {
                    pending.Add(dependencies[dependencyIndex]);
                    dependencyIndex++;
                }
            }
        }

        return false;
    }

    internal void MarkStateDerivedDependents(StateStoreCore store, string scopeId, string dependencyId)
    {
        List<string> pending = new List<string>();
        List<string> visited = new List<string>();
        pending.Add(dependencyId);
        visited.Add(dependencyId);
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            string currentId = pending[pendingIndex];
            pendingIndex++;
            int derivedIndex = 0;
            while (derivedIndex < DerivedStates.Count)
            {
                StateScopedDerived candidate = DerivedStates[derivedIndex];
                if (ValueComparer.TextEquals(candidate.ScopeId, scopeId) && ContainsStateDerivedText(candidate.Derived.DependencyIds, currentId))
                {
                    candidate.Derived.IsDirty = true;
                    if (!ContainsVisitedStateDerivedText(visited, candidate.Derived.Id))
                    {
                        visited.Add(candidate.Derived.Id);
                        pending.Add(candidate.Derived.Id);
                    }
                }

                derivedIndex++;
            }
        }

        RefreshStateDerivedViews(store, scopeId);
    }

    internal int CountDirtyStateDerived(string scopeId)
    {
        int count = 0;
        int index = 0;
        while (index < DerivedStates.Count)
        {
            if (ValueComparer.TextEquals(DerivedStates[index].ScopeId, scopeId) && DerivedStates[index].Derived.IsDirty)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal int FindNextDirtyStateDerived(string scopeId, bool[] processed)
    {
        int index = 0;
        while (index < DerivedStates.Count)
        {
            StateScopedDerived candidate = DerivedStates[index];
            if (ValueComparer.TextEquals(candidate.ScopeId, scopeId) && candidate.Derived.IsDirty && !processed[index] && AreDirtyStateDerivedDependenciesProcessed(scopeId, candidate.Derived, processed))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal bool AreDirtyStateDerivedDependenciesProcessed(string scopeId, UiDerivedState derived, bool[] processed)
    {
        int dependencyIndex = 0;
        while (dependencyIndex < derived.DependencyIds.Length)
        {
            int index = FindStateDerivedIndex(scopeId, derived.DependencyIds[dependencyIndex]);
            if (index >= 0 && DerivedStates[index].Derived.IsDirty && !processed[index])
            {
                return false;
            }

            dependencyIndex++;
        }

        return true;
    }

    internal int FindStateDerivedIndex(string scopeId, string derivedId)
    {
        int index = 0;
        while (index < DerivedStates.Count)
        {
            StateScopedDerived candidate = DerivedStates[index];
            int comparison = CompareStateDerivedIdentity(candidate.ScopeId, candidate.Derived.Id, scopeId, derivedId);
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

    internal int CompareStateDerivedIdentity(string leftScopeId, string leftDerivedId, string rightScopeId, string rightDerivedId)
    {
        int scopeComparison = ValueComparer.CompareText(leftScopeId, rightScopeId);
        if (scopeComparison != 0)
        {
            return scopeComparison;
        }

        return ValueComparer.CompareText(leftDerivedId, rightDerivedId);
    }

    internal void InsertStateDerived(StateScopedDerived scoped)
    {
        int index = 0;
        while (index < DerivedStates.Count && CompareStateDerivedIdentity(DerivedStates[index].ScopeId, DerivedStates[index].Derived.Id, scoped.ScopeId, scoped.Derived.Id) < 0)
        {
            index++;
        }

        DerivedStates.Insert(index, scoped);
    }

    internal void RefreshStateDerivedViews(StateStoreCore store, string scopeId)
    {
        int entryIndex = 0;
        while (entryIndex < store.Entries.Count)
        {
            StateScopedEntry entry = store.Entries[entryIndex];
            if (ValueComparer.TextEquals(entry.ScopeId, scopeId))
            {
                List<UiDerivedState> dependents = new List<UiDerivedState>();
                int derivedIndex = 0;
                while (derivedIndex < DerivedStates.Count)
                {
                    StateScopedDerived candidate = DerivedStates[derivedIndex];
                    if (ValueComparer.TextEquals(candidate.ScopeId, scopeId) && ContainsStateDerivedText(candidate.Derived.DependencyIds, entry.Entry.Id))
                    {
                        dependents.Add(CopyStateDerived(store, candidate.Derived));
                    }

                    derivedIndex++;
                }

                entry.Entry.Derived = dependents.ToArray();
            }

            entryIndex++;
        }
    }

    internal UiDerivedState CopyStateDerived(StateStoreCore store, UiDerivedState source)
    {
        UiDerivedState result = new UiDerivedState();
        result.Id = source.Id;
        result.DependencyIds = ValueComparer.SortText(source.DependencyIds ?? Array.Empty<string>());
        result.HandlerId = source.HandlerId;
        result.Value = store.CopyStateValue(source.Value);
        result.Version = source.Version;
        result.IsDirty = source.IsDirty;
        return result;
    }

    internal StateDerivedOperationResult CreateStateDerivedSuccess(StateStoreCore store, string scopeId, UiDerivedState derived, bool applied)
    {
        StateDerivedOperationResult result = new StateDerivedOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.ScopeId = scopeId;
        result.Derived = CopyStateDerived(store, derived);
        return result;
    }

    internal StateDerivedOperationResult CreateStateDerivedFailure(string code, string scopeId)
    {
        return new StateDerivedOperationResult { Success = false, Code = code, ScopeId = scopeId };
    }

    internal bool ContainsStateDerivedText(string[] values, string value)
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

    internal bool ContainsVisitedStateDerivedText(List<string> values, string value)
    {
        int index = 0;
        while (index < values.Count)
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
