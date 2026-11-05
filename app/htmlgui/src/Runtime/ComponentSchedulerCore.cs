using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ComponentInvalidation
{
    internal string ComponentId = string.Empty;
    internal UiUpdatePriority Priority = UiUpdatePriority.Normal;
    internal bool IsHydration;
}

internal class ComponentPendingContent
{
    internal string ComponentId = string.Empty;
    internal int Generation;
    internal UiUpdatePriority Priority = UiUpdatePriority.Normal;
    internal bool IsHydration;
}

internal class ComponentScheduleResult
{
    internal bool Success;
    internal bool Applied;
    internal string Code = string.Empty;
    internal string BatchId = string.Empty;
    internal int Depth;
    internal ComponentInvalidation[] Invalidations = Array.Empty<ComponentInvalidation>();
    internal UiComponentRenderRequest[] Requests = Array.Empty<UiComponentRenderRequest>();
}

internal class ComponentSchedulerCore
{
    internal const int MaximumUpdateDepth = 64;

    internal ComponentCore Components;
    internal List<ComponentInvalidation> PendingInvalidations = new List<ComponentInvalidation>();
    internal List<ComponentInvalidation> DeferredInvalidations = new List<ComponentInvalidation>();
    internal List<ComponentInvalidation> BatchInvalidations = new List<ComponentInvalidation>();
    internal List<ComponentPendingContent> PendingContent = new List<ComponentPendingContent>();
    internal bool BatchActive;
    internal bool UpdateCycleActive;
    internal string BatchId = string.Empty;
    internal int UpdateDepth;

    internal ComponentSchedulerCore(ComponentCore components)
    {
        Components = components;
    }

    internal ComponentScheduleResult InvalidateComponent(string? componentId, UiUpdatePriority priority, bool isHydration)
    {
        if (!IsComponentUpdatePriority(priority))
        {
            return CreateComponentScheduleFailure("component.invalid-priority");
        }

        ComponentOperationResult component = Components.ReadComponentInstance(componentId);
        if (!component.Success || component.Context == null)
        {
            return CreateComponentScheduleFailure(component.Code);
        }

        List<ComponentInvalidation> target = BatchActive ? BatchInvalidations : UpdateCycleActive ? DeferredInvalidations : PendingInvalidations;
        bool applied = MergeComponentInvalidation(target, component.Context.ComponentId, priority, isHydration);
        ComponentScheduleResult result = CreateComponentScheduleSuccess(applied);
        result.Invalidations = CopyComponentInvalidationList(target);
        return result;
    }

    internal ComponentScheduleResult BeginComponentInvalidationBatch(string? batchId)
    {
        if (UpdateCycleActive)
        {
            return CreateComponentScheduleFailure("component.update-cycle-active");
        }

        if (BatchActive)
        {
            return CreateComponentScheduleFailure("component.invalidation-batch-active");
        }

        if (string.IsNullOrWhiteSpace(batchId))
        {
            return CreateComponentScheduleFailure("component.invalid-batch-id");
        }

        BatchActive = true;
        BatchId = batchId;
        BatchInvalidations.Clear();
        ComponentScheduleResult result = CreateComponentScheduleSuccess(true);
        result.BatchId = BatchId;
        return result;
    }

    internal ComponentScheduleResult CommitComponentInvalidationBatch()
    {
        if (!BatchActive)
        {
            return CreateComponentScheduleFailure("component.invalidation-batch-not-active");
        }

        string committedId = BatchId;
        int index = 0;
        while (index < BatchInvalidations.Count)
        {
            ComponentInvalidation invalidation = BatchInvalidations[index];
            MergeComponentInvalidation(PendingInvalidations, invalidation.ComponentId, invalidation.Priority, invalidation.IsHydration);
            index++;
        }

        bool applied = BatchInvalidations.Count > 0;
        BatchInvalidations.Clear();
        BatchActive = false;
        BatchId = string.Empty;
        ComponentScheduleResult result = CreateComponentScheduleSuccess(applied);
        result.BatchId = committedId;
        result.Invalidations = CopyComponentInvalidationList(PendingInvalidations);
        return result;
    }

    internal ComponentScheduleResult RollbackComponentInvalidationBatch()
    {
        if (!BatchActive)
        {
            return CreateComponentScheduleFailure("component.invalidation-batch-not-active");
        }

        string rolledBackId = BatchId;
        bool applied = BatchInvalidations.Count > 0;
        BatchInvalidations.Clear();
        BatchActive = false;
        BatchId = string.Empty;
        ComponentScheduleResult result = CreateComponentScheduleSuccess(applied);
        result.BatchId = rolledBackId;
        return result;
    }

