using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace htmlgui;

/// <summary>Provides route registration, exceptionless resolution, guarded navigation, history, and callbacks.</summary>
public class UiRouter : IUiRouter
{
    internal NavigationFlowCore Flow;
    internal List<UiRoute> Routes;
    internal List<string> GuardHandlerIds;
    internal List<IUiNavigationObserver> GuardHandlers;
    internal List<string> ObserverHandlerIds;
    internal List<IUiNavigationObserver> ObserverHandlers;

    /// <summary>Initializes an empty router using the root base path.</summary>
    public UiRouter()
    {
        Flow = new NavigationFlowCore();
        Routes = new List<UiRoute>();
        GuardHandlerIds = new List<string>();
        GuardHandlers = new List<IUiNavigationObserver>();
        ObserverHandlerIds = new List<string>();
        ObserverHandlers = new List<IUiNavigationObserver>();
    }

    /// <summary>Configures copied page metadata, the base path, and external-navigation policy.</summary>
    public UiNavigationResult ConfigureUiRouting(UiPage[]? pages, UiDocumentMetadata? document, string? basePath, bool allowExternalNavigation)
    {
        if (pages == null)
        {
            return CreatePublicNavigationFailure("navigation.null-pages", 0);
        }

        RouteUrlResult normalizedBase = Flow.Navigator.Resolver.UrlCore.CanonicalizeRouteBasePath(basePath);
        if (!normalizedBase.Success)
        {
            return CreatePublicNavigationFailure(normalizedBase.Code, 0);
        }

        UiPage[]? copiedPages = CopyPublicNavigationPages(pages);
        if (copiedPages == null)
        {
            return CreatePublicNavigationFailure("navigation.invalid-pages", 0);
        }

        Flow.Navigator.Pages = copiedPages;
        Flow.Navigator.Document = CopyPublicNavigationMetadata(document);
        Flow.Navigator.BasePath = normalizedBase.BasePath;
        Flow.Navigator.AllowExternalNavigation = allowExternalNavigation;
        return CreatePublicNavigationSuccess(true, 0);
    }

    /// <summary>Registers one copied route.</summary>
    public UiNavigationResult RegisterUiRoute(UiRoute? route)
    {
        if (route == null)
        {
            return CreatePublicNavigationFailure("route.null-route", 0);
        }

        UiRoute? copied = CopyPublicNavigationRoute(route);
        if (copied == null)
        {
            return CreatePublicNavigationFailure("route.invalid-route", 0);
        }

        UiRoute[] candidate = new UiRoute[Routes.Count + 1];
        int index = 0;
        while (index < Routes.Count)
        {
            candidate[index] = Routes[index];
            index++;
        }

        candidate[Routes.Count] = copied;
        RouteDefinitionResult validation = Flow.Navigator.Resolver.PrepareRouteDefinitions(candidate);
        if (!validation.Success)
        {
            return CreatePublicNavigationFailure(validation.Code, 0);
        }

        Routes.Add(copied);
        RefreshPublicNavigationRoutes();
        return CreatePublicNavigationSuccess(true, 0);
    }

    /// <summary>Unregisters one route that has no registered children.</summary>
    public UiNavigationResult UnregisterUiRoute(string? routeId)
    {
        if (string.IsNullOrWhiteSpace(routeId))
        {
            return CreatePublicNavigationFailure("route.invalid-id", 0);
        }

        int routeIndex = FindPublicNavigationRouteIndex(routeId);
        if (routeIndex < 0)
        {
            return CreatePublicNavigationFailure("route.unknown-route", 0);
        }

        int index = 0;
        while (index < Routes.Count)
        {
            if (ValueComparer.TextEquals(Routes[index].ParentId, routeId))
            {
                return CreatePublicNavigationFailure("route.has-children", 0);
            }

            index++;
        }

        Routes.RemoveAt(routeIndex);
        RefreshPublicNavigationRoutes();
        return CreatePublicNavigationSuccess(true, 0);
    }

