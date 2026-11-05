using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ComponentInstance
{
    internal string RootSeed = string.Empty;
    internal string? ParentComponentId;
    internal UiComponentContext Context = new UiComponentContext();
    internal bool IsAttached;
    internal bool IsActive;
    internal bool IsDisposed;
    internal int RenderGeneration;
    internal int HydrationGeneration;
    internal UiNode? RenderedNode;
    internal UiErrorBoundaryState? ErrorBoundary;
}

internal class ComponentRenderInvocation
{
    internal string HandlerId = string.Empty;
    internal UiComponentRenderRequest Request = new UiComponentRenderRequest();
}

internal class ComponentLifecycleInvocation
{
    internal string HandlerId = string.Empty;
    internal UiLifecycleNotification Notification = new UiLifecycleNotification();
}

internal class ComponentErrorInvocation
{
    internal string HandlerId = string.Empty;
    internal string FailedComponentId = string.Empty;
    internal UiLifecycleStage Stage;
    internal UiErrorBoundaryState Boundary = new UiErrorBoundaryState();
}

internal class ComponentOperationResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string ProviderComponentId = string.Empty;
    internal UiComponentDefinition? Definition;
    internal UiComponentContext? Context;
    internal UiComponentRenderRequest? RenderRequest;
    internal UiErrorBoundaryState? ErrorBoundary;
    internal UiDiagnostic? Diagnostic;
    internal UiNode? Node;
    internal UiValue? Value;
}

internal class ComponentCore
{
    internal List<UiComponentDefinition> Definitions = new List<UiComponentDefinition>();
    internal List<ComponentInstance> Instances = new List<ComponentInstance>();
    internal List<string> DisposedComponentIds = new List<string>();
    internal List<ComponentRenderInvocation> RenderInvocations = new List<ComponentRenderInvocation>();
    internal List<ComponentLifecycleInvocation> LifecycleInvocations = new List<ComponentLifecycleInvocation>();
    internal List<ComponentErrorInvocation> ErrorInvocations = new List<ComponentErrorInvocation>();
    internal StateStoreCore StateStore = new StateStoreCore();

    internal ComponentOperationResult RegisterComponentDefinition(UiComponentDefinition? definition)
    {
        string code = ValidateComponentDefinition(definition);
        if (code.Length > 0 || definition == null)
        {
            return CreateComponentFailure(code);
        }

        if (FindComponentDefinitionIndex(definition.TypeId) >= 0)
        {
            return CreateComponentFailure("component.duplicate-definition");
        }

        UiComponentDefinition copy = CopyComponentDefinition(definition);
        InsertComponentDefinition(copy);
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Definition = CopyComponentDefinition(copy);
        return result;
    }

    internal ComponentOperationResult ReadComponentDefinition(string? typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            return CreateComponentFailure("component.invalid-type");
        }

        int index = FindComponentDefinitionIndex(typeId);
        if (index < 0)
        {
            return CreateComponentFailure("component.unknown-definition");
        }

