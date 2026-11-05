using System;
using System.Collections.Generic;

namespace htmlgui;

/// <summary>Coordinates binding lifecycle, observable state, normalized DOM events, and target updates.</summary>
public class UiBindingEngine : IUiBindingEngine
{
    internal BindingCore Core;
    internal UiStateStore StateStore;
    internal int EventBatchGeneration;

    /// <summary>Initializes a binding engine with an empty coordinated state store.</summary>
    public UiBindingEngine()
    {
        Core = new BindingCore();
        StateStore = new UiStateStore();
    }

    /// <summary>Gets the state store owned by this binding engine.</summary>
    public IUiStateStore GetBindingStateStore()
    {
        return StateStore;
    }

    /// <summary>Registers one binding value converter.</summary>
    public UiResult RegisterBindingConverter(string? converterId, IUiValueConverter? converter)
    {
        BindingOperationResult registered = Core.RegisterBindingConverter(converterId, converter);
        UiResult result = new UiResult();
        result.Success = registered.Success;
        result.Diagnostics = Core.CopyBindingDiagnosticArray(registered.Diagnostics);
        return result;
    }

    /// <summary>Attaches one binding and evaluates its initial target value.</summary>
    public UiBindingResult AttachBinding(string? scopeId, UiBinding? binding)
    {
        BindingOperationResult attached = Core.AttachBinding(StateStore.Core, scopeId, binding);
        UiBindingResult result = CreatePublicBindingOperationResult(attached);
        if (!attached.Success || attached.Binding == null)
        {
            return result;
        }

        BindingTargetResult evaluated = Core.EvaluateBindingTarget(StateStore.Core, scopeId, attached.Binding.Id);
        AddPublicBindingTargetResult(result, evaluated);
        result.Applied = true;
        return result;
    }

    /// <summary>Detaches one binding and discards its runtime evaluation state.</summary>
    public UiBindingResult DetachBinding(string? scopeId, string? bindingId)
    {
        return CreatePublicBindingOperationResult(Core.DetachBinding(StateStore.Core, scopeId, bindingId));
    }

    /// <summary>Evaluates one binding from state to target.</summary>
    public UiBindingResult EvaluateBinding(string? scopeId, string? bindingId)
    {
        BindingTargetResult evaluated = Core.EvaluateBindingTarget(StateStore.Core, scopeId, bindingId);
        UiBindingResult result = new UiBindingResult { Success = evaluated.Success, Applied = evaluated.Applied };
        AddPublicBindingTargetResult(result, evaluated);
        return result;
    }

    /// <summary>Evaluates every target-directed binding in one scope.</summary>
    public UiBindingResult EvaluateStateBindings(string? scopeId)
    {
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreatePublicBindingFailure("binding.invalid-scope");
        }

