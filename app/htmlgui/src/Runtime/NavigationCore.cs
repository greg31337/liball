using System;
using System.Collections.Generic;

namespace htmlgui;

internal class NavigationLinkRequest
{
    internal string Url = string.Empty;
    internal UiValue? State;
    internal int Generation;
    internal int Button;
    internal bool HasModifier;
    internal bool HasDownload;
    internal string? Target;
    internal bool MoveFocus = true;
}

internal class NavigationCoreResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal bool Intercepted;
    internal bool HistoryChanged;
    internal bool ReplaceHistory;
    internal bool Reload;
    internal int HistoryIndex = -1;
    internal UiHistoryEntry[] History = Array.Empty<UiHistoryEntry>();
    internal RouteResolutionResult? Resolution;
    internal UiNavigationResult Navigation = new UiNavigationResult();
}

internal class NavigationCore
{
    internal const int MaximumRedirectCount = 16;

    internal RouteResolverCore Resolver = new RouteResolverCore();
    internal UiRoute[] Routes = Array.Empty<UiRoute>();
    internal UiPage[] Pages = Array.Empty<UiPage>();
    internal UiDocumentMetadata? Document;
    internal string BasePath = "/";
    internal bool AllowExternalNavigation;
    internal UiHistoryEntry[] History = Array.Empty<UiHistoryEntry>();
    internal int HistoryIndex = -1;
    internal int NextHistoryIdentifier = 1;

    internal NavigationCoreResult InitializeNavigationHistory(string? url, UiValue? state, int generation)
    {
        if (History.Length != 0 || HistoryIndex != -1)
        {
            return CreateNavigationFailure("navigation.already-initialized", generation);
        }

        UiNavigationRequest request = new UiNavigationRequest();
        request.Kind = UiNavigationKind.Replace;
        request.Url = url ?? string.Empty;
        request.State = state;
        request.Generation = generation;
        request.MoveFocus = false;
        return ExecuteNavigation(request);
    }

    internal void CaptureNavigationRestoration(double scrollX, double scrollY, string? focusedNodeId)
    {
        if (HistoryIndex < 0 || HistoryIndex >= History.Length || double.IsNaN(scrollX) || double.IsInfinity(scrollX) || double.IsNaN(scrollY) || double.IsInfinity(scrollY))
        {
            return;
        }

        UiHistoryEntry current = History[HistoryIndex];
        current.HasScrollOffset = true;
        current.ScrollX = scrollX;
        current.ScrollY = scrollY;
        current.FocusedNodeId = string.IsNullOrWhiteSpace(focusedNodeId) ? null : focusedNodeId;
    }

    internal NavigationCoreResult ExecuteNavigation(UiNavigationRequest? request)
    {
        if (request == null)
        {
            return CreateNavigationFailure("navigation.null-request", 0);
        }

        if (request.Generation < 0)
        {
            return CreateNavigationFailure("navigation.invalid-generation", request.Generation);
        }

        switch (request.Kind)
        {
            case UiNavigationKind.Back:
                return MoveNavigationHistory(-1, request);
            case UiNavigationKind.Forward:
                return MoveNavigationHistory(1, request);
            case UiNavigationKind.Reload:
                return ReloadNavigationHistory(request);
            case UiNavigationKind.Push:
            case UiNavigationKind.Replace:
            case UiNavigationKind.Link:
            case UiNavigationKind.Fragment:
            case UiNavigationKind.External:
                return NavigateToRequestedLocation(request);
            default:
                return CreateNavigationFailure("navigation.invalid-kind", request.Generation);
        }
    }

    internal NavigationCoreResult ExecuteLinkNavigation(NavigationLinkRequest? link)
    {
        if (link == null)
        {
            return CreateNavigationFailure("navigation.null-link", 0);
        }

        if (link.Button != 0 || link.HasModifier || link.HasDownload || !IsSelfNavigationTarget(link.Target))
        {
            NavigationCoreResult ignored = CreateNavigationCancellation("navigation.link-not-intercepted", link.Generation);
            ignored.Intercepted = false;
            return SnapshotNavigationResult(ignored);
        }

        UiNavigationRequest request = new UiNavigationRequest();
        request.Kind = UiNavigationKind.Link;
        request.Url = link.Url;
        request.State = link.State;
        request.Generation = link.Generation;
        request.MoveFocus = link.MoveFocus;
        NavigationCoreResult result = ExecuteNavigation(request);
        result.Intercepted = result.Navigation.Status != UiNavigationStatus.External;
        return result;
    }