    internal ComponentScheduleResult BeginComponentUpdateCycle()
    {
        if (BatchActive)
        {
            return CreateComponentScheduleFailure("component.invalidation-batch-active");
        }

        if (UpdateCycleActive)
        {
            return CreateComponentScheduleFailure("component.update-cycle-active");
        }

        if (PendingInvalidations.Count == 0)
        {
            UpdateDepth = 0;
            return CreateComponentScheduleSuccess(false);
        }

        if (UpdateDepth == MaximumUpdateDepth)
        {
            int exhaustedDepth = UpdateDepth + 1;
            PendingInvalidations.Clear();
            DeferredInvalidations.Clear();
            UpdateDepth = 0;
            ComponentScheduleResult exhausted = CreateComponentScheduleFailure("component.maximum-update-depth");
            exhausted.Depth = exhaustedDepth;
            return exhausted;
        }

        UpdateDepth++;
        UpdateCycleActive = true;
        SortComponentInvalidations(PendingInvalidations);
        List<UiComponentRenderRequest> requests = new List<UiComponentRenderRequest>();
        int index = 0;
        while (index < PendingInvalidations.Count)
        {
            ComponentInvalidation invalidation = PendingInvalidations[index];
            ComponentOperationResult begun = Components.BeginComponentRender(invalidation.ComponentId, invalidation.Priority, invalidation.IsHydration);
            if (begun.Success && begun.RenderRequest != null)
            {
                requests.Add(Components.CopyComponentRenderRequest(begun.RenderRequest));
                StoreComponentPendingContent(begun.RenderRequest);
            }

            index++;
        }

        PendingInvalidations.Clear();
        ComponentScheduleResult result = CreateComponentScheduleSuccess(requests.Count > 0);
        result.Depth = UpdateDepth;
        result.Requests = CopyComponentRenderRequestList(requests);
        return result;
    }

    internal ComponentScheduleResult EndComponentUpdateCycle()
    {
        if (!UpdateCycleActive)
        {
            return CreateComponentScheduleFailure("component.update-cycle-not-active");
        }

        UpdateCycleActive = false;
        int index = 0;
        while (index < DeferredInvalidations.Count)
        {
            ComponentInvalidation invalidation = DeferredInvalidations[index];
            MergeComponentInvalidation(PendingInvalidations, invalidation.ComponentId, invalidation.Priority, invalidation.IsHydration);
            index++;
        }

        bool deferred = DeferredInvalidations.Count > 0;
        DeferredInvalidations.Clear();
        if (PendingInvalidations.Count == 0)
        {
            UpdateDepth = 0;
        }

        ComponentScheduleResult result = CreateComponentScheduleSuccess(deferred);
        result.Depth = UpdateDepth;
        result.Invalidations = CopyComponentInvalidationList(PendingInvalidations);
        return result;
    }

    internal ComponentOperationResult CompletePendingComponentContent(string? componentId, int generation, UiNode? content)
    {
        int index = FindComponentPendingContentIndex(componentId);
        if (index < 0)
        {
            return Components.CreateComponentFailure(string.IsNullOrWhiteSpace(componentId) ? "component.invalid-id" : "component.no-pending-content");
        }

        ComponentPendingContent pending = PendingContent[index];
        if (generation != pending.Generation)
        {
            return Components.CreateComponentFailure("component.stale-pending-content");
        }

        ComponentOperationResult completed = Components.CompleteComponentRender(pending.ComponentId, generation, content);
        if (completed.Success)
        {
            PendingContent.RemoveAt(index);
        }

        return completed;
    }

    internal ComponentPendingContent[] ReadPendingComponentContent()
    {
        ComponentPendingContent[] result = new ComponentPendingContent[PendingContent.Count];
        int index = 0;
        while (index < PendingContent.Count)
        {
            result[index] = CopyComponentPendingContent(PendingContent[index]);
            index++;
        }

        return result;
    }

    internal bool MergeComponentInvalidation(List<ComponentInvalidation> invalidations, string componentId, UiUpdatePriority priority, bool isHydration)
    {
        int index = FindComponentInvalidationIndex(invalidations, componentId);
        if (index < 0)
        {
            ComponentInvalidation invalidation = new ComponentInvalidation();
            invalidation.ComponentId = componentId;
            invalidation.Priority = priority;
            invalidation.IsHydration = isHydration;
            invalidations.Add(invalidation);
            return true;
        }

        ComponentInvalidation existing = invalidations[index];
        bool applied = false;
        if ((int)priority < (int)existing.Priority)
        {
            existing.Priority = priority;
            applied = true;
        }

        if (isHydration && !existing.IsHydration)
        {
            existing.IsHydration = true;
            applied = true;
        }

        return applied;
    }

