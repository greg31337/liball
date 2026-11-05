using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Provides deterministic component registration, rendering, lifecycle, context, and disposal.</summary>
public class UiComponentRuntime : IUiComponentRuntime
{
    internal ComponentCore Core;
    internal ComponentSchedulerCore Scheduler;
    internal List<string> RendererIds;
    internal List<IUiComponentRenderer> Renderers;
    internal List<string> LifecycleHandlerIds;
    internal List<IUiLifecycleHandler> LifecycleHandlers;

    /// <summary>Initializes an empty component runtime.</summary>
    public UiComponentRuntime()
    {
        Core = new ComponentCore();
        Scheduler = new ComponentSchedulerCore(Core);
        RendererIds = new List<string>();
        Renderers = new List<IUiComponentRenderer>();
        LifecycleHandlerIds = new List<string>();
        LifecycleHandlers = new List<IUiLifecycleHandler>();
    }

    /// <summary>Registers one copied component definition.</summary>
    public UiComponentResult RegisterUiComponentDefinition(UiComponentDefinition? definition)
    {
        return CreatePublicComponentResult(Core.RegisterComponentDefinition(definition));
    }

    /// <summary>Unregisters one unused component definition.</summary>
    public UiComponentResult UnregisterUiComponentDefinition(string? typeId)
    {
        return CreatePublicComponentResult(Core.UnregisterComponentDefinition(typeId));
    }

