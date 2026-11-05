using System;
using System.Collections.Generic;

namespace htmlgui;

internal class StateSubscriptionLifetime
{
    internal string Id = string.Empty;
    internal bool Active = true;
}

internal class StateScopedSubscription
{
    internal string StateScopeId = string.Empty;
    internal UiSubscription Subscription = new UiSubscription();
}

internal class StateSubscriptionInvocation
{
    internal string StateScopeId = string.Empty;
    internal string SubscriptionId = string.Empty;
    internal string HandlerId = string.Empty;
    internal UiStateChange Change = new UiStateChange();
    internal UiValue? PreviousValue;
    internal UiValue? CurrentValue;
}

internal class StateSubscriptionOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal UiSubscription? Subscription;
}

internal class StateSubscriptionDispatchResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal StateSubscriptionInvocation[] Invocations = Array.Empty<StateSubscriptionInvocation>();
}

internal class StateSubscriptionCore
{
    internal List<StateSubscriptionLifetime> Lifetimes = new List<StateSubscriptionLifetime>();
    internal List<StateScopedSubscription> Subscriptions = new List<StateScopedSubscription>();
    internal List<StateSubscriptionInvocation> PendingInvocations = new List<StateSubscriptionInvocation>();
    internal List<StateSubscriptionInvocation> DeferredInvocations = new List<StateSubscriptionInvocation>();
    internal bool DispatchActive;

    internal StateSubscriptionOperationResult RegisterStateSubscriptionLifetime(StateStoreCore store, string? lifetimeId)
    {
        if (store.RenderActive)
        {
            return CreateStateSubscriptionFailure("state.render-write");
        }

        if (store.BatchActive)
        {
            return CreateStateSubscriptionFailure("state.batch-active");
        }

        if (string.IsNullOrWhiteSpace(lifetimeId))
        {
            return CreateStateSubscriptionFailure("state.invalid-subscription-scope");
        }

        int index = FindStateSubscriptionLifetimeIndex(lifetimeId);
        if (index >= 0)
        {
            return CreateStateSubscriptionFailure(Lifetimes[index].Active ? "state.duplicate-subscription-scope" : "state.subscription-scope-disposed");
        }

        StateSubscriptionLifetime lifetime = new StateSubscriptionLifetime { Id = lifetimeId };
        InsertStateSubscriptionLifetime(lifetime);
        return new StateSubscriptionOperationResult { Success = true, Applied = true };
    }

    internal StateSubscriptionOperationResult DisposeStateSubscriptionLifetime(StateStoreCore store, string? lifetimeId)
    {
        if (store.RenderActive)
        {
            return CreateStateSubscriptionFailure("state.render-write");
        }

        if (store.BatchActive)
        {
            return CreateStateSubscriptionFailure("state.batch-active");
        }

        if (string.IsNullOrWhiteSpace(lifetimeId))
        {
            return CreateStateSubscriptionFailure("state.invalid-subscription-scope");
        }

        int lifetimeIndex = FindStateSubscriptionLifetimeIndex(lifetimeId);
        if (lifetimeIndex < 0)
        {
            return CreateStateSubscriptionFailure("state.unknown-subscription-scope");
        }

        if (!Lifetimes[lifetimeIndex].Active)
        {
            return new StateSubscriptionOperationResult { Success = true };
        }

        Lifetimes[lifetimeIndex].Active = false;
        int index = Subscriptions.Count - 1;
        while (index >= 0)
        {
            UiSubscription subscription = Subscriptions[index].Subscription;
            if (ValueComparer.TextEquals(subscription.ScopeId, lifetimeId))
            {
                RemoveStateSubscriptionInvocations(Subscriptions[index].StateScopeId, subscription.Id);
                Subscriptions.RemoveAt(index);
            }

            index--;
        }

        RefreshAllStateSubscriptionViews(store);
        return new StateSubscriptionOperationResult { Success = true, Applied = true };
    }