    internal NavigationCoreResult NavigateToRequestedLocation(UiNavigationRequest request)
    {
        if (request.Url == null || request.Url.Length == 0)
        {
            return CreateNavigationFailure("navigation.invalid-url", request.Generation);
        }

        string destination = ExpandSameDocumentReference(request.Url);
        RouteResolutionResult resolved = ResolveNavigationDestination(destination);
        if (!resolved.Success)
        {
            return CreateNavigationFailure(resolved.Code, request.Generation);
        }

        if (request.Kind == UiNavigationKind.External && resolved.Status != UiNavigationStatus.External)
        {
            return CreateNavigationFailure("navigation.expected-external-url", request.Generation);
        }

        if (resolved.Status == UiNavigationStatus.External)
        {
            return CompleteExternalNavigation(resolved, request);
        }

        if (request.Kind == UiNavigationKind.Fragment && !IsSameDocumentDestination(resolved.CanonicalUrl))
        {
            return CreateNavigationFailure("navigation.fragment-cross-document", request.Generation);
        }

        bool replace = request.Kind == UiNavigationKind.Replace;
        StoreNavigationEntry(resolved.CanonicalUrl, request.State, replace);
        NavigationCoreResult result = CompleteInternalNavigation(resolved, request, false);
        result.HistoryChanged = true;
        result.ReplaceHistory = replace;
        return SnapshotNavigationResult(result);
    }

    internal NavigationCoreResult MoveNavigationHistory(int offset, UiNavigationRequest request)
    {
        int targetIndex = HistoryIndex + offset;
        if (targetIndex < 0 || targetIndex >= History.Length)
        {
            return SnapshotNavigationResult(CreateNavigationCancellation("navigation.history-boundary", request.Generation));
        }

        UiHistoryEntry target = History[targetIndex];
        RouteResolutionResult resolved = ResolveNavigationDestination(target.Url);
        if (!resolved.Success)
        {
            return CreateNavigationFailure(resolved.Code, request.Generation);
        }

        if (resolved.Status == UiNavigationStatus.External)
        {
            return CreateNavigationFailure("navigation.external-history-entry", request.Generation);
        }

        HistoryIndex = targetIndex;
        NavigationCoreResult result = CompleteInternalNavigation(resolved, request, true);
        result.Navigation.State = CopyNavigationValue(target.State);
        ApplyRestorationIntents(result.Navigation, target, request.MoveFocus);
        return SnapshotNavigationResult(result);
    }

    internal NavigationCoreResult ReloadNavigationHistory(UiNavigationRequest request)
    {
        if (HistoryIndex < 0 || HistoryIndex >= History.Length)
        {
            return CreateNavigationFailure("navigation.history-empty", request.Generation);
        }

        UiHistoryEntry current = History[HistoryIndex];
        RouteResolutionResult resolved = ResolveNavigationDestination(current.Url);
        if (!resolved.Success)
        {
            return CreateNavigationFailure(resolved.Code, request.Generation);
        }

        NavigationCoreResult result = CompleteInternalNavigation(resolved, request, true);
        result.Reload = true;
        result.Navigation.State = CopyNavigationValue(current.State);
        ApplyRestorationIntents(result.Navigation, current, request.MoveFocus);
        return SnapshotNavigationResult(result);
    }

