using System;
using System.Collections.Generic;

namespace htmlgui;

internal class FocusEngine
{
    internal List<UiDiagnostic> Diagnostics;

    internal FocusEngine()
    {
        Diagnostics = new List<UiDiagnostic>();
    }

    internal UiResult ValidateFocusRegistration(UiFocusScope[]? scopes, UiFocusTarget[]? targets)
    {
        Diagnostics.Clear();
        if (scopes == null)
        {
            AddFocusError("focus.null-scopes", "The focus scope collection cannot be null.", "/focus/scopes");
        }

        if (targets == null)
        {
            AddFocusError("focus.null-targets", "The focus target collection cannot be null.", "/focus/targets");
        }

        if (scopes != null && targets != null)
        {
            ValidateFocusScopes(scopes);
            ValidateFocusTargets(scopes, targets);
        }

        return new UiResult
        {
            Success = Diagnostics.Count == 0,
            Diagnostics = Diagnostics.ToArray()
        };
    }

    internal UiFocusResult ResolveFocus(UiFocusScope[]? scopes, UiFocusTarget[]? targets, UiFocusState? state, UiFocusRequest? request)
    {
        Diagnostics.Clear();
        UiFocusResult result = new UiFocusResult();
        result.State = CopyFocusState(state);
        result.Targets = CopyFocusTargets(targets);
        if (scopes == null)
        {
            AddFocusError("focus.null-scopes", "The focus scope collection cannot be null.", "/focus/scopes");
        }

        if (targets == null)
        {
            AddFocusError("focus.null-targets", "The focus target collection cannot be null.", "/focus/targets");
        }

        if (state == null)
        {
            AddFocusError("focus.null-state", "The current focus state cannot be null.", "/focus/state");
        }

        if (request == null)
        {
            AddFocusError("focus.null-request", "The focus request cannot be null.", "/focus/request");
        }

        if (scopes == null || targets == null || state == null || request == null)
        {
            return CompleteFocusResult(result);
        }

        ValidateFocusScopes(scopes);
        ValidateFocusTargets(scopes, targets);
        ValidateFocusRequest(request);
        if (Diagnostics.Count > 0)
        {
            return CompleteFocusResult(result);
        }

        UiFocusOperation operation = request.Operation;
        if (request.Restore)
        {
            operation = UiFocusOperation.Restore;
        }
        else if (request.Direction != UiFocusDirection.None && operation == UiFocusOperation.Focus)
        {
            operation = UiFocusOperation.Move;
        }

        if (operation == UiFocusOperation.Blur)
        {
            result.State.FocusedNodeId = null;
            result.State.ActiveDescendantId = null;
            result.State.ScopeId = null;
            result.State.IsTrapped = false;
            return CompleteFocusResult(result);
        }

        UiFocusScope? scope = SelectFocusScope(scopes, targets, state, request, operation);
        if (scope == null)
        {
            AddFocusError("focus.unknown-scope", "The focus request does not identify a registered scope.", "/focus/request/scopeId");
            return CompleteFocusResult(result);
        }

        UiFocusTarget? target = SelectFocusTarget(scopes, targets, state, request, scope, operation);
        if (target == null)
        {
            if (Diagnostics.Count == 0)
            {
                AddFocusError("focus.no-target", "The focus scope does not contain an eligible target.", "/focus/request/targetId");
            }

            return CompleteFocusResult(result);
        }

        UiFocusScope? targetScope = FindFocusScope(scopes, target.ScopeId);
        if (targetScope != null)
        {
            scope = targetScope;
        }

        ApplyFocusTarget(result, scope, target, request);
        return CompleteFocusResult(result);
    }