    /// <summary>Resolves a route without changing history or invoking callbacks.</summary>
    public UiNavigationResult ResolveUiRoute(string? url)
    {
        RouteResolutionResult resolution = Flow.Navigator.Resolver.ResolveRouteTree(Flow.Navigator.Routes, Flow.Navigator.Pages, Flow.Navigator.Document, url, Flow.Navigator.BasePath);
        return CreatePublicRouteResolution(resolution);
    }

    /// <summary>Initializes history from the browser's current deep link.</summary>
    public UiNavigationResult InitializeUiNavigation(string? url, UiValue? state, int generation)
    {
        NavigationCoreResult navigation = Flow.Navigator.InitializeNavigationHistory(url, state, generation);
        return CreatePublicNavigationCoreResult(navigation);
    }

    /// <summary>Runs one guarded navigation and its ordered callbacks.</summary>
    public Task<UiNavigationResult> NavigateUi(UiNavigationRequest? request)
    {
        PublicNavigationDispatchState state = new PublicNavigationDispatchState();
        state.Flow = Flow.BeginNavigationFlow(request);
        return DispatchPublicNavigationCycle(state);
    }

    /// <summary>Cancels the active navigation generation and delivers cancellation callbacks.</summary>
    public Task<UiNavigationResult> CancelUiNavigation(int generation)
    {
        PublicNavigationDispatchState state = new PublicNavigationDispatchState();
        state.Flow = Flow.CancelNavigationFlow(generation);
        return DispatchPublicNavigationCycle(state);
    }

    /// <summary>Captures the current history entry's scroll and focus restoration state.</summary>
    public UiNavigationResult CaptureUiNavigationRestoration(double scrollX, double scrollY, string? focusedNodeId)
    {
        if (Flow.Navigator.HistoryIndex < 0 || double.IsNaN(scrollX) || double.IsInfinity(scrollX) || double.IsNaN(scrollY) || double.IsInfinity(scrollY))
        {
            return CreatePublicNavigationFailure("navigation.invalid-restoration", Flow.Generation);
        }

        Flow.Navigator.CaptureNavigationRestoration(scrollX, scrollY, focusedNodeId);
        UiNavigationResult result = CreatePublicNavigationSuccess(true, Flow.Generation);
        result.History = Flow.Navigator.CopyNavigationHistory(Flow.Navigator.History);
        result.HistoryIndex = Flow.Navigator.HistoryIndex;
        result.CanonicalUrl = result.History[result.HistoryIndex].Url;
        return result;
    }

    /// <summary>Registers a cancellable navigation guard.</summary>
    public UiNavigationResult RegisterUiNavigationGuard(string? handlerId, IUiNavigationObserver? observer)
    {
        if (observer == null)
        {
            return CreatePublicNavigationFailure("navigation.null-guard", Flow.Generation);
        }

        NavigationRegistrationResult registered = Flow.RegisterNavigationGuard(handlerId);
        if (!registered.Success || handlerId == null)
        {
            return CreatePublicNavigationFailure(registered.Code, Flow.Generation);
        }

        GuardHandlerIds.Add(handlerId);
        GuardHandlers.Add(observer);
        return CreatePublicNavigationSuccess(true, Flow.Generation);
    }

    /// <summary>Unregisters a navigation guard.</summary>
    public UiNavigationResult UnregisterUiNavigationGuard(string? handlerId)
    {
        NavigationRegistrationResult removed = Flow.UnregisterNavigationGuard(handlerId);
        if (!removed.Success)
        {
            return CreatePublicNavigationFailure(removed.Code, Flow.Generation);
        }

        int index = FindPublicNavigationHandlerIndex(GuardHandlerIds, handlerId);
        if (index >= 0)
        {
            GuardHandlerIds.RemoveAt(index);
            GuardHandlers.RemoveAt(index);
        }

        return CreatePublicNavigationSuccess(true, Flow.Generation);
    }