    internal int FindComponentInvalidationIndex(List<ComponentInvalidation> invalidations, string componentId)
    {
        int index = 0;
        while (index < invalidations.Count)
        {
            if (ValueComparer.TextEquals(invalidations[index].ComponentId, componentId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindComponentPendingContentIndex(string? componentId)
    {
        if (string.IsNullOrWhiteSpace(componentId))
        {
            return -1;
        }

        int index = 0;
        while (index < PendingContent.Count)
        {
            if (ValueComparer.TextEquals(PendingContent[index].ComponentId, componentId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void StoreComponentPendingContent(UiComponentRenderRequest request)
    {
        int index = FindComponentPendingContentIndex(request.Context.ComponentId);
        ComponentPendingContent pending = new ComponentPendingContent();
        pending.ComponentId = request.Context.ComponentId;
        pending.Generation = request.Generation;
        pending.Priority = request.Priority;
        pending.IsHydration = request.IsHydration;
        if (index < 0)
        {
            PendingContent.Add(pending);
            SortComponentPendingContent();
        }
        else
        {
            PendingContent[index] = pending;
        }
    }

    internal void SortComponentInvalidations(List<ComponentInvalidation> invalidations)
    {
        int index = 1;
        while (index < invalidations.Count)
        {
            ComponentInvalidation candidate = invalidations[index];
            int insertion = index;
            while (insertion > 0 && CompareComponentInvalidations(candidate, invalidations[insertion - 1]) < 0)
            {
                invalidations[insertion] = invalidations[insertion - 1];
                insertion--;
            }

            invalidations[insertion] = candidate;
            index++;
        }
    }

    internal int CompareComponentInvalidations(ComponentInvalidation left, ComponentInvalidation right)
    {
        int priorityComparison = (int)left.Priority - (int)right.Priority;
        if (priorityComparison != 0)
        {
            return priorityComparison;
        }

        if (IsComponentAncestor(left.ComponentId, right.ComponentId))
        {
            return -1;
        }

        if (IsComponentAncestor(right.ComponentId, left.ComponentId))
        {
            return 1;
        }

        int depthComparison = GetComponentDepth(left.ComponentId) - GetComponentDepth(right.ComponentId);
        return depthComparison != 0 ? depthComparison : ValueComparer.CompareText(left.ComponentId, right.ComponentId);
    }

    internal bool IsComponentAncestor(string ancestorId, string componentId)
    {
        int index = Components.FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return false;
        }

        string? parentId = Components.Instances[index].ParentComponentId;
        while (parentId != null)
        {
            if (ValueComparer.TextEquals(parentId, ancestorId))
            {
                return true;
            }

            int parentIndex = Components.FindComponentInstanceIndex(parentId);
            if (parentIndex < 0)
            {
                return false;
            }

            parentId = Components.Instances[parentIndex].ParentComponentId;
        }

        return false;
    }

    internal int GetComponentDepth(string componentId)
    {
        int depth = 0;
        int index = Components.FindComponentInstanceIndex(componentId);
        if (index < 0)
        {
            return depth;
        }

        string? parentId = Components.Instances[index].ParentComponentId;
        while (parentId != null)
        {
            depth++;
            int parentIndex = Components.FindComponentInstanceIndex(parentId);
            if (parentIndex < 0)
            {
                return depth;
            }

            parentId = Components.Instances[parentIndex].ParentComponentId;
        }

        return depth;
    }

    internal void SortComponentPendingContent()
    {
        int index = 1;
        while (index < PendingContent.Count)
        {
            ComponentPendingContent candidate = PendingContent[index];
            int insertion = index;
            while (insertion > 0 && ValueComparer.CompareText(candidate.ComponentId, PendingContent[insertion - 1].ComponentId) < 0)
            {
                PendingContent[insertion] = PendingContent[insertion - 1];
                insertion--;
            }

            PendingContent[insertion] = candidate;
            index++;
        }
    }

    internal bool IsComponentUpdatePriority(UiUpdatePriority priority)
    {
        return priority == UiUpdatePriority.Immediate || priority == UiUpdatePriority.UserInteraction || priority == UiUpdatePriority.Normal || priority == UiUpdatePriority.Background;
    }

    internal ComponentInvalidation CopyComponentInvalidation(ComponentInvalidation source)
    {
        ComponentInvalidation result = new ComponentInvalidation();
        result.ComponentId = source.ComponentId;
        result.Priority = source.Priority;
        result.IsHydration = source.IsHydration;
        return result;
    }

    internal ComponentInvalidation[] CopyComponentInvalidationList(List<ComponentInvalidation> source)
    {
        ComponentInvalidation[] result = new ComponentInvalidation[source.Count];
        int index = 0;
        while (index < source.Count)
        {
            result[index] = CopyComponentInvalidation(source[index]);
            index++;
        }

        return result;
    }

    internal ComponentPendingContent CopyComponentPendingContent(ComponentPendingContent source)
    {
        ComponentPendingContent result = new ComponentPendingContent();
        result.ComponentId = source.ComponentId;
        result.Generation = source.Generation;
        result.Priority = source.Priority;
        result.IsHydration = source.IsHydration;
        return result;
    }

    internal UiComponentRenderRequest[] CopyComponentRenderRequestList(List<UiComponentRenderRequest> source)
    {
        UiComponentRenderRequest[] result = new UiComponentRenderRequest[source.Count];
        int index = 0;
        while (index < source.Count)
        {
            result[index] = Components.CopyComponentRenderRequest(source[index]);
            index++;
        }

        return result;
    }

    internal ComponentScheduleResult CreateComponentScheduleSuccess(bool applied)
    {
        return new ComponentScheduleResult { Success = true, Applied = applied };
    }

    internal ComponentScheduleResult CreateComponentScheduleFailure(string code)
    {
        return new ComponentScheduleResult { Code = code };
    }
}