    /// <summary>Registers a renderer under its stable handler identifier.</summary>
    public UiComponentResult RegisterUiComponentRenderer(string? handlerId, IUiComponentRenderer? renderer)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicComponentFailure("component.invalid-render-handler");
        }

        if (renderer == null)
        {
            return CreatePublicComponentFailure("component.null-renderer");
        }

        if (FindPublicComponentRendererIndex(handlerId) >= 0)
        {
            return CreatePublicComponentFailure("component.duplicate-renderer");
        }

        RendererIds.Add(handlerId);
        Renderers.Add(renderer);
        return new UiComponentResult { Success = true, Applied = true };
    }

    /// <summary>Unregisters a renderer callback.</summary>
    public UiComponentResult UnregisterUiComponentRenderer(string? handlerId)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicComponentFailure("component.invalid-render-handler");
        }

        int index = FindPublicComponentRendererIndex(handlerId);
        if (index < 0)
        {
            return CreatePublicComponentFailure("component.unknown-renderer");
        }

        RendererIds.RemoveAt(index);
        Renderers.RemoveAt(index);
        return new UiComponentResult { Success = true, Applied = true };
    }

    /// <summary>Registers a lifecycle or error callback under its stable handler identifier.</summary>
    public UiComponentResult RegisterUiLifecycleHandler(string? handlerId, IUiLifecycleHandler? handler)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicComponentFailure("component.invalid-lifecycle-handler");
        }

        if (handler == null)
        {
            return CreatePublicComponentFailure("component.null-lifecycle-handler");
        }

        if (FindPublicLifecycleHandlerIndex(handlerId) >= 0)
        {
            return CreatePublicComponentFailure("component.duplicate-lifecycle-handler");
        }

        LifecycleHandlerIds.Add(handlerId);
        LifecycleHandlers.Add(handler);
        return new UiComponentResult { Success = true, Applied = true };
    }

    /// <summary>Unregisters a lifecycle or error callback.</summary>
    public UiComponentResult UnregisterUiLifecycleHandler(string? handlerId)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            return CreatePublicComponentFailure("component.invalid-lifecycle-handler");
        }

        int index = FindPublicLifecycleHandlerIndex(handlerId);
        if (index < 0)
        {
            return CreatePublicComponentFailure("component.unknown-lifecycle-handler");
        }

        LifecycleHandlerIds.RemoveAt(index);
        LifecycleHandlers.RemoveAt(index);
        return new UiComponentResult { Success = true, Applied = true };
    }

    /// <summary>Creates or preserves and activates one keyed component instance.</summary>
    public UiComponentResult MountUiComponent(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues)
    {
        return CreatePublicComponentResult(Core.ReconcileComponentInstance(rootSeed, parentComponentId, typeId, key, parameters, slots, contextValues, true, false));
    }

    /// <summary>Remounts one keyed component with a fresh local state scope.</summary>
    public UiComponentResult RemountUiComponent(string? rootSeed, string? parentComponentId, string? typeId, string? key, UiValueEntry[]? parameters, UiSlot[]? slots, UiValueEntry[]? contextValues)
    {
        return CreatePublicComponentResult(Core.ReconcileComponentInstance(rootSeed, parentComponentId, typeId, key, parameters, slots, contextValues, true, true));
    }

    /// <summary>Reads one copied component instance.</summary>
    public UiComponentResult ReadUiComponent(string? componentId)
    {
        return CreatePublicComponentResult(Core.ReadComponentInstance(componentId));
    }

    /// <summary>Updates component parameters.</summary>
    public UiComponentResult UpdateUiComponentParameters(string? componentId, UiValueEntry[]? parameters)
    {
        return CreatePublicComponentResult(Core.UpdateComponentParameters(componentId, parameters));
    }

    /// <summary>Activates or conditionally deactivates a preserved component subtree.</summary>
    public UiComponentResult SetUiComponentPresence(string? componentId, bool isPresent)
    {
        return CreatePublicComponentResult(Core.SetComponentPresence(componentId, isPresent));
    }

    /// <summary>Provides one copied context value from a component.</summary>
    public UiComponentResult ProvideUiComponentContext(string? componentId, string? contextKey, UiValue? value)
    {
        return CreatePublicComponentResult(Core.ProvideComponentContextValue(componentId, contextKey, value));
    }

    /// <summary>Consumes the nearest copied ancestor context value.</summary>
    public UiComponentResult ConsumeUiComponentContext(string? componentId, string? contextKey)
    {
        return CreatePublicComponentResult(Core.ConsumeComponentContextValue(componentId, contextKey));
    }

    /// <summary>Configures fallback content and a reset key on an error boundary.</summary>
    public UiComponentResult ConfigureUiComponentErrorBoundary(string? componentId, UiNode? fallback, string? resetKey)
    {
        return CreatePublicComponentResult(Core.ConfigureComponentErrorBoundary(componentId, fallback, resetKey));
    }

    /// <summary>Clears boundary error state when its reset key changes.</summary>
    public UiComponentResult ResetUiComponentErrorBoundary(string? componentId, string? resetKey)
    {
        return CreatePublicComponentResult(Core.ResetComponentErrorBoundary(componentId, resetKey));
    }

    /// <summary>Queues one component invalidation.</summary>
    public UiComponentResult InvalidateUiComponent(string? componentId, UiUpdatePriority priority, bool isHydration)
    {
        ComponentScheduleResult scheduled = Scheduler.InvalidateComponent(componentId, priority, isHydration);
        return CreatePublicComponentScheduleResult(scheduled);
    }

    /// <summary>Processes one scheduled update cycle and its renderer and lifecycle tasks.</summary>
    public Task<UiComponentResult> ProcessUiComponentUpdates()
    {
        ComponentScheduleResult cycle = Scheduler.BeginComponentUpdateCycle();
        UiComponentResult result = CreatePublicComponentScheduleResult(cycle);
        if (!cycle.Success)
        {
            return Task.FromResult(result);
        }

        result.RenderRequests = CopyPublicComponentRenderRequests(cycle.Requests);
        ComponentRenderInvocation[] invocations = Core.DrainComponentRenderInvocations();
        return DispatchPublicComponentRenderers(invocations, 0, result);
    }

    /// <summary>Disposes one component subtree and owned resources.</summary>
    public UiComponentResult DisposeUiComponent(string? componentId)
    {
        return CreatePublicComponentResult(Core.DisposeComponentTree(componentId));
    }

    internal Task<UiComponentResult> DispatchPublicComponentRenderers(ComponentRenderInvocation[] invocations, int index, UiComponentResult result)
    {
        if (index >= invocations.Length)
        {
            if (Scheduler.UpdateCycleActive)
            {
                Scheduler.EndComponentUpdateCycle();
            }

            return DispatchPublicComponentLifecycle(result);
        }

        ComponentRenderInvocation invocation = invocations[index];
        int rendererIndex = FindPublicComponentRendererIndex(invocation.HandlerId);
        if (rendererIndex < 0)
        {
            CompletePublicComponentRenderFailure(invocation.Request, result, CreatePublicComponentDiagnostic("component.missing-renderer", "/component/renderers/" + invocation.HandlerId));
            return DispatchPublicComponentRenderers(invocations, index + 1, result);
        }

        IUiComponentRenderer renderer = Renderers[rendererIndex];
        UiComponentRenderRequest request = Core.CopyComponentRenderRequest(invocation.Request);
        Task<UiComponentRenderResult> callback = Task.Run(() => renderer.RenderUiComponent(request));
        return callback.ContinueWith(task => CompletePublicComponentRenderer(invocations, index, result, task)).Unwrap();
    }

    internal Task<UiComponentResult> CompletePublicComponentRenderer(ComponentRenderInvocation[] invocations, int index, UiComponentResult result, Task<UiComponentRenderResult> callback)
    {
        ComponentRenderInvocation invocation = invocations[index];
        if (callback.IsCanceled)
        {
            CompletePublicComponentRenderFailure(invocation.Request, result, CreatePublicComponentDiagnostic("component.renderer-canceled", "/component/renderers/" + invocation.HandlerId));
        }
        else if (callback.IsFaulted)
        {
            CompletePublicComponentRenderFailure(invocation.Request, result, CreatePublicComponentDiagnostic("component.renderer-failed", "/component/renderers/" + invocation.HandlerId));
        }
        else
        {
            UiComponentRenderResult? callbackResult = callback.Result;
            if (callbackResult == null)
            {
                CompletePublicComponentRenderFailure(invocation.Request, result, CreatePublicComponentDiagnostic("component.null-renderer-result", "/component/renderers/" + invocation.HandlerId));
            }
            else
            {
                AddPublicComponentDiagnostics(result, callbackResult.Diagnostics);
                if (!callbackResult.Success)
                {
                    UiDiagnostic diagnostic = FirstPublicComponentDiagnostic(callbackResult.Diagnostics, "component.renderer-rejected", "/component/renderers/" + invocation.HandlerId);
                    CompletePublicComponentRenderFailure(invocation.Request, result, diagnostic);
                }
                else if (callbackResult.Content == null)
                {
                    CompletePublicComponentRenderFailure(invocation.Request, result, CreatePublicComponentDiagnostic("component.null-render-content", "/component/renderers/" + invocation.HandlerId));
                }
                else
                {
                    ComponentOperationResult completed = Scheduler.CompletePendingComponentContent(invocation.Request.Context.ComponentId, invocation.Request.Generation, callbackResult.Content);
                    if (!completed.Success)
                    {
                        result.Success = false;
                        AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic(completed.Code, "/component/render"));
                    }
                    else
                    {
                        result.Applied = true;
                        result.Content = completed.Node == null ? null : Core.CopyComponentNode(completed.Node);
                    }
                }
            }
        }

        return DispatchPublicComponentRenderers(invocations, index + 1, result);
    }

    internal void CompletePublicComponentRenderFailure(UiComponentRenderRequest request, UiComponentResult result, UiDiagnostic diagnostic)
    {
        result.Success = false;
        AddPublicComponentDiagnostic(result, diagnostic);
        ComponentOperationResult routed = Core.ReportComponentError(request.Context.ComponentId, UiLifecycleStage.BeforeRender, diagnostic);
        if (routed.Success && routed.Node != null)
        {
            result.Content = Core.CopyComponentNode(routed.Node);
            result.ErrorBoundary = routed.ErrorBoundary == null ? null : Core.CopyComponentErrorBoundary(routed.ErrorBoundary);
        }
    }

    internal Task<UiComponentResult> DispatchPublicComponentLifecycle(UiComponentResult result)
    {
        ComponentLifecycleInvocation[] invocations = Core.DrainComponentLifecycleInvocations();
        UiLifecycleNotification[] notifications = new UiLifecycleNotification[invocations.Length];
        int index = 0;
        while (index < invocations.Length)
        {
            notifications[index] = CopyPublicLifecycleNotification(invocations[index].Notification);
            index++;
        }

        result.LifecycleNotifications = notifications;
        return DispatchPublicLifecycleHandlers(invocations, 0, result);
    }

    internal Task<UiComponentResult> DispatchPublicLifecycleHandlers(ComponentLifecycleInvocation[] invocations, int index, UiComponentResult result)
    {
        if (index >= invocations.Length)
        {
            return DispatchPublicComponentErrors(result);
        }

        ComponentLifecycleInvocation invocation = invocations[index];
        int handlerIndex = FindPublicLifecycleHandlerIndex(invocation.HandlerId);
        if (handlerIndex < 0)
        {
            result.Success = false;
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic("component.missing-lifecycle-handler", "/component/lifecycle/" + invocation.HandlerId));
            return DispatchPublicLifecycleHandlers(invocations, index + 1, result);
        }

        IUiLifecycleHandler handler = LifecycleHandlers[handlerIndex];
        UiLifecycleNotification notification = CopyPublicLifecycleNotification(invocation.Notification);
        Task<UiLifecycleHandlerResult> callback = Task.Run(() => handler.HandleUiComponentLifecycle(notification));
        return callback.ContinueWith(task => CompletePublicLifecycleHandler(invocations, index, result, task)).Unwrap();
    }

    internal Task<UiComponentResult> CompletePublicLifecycleHandler(ComponentLifecycleInvocation[] invocations, int index, UiComponentResult result, Task<UiLifecycleHandlerResult> callback)
    {
        if (callback.IsCanceled || callback.IsFaulted || callback.Result == null)
        {
            result.Success = false;
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic("component.lifecycle-handler-failed", "/component/lifecycle/" + invocations[index].HandlerId));
        }
        else
        {
            UiLifecycleHandlerResult callbackResult = callback.Result;
            AddPublicComponentDiagnostics(result, callbackResult.Diagnostics);
            if (!callbackResult.Success)
            {
                result.Success = false;
            }
        }

        return DispatchPublicLifecycleHandlers(invocations, index + 1, result);
    }

    internal Task<UiComponentResult> DispatchPublicComponentErrors(UiComponentResult result)
    {
        ComponentErrorInvocation[] errors = Core.DrainComponentErrorInvocations();
        return DispatchPublicErrorHandlers(errors, 0, result);
    }

    internal Task<UiComponentResult> DispatchPublicErrorHandlers(ComponentErrorInvocation[] errors, int index, UiComponentResult result)
    {
        if (index >= errors.Length)
        {
            return Task.FromResult(result);
        }

        ComponentErrorInvocation error = errors[index];
        int handlerIndex = FindPublicLifecycleHandlerIndex(error.HandlerId);
        if (handlerIndex < 0)
        {
            result.Success = false;
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic("component.missing-error-handler", "/component/errors/" + error.HandlerId));
            return DispatchPublicErrorHandlers(errors, index + 1, result);
        }

        UiLifecycleNotification notification = new UiLifecycleNotification();
        notification.ComponentId = error.Boundary.BoundaryId;
        notification.Stage = UiLifecycleStage.Error;
        notification.Diagnostic = error.Boundary.Diagnostic == null ? null : Core.CopyComponentDiagnostic(error.Boundary.Diagnostic);
        IUiLifecycleHandler handler = LifecycleHandlers[handlerIndex];
        Task<UiLifecycleHandlerResult> callback = Task.Run(() => handler.HandleUiComponentLifecycle(notification));
        return callback.ContinueWith(task => CompletePublicErrorHandler(errors, index, result, task)).Unwrap();
    }

    internal Task<UiComponentResult> CompletePublicErrorHandler(ComponentErrorInvocation[] errors, int index, UiComponentResult result, Task<UiLifecycleHandlerResult> callback)
    {
        if (callback.IsCanceled || callback.IsFaulted || callback.Result == null)
        {
            result.Success = false;
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic("component.error-handler-failed", "/component/errors/" + errors[index].HandlerId));
        }
        else
        {
            UiLifecycleHandlerResult callbackResult = callback.Result;
            AddPublicComponentDiagnostics(result, callbackResult.Diagnostics);
            if (!callbackResult.Success)
            {
                result.Success = false;
            }
        }

        return DispatchPublicErrorHandlers(errors, index + 1, result);
    }

    internal int FindPublicComponentRendererIndex(string handlerId)
    {
        int index = 0;
        while (index < RendererIds.Count)
        {
            if (ValueComparer.TextEquals(RendererIds[index], handlerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindPublicLifecycleHandlerIndex(string handlerId)
    {
        int index = 0;
        while (index < LifecycleHandlerIds.Count)
        {
            if (ValueComparer.TextEquals(LifecycleHandlerIds[index], handlerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiComponentResult CreatePublicComponentResult(ComponentOperationResult operation)
    {
        UiComponentResult result = new UiComponentResult();
        result.Success = operation.Success;
        result.Applied = operation.Applied;
        result.Definition = operation.Definition == null ? null : Core.CopyComponentDefinition(operation.Definition);
        result.Context = operation.Context == null ? null : Core.CopyComponentContext(operation.Context);
        result.Content = operation.Node == null ? null : Core.CopyComponentNode(operation.Node);
        result.Value = operation.Value == null ? null : Core.StateStore.CopyStateValue(operation.Value);
        result.ErrorBoundary = operation.ErrorBoundary == null ? null : Core.CopyComponentErrorBoundary(operation.ErrorBoundary);
        if (operation.Diagnostic != null)
        {
            AddPublicComponentDiagnostic(result, Core.CopyComponentDiagnostic(operation.Diagnostic));
        }

        if (!operation.Success)
        {
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic(operation.Code, "/component"));
        }

        return result;
    }

    internal UiComponentResult CreatePublicComponentScheduleResult(ComponentScheduleResult scheduled)
    {
        UiComponentResult result = new UiComponentResult();
        result.Success = scheduled.Success;
        result.Applied = scheduled.Applied;
        result.RenderRequests = CopyPublicComponentRenderRequests(scheduled.Requests);
        if (!scheduled.Success)
        {
            AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic(scheduled.Code, "/component/scheduler"));
        }

        return result;
    }

    internal UiComponentResult CreatePublicComponentFailure(string code)
    {
        UiComponentResult result = new UiComponentResult { Success = false };
        AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic(code, "/component"));
        return result;
    }

    internal UiDiagnostic CreatePublicComponentDiagnostic(string code, string location)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = UiDiagnosticSeverity.Error;
        result.Code = string.IsNullOrWhiteSpace(code) ? "component.operation-failed" : code;
        result.Message = result.Code;
        result.Location = location;
        return result;
    }

    internal UiDiagnostic FirstPublicComponentDiagnostic(UiDiagnostic[]? diagnostics, string code, string location)
    {
        if (diagnostics != null && diagnostics.Length > 0 && diagnostics[0] != null)
        {
            return Core.CopyComponentDiagnostic(diagnostics[0]);
        }

        return CreatePublicComponentDiagnostic(code, location);
    }

    internal void AddPublicComponentDiagnostic(UiComponentResult result, UiDiagnostic diagnostic)
    {
        UiDiagnostic[] diagnostics = new UiDiagnostic[result.Diagnostics.Length + 1];
        int index = 0;
        while (index < result.Diagnostics.Length)
        {
            diagnostics[index] = Core.CopyComponentDiagnostic(result.Diagnostics[index]);
            index++;
        }

        diagnostics[diagnostics.Length - 1] = Core.CopyComponentDiagnostic(diagnostic);
        result.Diagnostics = diagnostics;
    }

    internal void AddPublicComponentDiagnostics(UiComponentResult result, UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] supplied = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < supplied.Length)
        {
            if (supplied[index] == null)
            {
                AddPublicComponentDiagnostic(result, CreatePublicComponentDiagnostic("component.null-handler-diagnostic", "/component/diagnostics"));
                result.Success = false;
            }
            else
            {
                AddPublicComponentDiagnostic(result, supplied[index]);
            }

            index++;
        }
    }

    internal UiLifecycleNotification CopyPublicLifecycleNotification(UiLifecycleNotification source)
    {
        UiLifecycleNotification result = new UiLifecycleNotification();
        result.ComponentId = source.ComponentId;
        result.Stage = source.Stage;
        result.Generation = source.Generation;
        result.Diagnostic = source.Diagnostic == null ? null : Core.CopyComponentDiagnostic(source.Diagnostic);
        return result;
    }

    internal UiComponentRenderRequest[] CopyPublicComponentRenderRequests(UiComponentRenderRequest[] source)
    {
        UiComponentRenderRequest[] result = new UiComponentRenderRequest[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = Core.CopyComponentRenderRequest(source[index]);
            index++;
        }

        return result;
    }
}