        ComponentOperationResult result = CreateComponentSuccess(false);
        result.Definition = CopyComponentDefinition(Definitions[index]);
        return result;
    }

    internal ComponentOperationResult UnregisterComponentDefinition(string? typeId)
    {
        if (string.IsNullOrWhiteSpace(typeId))
        {
            return CreateComponentFailure("component.invalid-type");
        }

        int definitionIndex = FindComponentDefinitionIndex(typeId);
        if (definitionIndex < 0)
        {
            return CreateComponentFailure("component.unknown-definition");
        }

        int instanceIndex = 0;
        while (instanceIndex < Instances.Count)
        {
            if (ValueComparer.TextEquals(Instances[instanceIndex].Context.TypeId, typeId))
            {
                return CreateComponentFailure("component.definition-in-use");
            }

            instanceIndex++;
        }

        UiComponentDefinition removed = Definitions[definitionIndex];
        Definitions.RemoveAt(definitionIndex);
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Definition = CopyComponentDefinition(removed);
        return result;
    }

    internal ComponentOperationResult CreateComponentInstance(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues)
    {
        if (string.IsNullOrWhiteSpace(rootSeed))
        {
            return CreateComponentFailure("component.invalid-root-seed");
        }

        if (string.IsNullOrWhiteSpace(typeId))
        {
            return CreateComponentFailure("component.invalid-type");
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            return CreateComponentFailure("component.invalid-key");
        }

        int definitionIndex = FindComponentDefinitionIndex(typeId);
        if (definitionIndex < 0)
        {
            return CreateComponentFailure("component.unknown-definition");
        }

        ComponentInstance? parent = null;
        if (parentComponentId != null)
        {
            if (string.IsNullOrWhiteSpace(parentComponentId))
            {
                return CreateComponentFailure("component.invalid-parent");
            }

            int parentIndex = FindComponentInstanceIndex(parentComponentId);
            if (parentIndex < 0)
            {
                if (FindDisposedComponentIdIndex(parentComponentId) >= 0)
                {
                    return CreateComponentFailure("component.parent-disposed");
                }

                return CreateComponentFailure("component.unknown-parent");
            }

            parent = Instances[parentIndex];
            if (parent.IsDisposed)
            {
                return CreateComponentFailure("component.parent-disposed");
            }

            if (!ValueComparer.TextEquals(parent.RootSeed, rootSeed))
            {
                return CreateComponentFailure("component.root-mismatch");
            }
        }

        string parameterCode = ValidateComponentValues(parameters, "component.null-parameters", "component.invalid-parameter", "component.duplicate-parameter");
        if (parameterCode.Length > 0)
        {
            return CreateComponentFailure(parameterCode);
        }

        string slotCode = ValidateComponentSlots(slots);
        if (slotCode.Length > 0)
        {
            return CreateComponentFailure(slotCode);
        }

        string contextCode = ValidateComponentValues(contextValues, "component.null-context", "component.invalid-context", "component.duplicate-context");
        if (contextCode.Length > 0)
        {
            return CreateComponentFailure(contextCode);
        }

        string identity = typeId + ":" + key;
        string componentId = parent == null ? StableId.CreateRoot(rootSeed, "component", identity) : StableId.CreateChild(rootSeed, parent.Context.ComponentId, "component", identity);
        if (FindComponentInstanceIndex(componentId) >= 0)
        {
            return CreateComponentFailure("component.duplicate-instance");
        }

        int disposedIndex = FindDisposedComponentIdIndex(componentId);
        if (disposedIndex >= 0)
        {
            DisposedComponentIds.RemoveAt(disposedIndex);
        }

        ComponentInstance instance = new ComponentInstance();
        instance.RootSeed = rootSeed;
        instance.ParentComponentId = parentComponentId;
        instance.Context.ComponentId = componentId;
        instance.Context.TypeId = typeId;
        instance.Context.Key = key;
        instance.Context.Parameters = MergeComponentValues(Definitions[definitionIndex].DefaultParameters, parameters ?? Array.Empty<UiValueEntry>());
        instance.Context.StateScopeId = StableId.CreateChild(rootSeed, componentId, "state-scope", "local");
        instance.Context.Slots = CopyComponentSlots(slots ?? Array.Empty<UiSlot>());
        instance.Context.ContextValues = CopyComponentValues(contextValues ?? Array.Empty<UiValueEntry>());
        if (Definitions[definitionIndex].ErrorHandlerId != null)
        {
            instance.ErrorBoundary = new UiErrorBoundaryState { BoundaryId = componentId };
        }

        StateSubscriptionOperationResult lifetime = StateStore.RegisterStateSubscriptionLifetime(instance.Context.StateScopeId);
        if (!lifetime.Success)
        {
            return CreateComponentFailure("component.state-lifetime");
        }

        InsertComponentInstance(instance);
        QueueComponentLifecycle(instance, UiLifecycleStage.Create, 0);
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult ReadComponentInstance(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentOperationResult result = CreateComponentSuccess(false);
        result.Context = CopyComponentContext(Instances[index].Context);
        UiNode? renderedNode = Instances[index].RenderedNode;
        if (renderedNode != null)
        {
            result.Node = CopyComponentNode(renderedNode);
        }

        return result;
    }

    internal ComponentOperationResult UpdateComponentParameters(string? componentId, UiValueEntry[]? parameters)
    {
        int instanceIndex = FindCheckedComponentInstanceIndex(componentId);
        if (instanceIndex < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        string code = ValidateComponentValues(parameters, "component.null-parameters", "component.invalid-parameter", "component.duplicate-parameter");
        if (code.Length > 0 || parameters == null)
        {
            return CreateComponentFailure(code);
        }

        ComponentInstance instance = Instances[instanceIndex];
        if (instance.IsDisposed)
        {
            return CreateComponentFailure("component.disposed");
        }

        int definitionIndex = FindComponentDefinitionIndex(instance.Context.TypeId);
        if (definitionIndex < 0)
        {
            return CreateComponentFailure("component.unknown-definition");
        }

        UiValueEntry[] merged = MergeComponentValues(Definitions[definitionIndex].DefaultParameters, parameters);
        bool changed = !ComponentValuesEqual(instance.Context.Parameters, merged);
        if (changed)
        {
            instance.Context.Parameters = merged;
            QueueComponentLifecycle(instance, UiLifecycleStage.ParametersChanged, instance.RenderGeneration);
        }

        ComponentOperationResult result = CreateComponentSuccess(changed);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult AttachComponentTree(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (ContainsDisposedComponent(instance.Context.ComponentId))
        {
            return CreateComponentFailure("component.disposed");
        }

        if (instance.ParentComponentId != null)
        {
            int parentIndex = FindComponentInstanceIndex(instance.ParentComponentId);
            if (parentIndex < 0)
            {
                return CreateComponentFailure("component.unknown-parent");
            }

            if (!Instances[parentIndex].IsAttached)
            {
                return CreateComponentFailure("component.parent-detached");
            }
        }

        bool applied = AttachComponentBranch(instance.Context.ComponentId);
        ComponentOperationResult result = CreateComponentSuccess(applied);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult ActivateComponentTree(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (ContainsDisposedComponent(instance.Context.ComponentId))
        {
            return CreateComponentFailure("component.disposed");
        }

        if (ContainsDetachedComponent(instance.Context.ComponentId))
        {
            return CreateComponentFailure("component.detached");
        }

        bool applied = ActivateComponentBranch(instance.Context.ComponentId);
        ComponentOperationResult result = CreateComponentSuccess(applied);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult DeactivateComponentTree(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return CreateComponentFailure("component.disposed");
        }

        bool applied = DeactivateComponentBranch(instance.Context.ComponentId);
        ComponentOperationResult result = CreateComponentSuccess(applied);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult DetachComponentTree(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return CreateComponentFailure("component.disposed");
        }

        bool deactivated = DeactivateComponentBranch(instance.Context.ComponentId);
        bool detached = DetachComponentBranch(instance.Context.ComponentId);
        ComponentOperationResult result = CreateComponentSuccess(deactivated || detached);
        result.Context = CopyComponentContext(instance.Context);
        return result;
    }

    internal ComponentOperationResult DisposeComponentTree(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            if (!string.IsNullOrWhiteSpace(componentId) && FindDisposedComponentIdIndex(componentId) >= 0)
            {
                return CreateComponentSuccess(false);
            }

            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        UiComponentContext context = CopyComponentContext(instance.Context);
        bool deactivated = DeactivateComponentBranch(instance.Context.ComponentId);
        bool detached = DetachComponentBranch(instance.Context.ComponentId);
        bool disposed = DisposeComponentBranch(instance.Context.ComponentId);
        CleanupComponentBranch(instance.Context.ComponentId);
        ComponentOperationResult result = CreateComponentSuccess(deactivated || detached || disposed);
        result.Context = context;
        return result;
    }

    internal ComponentLifecycleInvocation[] DrainComponentLifecycleInvocations()
    {
        ComponentLifecycleInvocation[] result = new ComponentLifecycleInvocation[LifecycleInvocations.Count];
        int index = 0;
        while (index < LifecycleInvocations.Count)
        {
            result[index] = CopyComponentLifecycleInvocation(LifecycleInvocations[index]);
            index++;
        }

        LifecycleInvocations.Clear();
        return result;
    }

    internal ComponentOperationResult ReconcileComponentInstance(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues, bool isPresent, bool forceRemount)
    {
        ComponentOperationResult created = CreateComponentInstance(rootSeed, parentComponentId, typeId, key, parameters, slots, contextValues);
        if (created.Success)
        {
            if (isPresent && created.Context != null)
            {
                AttachComponentTree(created.Context.ComponentId);
                ActivateComponentTree(created.Context.ComponentId);
            }

            return created;
        }

        if (!ValueComparer.TextEquals(created.Code, "component.duplicate-instance") || string.IsNullOrWhiteSpace(rootSeed) || string.IsNullOrWhiteSpace(typeId) || string.IsNullOrWhiteSpace(key))
        {
            return created;
        }

        string identity = typeId + ":" + key;
        string componentId = parentComponentId == null ? StableId.CreateRoot(rootSeed, "component", identity) : StableId.CreateChild(rootSeed, parentComponentId, "component", identity);
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure("component.unknown");
        }

        if (forceRemount)
        {
            ComponentOperationResult disposed = DisposeComponentTree(componentId);
            if (!disposed.Success)
            {
                return disposed;
            }

            ComponentOperationResult remounted = CreateComponentInstance(rootSeed, parentComponentId, typeId, key, parameters, slots, contextValues);
            if (remounted.Success && isPresent && remounted.Context != null)
            {
                AttachComponentTree(remounted.Context.ComponentId);
                ActivateComponentTree(remounted.Context.ComponentId);
            }

            return remounted;
        }

        ComponentInstance instance = Instances[index];
        ComponentOperationResult updated = UpdateComponentParameters(componentId, parameters);
        if (!updated.Success || slots == null || contextValues == null)
        {
            return updated;
        }

        instance.Context.Slots = CopyComponentSlots(slots);
        instance.Context.ContextValues = CopyComponentValues(contextValues);
        if (isPresent)
        {
            if (!instance.IsAttached)
            {
                AttachComponentTree(componentId);
            }

            ActivateComponentTree(componentId);
        }
        else
        {
            DeactivateComponentTree(componentId);
        }

        ComponentOperationResult result = CreateComponentSuccess(updated.Applied);
        result.Context = CopyComponentContext(instance.Context);
        result.Node = instance.RenderedNode == null ? null : CopyComponentNode(instance.RenderedNode);
        return result;
    }

    internal ComponentOperationResult SetComponentPresence(string? componentId, bool isPresent)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (isPresent)
        {
            bool applied = false;
            if (!instance.IsAttached)
            {
                ComponentOperationResult attached = AttachComponentTree(componentId);
                if (!attached.Success)
                {
                    return attached;
                }

                applied = attached.Applied;
            }

            ComponentOperationResult activated = ActivateComponentTree(componentId);
            if (!activated.Success)
            {
                return activated;
            }

            ComponentOperationResult result = CreateComponentSuccess(applied || activated.Applied);
            result.Context = CopyComponentContext(instance.Context);
            return result;
        }

        return DeactivateComponentTree(componentId);
    }

    internal StateEffectOperationResult RegisterComponentEffect(string? componentId, UiEffect? effect)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return new StateEffectOperationResult { Code = GetComponentLookupCode(componentId) };
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return new StateEffectOperationResult { Code = "component.disposed" };
        }

        return StateStore.RegisterStateEffect(instance.Context.StateScopeId, effect);
    }

    internal StateSubscriptionOperationResult RegisterComponentSubscription(string? componentId, UiSubscription? subscription)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return new StateSubscriptionOperationResult { Code = GetComponentLookupCode(componentId) };
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return new StateSubscriptionOperationResult { Code = "component.disposed" };
        }

        if (subscription == null)
        {
            return StateStore.RegisterStateSubscription(instance.Context.StateScopeId, null);
        }

        UiSubscription copy = StateStore.SubscriptionCore.CopyStateSubscription(subscription);
        copy.ScopeId = instance.Context.StateScopeId;
        return StateStore.RegisterStateSubscription(instance.Context.StateScopeId, copy);
    }

    internal ComponentOperationResult ConfigureComponentErrorBoundary(string? componentId, UiNode? fallback, string? resetKey)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (resetKey != null && string.IsNullOrWhiteSpace(resetKey))
        {
            return CreateComponentFailure("component.invalid-reset-key");
        }

        ComponentInstance instance = Instances[index];
        if (instance.ErrorBoundary == null)
        {
            return CreateComponentFailure("component.not-error-boundary");
        }

        instance.ErrorBoundary.Fallback = fallback == null ? null : CopyComponentNode(fallback);
        instance.ErrorBoundary.ResetKey = resetKey;
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.ErrorBoundary = CopyComponentErrorBoundary(instance.ErrorBoundary);
        return result;
    }

    internal ComponentOperationResult ReadComponentErrorBoundary(string? componentId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        ComponentInstance instance = Instances[index];
        if (instance.ErrorBoundary == null)
        {
            return CreateComponentFailure("component.not-error-boundary");
        }

        ComponentOperationResult result = CreateComponentSuccess(false);
        result.ErrorBoundary = CopyComponentErrorBoundary(instance.ErrorBoundary);
        return result;
    }

    internal ComponentOperationResult ResetComponentErrorBoundary(string? componentId, string? resetKey)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (resetKey != null && string.IsNullOrWhiteSpace(resetKey))
        {
            return CreateComponentFailure("component.invalid-reset-key");
        }

        ComponentInstance instance = Instances[index];
        if (instance.ErrorBoundary == null)
        {
            return CreateComponentFailure("component.not-error-boundary");
        }

        string? currentKey = instance.ErrorBoundary.ResetKey;
        bool sameKey = currentKey == null && resetKey == null;
        if (currentKey != null && resetKey != null && ValueComparer.TextEquals(currentKey, resetKey))
        {
            sameKey = true;
        }

        if (sameKey)
        {
            ComponentOperationResult unchanged = CreateComponentSuccess(false);
            unchanged.ErrorBoundary = CopyComponentErrorBoundary(instance.ErrorBoundary);
            return unchanged;
        }

        instance.ErrorBoundary.ResetKey = resetKey;
        instance.ErrorBoundary.HasError = false;
        instance.ErrorBoundary.Diagnostic = null;
        instance.RenderedNode = null;
        if (instance.IsActive)
        {
            ActivateComponentBranch(instance.Context.ComponentId);
        }

        ComponentOperationResult result = CreateComponentSuccess(true);
        result.ErrorBoundary = CopyComponentErrorBoundary(instance.ErrorBoundary);
        return result;
    }

    internal ComponentOperationResult ReportComponentError(string? componentId, UiLifecycleStage stage, UiDiagnostic? diagnostic)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        string diagnosticCode = ValidateComponentDiagnostic(diagnostic);
        if (diagnosticCode.Length > 0 || diagnostic == null)
        {
            return CreateComponentFailure(diagnosticCode);
        }

        if (!IsComponentLifecycleStage(stage))
        {
            return CreateComponentFailure("component.invalid-lifecycle-stage");
        }

        ComponentInstance failed = Instances[index];
        UiDiagnostic copiedDiagnostic = CopyComponentDiagnostic(diagnostic);
        QueueComponentErrorLifecycle(failed, copiedDiagnostic);
        ComponentInstance? boundary = FindComponentErrorBoundary(failed.ParentComponentId);
        if (boundary == null || boundary.ErrorBoundary == null)
        {
            throw new UiComponentException(copiedDiagnostic.Message, failed.Context.ComponentId, stage);
        }

        DeactivateComponentBranch(failed.Context.ComponentId);
        boundary.ErrorBoundary.HasError = true;
        boundary.ErrorBoundary.Diagnostic = CopyComponentDiagnostic(copiedDiagnostic);
        if (boundary.ErrorBoundary.Fallback != null)
        {
            boundary.RenderedNode = CopyComponentNode(boundary.ErrorBoundary.Fallback);
        }

        QueueComponentErrorInvocation(boundary, failed.Context.ComponentId, stage);
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Context = CopyComponentContext(boundary.Context);
        result.ErrorBoundary = CopyComponentErrorBoundary(boundary.ErrorBoundary);
        result.Diagnostic = CopyComponentDiagnostic(copiedDiagnostic);
        if (boundary.RenderedNode != null)
        {
            result.Node = CopyComponentNode(boundary.RenderedNode);
        }

        return result;
    }

    internal ComponentErrorInvocation[] DrainComponentErrorInvocations()
    {
        ComponentErrorInvocation[] result = new ComponentErrorInvocation[ErrorInvocations.Count];
        int index = 0;
        while (index < ErrorInvocations.Count)
        {
            result[index] = CopyComponentErrorInvocation(ErrorInvocations[index]);
            index++;
        }

        ErrorInvocations.Clear();
        return result;
    }

    internal ComponentOperationResult ProvideComponentContextValue(string? componentId, string? contextKey, UiValue? value)
    {
        int instanceIndex = FindCheckedComponentInstanceIndex(componentId);
        if (instanceIndex < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (string.IsNullOrWhiteSpace(contextKey))
        {
            return CreateComponentFailure("component.invalid-context-key");
        }

        string valueCode = StateStore.ValidateStateValue(value);
        if (valueCode.Length > 0 || value == null)
        {
            return CreateComponentFailure(valueCode);
        }

        ComponentInstance instance = Instances[instanceIndex];
        int valueIndex = FindComponentValueIndex(instance.Context.ContextValues, contextKey);
        bool changed = valueIndex < 0 || !StateStore.StateValuesEqual(instance.Context.ContextValues[valueIndex].Value, value);
        if (changed)
        {
            instance.Context.ContextValues = SetComponentValue(instance.Context.ContextValues, contextKey, value);
        }

        ComponentOperationResult result = CreateComponentSuccess(changed);
        result.Context = CopyComponentContext(instance.Context);
        result.Value = StateStore.CopyStateValue(value);
        return result;
    }

    internal ComponentOperationResult ConsumeComponentContextValue(string? componentId, string? contextKey)
    {
        int instanceIndex = FindCheckedComponentInstanceIndex(componentId);
        if (instanceIndex < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (string.IsNullOrWhiteSpace(contextKey))
        {
            return CreateComponentFailure("component.invalid-context-key");
        }

        string? parentId = Instances[instanceIndex].ParentComponentId;
        while (parentId != null)
        {
            int parentIndex = FindComponentInstanceIndex(parentId);
            if (parentIndex < 0)
            {
                return CreateComponentFailure("component.unknown-parent");
            }

            ComponentInstance parent = Instances[parentIndex];
            int valueIndex = FindComponentValueIndex(parent.Context.ContextValues, contextKey);
            if (valueIndex >= 0)
            {
                ComponentOperationResult result = CreateComponentSuccess(false);
                result.ProviderComponentId = parent.Context.ComponentId;
                result.Value = StateStore.CopyStateValue(parent.Context.ContextValues[valueIndex].Value);
                return result;
            }

            parentId = parent.ParentComponentId;
        }

        return CreateComponentFailure("component.unknown-context");
    }

    internal StateOperationResult CreateComponentLocalState(string? componentId, string? stateId, UiValue? value)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return new StateOperationResult { Code = GetComponentLookupCode(componentId) };
        }

        return StateStore.CreateStateEntry(Instances[index].Context.StateScopeId, stateId, value, "component");
    }

    internal StateOperationResult ReadComponentLocalState(string? componentId, string? stateId)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return new StateOperationResult { Code = GetComponentLookupCode(componentId) };
        }

        return StateStore.ReadStateEntry(Instances[index].Context.StateScopeId, stateId);
    }

    internal StateOperationResult WriteComponentLocalState(string? componentId, string? stateId, UiValue? value)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return new StateOperationResult { Code = GetComponentLookupCode(componentId) };
        }

        return StateStore.WriteStateEntry(Instances[index].Context.StateScopeId, stateId, value, "component");
    }

    internal ComponentOperationResult BeginComponentRender(string? componentId, UiUpdatePriority priority, bool isHydration)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (priority != UiUpdatePriority.Immediate && priority != UiUpdatePriority.UserInteraction && priority != UiUpdatePriority.Normal && priority != UiUpdatePriority.Background)
        {
            return CreateComponentFailure("component.invalid-priority");
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return CreateComponentFailure("component.disposed");
        }

        if (instance.RenderGeneration == int.MaxValue)
        {
            return CreateComponentFailure("component.generation-exhausted");
        }

        int definitionIndex = FindComponentDefinitionIndex(instance.Context.TypeId);
        if (definitionIndex < 0)
        {
            return CreateComponentFailure("component.unknown-definition");
        }

        instance.RenderGeneration++;
        UiComponentRenderRequest request = new UiComponentRenderRequest();
        request.Context = CopyComponentContext(instance.Context);
        request.Generation = instance.RenderGeneration;
        request.Priority = priority;
        request.IsHydration = isHydration;
        instance.HydrationGeneration = isHydration ? instance.RenderGeneration : 0;
        if (isHydration)
        {
            QueueComponentLifecycle(instance, UiLifecycleStage.HydrationStart, instance.RenderGeneration);
        }

        QueueComponentLifecycle(instance, UiLifecycleStage.BeforeRender, instance.RenderGeneration);
        ComponentRenderInvocation invocation = new ComponentRenderInvocation();
        invocation.HandlerId = Definitions[definitionIndex].RenderHandlerId;
        invocation.Request = CopyComponentRenderRequest(request);
        RenderInvocations.Add(invocation);
        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Context = CopyComponentContext(instance.Context);
        result.RenderRequest = CopyComponentRenderRequest(request);
        return result;
    }

    internal ComponentRenderInvocation[] DrainComponentRenderInvocations()
    {
        ComponentRenderInvocation[] result = new ComponentRenderInvocation[RenderInvocations.Count];
        int index = 0;
        while (index < RenderInvocations.Count)
        {
            result[index] = CopyComponentRenderInvocation(RenderInvocations[index]);
            index++;
        }

        RenderInvocations.Clear();
        return result;
    }

    internal ComponentOperationResult CompleteComponentRender(string? componentId, int generation, UiNode? node)
    {
        int index = FindCheckedComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return CreateComponentFailure(GetComponentLookupCode(componentId));
        }

        if (node == null)
        {
            return CreateComponentFailure("component.null-render");
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsDisposed)
        {
            return CreateComponentFailure("component.disposed");
        }

        if (generation != instance.RenderGeneration)
        {
            return CreateComponentFailure("component.stale-render");
        }

        instance.RenderedNode = CopyComponentNode(node);
        QueueComponentLifecycle(instance, UiLifecycleStage.AfterRender, generation);
        if (instance.HydrationGeneration == generation)
        {
            QueueComponentLifecycle(instance, UiLifecycleStage.HydrationComplete, generation);
            instance.HydrationGeneration = 0;
        }

        ComponentOperationResult result = CreateComponentSuccess(true);
        result.Context = CopyComponentContext(instance.Context);
        result.Node = CopyComponentNode(instance.RenderedNode);
        return result;
    }

    internal bool ContainsDisposedComponent(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        if (Instances[index].IsDisposed)
        {
            return true;
        }

        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && ContainsDisposedComponent(child.Context.ComponentId))
            {
                return true;
            }

            childIndex++;
        }

        return false;
    }

    internal bool ContainsDetachedComponent(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return true;
        }

        if (!Instances[index].IsAttached)
        {
            return true;
        }

        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && ContainsDetachedComponent(child.Context.ComponentId))
            {
                return true;
            }

            childIndex++;
        }

        return false;
    }

    internal bool AttachComponentBranch(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        ComponentInstance instance = Instances[index];
        bool applied = false;
        if (!instance.IsAttached)
        {
            instance.IsAttached = true;
            QueueComponentLifecycle(instance, UiLifecycleStage.Attach, instance.RenderGeneration);
            applied = true;
        }

        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && AttachComponentBranch(child.Context.ComponentId))
            {
                applied = true;
            }

            childIndex++;
        }

        return applied;
    }

    internal bool ActivateComponentBranch(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        ComponentInstance instance = Instances[index];
        bool applied = false;
        if (!instance.IsActive)
        {
            instance.IsActive = true;
            QueueComponentLifecycle(instance, UiLifecycleStage.Activate, instance.RenderGeneration);
            applied = true;
        }

        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && ActivateComponentBranch(child.Context.ComponentId))
            {
                applied = true;
            }

            childIndex++;
        }

        return applied;
    }

    internal bool DeactivateComponentBranch(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        bool applied = false;
        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && DeactivateComponentBranch(child.Context.ComponentId))
            {
                applied = true;
            }

            childIndex++;
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsActive && !instance.IsDisposed)
        {
            instance.IsActive = false;
            QueueComponentLifecycle(instance, UiLifecycleStage.Deactivate, instance.RenderGeneration);
            applied = true;
        }

        return applied;
    }

    internal bool DetachComponentBranch(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        bool applied = false;
        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && DetachComponentBranch(child.Context.ComponentId))
            {
                applied = true;
            }

            childIndex++;
        }

        ComponentInstance instance = Instances[index];
        if (instance.IsAttached && !instance.IsDisposed)
        {
            instance.IsAttached = false;
            QueueComponentLifecycle(instance, UiLifecycleStage.Detach, instance.RenderGeneration);
            applied = true;
        }

        return applied;
    }

    internal bool DisposeComponentBranch(string componentId)
    {
        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        bool applied = false;
        int childIndex = 0;
        while (childIndex < Instances.Count)
        {
            ComponentInstance child = Instances[childIndex];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId) && DisposeComponentBranch(child.Context.ComponentId))
            {
                applied = true;
            }

            childIndex++;
        }

        ComponentInstance instance = Instances[index];
        if (!instance.IsDisposed)
        {
            instance.IsDisposed = true;
            instance.HydrationGeneration = 0;
            QueueComponentLifecycle(instance, UiLifecycleStage.Dispose, instance.RenderGeneration);
            applied = true;
        }

        return applied;
    }

    internal void CleanupComponentBranch(string componentId)
    {
        string childId = FindFirstComponentChildId(componentId);
        while (childId.Length > 0)
        {
            CleanupComponentBranch(childId);
            childId = FindFirstComponentChildId(componentId);
        }

        int index = FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return;
        }

        ComponentInstance instance = Instances[index];
        CleanupComponentResources(instance);
        Instances.RemoveAt(index);
        InsertDisposedComponentId(componentId);
    }

    internal string FindFirstComponentChildId(string componentId)
    {
        int index = 0;
        while (index < Instances.Count)
        {
            ComponentInstance child = Instances[index];
            if (child.ParentComponentId != null && ValueComparer.TextEquals(child.ParentComponentId, componentId))
            {
                return child.Context.ComponentId;
            }

            index++;
        }

        return string.Empty;
    }

    internal void CleanupComponentResources(ComponentInstance instance)
    {
        string scopeId = instance.Context.StateScopeId;
        StateStore.DisposeStateSubscriptionLifetime(scopeId);

        int subscriptionIndex = StateStore.SubscriptionCore.Subscriptions.Count - 1;
        while (subscriptionIndex >= 0)
        {
            StateScopedSubscription subscription = StateStore.SubscriptionCore.Subscriptions[subscriptionIndex];
            string? lifetimeId = subscription.Subscription.ScopeId;
            if (ValueComparer.TextEquals(subscription.StateScopeId, scopeId) || lifetimeId != null && ValueComparer.TextEquals(lifetimeId, scopeId))
            {
                StateStore.SubscriptionCore.RemoveStateSubscriptionInvocations(subscription.StateScopeId, subscription.Subscription.Id);
                StateStore.SubscriptionCore.Subscriptions.RemoveAt(subscriptionIndex);
            }

            subscriptionIndex--;
        }

        int lifetimeIndex = StateStore.SubscriptionCore.FindStateSubscriptionLifetimeIndex(scopeId);
        if (lifetimeIndex >= 0)
        {
            StateStore.SubscriptionCore.Lifetimes.RemoveAt(lifetimeIndex);
        }

        int effectIndex = StateStore.EffectCore.Effects.Count - 1;
        while (effectIndex >= 0)
        {
            StateScopedEffect effect = StateStore.EffectCore.Effects[effectIndex];
            if (ValueComparer.TextEquals(effect.ScopeId, scopeId))
            {
                StateStore.CancelStateEffect(scopeId, effect.Effect.Id);
                int removalIndex = StateStore.EffectCore.FindStateEffectIndex(scopeId, effect.Effect.Id);
                if (removalIndex >= 0)
                {
                    StateStore.EffectCore.Effects.RemoveAt(removalIndex);
                }
            }

            effectIndex--;
        }

        int derivedIndex = StateStore.DerivedCore.DerivedStates.Count - 1;
        while (derivedIndex >= 0)
        {
            if (ValueComparer.TextEquals(StateStore.DerivedCore.DerivedStates[derivedIndex].ScopeId, scopeId))
            {
                StateStore.DerivedCore.DerivedStates.RemoveAt(derivedIndex);
            }

            derivedIndex--;
        }

        int entryIndex = StateStore.Entries.Count - 1;
        while (entryIndex >= 0)
        {
            if (ValueComparer.TextEquals(StateStore.Entries[entryIndex].ScopeId, scopeId))
            {
                StateStore.Entries.RemoveAt(entryIndex);
            }

            entryIndex--;
        }

        int versionIndex = StateStore.Versions.Count - 1;
        while (versionIndex >= 0)
        {
            if (ValueComparer.TextEquals(StateStore.Versions[versionIndex].ScopeId, scopeId))
            {
                StateStore.Versions.RemoveAt(versionIndex);
            }

            versionIndex--;
        }

        RemoveComponentStateChanges(scopeId, StateStore.CommittedChanges);
        RemoveComponentStateChanges(scopeId, StateStore.PendingChanges);
        RemoveComponentSnapshotState(scopeId);
        RemoveComponentRenderInvocations(instance.Context.ComponentId);
        RemoveComponentErrorInvocations(instance.Context.ComponentId);
        instance.Context.ContextValues = Array.Empty<UiValueEntry>();
        instance.Context.Slots = Array.Empty<UiSlot>();
        instance.RenderedNode = null;
    }

    internal void RemoveComponentStateChanges(string scopeId, List<StateScopedChange> changes)
    {
        int index = changes.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(changes[index].ScopeId, scopeId))
            {
                changes.RemoveAt(index);
            }

            index--;
        }
    }

    internal void RemoveComponentSnapshotState(string scopeId)
    {
        int snapshotIndex = 0;
        while (snapshotIndex < StateStore.Snapshots.Count)
        {
            List<StateScopedEntry> entries = StateStore.Snapshots[snapshotIndex].Entries;
            int entryIndex = entries.Count - 1;
            while (entryIndex >= 0)
            {
                if (ValueComparer.TextEquals(entries[entryIndex].ScopeId, scopeId))
                {
                    entries.RemoveAt(entryIndex);
                }

                entryIndex--;
            }

            snapshotIndex++;
        }

        int batchEntryIndex = StateStore.BatchEntrySnapshot.Count - 1;
        while (batchEntryIndex >= 0)
        {
            if (ValueComparer.TextEquals(StateStore.BatchEntrySnapshot[batchEntryIndex].ScopeId, scopeId))
            {
                StateStore.BatchEntrySnapshot.RemoveAt(batchEntryIndex);
            }

            batchEntryIndex--;
        }

        int batchVersionIndex = StateStore.BatchVersionSnapshot.Count - 1;
        while (batchVersionIndex >= 0)
        {
            if (ValueComparer.TextEquals(StateStore.BatchVersionSnapshot[batchVersionIndex].ScopeId, scopeId))
            {
                StateStore.BatchVersionSnapshot.RemoveAt(batchVersionIndex);
            }

            batchVersionIndex--;
        }
    }

    internal void RemoveComponentRenderInvocations(string componentId)
    {
        int index = RenderInvocations.Count - 1;
        while (index >= 0)
        {
            if (ValueComparer.TextEquals(RenderInvocations[index].Request.Context.ComponentId, componentId))
            {
                RenderInvocations.RemoveAt(index);
            }

            index--;
        }
    }

    internal void RemoveComponentErrorInvocations(string componentId)
    {
        int index = ErrorInvocations.Count - 1;
        while (index >= 0)
        {
            ComponentErrorInvocation invocation = ErrorInvocations[index];
            if (ValueComparer.TextEquals(invocation.FailedComponentId, componentId) || ValueComparer.TextEquals(invocation.Boundary.BoundaryId, componentId))
            {
                ErrorInvocations.RemoveAt(index);
            }

            index--;
        }
    }

    internal void QueueComponentLifecycle(ComponentInstance instance, UiLifecycleStage stage, int generation)
    {
        int definitionIndex = FindComponentDefinitionIndex(instance.Context.TypeId);
        if (definitionIndex < 0)
        {
            return;
        }

        string? handlerId = Definitions[definitionIndex].LifecycleHandlerId;
        if (handlerId == null)
        {
            return;
        }

        ComponentLifecycleInvocation invocation = new ComponentLifecycleInvocation();
        invocation.HandlerId = handlerId;
        invocation.Notification.ComponentId = instance.Context.ComponentId;
        invocation.Notification.Stage = stage;
        invocation.Notification.Generation = generation;
        LifecycleInvocations.Add(invocation);
    }

    internal void QueueComponentErrorLifecycle(ComponentInstance instance, UiDiagnostic diagnostic)
    {
        int definitionIndex = FindComponentDefinitionIndex(instance.Context.TypeId);
        if (definitionIndex < 0)
        {
            return;
        }

        string? handlerId = Definitions[definitionIndex].LifecycleHandlerId;
        if (handlerId == null)
        {
            return;
        }

        ComponentLifecycleInvocation invocation = new ComponentLifecycleInvocation();
        invocation.HandlerId = handlerId;
        invocation.Notification.ComponentId = instance.Context.ComponentId;
        invocation.Notification.Stage = UiLifecycleStage.Error;
        invocation.Notification.Generation = instance.RenderGeneration;
        invocation.Notification.Diagnostic = CopyComponentDiagnostic(diagnostic);
        LifecycleInvocations.Add(invocation);
    }

    internal ComponentInstance? FindComponentErrorBoundary(string? componentId)
    {
        string? candidateId = componentId;
        while (candidateId != null)
        {
            int index = FindComponentInstanceIndex(candidateId);
            if (index < 0)
            {
                return null;
            }

            ComponentInstance candidate = Instances[index];
            if (candidate.ErrorBoundary != null && !candidate.ErrorBoundary.HasError)
            {
                return candidate;
            }

            candidateId = candidate.ParentComponentId;
        }

        return null;
    }

    internal void QueueComponentErrorInvocation(ComponentInstance boundary, string failedComponentId, UiLifecycleStage stage)
    {
        if (boundary.ErrorBoundary == null)
        {
            return;
        }

        int definitionIndex = FindComponentDefinitionIndex(boundary.Context.TypeId);
        if (definitionIndex < 0)
        {
            return;
        }

        string? handlerId = Definitions[definitionIndex].ErrorHandlerId;
        if (handlerId == null)
        {
            return;
        }

        ComponentErrorInvocation invocation = new ComponentErrorInvocation();
        invocation.HandlerId = handlerId;
        invocation.FailedComponentId = failedComponentId;
        invocation.Stage = stage;
        invocation.Boundary = CopyComponentErrorBoundary(boundary.ErrorBoundary);
        ErrorInvocations.Add(invocation);
    }

    internal string ValidateComponentDiagnostic(UiDiagnostic? diagnostic)
    {
        if (diagnostic == null)
        {
            return "component.null-diagnostic";
        }

        if (diagnostic.Severity != UiDiagnosticSeverity.Information && diagnostic.Severity != UiDiagnosticSeverity.Warning && diagnostic.Severity != UiDiagnosticSeverity.Error)
        {
            return "component.invalid-diagnostic-severity";
        }

        if (string.IsNullOrWhiteSpace(diagnostic.Code))
        {
            return "component.invalid-diagnostic-code";
        }

        if (string.IsNullOrWhiteSpace(diagnostic.Message))
        {
            return "component.invalid-diagnostic-message";
        }

        if (diagnostic.Location != null && string.IsNullOrWhiteSpace(diagnostic.Location))
        {
            return "component.invalid-diagnostic-location";
        }

        return string.Empty;
    }

    internal bool IsComponentLifecycleStage(UiLifecycleStage stage)
    {
        return stage == UiLifecycleStage.Create || stage == UiLifecycleStage.Attach || stage == UiLifecycleStage.ParametersChanged || stage == UiLifecycleStage.BeforeRender || stage == UiLifecycleStage.AfterRender || stage == UiLifecycleStage.Activate || stage == UiLifecycleStage.Deactivate || stage == UiLifecycleStage.Detach || stage == UiLifecycleStage.Dispose || stage == UiLifecycleStage.HydrationStart || stage == UiLifecycleStage.HydrationComplete || stage == UiLifecycleStage.Error;
    }

    internal string ValidateComponentDefinition(UiComponentDefinition? definition)
    {
        if (definition == null)
        {
            return "component.null-definition";
        }

        if (string.IsNullOrWhiteSpace(definition.TypeId))
        {
            return "component.invalid-type";
        }

        if (string.IsNullOrWhiteSpace(definition.RenderHandlerId))
        {
            return "component.invalid-render-handler";
        }

        if (definition.LifecycleHandlerId != null && string.IsNullOrWhiteSpace(definition.LifecycleHandlerId))
        {
            return "component.invalid-lifecycle-handler";
        }

        if (definition.ErrorHandlerId != null && string.IsNullOrWhiteSpace(definition.ErrorHandlerId))
        {
            return "component.invalid-error-handler";
        }

        return ValidateComponentValues(definition.DefaultParameters, "component.null-default-parameters", "component.invalid-default-parameter", "component.duplicate-default-parameter");
    }

    internal string ValidateComponentValues(UiValueEntry[]? values, string nullCode, string invalidCode, string duplicateCode)
    {
        if (values == null)
        {
            return nullCode;
        }

        int index = 0;
        while (index < values.Length)
        {
            UiValueEntry? entry = values[index];
            if (entry == null || string.IsNullOrWhiteSpace(entry.Key) || StateStore.ValidateStateValue(entry.Value).Length > 0)
            {
                return invalidCode;
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (ValueComparer.TextEquals(values[priorIndex].Key, entry.Key))
                {
                    return duplicateCode;
                }

                priorIndex++;
            }

            index++;
        }

        return string.Empty;
    }

    internal string ValidateComponentSlots(UiSlot[]? slots)
    {
        if (slots == null)
        {
            return "component.null-slots";
        }

        int index = 0;
        while (index < slots.Length)
        {
            UiSlot? slot = slots[index];
            if (slot == null || string.IsNullOrWhiteSpace(slot.Name) || slot.Content == null || slot.FallbackContent == null)
            {
                return "component.invalid-slot";
            }

            int priorIndex = 0;
            while (priorIndex < index)
            {
                if (ValueComparer.TextEquals(slots[priorIndex].Name, slot.Name))
                {
                    return "component.duplicate-slot";
                }

                priorIndex++;
            }

            int contentIndex = 0;
            while (contentIndex < slot.Content.Length)
            {
                if (slot.Content[contentIndex] == null)
                {
                    return "component.null-slot-node";
                }

                contentIndex++;
            }

            contentIndex = 0;
            while (contentIndex < slot.FallbackContent.Length)
            {
                if (slot.FallbackContent[contentIndex] == null)
                {
                    return "component.null-slot-node";
                }

                contentIndex++;
            }

            index++;
        }

        return string.Empty;
    }

    internal int FindComponentDefinitionIndex(string typeId)
    {
        int index = 0;
        while (index < Definitions.Count)
        {
            int comparison = ValueComparer.CompareText(Definitions[index].TypeId, typeId);
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

    internal int FindComponentInstanceIndex(string componentId)
    {
        int index = 0;
        while (index < Instances.Count)
        {
            int comparison = ValueComparer.CompareText(Instances[index].Context.ComponentId, componentId);
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

    internal int FindDisposedComponentIdIndex(string componentId)
    {
        int index = 0;
        while (index < DisposedComponentIds.Count)
        {
            int comparison = ValueComparer.CompareText(DisposedComponentIds[index], componentId);
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

    internal int FindCheckedComponentInstanceIndex(string? componentId)
    {
        return string.IsNullOrWhiteSpace(componentId) ? -1 : FindComponentInstanceIndex(componentId);
    }

    internal string GetComponentLookupCode(string? componentId)
    {
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return "component.invalid-id";
        }

        return FindDisposedComponentIdIndex(componentId) >= 0 ? "component.disposed" : "component.unknown";
    }

    internal int FindComponentValueIndex(UiValueEntry[] values, string key)
    {
        int index = 0;
        while (index < values.Length)
        {
            int comparison = ValueComparer.CompareText(values[index].Key, key);
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

    internal void InsertComponentDefinition(UiComponentDefinition definition)
    {
        int index = 0;
        while (index < Definitions.Count && ValueComparer.CompareText(Definitions[index].TypeId, definition.TypeId) < 0)
        {
            index++;
        }

        Definitions.Insert(index, definition);
    }

    internal void InsertComponentInstance(ComponentInstance instance)
    {
        int index = 0;
        while (index < Instances.Count && ValueComparer.CompareText(Instances[index].Context.ComponentId, instance.Context.ComponentId) < 0)
        {
            index++;
        }

        Instances.Insert(index, instance);
    }

    internal void InsertDisposedComponentId(string componentId)
    {
        int index = 0;
        while (index < DisposedComponentIds.Count && ValueComparer.CompareText(DisposedComponentIds[index], componentId) < 0)
        {
            index++;
        }

        if (index >= DisposedComponentIds.Count || !ValueComparer.TextEquals(DisposedComponentIds[index], componentId))
        {
            DisposedComponentIds.Insert(index, componentId);
        }
    }

    internal UiComponentDefinition CopyComponentDefinition(UiComponentDefinition source)
    {
        UiComponentDefinition result = new UiComponentDefinition();
        result.TypeId = source.TypeId;
        result.RenderHandlerId = source.RenderHandlerId;
        result.LifecycleHandlerId = source.LifecycleHandlerId;
        result.ErrorHandlerId = source.ErrorHandlerId;
        result.DefaultParameters = CopyComponentValues(source.DefaultParameters);
        return result;
    }

    internal UiComponentContext CopyComponentContext(UiComponentContext source)
    {
        UiComponentContext result = new UiComponentContext();
        result.ComponentId = source.ComponentId;
        result.TypeId = source.TypeId;
        result.Key = source.Key;
        result.Parameters = CopyComponentValues(source.Parameters);
        result.StateScopeId = source.StateScopeId;
        result.Slots = CopyComponentSlots(source.Slots);
        result.ContextValues = CopyComponentValues(source.ContextValues);
        return result;
    }

    internal UiComponentRenderRequest CopyComponentRenderRequest(UiComponentRenderRequest source)
    {
        UiComponentRenderRequest result = new UiComponentRenderRequest();
        result.Context = CopyComponentContext(source.Context);
        result.Generation = source.Generation;
        result.Priority = source.Priority;
        result.IsHydration = source.IsHydration;
        return result;
    }

    internal ComponentRenderInvocation CopyComponentRenderInvocation(ComponentRenderInvocation source)
    {
        ComponentRenderInvocation result = new ComponentRenderInvocation();
        result.HandlerId = source.HandlerId;
        result.Request = CopyComponentRenderRequest(source.Request);
        return result;
    }

    internal ComponentLifecycleInvocation CopyComponentLifecycleInvocation(ComponentLifecycleInvocation source)
    {
        ComponentLifecycleInvocation result = new ComponentLifecycleInvocation();
        result.HandlerId = source.HandlerId;
        result.Notification.ComponentId = source.Notification.ComponentId;
        result.Notification.Stage = source.Notification.Stage;
        result.Notification.Generation = source.Notification.Generation;
        if (source.Notification.Diagnostic != null)
        {
            object? diagnostic = new ModelNormalizer().CopyValue(source.Notification.Diagnostic);
            if (diagnostic == null)
            {
                throw new InvalidOperationException("The lifecycle diagnostic could not be copied.");
            }

            result.Notification.Diagnostic = (UiDiagnostic)diagnostic;
        }

        return result;
    }

    internal ComponentErrorInvocation CopyComponentErrorInvocation(ComponentErrorInvocation source)
    {
        ComponentErrorInvocation result = new ComponentErrorInvocation();
        result.HandlerId = source.HandlerId;
        result.FailedComponentId = source.FailedComponentId;
        result.Stage = source.Stage;
        result.Boundary = CopyComponentErrorBoundary(source.Boundary);
        return result;
    }

    internal UiErrorBoundaryState CopyComponentErrorBoundary(UiErrorBoundaryState source)
    {
        UiErrorBoundaryState result = new UiErrorBoundaryState();
        result.BoundaryId = source.BoundaryId;
        result.HasError = source.HasError;
        result.ResetKey = source.ResetKey;
        if (source.Diagnostic != null)
        {
            result.Diagnostic = CopyComponentDiagnostic(source.Diagnostic);
        }

        if (source.Fallback != null)
        {
            result.Fallback = CopyComponentNode(source.Fallback);
        }

        return result;
    }

    internal UiDiagnostic CopyComponentDiagnostic(UiDiagnostic source)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = source.Severity;
        result.Code = source.Code;
        result.Message = source.Message;
        result.Location = source.Location;
        return result;
    }

    internal UiValueEntry[] CopyComponentValues(UiValueEntry[] source)
    {
        UiValueEntry[] result = new UiValueEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new UiValueEntry { Key = source[index].Key, Value = StateStore.CopyStateValue(source[index].Value) };
            index++;
        }

        SortComponentValues(result);
        return result;
    }

    internal UiValueEntry[] MergeComponentValues(UiValueEntry[] defaults, UiValueEntry[] supplied)
    {
        UiValueEntry[] result = CopyComponentValues(defaults);
        int index = 0;
        while (index < supplied.Length)
        {
            result = SetComponentValue(result, supplied[index].Key, supplied[index].Value);
            index++;
        }

        return result;
    }

    internal UiValueEntry[] SetComponentValue(UiValueEntry[] source, string key, UiValue value)
    {
        int existingIndex = FindComponentValueIndex(source, key);
        if (existingIndex >= 0)
        {
            UiValueEntry[] replaced = CopyComponentValues(source);
            replaced[existingIndex].Value = StateStore.CopyStateValue(value);
            return replaced;
        }

        UiValueEntry[] result = new UiValueEntry[source.Length + 1];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = new UiValueEntry { Key = source[index].Key, Value = StateStore.CopyStateValue(source[index].Value) };
            index++;
        }

        result[result.Length - 1] = new UiValueEntry { Key = key, Value = StateStore.CopyStateValue(value) };
        SortComponentValues(result);
        return result;
    }

    internal void SortComponentValues(UiValueEntry[] values)
    {
        int index = 1;
        while (index < values.Length)
        {
            UiValueEntry candidate = values[index];
            int insertion = index;
            while (insertion > 0 && ValueComparer.CompareText(candidate.Key, values[insertion - 1].Key) < 0)
            {
                values[insertion] = values[insertion - 1];
                insertion--;
            }

            values[insertion] = candidate;
            index++;
        }
    }

    internal bool ComponentValuesEqual(UiValueEntry[] left, UiValueEntry[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (!ValueComparer.TextEquals(left[index].Key, right[index].Key) || !StateStore.StateValuesEqual(left[index].Value, right[index].Value))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal UiSlot[] CopyComponentSlots(UiSlot[] source)
    {
        UiSlot[] result = new UiSlot[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            object? copy = new ModelNormalizer().CopyValue(source[index]);
            if (copy == null)
            {
                throw new InvalidOperationException("The component slot could not be copied.");
            }

            result[index] = (UiSlot)copy;
            index++;
        }

        int sortIndex = 1;
        while (sortIndex < result.Length)
        {
            UiSlot candidate = result[sortIndex];
            int insertion = sortIndex;
            while (insertion > 0 && ValueComparer.CompareText(candidate.Name, result[insertion - 1].Name) < 0)
            {
                result[insertion] = result[insertion - 1];
                insertion--;
            }

            result[insertion] = candidate;
            sortIndex++;
        }

        return result;
    }

    internal UiNode CopyComponentNode(UiNode source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        if (copy == null)
        {
            throw new InvalidOperationException("The component node could not be copied.");
        }

        return (UiNode)copy;
    }

    internal ComponentOperationResult CreateComponentSuccess(bool applied)
    {
        return new ComponentOperationResult { Success = true, Applied = applied };
    }

    internal ComponentOperationResult CreateComponentFailure(string code)
    {
        return new ComponentOperationResult { Code = code };
    }
}