    internal void ValidateFocusScopes(UiFocusScope[] scopes)
    {
        Dictionary<string, int> identifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        while (index < scopes.Length)
        {
            UiFocusScope? scope = scopes[index];
            string location = "/focus/scopes/" + index;
            if (scope == null)
            {
                AddFocusError("focus.null-scope", "A focus scope cannot be null.", location);
            }
            else if (string.IsNullOrWhiteSpace(scope.Id))
            {
                AddFocusError("focus.invalid-scope", "A focus scope requires a stable identifier.", location + "/id");
            }
            else if (identifiers.ContainsKey(scope.Id))
            {
                AddFocusError("focus.duplicate-scope", "A focus scope identifier must be unique.", location + "/id");
            }
            else
            {
                identifiers.Add(scope.Id, index);
                if (scope.TargetIds == null)
                {
                    AddFocusError("focus.null-order", "A focus scope target order cannot be null.", location + "/targetIds");
                }

                if (scope.RovingTabIndex && scope.UseActiveDescendant)
                {
                    AddFocusError("focus.conflicting-strategy", "A focus scope cannot combine roving tab indexes with active-descendant focus.", location);
                }

                if (scope.UseActiveDescendant && string.IsNullOrWhiteSpace(scope.OwnerNodeId))
                {
                    AddFocusError("focus.missing-owner", "An active-descendant scope requires an owner node identifier.", location + "/ownerNodeId");
                }
            }

            index++;
        }

        index = 0;
        while (index < scopes.Length)
        {
            UiFocusScope? scope = scopes[index];
            if (scope != null && scope.ParentScopeId != null && !identifiers.ContainsKey(scope.ParentScopeId))
            {
                AddFocusError("focus.unknown-parent-scope", "The parent focus scope is not registered.", "/focus/scopes/" + index + "/parentScopeId");
            }
            else if (scope != null && HasFocusScopeCycle(scopes, scope))
            {
                AddFocusError("focus.scope-cycle", "The focus scope parent chain contains a cycle.", "/focus/scopes/" + index + "/parentScopeId");
            }

            index++;
        }
    }

    internal bool HasFocusScopeCycle(UiFocusScope[] scopes, UiFocusScope scope)
    {
        string? parentId = scope.ParentScopeId;
        int depth = 0;
        while (parentId != null && depth <= scopes.Length)
        {
            if (parentId == scope.Id)
            {
                return true;
            }

            UiFocusScope? parent = FindFocusScope(scopes, parentId);
            if (parent == null)
            {
                return false;
            }

            parentId = parent.ParentScopeId;
            depth++;
        }

        return false;
    }

    internal void ValidateFocusTargets(UiFocusScope[] scopes, UiFocusTarget[] targets)
    {
        Dictionary<string, int> identifiers = new Dictionary<string, int>(StringComparer.Ordinal);
        int index = 0;
        while (index < targets.Length)
        {
            UiFocusTarget? target = targets[index];
            string location = "/focus/targets/" + index;
            if (target == null)
            {
                AddFocusError("focus.null-target", "A focus target cannot be null.", location);
            }
            else if (string.IsNullOrWhiteSpace(target.Id))
            {
                AddFocusError("focus.invalid-target", "A focus target requires a stable identifier.", location + "/id");
            }
            else if (identifiers.ContainsKey(target.Id))
            {
                AddFocusError("focus.duplicate-target", "A focus target identifier must be unique.", location + "/id");
            }
            else
            {
                identifiers.Add(target.Id, index);
                if (FindFocusScope(scopes, target.ScopeId) == null)
                {
                    AddFocusError("focus.unknown-target-scope", "The focus target scope is not registered.", location + "/scopeId");
                }
            }

            index++;
        }

        index = 0;
        while (index < scopes.Length)
        {
            UiFocusScope? scope = scopes[index];
            if (scope != null && scope.TargetIds != null)
            {
                Dictionary<string, int> ordered = new Dictionary<string, int>(StringComparer.Ordinal);
                int targetIndex = 0;
                while (targetIndex < scope.TargetIds.Length)
                {
                    string? targetId = scope.TargetIds[targetIndex];
                    string location = "/focus/scopes/" + index + "/targetIds/" + targetIndex;
                    if (string.IsNullOrWhiteSpace(targetId) || !identifiers.ContainsKey(targetId))
                    {
                        AddFocusError("focus.unknown-target", "The scope target is not registered.", location);
                    }
                    else if (ordered.ContainsKey(targetId))
                    {
                        AddFocusError("focus.duplicate-order-target", "A target can occur only once in a scope order.", location);
                    }
                    else
                    {
                        ordered.Add(targetId, targetIndex);
                        UiFocusTarget? target = FindFocusTarget(targets, targetId);
                        if (target != null && target.ScopeId != scope.Id)
                        {
                            AddFocusError("focus.target-scope-mismatch", "The ordered target belongs to a different focus scope.", location);
                        }
                    }

                    targetIndex++;
                }

                ValidateOptionalScopeTarget(identifiers, scope.InitialFocusId, "/focus/scopes/" + index + "/initialFocusId");
                ValidateOptionalScopeTarget(identifiers, scope.ReturnFocusId, "/focus/scopes/" + index + "/returnFocusId");
                ValidateOptionalScopeTarget(identifiers, scope.ActiveTargetId, "/focus/scopes/" + index + "/activeTargetId");
                ValidateOwnedScopeTarget(targets, scope, scope.InitialFocusId, "/focus/scopes/" + index + "/initialFocusId");
                ValidateOwnedScopeTarget(targets, scope, scope.ActiveTargetId, "/focus/scopes/" + index + "/activeTargetId");
            }

            index++;
        }
    }