    /// <summary>Registers a navigation lifecycle observer.</summary>
    public UiNavigationResult RegisterUiNavigationObserver(string? handlerId, IUiNavigationObserver? observer)
    {
        if (observer == null)
        {
            return CreatePublicNavigationFailure("navigation.null-observer", Flow.Generation);
        }

        NavigationRegistrationResult registered = Flow.RegisterNavigationObserver(handlerId);
        if (!registered.Success || handlerId == null)
        {
            return CreatePublicNavigationFailure(registered.Code, Flow.Generation);
        }

        ObserverHandlerIds.Add(handlerId);
        ObserverHandlers.Add(observer);
        return CreatePublicNavigationSuccess(true, Flow.Generation);
    }

    /// <summary>Unregisters a navigation lifecycle observer.</summary>
    public UiNavigationResult UnregisterUiNavigationObserver(string? handlerId)
    {
        NavigationRegistrationResult removed = Flow.UnregisterNavigationObserver(handlerId);
        if (!removed.Success)
        {
            return CreatePublicNavigationFailure(removed.Code, Flow.Generation);
        }

        int index = FindPublicNavigationHandlerIndex(ObserverHandlerIds, handlerId);
        if (index >= 0)
        {
            ObserverHandlerIds.RemoveAt(index);
            ObserverHandlers.RemoveAt(index);
        }

        return CreatePublicNavigationSuccess(true, Flow.Generation);
    }

    internal Task<UiNavigationResult> DispatchPublicNavigationCycle(PublicNavigationDispatchState state)
    {
        NavigationObserverInvocation[] observers = Flow.DrainNavigationObserverInvocations();
        return DispatchPublicNavigationObservers(state, observers, 0).ContinueWith(task => ContinuePublicNavigationCycle(state, task)).Unwrap();
    }

    internal Task<bool> DispatchPublicNavigationObservers(PublicNavigationDispatchState state, NavigationObserverInvocation[] invocations, int index)
    {
        if (index >= invocations.Length)
        {
            return Task.FromResult(true);
        }

        NavigationObserverInvocation invocation = invocations[index];
        int handlerIndex = FindPublicNavigationHandlerIndex(ObserverHandlerIds, invocation.HandlerId);
        if (handlerIndex < 0)
        {
            state.ObserverFailed = true;
            state.Diagnostics.Add(CreatePublicNavigationDiagnostic("navigation.missing-observer", "/navigation/observers/" + invocation.HandlerId));
            return DispatchPublicNavigationObservers(state, invocations, index + 1);
        }

        UiNavigationNotification notification = CreatePublicNavigationNotification(invocation);
        Task<UiNavigationObserverResult> callback = Task.Run(() => ObserverHandlers[handlerIndex].HandleUiNavigation(notification));
        return callback.ContinueWith(task => CompletePublicNavigationObserver(state, invocations, index, task)).Unwrap();
    }

    internal Task<bool> CompletePublicNavigationObserver(PublicNavigationDispatchState state, NavigationObserverInvocation[] invocations, int index, Task<UiNavigationObserverResult> callback)
    {
        if (callback.IsCanceled)
        {
            state.ObserverFailed = true;
            state.Diagnostics.Add(CreatePublicNavigationDiagnostic("navigation.observer-cancelled", "/navigation/observers/" + invocations[index].HandlerId));
        }
        else if (callback.IsFaulted)
        {
            state.ObserverFailed = true;
            state.Diagnostics.Add(CreatePublicNavigationDiagnostic("navigation.observer-failed", "/navigation/observers/" + invocations[index].HandlerId));
        }
        else
        {
            UiNavigationObserverResult? result = callback.Result;
            if (result == null)
            {
                state.ObserverFailed = true;
                state.Diagnostics.Add(CreatePublicNavigationDiagnostic("navigation.null-observer-result", "/navigation/observers/" + invocations[index].HandlerId));
            }
            else
            {
                AddPublicNavigationDiagnostics(state.Diagnostics, result.Diagnostics);
                if (!result.Success)
                {
                    state.ObserverFailed = true;
                }
            }
        }

        return DispatchPublicNavigationObservers(state, invocations, index + 1);
    }

