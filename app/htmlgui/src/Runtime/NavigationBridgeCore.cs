using System;
using System.Collections.Generic;

namespace htmlgui;

internal class NavigationBridgePayloadResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal string Operation = string.Empty;
    internal string Url = string.Empty;
    internal int Generation;
    internal int HistoryIndex = -1;
    internal UiValue? State;
    internal UiHistoryEntry[] History = Array.Empty<UiHistoryEntry>();
}

internal class NavigationBridgeResult
{
    internal bool Success;
    internal bool Applied;
    internal bool IsStale;
    internal bool ReplacedHistory;
    internal string Code = string.Empty;
    internal UiBridgeEnvelope? Envelope;
    internal NavigationCoreResult? Navigation;
    internal UiHydrationManifest? Hydration;
}

internal class NavigationBridgeCore
{
    internal const string InitialOperation = "initial";
    internal const string PushOperation = "push";
    internal const string ReplaceOperation = "replace";
    internal const string BackOperation = "back";
    internal const string ForwardOperation = "forward";
    internal const string ReloadOperation = "reload";
    internal const string LinkOperation = "link";
    internal const string FragmentOperation = "fragment";
    internal const string ExternalOperation = "external";
    internal const string PopOperation = "pop";
    internal const string ReconnectOperation = "reconnect";

    internal NavigationCore Navigator = new NavigationCore();
    internal string Session = string.Empty;
    internal int OutboundSequence;
    internal int LastBrowserGeneration = -1;

    internal NavigationBridgeResult CreateBrowserHistoryMessage(NavigationCoreResult? navigation, UiNavigationKind kind)
    {
        if (navigation == null)
        {
            return CreateNavigationBridgeFailure("navigation.bridge-null-navigation");
        }

        if (!navigation.Success)
        {
            return CreateNavigationBridgeFailure("navigation.bridge-failed-navigation");
        }

        if (string.IsNullOrWhiteSpace(Session))
        {
            return CreateNavigationBridgeFailure("navigation.bridge-invalid-session");
        }

        if (OutboundSequence == int.MaxValue)
        {
            return CreateNavigationBridgeFailure("navigation.bridge-sequence-exhausted");
        }

        OutboundSequence++;
        UiBridgeEnvelope envelope = new UiBridgeEnvelope();
        envelope.Version = BridgeProtocolVersions.Current;
        envelope.Session = Session;
        envelope.Sequence = OutboundSequence;
        envelope.Origin = BridgeProtocolNames.HostOrigin;
        envelope.Kind = UiBridgeMessageKind.Navigation;
        envelope.Payload = CreateBrowserHistoryPayload(navigation, kind);
        NavigationBridgeResult result = new NavigationBridgeResult();
        result.Success = true;
        result.Envelope = envelope;
        result.Navigation = navigation;
        return result;
    }

    internal NavigationBridgeResult ResolveInitialDeepLink(UiBridgeEnvelope? envelope, UiHydrationManifest? manifest)
    {
        NavigationBridgePayloadResult payload = ParseBrowserNavigationPayload(envelope, InitialOperation, false);
        if (!payload.Success)
        {
            return CreateNavigationBridgeFailure(payload.Code);
        }

        if (LastBrowserGeneration >= 0 && payload.Generation < LastBrowserGeneration)
        {
            return CreateNavigationBridgeStale(payload.Generation);
        }

        NavigationCoreResult navigation = Navigator.InitializeNavigationHistory(payload.Url, payload.State, payload.Generation);
        if (!navigation.Success)
        {
            return CreateNavigationBridgeFailure(navigation.Code);
        }

        LastBrowserGeneration = payload.Generation;
        NavigationBridgeResult result = new NavigationBridgeResult();
        result.Success = true;
        result.Applied = true;
        result.Navigation = navigation;
        result.Hydration = CreateRouteHydrationHandoff(navigation, manifest);
        return result;
    }