    internal StateSubscriptionOperationResult RegisterStateSubscription(StateStoreCore store, string? stateScopeId, UiSubscription? subscription)
    {
        string checkedScope = store.NormalizeStateScope(stateScopeId);
        if (store.RenderActive)
        {
            return CreateStateSubscriptionFailure("state.render-write");
        }

        if (store.BatchActive)
        {
            return CreateStateSubscriptionFailure("state.batch-active");
        }

        if (stateScopeId != null && stateScopeId.Length > 0 && stateScopeId.Trim().Length == 0)
        {
            return CreateStateSubscriptionFailure("state.invalid-scope");
        }

        string validationCode = ValidateStateSubscription(store, checkedScope, subscription);
        if (validationCode.Length > 0 || subscription == null)
        {
            return CreateStateSubscriptionFailure(validationCode);
        }

        if (FindStateSubscriptionIndex(checkedScope, subscription.Id) >= 0)
        {
            return CreateStateSubscriptionFailure("state.duplicate-subscription");
        }

        StateScopedSubscription scoped = new StateScopedSubscription();
        scoped.StateScopeId = checkedScope;
        scoped.Subscription = CopyStateSubscription(subscription);
        InsertStateSubscription(scoped);
        RefreshStateSubscriptionViews(store, checkedScope);
        return CreateStateSubscriptionSuccess(scoped.Subscription, true);
    }

    internal StateSubscriptionOperationResult UnsubscribeStateSubscription(StateStoreCore store, string? stateScopeId, string? subscriptionId)
    {
        string checkedScope = store.NormalizeStateScope(stateScopeId);
        if (store.RenderActive)
        {
            return CreateStateSubscriptionFailure("state.render-write");
        }

        if (store.BatchActive)
        {
            return CreateStateSubscriptionFailure("state.batch-active");
        }

        if (string.IsNullOrWhiteSpace(subscriptionId))
        {
            return CreateStateSubscriptionFailure("state.invalid-subscription-id");
        }

        int index = FindStateSubscriptionIndex(checkedScope, subscriptionId);
        if (index < 0)
        {
            return CreateStateSubscriptionFailure("state.unknown-subscription");
        }

        UiSubscription removed = CopyStateSubscription(Subscriptions[index].Subscription);
        removed.IsActive = false;
        Subscriptions.RemoveAt(index);
        RemoveStateSubscriptionInvocations(checkedScope, subscriptionId);
        RefreshStateSubscriptionViews(store, checkedScope);
        return CreateStateSubscriptionSuccess(removed, true);
    }

    internal void ScheduleStateSubscriptions(StateStoreCore store, List<StateScopedChange> changes)
    {
        int subscriptionIndex = 0;
        while (subscriptionIndex < Subscriptions.Count)
        {
            StateScopedSubscription scoped = Subscriptions[subscriptionIndex];
            if (scoped.Subscription.IsActive)
            {
                int changeIndex = 0;
                while (changeIndex < changes.Count)
                {
                    StateScopedChange change = changes[changeIndex];
                    if (ValueComparer.TextEquals(scoped.StateScopeId, change.ScopeId) && ValueComparer.TextEquals(scoped.Subscription.StateId, change.Change.StateId))
                    {
                        StateSubscriptionInvocation invocation = CreateStateSubscriptionInvocation(store, scoped, change.Change);
                        if (DispatchActive)
                        {
                            DeferredInvocations.Add(invocation);
                        }
                        else
                        {
                            PendingInvocations.Add(invocation);
                        }

                        break;
                    }

                    changeIndex++;
                }
            }

            subscriptionIndex++;
        }
    }

    internal StateSubscriptionDispatchResult BeginStateSubscriptionDispatch(StateStoreCore store)
    {
        if (DispatchActive)
        {
            return new StateSubscriptionDispatchResult { Success = false, Code = "state.subscription-dispatch-active" };
        }

        DispatchActive = true;
        StateSubscriptionDispatchResult result = new StateSubscriptionDispatchResult();
        result.Success = true;
        result.Invocations = CopyStateSubscriptionInvocationArray(store, PendingInvocations);
        PendingInvocations.Clear();
        return result;
    }

    internal StateSubscriptionDispatchResult EndStateSubscriptionDispatch(StateStoreCore store)
    {
        if (!DispatchActive)
        {
            return new StateSubscriptionDispatchResult { Success = false, Code = "state.subscription-dispatch-not-active" };
        }

        DispatchActive = false;
        int index = 0;
        while (index < DeferredInvocations.Count)
        {
            PendingInvocations.Add(DeferredInvocations[index]);
            index++;
        }

        DeferredInvocations.Clear();
        return new StateSubscriptionDispatchResult { Success = true };
    }