    internal Task<UiNavigationResult> ContinuePublicNavigationCycle(PublicNavigationDispatchState state, Task<bool> observerDispatch)
    {
        NavigationGuardInvocation[] guards = Flow.DrainNavigationGuardInvocations();
        if (guards.Length > 0)
        {
            return DispatchPublicNavigationGuard(state, guards[0]);
        }

        if (state.Flow.IsPending)
        {
            state.Flow = Flow.CancelNavigationFlow(state.Flow.Generation);
            state.Diagnostics.Add(CreatePublicNavigationDiagnostic("navigation.missing-guard-invocation", "/navigation/guards"));
            state.ObserverFailed = true;
            return DispatchPublicNavigationCycle(state);
        }

        UiNavigationResult result = CreatePublicNavigationFlowResult(state.Flow);
        AddPublicNavigationDiagnosticsToResult(result, state.Diagnostics);
        if (state.ObserverFailed)
        {
            result.Success = false;
        }

        return Task.FromResult(result);
    }

    internal Task<UiNavigationResult> DispatchPublicNavigationGuard(PublicNavigationDispatchState state, NavigationGuardInvocation invocation)
    {
        int handlerIndex = FindPublicNavigationHandlerIndex(GuardHandlerIds, invocation.HandlerId);
        if (handlerIndex < 0)
        {
            state.Flow = Flow.CompleteNavigationGuard(invocation.HandlerId, invocation.Generation, false, false, "navigation.missing-guard");
            return DispatchPublicNavigationCycle(state);
        }

        UiNavigationNotification notification = CreatePublicNavigationGuardNotification(invocation);
        Task<UiNavigationObserverResult> callback = Task.Run(() => GuardHandlers[handlerIndex].HandleUiNavigation(notification));
        return callback.ContinueWith(task => CompletePublicNavigationGuard(state, invocation, task)).Unwrap();
    }

    internal Task<UiNavigationResult> CompletePublicNavigationGuard(PublicNavigationDispatchState state, NavigationGuardInvocation invocation, Task<UiNavigationObserverResult> callback)
    {
        bool callbackSucceeded = false;
        bool allowNavigation = false;
        string? errorCode = null;
        if (callback.IsCanceled)
        {
            errorCode = "navigation.guard-callback-cancelled";
        }
        else if (callback.IsFaulted)
        {
            errorCode = "navigation.guard-callback-failed";
        }
        else
        {
            UiNavigationObserverResult? result = callback.Result;
            if (result == null)
            {
                errorCode = "navigation.null-guard-result";
            }
            else
            {
                AddPublicNavigationDiagnostics(state.Diagnostics, result.Diagnostics);
                callbackSucceeded = result.Success;
                allowNavigation = result.AllowNavigation;
                if (!result.Success)
                {
                    errorCode = GetPublicNavigationDiagnosticCode(result.Diagnostics, "navigation.guard-rejected");
                }
            }
        }

        state.Flow = Flow.CompleteNavigationGuard(invocation.HandlerId, invocation.Generation, callbackSucceeded, allowNavigation, errorCode);
        return DispatchPublicNavigationCycle(state);
    }

    internal UiNavigationResult CreatePublicNavigationFlowResult(NavigationFlowResult flow)
    {
        UiNavigationResult result;
        if (flow.Navigation == null)
        {
            result = flow.Success ? CreatePublicNavigationSuccess(flow.Applied, flow.Generation) : CreatePublicNavigationFailure(flow.Code, flow.Generation);
        }
        else
        {
            result = CreatePublicNavigationCoreResult(flow.Navigation);
        }

        result.Applied = flow.Applied;
        result.IsPending = flow.IsPending;
        result.IsStale = flow.IsStale;
        result.Generation = flow.Generation;
        if (flow.Code.Length > 0 && result.Diagnostic == null && !flow.IsStale)
        {
            AddPublicNavigationDiagnosticsToResult(result, new List<UiDiagnostic> { CreatePublicNavigationDiagnostic(flow.Code, "/navigation") });
        }

        return result;
    }