    internal NavigationBridgeResult ReconcileBrowserHistory(UiBridgeEnvelope? envelope)
    {
        NavigationBridgePayloadResult payload = ParseBrowserNavigationPayload(envelope, ReconnectOperation, true);
        if (!payload.Success)
        {
            NavigationBridgePayloadResult popPayload = ParseBrowserNavigationPayload(envelope, PopOperation, true);
            if (!popPayload.Success)
            {
                return CreateNavigationBridgeFailure(payload.Code);
            }

            payload = popPayload;
        }

        if (payload.Generation < LastBrowserGeneration)
        {
            return CreateNavigationBridgeStale(payload.Generation);
        }

        NavigationBridgePayloadResult validated = ValidateBrowserHistory(payload);
        if (!validated.Success)
        {
            return CreateNavigationBridgeFailure(validated.Code);
        }

        bool replaced = !NavigationHistoriesEqual(Navigator.History, Navigator.HistoryIndex, validated.History, validated.HistoryIndex);
        Navigator.History = Navigator.CopyNavigationHistory(validated.History);
        Navigator.HistoryIndex = validated.HistoryIndex;
        UiNavigationRequest reload = new UiNavigationRequest();
        reload.Kind = UiNavigationKind.Reload;
        reload.Generation = validated.Generation;
        reload.MoveFocus = true;
        NavigationCoreResult navigation = Navigator.ExecuteNavigation(reload);
        if (!navigation.Success)
        {
            return CreateNavigationBridgeFailure(navigation.Code);
        }

        LastBrowserGeneration = validated.Generation;
        NavigationBridgeResult result = new NavigationBridgeResult();
        result.Success = true;
        result.Applied = replaced;
        result.ReplacedHistory = replaced;
        result.Navigation = navigation;
        return result;
    }

    internal UiHydrationManifest CreateRouteHydrationHandoff(NavigationCoreResult navigation, UiHydrationManifest? manifest)
    {
        UiHydrationManifest result;
        if (manifest == null)
        {
            result = new UiHydrationManifest();
        }
        else
        {
            object? copiedManifest = new ModelNormalizer().CopyValue(manifest);
            result = copiedManifest == null ? new UiHydrationManifest() : (UiHydrationManifest)copiedManifest;
        }

        result.RouteUrl = navigation.Navigation.CanonicalUrl;
        result.RouteData = CopyHydrationRouteData(navigation.Navigation.RouteData);
        result.HistoryState = Navigator.CopyNavigationValue(navigation.Navigation.State);
        result.NavigationGeneration = navigation.Navigation.Generation;
        if (navigation.HistoryIndex >= 0 && navigation.HistoryIndex < navigation.History.Length)
        {
            result.HistoryEntryId = navigation.History[navigation.HistoryIndex].Id;
        }

        return result;
    }