    internal string ValidateStateSubscription(StateStoreCore store, string stateScopeId, UiSubscription? subscription)
    {
        if (subscription == null)
        {
            return "state.null-subscription";
        }

        if (string.IsNullOrWhiteSpace(subscription.Id))
        {
            return "state.invalid-subscription-id";
        }

        if (string.IsNullOrWhiteSpace(subscription.StateId))
        {
            return "state.invalid-subscription-state";
        }

        if (string.IsNullOrWhiteSpace(subscription.HandlerId))
        {
            return "state.invalid-subscription-handler";
        }

        if (store.FindStateEntryIndex(stateScopeId, subscription.StateId) < 0)
        {
            return "state.unknown-subscription-state";
        }

        if (subscription.Path != null)
        {
            StatePathResult path = new StatePatchCore().ParseStatePath(subscription.Path);
            if (!path.Success)
            {
                return path.Code;
            }
        }

        if (subscription.ScopeId != null)
        {
            if (string.IsNullOrWhiteSpace(subscription.ScopeId))
            {
                return "state.invalid-subscription-scope";
            }

            int lifetimeIndex = FindStateSubscriptionLifetimeIndex(subscription.ScopeId);
            if (lifetimeIndex < 0)
            {
                return "state.unknown-subscription-scope";
            }

            if (!Lifetimes[lifetimeIndex].Active)
            {
                return "state.subscription-scope-disposed";
            }
        }

        return string.Empty;
    }

    internal StateSubscriptionInvocation CreateStateSubscriptionInvocation(StateStoreCore store, StateScopedSubscription scoped, UiStateChange change)
    {
        StateSubscriptionInvocation result = new StateSubscriptionInvocation();
        result.StateScopeId = scoped.StateScopeId;
        result.SubscriptionId = scoped.Subscription.Id;
        result.HandlerId = scoped.Subscription.HandlerId;
        result.Change = store.CopyStateChange(change);
        string path = scoped.Subscription.Path ?? string.Empty;
        result.PreviousValue = ResolveStateSubscriptionValue(store, change.PreviousValue, path);
        result.CurrentValue = ResolveStateSubscriptionValue(store, change.CurrentValue, path);
        return result;
    }

    internal UiValue? ResolveStateSubscriptionValue(StateStoreCore store, UiValue? value, string path)
    {
        if (value == null)
        {
            return null;
        }

        StatePathResult parsed = new StatePatchCore().ParseStatePath(path);
        if (!parsed.Success)
        {
            return null;
        }

        StatePathValueResult resolved = new StatePatchCore().ResolveStatePathValue(value, parsed.Tokens);
        if (!resolved.Success || resolved.Value == null)
        {
            return null;
        }

        return store.CopyStateValue(resolved.Value);
    }

    internal void RemoveStateSubscriptionInvocations(string stateScopeId, string subscriptionId)
    {
        int pendingIndex = PendingInvocations.Count - 1;
        while (pendingIndex >= 0)
        {
            StateSubscriptionInvocation invocation = PendingInvocations[pendingIndex];
            if (ValueComparer.TextEquals(invocation.StateScopeId, stateScopeId) && ValueComparer.TextEquals(invocation.SubscriptionId, subscriptionId))
            {
                PendingInvocations.RemoveAt(pendingIndex);
            }

            pendingIndex--;
        }

        int deferredIndex = DeferredInvocations.Count - 1;
        while (deferredIndex >= 0)
        {
            StateSubscriptionInvocation invocation = DeferredInvocations[deferredIndex];
            if (ValueComparer.TextEquals(invocation.StateScopeId, stateScopeId) && ValueComparer.TextEquals(invocation.SubscriptionId, subscriptionId))
            {
                DeferredInvocations.RemoveAt(deferredIndex);
            }

            deferredIndex--;
        }
    }