    internal RouteResolutionResult ResolveNavigationDestination(string url)
    {
        List<string> visited = new List<string>();
        string current = url;
        int redirectCount = 0;
        while (redirectCount <= MaximumRedirectCount)
        {
            RouteResolutionResult resolved = Resolver.ResolveRouteTree(Routes, Pages, Document, current, BasePath);
            if (!resolved.Success || resolved.Status != UiNavigationStatus.Redirected)
            {
                return resolved;
            }

            if (resolved.RedirectUrl == null || ContainsNavigationUrl(visited, resolved.RedirectUrl))
            {
                return CreateRouteFailure("navigation.redirect-loop");
            }

            visited.Add(resolved.CanonicalUrl);
            current = resolved.RedirectUrl;
            redirectCount++;
        }

        return CreateRouteFailure("navigation.too-many-redirects");
    }

    internal NavigationCoreResult CompleteExternalNavigation(RouteResolutionResult resolved, UiNavigationRequest request)
    {
        if (!AllowExternalNavigation)
        {
            return CreateNavigationFailure("navigation.external-blocked", request.Generation);
        }

        NavigationCoreResult result = new NavigationCoreResult();
        result.Success = true;
        result.Resolution = resolved;
        result.Navigation.Status = UiNavigationStatus.External;
        result.Navigation.CanonicalUrl = resolved.CanonicalUrl;
        result.Navigation.ExternalUrl = resolved.CanonicalUrl;
        result.Navigation.Generation = request.Generation;
        return SnapshotNavigationResult(result);
    }

    internal NavigationCoreResult CompleteInternalNavigation(RouteResolutionResult resolved, UiNavigationRequest request, bool restoring)
    {
        NavigationCoreResult result = new NavigationCoreResult();
        result.Success = true;
        result.Resolution = resolved;
        result.Navigation.Status = resolved.Status;
        result.Navigation.RouteData = CopyRouteData(resolved.RouteData);
        result.Navigation.CanonicalUrl = resolved.CanonicalUrl;
        result.Navigation.State = CopyNavigationValue(request.State);
        result.Navigation.Generation = request.Generation;
        if (!restoring)
        {
            ApplyNewNavigationIntents(result.Navigation, request.MoveFocus);
        }

        return result;
    }

    internal void ApplyNewNavigationIntents(UiNavigationResult navigation, bool moveFocus)
    {
        string? fragment = navigation.RouteData == null ? null : navigation.RouteData.Fragment;
        navigation.FragmentTargetId = string.IsNullOrEmpty(fragment) ? null : fragment;
        navigation.ScrollToTop = navigation.FragmentTargetId == null;
        navigation.MoveFocus = moveFocus;
        navigation.FocusTargetId = moveFocus ? navigation.FragmentTargetId : null;
    }

    internal void ApplyRestorationIntents(UiNavigationResult navigation, UiHistoryEntry entry, bool moveFocus)
    {
        navigation.FragmentTargetId = navigation.RouteData == null || string.IsNullOrEmpty(navigation.RouteData.Fragment) ? null : navigation.RouteData.Fragment;
        navigation.HasScrollOffset = entry.HasScrollOffset;
        navigation.ScrollX = entry.ScrollX;
        navigation.ScrollY = entry.ScrollY;
        navigation.ScrollToTop = !entry.HasScrollOffset && navigation.FragmentTargetId == null;
        navigation.MoveFocus = moveFocus;
        navigation.RestoreFocus = moveFocus && !string.IsNullOrWhiteSpace(entry.FocusedNodeId);
        navigation.FocusTargetId = navigation.RestoreFocus ? entry.FocusedNodeId : (moveFocus ? navigation.FragmentTargetId : null);
    }

    internal void StoreNavigationEntry(string canonicalUrl, UiValue? state, bool replace)
    {
        UiHistoryEntry entry = new UiHistoryEntry();
        entry.Url = canonicalUrl;
        entry.State = CopyNavigationValue(state);
        if (replace && HistoryIndex >= 0 && HistoryIndex < History.Length)
        {
            entry.Id = History[HistoryIndex].Id;
            UiHistoryEntry[] replaced = CopyNavigationHistory(History);
            replaced[HistoryIndex] = entry;
            History = replaced;
            return;
        }

        entry.Id = "history-" + InvariantFormatter.FormatInt32(NextHistoryIdentifier);
        NextHistoryIdentifier++;
        int retainedCount = HistoryIndex + 1;
        UiHistoryEntry[] pushed = new UiHistoryEntry[retainedCount + 1];
        int index = 0;
        while (index < retainedCount)
        {
            pushed[index] = CopyNavigationHistoryEntry(History[index]);
            index++;
        }

        pushed[retainedCount] = entry;
        History = pushed;
        HistoryIndex = retainedCount;
    }