    internal void ValidateOwnedScopeTarget(UiFocusTarget[] targets, UiFocusScope scope, string? targetId, string location)
    {
        UiFocusTarget? target = FindFocusTarget(targets, targetId);
        if (target != null && target.ScopeId != scope.Id)
        {
            AddFocusError("focus.target-scope-mismatch", "The focus target belongs to a different focus scope.", location);
        }
    }

    internal void ValidateFocusRequest(UiFocusRequest request)
    {
        switch (request.Operation)
        {
            case UiFocusOperation.Focus:
            case UiFocusOperation.Blur:
            case UiFocusOperation.Move:
            case UiFocusOperation.Restore:
            case UiFocusOperation.SetActiveDescendant:
                break;
            default:
                AddFocusError("focus.invalid-operation", "The focus operation is not supported.", "/focus/request/operation");
                break;
        }

        switch (request.Direction)
        {
            case UiFocusDirection.None:
            case UiFocusDirection.First:
            case UiFocusDirection.Last:
            case UiFocusDirection.Next:
            case UiFocusDirection.Previous:
            case UiFocusDirection.Up:
            case UiFocusDirection.Down:
            case UiFocusDirection.Left:
            case UiFocusDirection.Right:
                break;
            default:
                AddFocusError("focus.invalid-direction", "The focus direction is not supported.", "/focus/request/direction");
                break;
        }
    }

    internal void ValidateOptionalScopeTarget(Dictionary<string, int> identifiers, string? targetId, string location)
    {
        if (targetId != null && (targetId.Length == 0 || !identifiers.ContainsKey(targetId)))
        {
            AddFocusError("focus.unknown-target", "The focus target is not registered.", location);
        }
    }

    internal UiFocusScope? SelectFocusScope(UiFocusScope[] scopes, UiFocusTarget[] targets, UiFocusState state, UiFocusRequest request, UiFocusOperation operation)
    {
        if (operation == UiFocusOperation.Restore)
        {
            UiFocusScope? restoreScope = FindFocusScope(scopes, request.ScopeId ?? state.ScopeId);
            if (restoreScope != null)
            {
                return restoreScope;
            }
        }

        if (request.TargetId != null)
        {
            UiFocusTarget? explicitTarget = FindFocusTarget(targets, request.TargetId);
            if (explicitTarget != null)
            {
                UiFocusScope? activeScope = FindFocusScope(scopes, state.ScopeId);
                if (activeScope != null && activeScope.TrapFocus && explicitTarget.ScopeId != activeScope.Id)
                {
                    return activeScope;
                }

                UiFocusScope? explicitScope = FindFocusScope(scopes, explicitTarget.ScopeId);
                if (explicitScope != null)
                {
                    return explicitScope;
                }
            }
        }

        UiFocusScope? requested = FindFocusScope(scopes, request.ScopeId);
        if (requested != null)
        {
            return requested;
        }

        UiFocusScope? current = FindFocusScope(scopes, state.ScopeId);
        if (current != null)
        {
            return current;
        }

        if (scopes.Length == 1)
        {
            return scopes[0];
        }

        return null;
    }