    internal int FindStateSubscriptionLifetimeIndex(string lifetimeId)
    {
        int index = 0;
        while (index < Lifetimes.Count)
        {
            int comparison = ValueComparer.CompareText(Lifetimes[index].Id, lifetimeId);
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

    internal void InsertStateSubscriptionLifetime(StateSubscriptionLifetime lifetime)
    {
        int index = 0;
        while (index < Lifetimes.Count && ValueComparer.CompareText(Lifetimes[index].Id, lifetime.Id) < 0)
        {
            index++;
        }

        Lifetimes.Insert(index, lifetime);
    }

    internal int FindStateSubscriptionIndex(string stateScopeId, string subscriptionId)
    {
        int index = 0;
        while (index < Subscriptions.Count)
        {
            StateScopedSubscription candidate = Subscriptions[index];
            int comparison = CompareStateSubscriptionIdentity(candidate.StateScopeId, candidate.Subscription.Id, stateScopeId, subscriptionId);
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

    internal int CompareStateSubscriptionIdentity(string leftScopeId, string leftSubscriptionId, string rightScopeId, string rightSubscriptionId)
    {
        int scopeComparison = ValueComparer.CompareText(leftScopeId, rightScopeId);
        if (scopeComparison != 0)
        {
            return scopeComparison;
        }

        return ValueComparer.CompareText(leftSubscriptionId, rightSubscriptionId);
    }

    internal void InsertStateSubscription(StateScopedSubscription scoped)
    {
        int index = 0;
        while (index < Subscriptions.Count && CompareStateSubscriptionIdentity(Subscriptions[index].StateScopeId, Subscriptions[index].Subscription.Id, scoped.StateScopeId, scoped.Subscription.Id) < 0)
        {
            index++;
        }

        Subscriptions.Insert(index, scoped);
    }

    internal void RefreshStateSubscriptionViews(StateStoreCore store, string stateScopeId)
    {
        int entryIndex = 0;
        while (entryIndex < store.Entries.Count)
        {
            StateScopedEntry entry = store.Entries[entryIndex];
            if (ValueComparer.TextEquals(entry.ScopeId, stateScopeId))
            {
                List<UiSubscription> subscriptions = new List<UiSubscription>();
                int subscriptionIndex = 0;
                while (subscriptionIndex < Subscriptions.Count)
                {
                    StateScopedSubscription candidate = Subscriptions[subscriptionIndex];
                    if (ValueComparer.TextEquals(candidate.StateScopeId, stateScopeId) && ValueComparer.TextEquals(candidate.Subscription.StateId, entry.Entry.Id))
                    {
                        subscriptions.Add(CopyStateSubscription(candidate.Subscription));
                    }

                    subscriptionIndex++;
                }

                entry.Entry.Subscriptions = subscriptions.ToArray();
            }

            entryIndex++;
        }
    }

    internal void RefreshAllStateSubscriptionViews(StateStoreCore store)
    {
        int index = 0;
        while (index < store.Entries.Count)
        {
            RefreshStateSubscriptionViews(store, store.Entries[index].ScopeId);
            index++;
        }
    }

    internal UiSubscription CopyStateSubscription(UiSubscription source)
    {
        UiSubscription result = new UiSubscription();
        result.Id = source.Id;
        result.StateId = source.StateId;
        result.Path = source.Path;
        result.HandlerId = source.HandlerId;
        result.ScopeId = source.ScopeId;
        result.IsActive = source.IsActive;
        return result;
    }

    internal StateSubscriptionInvocation CopyStateSubscriptionInvocation(StateStoreCore store, StateSubscriptionInvocation source)
    {
        StateSubscriptionInvocation result = new StateSubscriptionInvocation();
        result.StateScopeId = source.StateScopeId;
        result.SubscriptionId = source.SubscriptionId;
        result.HandlerId = source.HandlerId;
        result.Change = store.CopyStateChange(source.Change);
        if (source.PreviousValue != null)
        {
            result.PreviousValue = store.CopyStateValue(source.PreviousValue);
        }

        if (source.CurrentValue != null)
        {
            result.CurrentValue = store.CopyStateValue(source.CurrentValue);
        }

        return result;
    }

    internal StateSubscriptionInvocation[] CopyStateSubscriptionInvocationArray(StateStoreCore store, List<StateSubscriptionInvocation> source)
    {
        StateSubscriptionInvocation[] result = new StateSubscriptionInvocation[source.Count];
        int index = 0;
        while (index < source.Count)
        {
            result[index] = CopyStateSubscriptionInvocation(store, source[index]);
            index++;
        }

        return result;
    }

    internal StateSubscriptionOperationResult CreateStateSubscriptionSuccess(UiSubscription subscription, bool applied)
    {
        StateSubscriptionOperationResult result = new StateSubscriptionOperationResult();
        result.Success = true;
        result.Applied = applied;
        result.Subscription = CopyStateSubscription(subscription);
        return result;
    }

    internal StateSubscriptionOperationResult CreateStateSubscriptionFailure(string code)
    {
        return new StateSubscriptionOperationResult { Success = false, Code = code };
    }
}