    internal UiNavigationResult CreatePublicNavigationCoreResult(NavigationCoreResult navigation)
    {
        UiNavigationResult result = CopyPublicNavigationResult(navigation.Navigation);
        result.Success = navigation.Success;
        result.Applied = navigation.HistoryChanged || navigation.ReplaceHistory || navigation.Reload;
        result.History = Flow.Navigator.CopyNavigationHistory(navigation.History);
        result.HistoryIndex = navigation.HistoryIndex;
        ApplyPublicRouteResolution(result, navigation.Resolution);
        if (navigation.Code.Length > 0 && result.Diagnostic == null)
        {
            AddPublicNavigationDiagnosticsToResult(result, new List<UiDiagnostic> { CreatePublicNavigationDiagnostic(navigation.Code, "/navigation") });
        }

        return result;
    }

    internal UiNavigationResult CreatePublicRouteResolution(RouteResolutionResult resolution)
    {
        UiNavigationResult result = new UiNavigationResult();
        result.Success = resolution.Success;
        result.Status = resolution.Status;
        result.CanonicalUrl = resolution.CanonicalUrl;
        result.RedirectUrl = resolution.RedirectUrl;
        result.RouteData = CopyPublicNavigationRouteData(resolution.RouteData);
        ApplyPublicRouteResolution(result, resolution);
        if (!resolution.Success)
        {
            AddPublicNavigationDiagnosticsToResult(result, new List<UiDiagnostic> { CreatePublicNavigationDiagnostic(resolution.Code, "/routes") });
        }

        return result;
    }

    internal void ApplyPublicRouteResolution(UiNavigationResult result, RouteResolutionResult? resolution)
    {
        if (resolution == null)
        {
            return;
        }

        result.View = CopyPublicNavigationView(resolution.View);
        result.Page = CopyPublicNavigationPage(resolution.Page);
        result.Title = resolution.Title;
        result.Metadata = CopyPublicNavigationMetadata(resolution.Metadata);
        if (result.RouteData == null)
        {
            result.RouteData = CopyPublicNavigationRouteData(resolution.RouteData);
        }
    }

    internal void RefreshPublicNavigationRoutes()
    {
        Flow.Navigator.Routes = Routes.ToArray();
    }