    internal UiFocusTarget? SelectFocusTarget(UiFocusScope[] scopes, UiFocusTarget[] targets, UiFocusState state, UiFocusRequest request, UiFocusScope scope, UiFocusOperation operation)
    {
        if (operation == UiFocusOperation.Restore)
        {
            return SelectEligibleTarget(targets, scope.ReturnFocusId, "/focus/scopes/returnFocusId");
        }

        if (operation == UiFocusOperation.SetActiveDescendant && !scope.UseActiveDescendant)
        {
            AddFocusError("focus.invalid-active-descendant", "The focus scope does not use active-descendant focus.", "/focus/request/operation");
            return null;
        }

        if (operation == UiFocusOperation.SetActiveDescendant && request.TargetId == null)
        {
            AddFocusError("focus.unknown-target", "An active-descendant request requires a target.", "/focus/request/targetId");
            return null;
        }

        if (request.TargetId != null)
        {
            UiFocusTarget? requested = FindFocusTarget(targets, request.TargetId);
            if (requested == null)
            {
                AddFocusError("focus.unknown-target", "The requested focus target is not registered.", "/focus/request/targetId");
                return null;
            }

            UiFocusScope? activeScope = FindFocusScope(scopes, state.ScopeId);
            if (activeScope != null && activeScope.TrapFocus && requested.ScopeId != activeScope.Id)
            {
                AddFocusError("focus.trapped-target", "Focus cannot leave the active trapped scope.", "/focus/request/targetId");
                return null;
            }

            if (!IsEligibleFocusTarget(requested))
            {
                AddFocusError("focus.ineligible-target", "The requested target is not visible, enabled, and focusable.", "/focus/request/targetId");
                return null;
            }

            return requested;
        }

        if (operation == UiFocusOperation.Move)
        {
            return SelectDirectionalTarget(targets, state, request, scope);
        }

        string? initialId = scope.InitialFocusId;
        if (initialId == null)
        {
            initialId = scope.ActiveTargetId;
        }

        UiFocusTarget? initial = SelectEligibleTarget(targets, initialId, "/focus/scopes/initialFocusId");
        if (initial != null)
        {
            return initial;
        }

        List<int> order = CreateTabOrder(targets, scope);
        if (order.Count == 0)
        {
            return null;
        }

        return targets[order[0]];
    }

    internal UiFocusTarget? SelectEligibleTarget(UiFocusTarget[] targets, string? targetId, string location)
    {
        if (targetId == null)
        {
            return null;
        }

        UiFocusTarget? target = FindFocusTarget(targets, targetId);
        if (target == null)
        {
            AddFocusError("focus.unknown-target", "The focus target is not registered.", location);
            return null;
        }

        if (!IsEligibleFocusTarget(target))
        {
            AddFocusError("focus.ineligible-target", "The focus target is not visible, enabled, and focusable.", location);
            return null;
        }

        return target;
    }

    internal UiFocusTarget? SelectDirectionalTarget(UiFocusTarget[] targets, UiFocusState state, UiFocusRequest request, UiFocusScope scope)
    {
        List<int> order = CreateTabOrder(targets, scope);
        if (order.Count == 0)
        {
            return null;
        }

        if (request.Direction == UiFocusDirection.First)
        {
            return targets[order[0]];
        }

        if (request.Direction == UiFocusDirection.Last)
        {
            return targets[order[order.Count - 1]];
        }

        string? currentId = state.FocusedNodeId;
        if (scope.UseActiveDescendant)
        {
            currentId = state.ActiveDescendantId;
        }

        int currentOrderIndex = FindOrderIndex(targets, order, currentId);
        if (currentOrderIndex < 0)
        {
            return targets[order[0]];
        }

        bool previous = request.Direction == UiFocusDirection.Previous || request.Direction == UiFocusDirection.Up || request.Direction == UiFocusDirection.Left;
        int nextIndex = currentOrderIndex + 1;
        if (previous)
        {
            nextIndex = currentOrderIndex - 1;
        }

        if (nextIndex < 0)
        {
            if (scope.Wrap || scope.TrapFocus)
            {
                nextIndex = order.Count - 1;
            }
            else
            {
                nextIndex = 0;
            }
        }
        else if (nextIndex >= order.Count)
        {
            if (scope.Wrap || scope.TrapFocus)
            {
                nextIndex = 0;
            }
            else
            {
                nextIndex = order.Count - 1;
            }
        }

        return targets[order[nextIndex]];
    }