        return EvaluatePublicStateBindings(StateStore.Core.NormalizeStateScope(scopeId));
    }

    /// <summary>Pushes one triggered target value into state and propagates resulting target updates.</summary>
    public UiBindingResult PushBindingValue(string? scopeId, string? bindingId, UiValue? value, string? origin, UiBindingTrigger trigger)
    {
        BindingOperationResult pushed = Core.UpdateTriggeredBindingSource(StateStore.Core, scopeId, bindingId, value, origin, trigger);
        UiBindingResult result = CreatePublicBindingOperationResult(pushed);
        if (!pushed.Success || !pushed.Applied)
        {
            return result;
        }

        if (pushed.Binding != null)
        {
            result.BindingIds = new string[] { pushed.Binding.Id };
        }

        UiResult observed = StateStore.DispatchStateObservers();
        AddPublicBindingDiagnostics(result, observed.Diagnostics);
        if (!observed.Success)
        {
            result.Success = false;
        }

        UiBindingResult targets = EvaluatePublicStateBindings(StateStore.Core.NormalizeStateScope(scopeId));
        MergePublicBindingResults(result, targets);
        return result;
    }

    /// <summary>Processes one normalized DOM input, change, blur, or focus-out event atomically.</summary>
    public UiBindingResult ProcessBindingEvent(string? scopeId, UiEventData? eventData, string? origin)
    {
        if (scopeId != null && scopeId.Length > 0 && scopeId.Trim().Length == 0)
        {
            return CreatePublicBindingFailure("binding.invalid-scope");
        }

        if (eventData == null)
        {
            return CreatePublicBindingFailure("binding.null-event");
        }

        if (string.IsNullOrWhiteSpace(eventData.TargetId))
        {
            return CreatePublicBindingFailure("binding.invalid-event-target");
        }

        int triggerValue = ConvertBindingEventTrigger(eventData.Kind);
        if (triggerValue < 0)
        {
            return CreatePublicBindingFailure("binding.unsupported-event");
        }

        if (eventData.Input == null)
        {
            return CreatePublicBindingFailure("binding.missing-event-value");
        }

        if (eventData.Input.IsComposing)
        {
            return new UiBindingResult { Success = true };
        }

        string checkedScope = StateStore.Core.NormalizeStateScope(scopeId);
        UiBindingTrigger trigger = (UiBindingTrigger)triggerValue;
        List<string> bindingIds = FindEventBindingIds(checkedScope, eventData.TargetId, trigger);
        if (bindingIds.Count == 0)
        {
            return new UiBindingResult { Success = true };
        }

        if (StateStore.Core.BatchActive)
        {
            return CreatePublicBindingFailure("binding.state-batch-active");
        }

        if (EventBatchGeneration == int.MaxValue)
        {
            return CreatePublicBindingFailure("binding.event-generation-exhausted");
        }

        EventBatchGeneration++;
        StateBatchResult begun = StateStore.Core.BeginStateBatch("binding-event-" + InvariantFormatter.FormatInt32(EventBatchGeneration));
        if (!begun.Success)
        {
            return CreatePublicBindingFailure(begun.Code);
        }

        UiBindingResult result = new UiBindingResult { Success = true };
        List<string> appliedBindingIds = new List<string>();
        int index = 0;
        while (index < bindingIds.Count)
        {
            BindingOperationResult pushed = Core.UpdateTriggeredBindingSource(StateStore.Core, checkedScope, bindingIds[index], eventData.Input.Value, origin, trigger);
            AddPublicBindingDiagnostics(result, pushed.Diagnostics);
            if (!pushed.Success)
            {
                StateStore.Core.RollbackStateBatch();
                result.Success = false;
                result.Binding = pushed.Binding == null ? null : Core.CopyBinding(StateStore.Core, pushed.Binding);
                result.BindingIds = appliedBindingIds.ToArray();
                return result;
            }

            if (pushed.Applied)
            {
                appliedBindingIds.Add(bindingIds[index]);
            }

            index++;
        }

        StateBatchResult committed = StateStore.Core.CommitStateBatch();
        if (!committed.Success)
        {
            AddPublicBindingDiagnostic(result, Core.CreateBindingDiagnostic(committed.Code));
            result.Success = false;
            result.BindingIds = appliedBindingIds.ToArray();
            return result;
        }

        result.Applied = committed.Applied;
        result.BindingIds = appliedBindingIds.ToArray();
        UiResult observed = StateStore.DispatchStateObservers();
        AddPublicBindingDiagnostics(result, observed.Diagnostics);
        if (!observed.Success)
        {
            result.Success = false;
        }

        UiBindingResult targets = EvaluatePublicStateBindings(checkedScope);
        MergePublicBindingResults(result, targets);
        return result;
    }

    internal UiBindingResult EvaluatePublicStateBindings(string scopeId)
    {
        UiBindingResult result = new UiBindingResult { Success = true };
        List<string> bindingIds = new List<string>();
        List<UiBindingUpdate> updates = new List<UiBindingUpdate>();
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        int index = 0;
        while (index < Core.Bindings.Count)
        {
            BindingRegistration registration = Core.Bindings[index];
            if (ValueComparer.TextEquals(registration.ScopeId, scopeId))
            {
                BindingTargetResult target = Core.EvaluateBindingTarget(StateStore.Core, scopeId, registration.Binding.Id);
                if (!target.Success)
                {
                    result.Success = false;
                }

                if (target.Applied && target.Update != null)
                {
                    bindingIds.Add(registration.Binding.Id);
                    UiBindingUpdate? update = Core.CopyBindingUpdate(StateStore.Core, target.Update);
                    if (update != null)
                    {
                        updates.Add(update);
                    }
                }

                AddPublicBindingDiagnosticsToList(diagnostics, target.Diagnostics);
            }

            index++;
        }

        result.Applied = updates.Count > 0;
        result.BindingIds = bindingIds.ToArray();
        result.Updates = updates.ToArray();
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal List<string> FindEventBindingIds(string scopeId, string targetNodeId, UiBindingTrigger trigger)
    {
        List<string> result = new List<string>();
        int index = 0;
        while (index < Core.Bindings.Count)
        {
            BindingRegistration registration = Core.Bindings[index];
            UiBinding binding = registration.Binding;
            if (ValueComparer.TextEquals(registration.ScopeId, scopeId)
                && ValueComparer.TextEquals(binding.TargetNodeId, targetNodeId)
                && binding.Trigger == trigger
                && (binding.Mode == UiBindingMode.TwoWay || binding.Mode == UiBindingMode.OneWayToSource))
            {
                result.Add(binding.Id);
            }

            index++;
        }

        return result;
    }

    internal int ConvertBindingEventTrigger(UiEventKind kind)
    {
        switch (kind)
        {
            case UiEventKind.Input:
                return (int)UiBindingTrigger.Input;
            case UiEventKind.Change:
                return (int)UiBindingTrigger.Change;
            case UiEventKind.Blur:
            case UiEventKind.FocusOut:
                return (int)UiBindingTrigger.Blur;
            default:
                return -1;
        }
    }

    internal UiBindingResult CreatePublicBindingOperationResult(BindingOperationResult operation)
    {
        UiBindingResult result = new UiBindingResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        if (operation.Binding != null)
        {
            result.Binding = Core.CopyBinding(StateStore.Core, operation.Binding);
        }

        result.Diagnostics = Core.CopyBindingDiagnosticArray(operation.Diagnostics);
        return result;
    }

    internal UiBindingResult CreatePublicBindingFailure(string code)
    {
        UiBindingResult result = new UiBindingResult();
        result.Diagnostics = new UiDiagnostic[] { Core.CreateBindingDiagnostic(code) };
        return result;
    }

    internal void AddPublicBindingTargetResult(UiBindingResult result, BindingTargetResult target)
    {
        if (!target.Success)
        {
            result.Success = false;
        }

        if (target.Binding != null)
        {
            result.Binding = Core.CopyBinding(StateStore.Core, target.Binding);
        }

        if (target.Applied && target.Update != null)
        {
            UiBindingUpdate? update = Core.CopyBindingUpdate(StateStore.Core, target.Update);
            if (update != null)
            {
                result.Updates = new UiBindingUpdate[] { update };
                result.BindingIds = new string[] { update.BindingId };
            }
        }

        AddPublicBindingDiagnostics(result, target.Diagnostics);
    }

    internal void MergePublicBindingResults(UiBindingResult target, UiBindingResult source)
    {
        if (!source.Success)
        {
            target.Success = false;
        }

        if (source.Applied)
        {
            target.Applied = true;
        }

        List<string> bindingIds = new List<string>();
        int bindingIndex = 0;
        while (bindingIndex < target.BindingIds.Length)
        {
            bindingIds.Add(target.BindingIds[bindingIndex]);
            bindingIndex++;
        }

        bindingIndex = 0;
        while (bindingIndex < source.BindingIds.Length)
        {
            bindingIds.Add(source.BindingIds[bindingIndex]);
            bindingIndex++;
        }

        target.BindingIds = bindingIds.ToArray();
        List<UiBindingUpdate> updates = new List<UiBindingUpdate>();
        int updateIndex = 0;
        while (updateIndex < target.Updates.Length)
        {
            updates.Add(target.Updates[updateIndex]);
            updateIndex++;
        }

        updateIndex = 0;
        while (updateIndex < source.Updates.Length)
        {
            UiBindingUpdate? update = Core.CopyBindingUpdate(StateStore.Core, source.Updates[updateIndex]);
            if (update != null)
            {
                updates.Add(update);
            }

            updateIndex++;
        }

        target.Updates = updates.ToArray();
        AddPublicBindingDiagnostics(target, source.Diagnostics);
    }

    internal void AddPublicBindingDiagnostics(UiBindingResult result, UiDiagnostic[]? diagnostics)
    {
        List<UiDiagnostic> combined = new List<UiDiagnostic>();
        AddPublicBindingDiagnosticsToList(combined, result.Diagnostics);
        AddPublicBindingDiagnosticsToList(combined, diagnostics);
        result.Diagnostics = combined.ToArray();
    }

    internal void AddPublicBindingDiagnostic(UiBindingResult result, UiDiagnostic diagnostic)
    {
        AddPublicBindingDiagnostics(result, new UiDiagnostic[] { diagnostic });
    }

    internal void AddPublicBindingDiagnosticsToList(List<UiDiagnostic> target, UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] source = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < source.Length)
        {
            target.Add(Core.CopyBindingDiagnostic(source[index]));
            index++;
        }
    }
}