    internal string ExpandSameDocumentReference(string url)
    {
        if ((url[0] != '#' && url[0] != '?') || HistoryIndex < 0 || HistoryIndex >= History.Length)
        {
            return url;
        }

        string current = History[HistoryIndex].Url;
        int fragmentIndex = current.IndexOf('#');
        if (fragmentIndex >= 0)
        {
            current = current.Substring(0, fragmentIndex);
        }

        if (url[0] == '#')
        {
            return current + url;
        }

        int queryIndex = current.IndexOf('?');
        if (queryIndex >= 0)
        {
            current = current.Substring(0, queryIndex);
        }

        return current + url;
    }

    internal bool IsSameDocumentDestination(string canonicalUrl)
    {
        if (HistoryIndex < 0 || HistoryIndex >= History.Length)
        {
            return false;
        }

        return ValueComparer.TextEquals(RemoveNavigationFragment(History[HistoryIndex].Url), RemoveNavigationFragment(canonicalUrl));
    }

    internal string RemoveNavigationFragment(string url)
    {
        int index = url.IndexOf('#');
        return index < 0 ? url : url.Substring(0, index);
    }

    internal bool IsSelfNavigationTarget(string? target)
    {
        return string.IsNullOrEmpty(target) || ValueComparer.TextEquals(target, "_self");
    }

    internal bool ContainsNavigationUrl(List<string> urls, string value)
    {
        int index = 0;
        while (index < urls.Count)
        {
            if (ValueComparer.TextEquals(urls[index], value))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal NavigationCoreResult SnapshotNavigationResult(NavigationCoreResult result)
    {
        result.History = CopyNavigationHistory(History);
        result.HistoryIndex = HistoryIndex;
        return result;
    }

    internal NavigationCoreResult CreateNavigationFailure(string code, int generation)
    {
        NavigationCoreResult result = new NavigationCoreResult();
        result.Code = code;
        result.Navigation.Status = UiNavigationStatus.Failed;
        result.Navigation.Generation = generation;
        result.Navigation.Diagnostic = new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = "Navigation could not be completed."
        };
        return SnapshotNavigationResult(result);
    }

    internal NavigationCoreResult CreateNavigationCancellation(string code, int generation)
    {
        NavigationCoreResult result = new NavigationCoreResult();
        result.Success = true;
        result.Code = code;
        result.Navigation.Status = UiNavigationStatus.Cancelled;
        result.Navigation.Generation = generation;
        return result;
    }

    internal RouteResolutionResult CreateRouteFailure(string code)
    {
        RouteResolutionResult result = new RouteResolutionResult();
        result.Code = code;
        result.Status = UiNavigationStatus.Failed;
        return result;
    }

    internal UiHistoryEntry[] CopyNavigationHistory(UiHistoryEntry[] source)
    {
        UiHistoryEntry[] result = new UiHistoryEntry[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            result[index] = CopyNavigationHistoryEntry(source[index]);
            index++;
        }

        return result;
    }

    internal UiHistoryEntry CopyNavigationHistoryEntry(UiHistoryEntry source)
    {
        UiHistoryEntry result = new UiHistoryEntry();
        result.Id = source.Id;
        result.Url = source.Url;
        result.State = CopyNavigationValue(source.State);
        result.HasScrollOffset = source.HasScrollOffset;
        result.ScrollX = source.ScrollX;
        result.ScrollY = source.ScrollY;
        result.FocusedNodeId = source.FocusedNodeId;
        return result;
    }

    internal UiRouteData? CopyRouteData(UiRouteData? source)
    {
        if (source == null)
        {
            return null;
        }

        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiRouteData)copy;
    }

    internal UiValue? CopyNavigationValue(UiValue? source)
    {
        if (source == null)
        {
            return null;
        }

        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiValue)copy;
    }
}