    internal UiValue CreateBrowserHistoryPayload(NavigationCoreResult navigation, UiNavigationKind kind)
    {
        UiNavigationResult destination = navigation.Navigation;
        List<UiValueEntry> entries = new List<UiValueEntry>();
        entries.Add(CreateBridgeTextEntry("operation", GetBrowserHistoryOperation(kind)));
        entries.Add(CreateBridgeTextEntry("url", destination.CanonicalUrl));
        entries.Add(CreateBridgeIntegerEntry("generation", destination.Generation));
        entries.Add(CreateBridgeIntegerEntry("historyIndex", navigation.HistoryIndex));
        entries.Add(CreateBridgeValueEntry("state", destination.State));
        entries.Add(CreateBridgeValueEntry("history", CreateBridgeHistoryValue(navigation.History)));
        entries.Add(CreateBridgeBooleanEntry("scrollToTop", destination.ScrollToTop));
        entries.Add(CreateBridgeBooleanEntry("hasScrollOffset", destination.HasScrollOffset));
        entries.Add(CreateBridgeFloatingPointEntry("scrollX", destination.ScrollX));
        entries.Add(CreateBridgeFloatingPointEntry("scrollY", destination.ScrollY));
        entries.Add(CreateBridgeBooleanEntry("moveFocus", destination.MoveFocus));
        entries.Add(CreateBridgeBooleanEntry("restoreFocus", destination.RestoreFocus));
        entries.Add(CreateBridgeTextEntry("focusTargetId", destination.FocusTargetId));
        entries.Add(CreateBridgeTextEntry("fragmentTargetId", destination.FragmentTargetId));
        entries.Add(CreateBridgeTextEntry("externalUrl", destination.ExternalUrl));
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries.ToArray() };
    }

    internal NavigationBridgePayloadResult ParseBrowserNavigationPayload(UiBridgeEnvelope? envelope, string expectedOperation, bool requireHistory)
    {
        if (envelope == null || envelope.Kind != UiBridgeMessageKind.Navigation || !ValueComparer.TextEquals(envelope.Origin, BridgeProtocolNames.BrowserOrigin))
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-envelope");
        }

        if (envelope.Payload == null || envelope.Payload.Kind != UiValueKind.Object || envelope.Payload.ObjectValue == null)
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-payload");
        }

        UiValueEntry[] entries = envelope.Payload.ObjectValue;
        UiValue? operationValue = FindBridgePayloadValue(entries, "operation");
        UiValue? urlValue = FindBridgePayloadValue(entries, "url");
        UiValue? generationValue = FindBridgePayloadValue(entries, "generation");
        if (operationValue == null || operationValue.Kind != UiValueKind.String || !ValueComparer.TextEquals(operationValue.StringValue, expectedOperation))
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-operation");
        }

        if (urlValue == null || urlValue.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(urlValue.StringValue))
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-url");
        }

        if (generationValue == null || generationValue.Kind != UiValueKind.Integer || generationValue.IntegerValue < 0 || generationValue.IntegerValue > int.MaxValue)
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-generation");
        }

        NavigationBridgePayloadResult result = new NavigationBridgePayloadResult();
        result.Success = true;
        result.Operation = expectedOperation;
        result.Url = urlValue.StringValue ?? string.Empty;
        result.Generation = (int)generationValue.IntegerValue;
        UiValue? state = FindBridgePayloadValue(entries, "state");
        result.State = state == null || state.Kind == UiValueKind.Null ? null : Navigator.CopyNavigationValue(state);
        if (!requireHistory)
        {
            return result;
        }

        UiValue? historyIndexValue = FindBridgePayloadValue(entries, "historyIndex");
        UiValue? historyValue = FindBridgePayloadValue(entries, "history");
        if (historyIndexValue == null || historyIndexValue.Kind != UiValueKind.Integer || historyIndexValue.IntegerValue < 0 || historyIndexValue.IntegerValue > int.MaxValue)
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-index");
        }

        if (historyValue == null || historyValue.Kind != UiValueKind.Array || historyValue.ArrayValue == null)
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-history");
        }

        result.HistoryIndex = (int)historyIndexValue.IntegerValue;
        NavigationBridgePayloadResult history = ParseBrowserHistoryEntries(historyValue.ArrayValue);
        if (!history.Success)
        {
            return history;
        }

        result.History = history.History;
        return result;
    }

    internal NavigationBridgePayloadResult ParseBrowserHistoryEntries(UiValue[] values)
    {
        UiHistoryEntry[] history = new UiHistoryEntry[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            UiValue value = values[index];
            if (value == null || value.Kind != UiValueKind.Object || value.ObjectValue == null)
            {
                return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-entry");
            }

            UiValueEntry[] entries = value.ObjectValue;
            UiValue? id = FindBridgePayloadValue(entries, "id");
            UiValue? url = FindBridgePayloadValue(entries, "url");
            UiValue? hasScroll = FindBridgePayloadValue(entries, "hasScrollOffset");
            UiValue? scrollX = FindBridgePayloadValue(entries, "scrollX");
            UiValue? scrollY = FindBridgePayloadValue(entries, "scrollY");
            if (id == null || id.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(id.StringValue) || url == null || url.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(url.StringValue) || hasScroll == null || hasScroll.Kind != UiValueKind.Boolean || !IsBridgeNumber(scrollX) || !IsBridgeNumber(scrollY))
            {
                return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-entry");
            }

            UiHistoryEntry entry = new UiHistoryEntry();
            entry.Id = id.StringValue ?? string.Empty;
            entry.Url = url.StringValue ?? string.Empty;
            UiValue? state = FindBridgePayloadValue(entries, "state");
            entry.State = state == null || state.Kind == UiValueKind.Null ? null : Navigator.CopyNavigationValue(state);
            entry.HasScrollOffset = hasScroll.BooleanValue;
            entry.ScrollX = ReadBridgeNumber(scrollX);
            entry.ScrollY = ReadBridgeNumber(scrollY);
            UiValue? focused = FindBridgePayloadValue(entries, "focusedNodeId");
            if (focused != null && focused.Kind != UiValueKind.Null)
            {
                if (focused.Kind != UiValueKind.String || string.IsNullOrWhiteSpace(focused.StringValue))
                {
                    return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-entry");
                }

                entry.FocusedNodeId = focused.StringValue;
            }

            history[index] = entry;
            index++;
        }

        NavigationBridgePayloadResult result = new NavigationBridgePayloadResult();
        result.Success = true;
        result.History = history;
        return result;
    }

    internal NavigationBridgePayloadResult ValidateBrowserHistory(NavigationBridgePayloadResult payload)
    {
        if (payload.History.Length == 0 || payload.HistoryIndex < 0 || payload.HistoryIndex >= payload.History.Length)
        {
            return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-index");
        }

        UiHistoryEntry[] canonical = new UiHistoryEntry[payload.History.Length];
        int index = 0;
        while (index < payload.History.Length)
        {
            UiHistoryEntry entry = payload.History[index];
            if (ContainsHistoryIdentifier(canonical, index, entry.Id) || double.IsNaN(entry.ScrollX) || double.IsInfinity(entry.ScrollX) || double.IsNaN(entry.ScrollY) || double.IsInfinity(entry.ScrollY))
            {
                return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-entry");
            }

            RouteResolutionResult resolved = Navigator.ResolveNavigationDestination(entry.Url);
            if (!resolved.Success || resolved.Status == UiNavigationStatus.External)
            {
                return CreateNavigationPayloadFailure("navigation.bridge-invalid-history-url");
            }

            UiHistoryEntry copied = Navigator.CopyNavigationHistoryEntry(entry);
            copied.Url = resolved.CanonicalUrl;
            canonical[index] = copied;
            index++;
        }

        if (!ValueComparer.TextEquals(canonical[payload.HistoryIndex].Url, Navigator.Resolver.UrlCore.ParseRouteUrl(payload.Url, Navigator.BasePath).CanonicalUrl))
        {
            return CreateNavigationPayloadFailure("navigation.bridge-current-url-mismatch");
        }

        NavigationBridgePayloadResult result = new NavigationBridgePayloadResult();
        result.Success = true;
        result.Operation = payload.Operation;
        result.Url = canonical[payload.HistoryIndex].Url;
        result.Generation = payload.Generation;
        result.HistoryIndex = payload.HistoryIndex;
        result.State = Navigator.CopyNavigationValue(canonical[payload.HistoryIndex].State);
        result.History = canonical;
        return result;
    }

    internal bool NavigationHistoriesEqual(UiHistoryEntry[] left, int leftIndex, UiHistoryEntry[] right, int rightIndex)
    {
        if (leftIndex != rightIndex || left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            UiHistoryEntry first = left[index];
            UiHistoryEntry second = right[index];
            if (!ValueComparer.TextEquals(first.Id, second.Id) || !ValueComparer.TextEquals(first.Url, second.Url) || first.HasScrollOffset != second.HasScrollOffset || first.ScrollX != second.ScrollX || first.ScrollY != second.ScrollY || !ValueComparer.TextEquals(first.FocusedNodeId, second.FocusedNodeId) || !NavigationValuesEqual(first.State, second.State))
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal bool NavigationValuesEqual(UiValue? left, UiValue? right)
    {
        if (left == null || right == null)
        {
            return left == null && right == null;
        }

        return ValueJsonCodec.ValuesEqual(left, right, 64, 10000, 1048576, 1048576);
    }

    internal string GetBrowserHistoryOperation(UiNavigationKind kind)
    {
        switch (kind)
        {
            case UiNavigationKind.Push:
                return PushOperation;
            case UiNavigationKind.Replace:
                return ReplaceOperation;
            case UiNavigationKind.Back:
                return BackOperation;
            case UiNavigationKind.Forward:
                return ForwardOperation;
            case UiNavigationKind.Reload:
                return ReloadOperation;
            case UiNavigationKind.Link:
                return LinkOperation;
            case UiNavigationKind.Fragment:
                return FragmentOperation;
            case UiNavigationKind.External:
                return ExternalOperation;
            default:
                return string.Empty;
        }
    }

    internal UiValue CreateBridgeHistoryValue(UiHistoryEntry[] history)
    {
        UiValue[] values = new UiValue[history.Length];
        int index = 0;
        while (index < history.Length)
        {
            UiHistoryEntry entry = history[index];
            values[index] = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    CreateBridgeTextEntry("id", entry.Id),
                    CreateBridgeTextEntry("url", entry.Url),
                    CreateBridgeValueEntry("state", entry.State),
                    CreateBridgeBooleanEntry("hasScrollOffset", entry.HasScrollOffset),
                    CreateBridgeFloatingPointEntry("scrollX", entry.ScrollX),
                    CreateBridgeFloatingPointEntry("scrollY", entry.ScrollY),
                    CreateBridgeTextEntry("focusedNodeId", entry.FocusedNodeId)
                }
            };
            index++;
        }

        return new UiValue { Kind = UiValueKind.Array, ArrayValue = values };
    }

    internal UiValueEntry CreateBridgeTextEntry(string key, string? value)
    {
        UiValue item = value == null ? new UiValue { Kind = UiValueKind.Null } : new UiValue { Kind = UiValueKind.String, StringValue = value };
        return new UiValueEntry { Key = key, Value = item };
    }

    internal UiValueEntry CreateBridgeIntegerEntry(string key, int value)
    {
        return new UiValueEntry { Key = key, Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = value } };
    }

    internal UiValueEntry CreateBridgeBooleanEntry(string key, bool value)
    {
        return new UiValueEntry { Key = key, Value = new UiValue { Kind = UiValueKind.Boolean, BooleanValue = value } };
    }

    internal UiValueEntry CreateBridgeFloatingPointEntry(string key, double value)
    {
        return new UiValueEntry { Key = key, Value = new UiValue { Kind = UiValueKind.FloatingPoint, FloatingPointValue = value } };
    }

    internal UiValueEntry CreateBridgeValueEntry(string key, UiValue? value)
    {
        return new UiValueEntry { Key = key, Value = value == null ? new UiValue { Kind = UiValueKind.Null } : Navigator.CopyNavigationValue(value) ?? new UiValue { Kind = UiValueKind.Null } };
    }

    internal UiValue? FindBridgePayloadValue(UiValueEntry[] entries, string key)
    {
        int index = 0;
        while (index < entries.Length)
        {
            UiValueEntry? entry = entries[index];
            if (entry != null && ValueComparer.TextEquals(entry.Key, key))
            {
                return entry.Value;
            }

            index++;
        }

        return null;
    }

    internal bool IsBridgeNumber(UiValue? value)
    {
        return value != null && (value.Kind == UiValueKind.Integer || value.Kind == UiValueKind.FloatingPoint);
    }

    internal double ReadBridgeNumber(UiValue? value)
    {
        if (value == null)
        {
            return 0;
        }

        return value.Kind == UiValueKind.Integer ? value.IntegerValue : value.FloatingPointValue;
    }

    internal bool ContainsHistoryIdentifier(UiHistoryEntry[] history, int count, string id)
    {
        int index = 0;
        while (index < count)
        {
            if (ValueComparer.TextEquals(history[index].Id, id))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiRouteData? CopyHydrationRouteData(UiRouteData? source)
    {
        if (source == null)
        {
            return null;
        }

        object? copy = new ModelNormalizer().CopyValue(source);
        return copy == null ? null : (UiRouteData)copy;
    }

    internal NavigationBridgeResult CreateNavigationBridgeFailure(string code)
    {
        NavigationBridgeResult result = new NavigationBridgeResult();
        result.Code = code;
        return result;
    }

    internal NavigationBridgeResult CreateNavigationBridgeStale(int generation)
    {
        NavigationBridgeResult result = new NavigationBridgeResult();
        result.Success = true;
        result.IsStale = true;
        result.Code = "navigation.bridge-stale-generation";
        return result;
    }

    internal NavigationBridgePayloadResult CreateNavigationPayloadFailure(string code)
    {
        NavigationBridgePayloadResult result = new NavigationBridgePayloadResult();
        result.Code = code;
        return result;
    }
}