    internal int FindPublicNavigationRouteIndex(string? routeId)
    {
        int index = 0;
        while (index < Routes.Count)
        {
            if (ValueComparer.TextEquals(Routes[index].Id, routeId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindPublicNavigationHandlerIndex(List<string> identifiers, string? handlerId)
    {
        int index = 0;
        while (index < identifiers.Count)
        {
            if (ValueComparer.TextEquals(identifiers[index], handlerId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal UiNavigationNotification CreatePublicNavigationNotification(NavigationObserverInvocation invocation)
    {
        UiNavigationNotification result = new UiNavigationNotification();
        result.Stage = invocation.Stage;
        result.Generation = invocation.Generation;
        result.Code = invocation.Code.Length == 0 ? null : invocation.Code;
        result.Request = Flow.CopyNavigationRequest(invocation.Request);
        result.Navigation = CopyPublicNavigationResult(invocation.Navigation);
        return result;
    }

    internal UiNavigationNotification CreatePublicNavigationGuardNotification(NavigationGuardInvocation invocation)
    {
        UiNavigationNotification result = new UiNavigationNotification();
        result.Stage = NavigationFlowCore.BeforeStage;
        result.Generation = invocation.Generation;
        result.Request = Flow.CopyNavigationRequest(invocation.Request);
        result.Navigation.Status = UiNavigationStatus.Pending;
        result.Navigation.IsPending = true;
        result.Navigation.Generation = invocation.Generation;
        return result;
    }

    internal UiNavigationResult CreatePublicNavigationSuccess(bool applied, int generation)
    {
        UiNavigationResult result = new UiNavigationResult();
        result.Success = true;
        result.Applied = applied;
        result.Status = UiNavigationStatus.Resolved;
        result.Generation = generation;
        return result;
    }

    internal UiNavigationResult CreatePublicNavigationFailure(string code, int generation)
    {
        UiNavigationResult result = new UiNavigationResult();
        result.Status = UiNavigationStatus.Failed;
        result.Generation = generation;
        AddPublicNavigationDiagnosticsToResult(result, new List<UiDiagnostic> { CreatePublicNavigationDiagnostic(code, "/navigation") });
        return result;
    }

    internal UiDiagnostic CreatePublicNavigationDiagnostic(string code, string location)
    {
        UiDiagnostic result = new UiDiagnostic();
        result.Severity = UiDiagnosticSeverity.Error;
        result.Code = code;
        result.Message = "Navigation could not be completed.";
        result.Location = location;
        return result;
    }

    internal void AddPublicNavigationDiagnostics(List<UiDiagnostic> target, UiDiagnostic[]? diagnostics)
    {
        UiDiagnostic[] source = diagnostics ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < source.Length)
        {
            UiDiagnostic? diagnostic = source[index];
            if (diagnostic != null)
            {
                target.Add(CopyPublicNavigationDiagnostic(diagnostic));
            }

            index++;
        }
    }

    internal void AddPublicNavigationDiagnosticsToResult(UiNavigationResult result, List<UiDiagnostic> diagnostics)
    {
        List<UiDiagnostic> combined = new List<UiDiagnostic>();
        if (result.Diagnostics.Length == 0 && result.Diagnostic != null)
        {
            combined.Add(CopyPublicNavigationDiagnostic(result.Diagnostic));
        }
        else
        {
            AddPublicNavigationDiagnostics(combined, result.Diagnostics);
        }

        int index = 0;
        while (index < diagnostics.Count)
        {
            combined.Add(CopyPublicNavigationDiagnostic(diagnostics[index]));
            index++;
        }

        result.Diagnostics = combined.ToArray();
        result.Diagnostic = result.Diagnostics.Length == 0 ? null : CopyPublicNavigationDiagnostic(result.Diagnostics[0]);
    }

    internal string GetPublicNavigationDiagnosticCode(UiDiagnostic[]? diagnostics, string fallback)
    {
        if (diagnostics != null && diagnostics.Length > 0 && diagnostics[0] != null && !string.IsNullOrWhiteSpace(diagnostics[0].Code))
        {
            return diagnostics[0].Code;
        }

        return fallback;
    }

    internal UiNavigationResult CopyPublicNavigationResult(UiNavigationResult source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? new UiNavigationResult() : (UiNavigationResult)copy;
    }

    internal UiRoute? CopyPublicNavigationRoute(UiRoute? source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiRoute)copy;
    }

    internal UiRouteData? CopyPublicNavigationRouteData(UiRouteData? source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiRouteData)copy;
    }

    internal UiPage[]? CopyPublicNavigationPages(UiPage[] source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiPage[])copy;
    }

    internal UiPage? CopyPublicNavigationPage(UiPage? source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiPage)copy;
    }

    internal UiView? CopyPublicNavigationView(UiView? source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiView)copy;
    }

    internal UiDocumentMetadata? CopyPublicNavigationMetadata(UiDocumentMetadata? source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiDocumentMetadata)copy;
    }

    internal UiDiagnostic CopyPublicNavigationDiagnostic(UiDiagnostic source)
    {
        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? new UiDiagnostic() : (UiDiagnostic)copy;
    }
}