    internal List<int> CreateTabOrder(UiFocusTarget[] targets, UiFocusScope scope)
    {
        List<int> order = new List<int>();
        string[] targetIds = scope.TargetIds ?? Array.Empty<string>();
        int idIndex = 0;
        while (idIndex < targetIds.Length)
        {
            int targetIndex = FindFocusTargetIndex(targets, targetIds[idIndex]);
            if (targetIndex >= 0)
            {
                UiFocusTarget target = targets[targetIndex];
                bool excludedByTabIndex = target.HasTabIndex && target.TabIndex < 0 && !scope.RovingTabIndex && !scope.UseActiveDescendant;
                if (IsEligibleFocusTarget(target) && !excludedByTabIndex)
                {
                    InsertTabOrderTarget(targets, order, targetIndex);
                }
            }

            idIndex++;
        }

        return order;
    }

    internal void InsertTabOrderTarget(UiFocusTarget[] targets, List<int> order, int targetIndex)
    {
        UiFocusTarget target = targets[targetIndex];
        int insertIndex = 0;
        while (insertIndex < order.Count)
        {
            UiFocusTarget existing = targets[order[insertIndex]];
            if (CompareTabOrderTargets(target, existing) < 0)
            {
                break;
            }

            insertIndex++;
        }

        order.Insert(insertIndex, targetIndex);
    }

    internal int CompareTabOrderTargets(UiFocusTarget left, UiFocusTarget right)
    {
        bool leftPositive = left.HasTabIndex && left.TabIndex > 0;
        bool rightPositive = right.HasTabIndex && right.TabIndex > 0;
        if (leftPositive && !rightPositive)
        {
            return -1;
        }

        if (!leftPositive && rightPositive)
        {
            return 1;
        }

        if (leftPositive && rightPositive)
        {
            if (left.TabIndex < right.TabIndex)
            {
                return -1;
            }

            if (left.TabIndex > right.TabIndex)
            {
                return 1;
            }
        }

        return 0;
    }

    internal int FindOrderIndex(UiFocusTarget[] targets, List<int> order, string? targetId)
    {
        int index = 0;
        while (targetId != null && index < order.Count)
        {
            if (targets[order[index]].Id == targetId)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void ApplyFocusTarget(UiFocusResult result, UiFocusScope scope, UiFocusTarget target, UiFocusRequest request)
    {
        result.State.ScopeId = scope.Id;
        result.State.IsTrapped = scope.TrapFocus;
        if (scope.UseActiveDescendant)
        {
            result.State.FocusedNodeId = scope.OwnerNodeId;
            result.State.ActiveDescendantId = target.Id;
        }
        else
        {
            result.State.FocusedNodeId = target.Id;
            result.State.ActiveDescendantId = null;
        }

        if (scope.RovingTabIndex)
        {
            ApplyRovingTabIndexes(result.Targets, scope, target.Id);
        }

        if (request.Reveal)
        {
            result.RevealNodeId = target.Id;
        }
    }

    internal void ApplyRovingTabIndexes(UiFocusTarget[] targets, UiFocusScope scope, string activeTargetId)
    {
        string[] targetIds = scope.TargetIds ?? Array.Empty<string>();
        int index = 0;
        while (index < targets.Length)
        {
            UiFocusTarget target = targets[index];
            if (ContainsFocusTargetId(targetIds, target.Id))
            {
                target.HasTabIndex = true;
                target.TabIndex = -1;
                if (target.Id == activeTargetId)
                {
                    target.TabIndex = 0;
                }
            }

            index++;
        }
    }

    internal bool ContainsFocusTargetId(string[] targetIds, string targetId)
    {
        int index = 0;
        while (index < targetIds.Length)
        {
            if (targetIds[index] == targetId)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsEligibleFocusTarget(UiFocusTarget target)
    {
        return target.IsFocusable && target.IsEnabled && target.IsVisible;
    }

    internal UiFocusScope? FindFocusScope(UiFocusScope[] scopes, string? scopeId)
    {
        int index = 0;
        while (scopeId != null && index < scopes.Length)
        {
            UiFocusScope? scope = scopes[index];
            if (scope != null && scope.Id == scopeId)
            {
                return scope;
            }

            index++;
        }

        return null;
    }

    internal UiFocusTarget? FindFocusTarget(UiFocusTarget[] targets, string? targetId)
    {
        int index = FindFocusTargetIndex(targets, targetId);
        if (index < 0)
        {
            return null;
        }

        return targets[index];
    }

    internal int FindFocusTargetIndex(UiFocusTarget[] targets, string? targetId)
    {
        int index = 0;
        while (targetId != null && index < targets.Length)
        {
            UiFocusTarget? target = targets[index];
            if (target != null && target.Id == targetId)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiFocusState CopyFocusState(UiFocusState? state)
    {
        if (state == null)
        {
            return new UiFocusState();
        }

        return new UiFocusState
        {
            FocusedNodeId = state.FocusedNodeId,
            ScopeId = state.ScopeId,
            ActiveDescendantId = state.ActiveDescendantId,
            IsTrapped = state.IsTrapped,
            IsFocusVisible = state.IsFocusVisible
        };
    }

    internal UiFocusScope[] CopyFocusScopes(UiFocusScope[]? scopes)
    {
        if (scopes == null)
        {
            return Array.Empty<UiFocusScope>();
        }

        UiFocusScope[] result = new UiFocusScope[scopes.Length];
        int index = 0;
        while (index < scopes.Length)
        {
            UiFocusScope? scope = scopes[index];
            if (scope == null)
            {
                result[index] = new UiFocusScope();
            }
            else
            {
                result[index] = new UiFocusScope
                {
                    Id = scope.Id,
                    ParentScopeId = scope.ParentScopeId,
                    OwnerNodeId = scope.OwnerNodeId,
                    TargetIds = CopyFocusTargetIds(scope.TargetIds),
                    InitialFocusId = scope.InitialFocusId,
                    ReturnFocusId = scope.ReturnFocusId,
                    ActiveTargetId = scope.ActiveTargetId,
                    TrapFocus = scope.TrapFocus,
                    RovingTabIndex = scope.RovingTabIndex,
                    UseActiveDescendant = scope.UseActiveDescendant,
                    Wrap = scope.Wrap
                };
            }

            index++;
        }

        return result;
    }

    internal string[] CopyFocusTargetIds(string[]? targetIds)
    {
        if (targetIds == null)
        {
            return Array.Empty<string>();
        }

        string[] result = new string[targetIds.Length];
        int index = 0;
        while (index < targetIds.Length)
        {
            result[index] = targetIds[index];
            index++;
        }

        return result;
    }

    internal UiFocusTarget[] CopyFocusTargets(UiFocusTarget[]? targets)
    {
        if (targets == null)
        {
            return Array.Empty<UiFocusTarget>();
        }

        UiFocusTarget[] result = new UiFocusTarget[targets.Length];
        int index = 0;
        while (index < targets.Length)
        {
            UiFocusTarget? target = targets[index];
            if (target == null)
            {
                result[index] = new UiFocusTarget();
            }
            else
            {
                result[index] = new UiFocusTarget
                {
                    Id = target.Id,
                    ScopeId = target.ScopeId,
                    IsFocusable = target.IsFocusable,
                    IsEnabled = target.IsEnabled,
                    IsVisible = target.IsVisible,
                    HasTabIndex = target.HasTabIndex,
                    TabIndex = target.TabIndex
                };
            }

            index++;
        }

        return result;
    }

    internal UiFocusResult CompleteFocusResult(UiFocusResult result)
    {
        result.Diagnostics = Diagnostics.ToArray();
        result.Success = Diagnostics.Count == 0;
        return result;
    }

    internal void AddFocusError(string code, string message, string location)
    {
        Diagnostics.Add(new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = location
        });
    }
}
