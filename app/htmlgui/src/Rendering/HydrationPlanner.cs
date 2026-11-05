using System;
using System.Collections.Generic;

namespace htmlgui;

internal class HydrationPlanner
{
    internal UiHydrationManifest CreateHydrationManifest(UiApplication? application, UiWindow? window, UiPage? page, DomNode[]? roots, string? parityHash)
    {
        if (application == null)
        {
            throw new ArgumentNullException("application");
        }

        if (window == null)
        {
            throw new ArgumentNullException("window");
        }

        if (page == null)
        {
            throw new ArgumentNullException("page");
        }

        if (roots == null)
        {
            throw new ArgumentNullException("roots");
        }

        string checkedHash = Guard.ArgumentNotWhiteSpace(parityHash, "parityHash");
        List<UiValueEntry> nodes = new List<UiValueEntry>();
        List<UiValueEntry> components = new List<UiValueEntry>();
        List<UiValueEntry> events = new List<UiValueEntry>();
        List<UiValueEntry> bindings = new List<UiValueEntry>();
        List<UiValueEntry> styles = new List<UiValueEntry>();
        int rootIndex = 0;
        while (rootIndex < roots.Length)
        {
            DomNode? root = roots[rootIndex];
            if (root == null)
            {
                throw new ArgumentException("Hydration roots cannot contain null.", "roots");
            }

            AddDomHydrationMarkers(root, nodes, components, events, styles);
            rootIndex++;
        }

        AddPageHydrationMarkers(page, components, events, bindings);
        UiView[] overlays = window.Overlays ?? Array.Empty<UiView>();
        int overlayIndex = 0;
        while (overlayIndex < overlays.Length)
        {
            AddViewHydrationMarkers(overlays[overlayIndex], components, events, bindings);
            overlayIndex++;
        }

        UiHydrationManifest result = new UiHydrationManifest();
        result.Hash = checkedHash;
        result.Nodes = nodes.ToArray();
        result.Components = components.ToArray();
        result.Events = events.ToArray();
        result.Bindings = bindings.ToArray();
        result.State = CopyAndSortHydrationMarkers(application.State, "state");
        result.Styles = styles.ToArray();
        ApplyNavigationHydrationMarkers(result, window.NavigationResult);
        return result;
    }

    internal UiHydrationResult PlanHydrationMode(UiHydrationMode mode, bool matched, bool repairable, DomMutation[]? repairMutations, DomMutation[]? replacementMutations, UiDiagnostic[]? diagnostics)
    {
        UiHydrationResult result = new UiHydrationResult();
        result.Mode = mode;
        result.Diagnostics = CopyHydrationDiagnostics(diagnostics);
        if (!IsValidHydrationMode(mode))
        {
            result.Status = UiHydrationStatus.Failed;
            result.Diagnostics = AppendHydrationDiagnostic(result.Diagnostics, "hydration.invalid-mode", "The hydration mode is invalid.");
            return result;
        }

        if (matched)
        {
            result.Status = UiHydrationStatus.Matched;
            return result;
        }

        if (result.Diagnostics.Length == 0)
        {
            result.Diagnostics = AppendHydrationDiagnostic(result.Diagnostics, "hydration.mismatch", "The existing DOM does not match the hydration manifest.");
        }

        switch (mode)
        {
            case UiHydrationMode.Exact:
                result.Status = UiHydrationStatus.Mismatch;
                break;
            case UiHydrationMode.Repair:
                if (repairable)
                {
                    result.Status = UiHydrationStatus.Repaired;
                    result.Mutations = CopyHydrationMutations(repairMutations);
                }
                else
                {
                    result.Status = UiHydrationStatus.Failed;
                    result.Diagnostics = AppendHydrationDiagnostic(result.Diagnostics, "hydration.not-repairable", "The hydration mismatch cannot be repaired safely.");
                }

                break;
            case UiHydrationMode.Replace:
                result.Status = UiHydrationStatus.Replaced;
                result.Mutations = CopyHydrationMutations(replacementMutations);
                break;
        }

        return result;
    }

    internal void AddDomHydrationMarkers(DomNode node, List<UiValueEntry> nodes, List<UiValueEntry> components, List<UiValueEntry> events, List<UiValueEntry> styles)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
        {
            throw new ArgumentException("Hydration nodes require stable identifiers.", "roots");
        }

        UiValue nodeValue = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                CreateHydrationValueEntry("key", CreateHydrationStringValue(node.Key)),
                CreateHydrationValueEntry("kind", CreateHydrationIntegerValue((int)node.Kind)),
                CreateHydrationValueEntry("marker", CreateHydrationStringValue(node.HydrationMarker ?? string.Empty)),
                CreateHydrationValueEntry("name", CreateHydrationStringValue(node.Name)),
                CreateHydrationValueEntry("sequence", CreateHydrationIntegerValue(node.Sequence))
            }
        };
        AddSortedHydrationMarker(nodes, new UiValueEntry { Key = node.Id, Value = nodeValue }, "node");

        if (!string.IsNullOrWhiteSpace(node.HydrationMarker))
        {
            UiValue markerValue = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    CreateHydrationValueEntry("marker", CreateHydrationStringValue(node.HydrationMarker)),
                    CreateHydrationValueEntry("nodeId", CreateHydrationStringValue(node.Id))
                }
            };
            AddSortedHydrationMarker(components, new UiValueEntry { Key = node.Id, Value = markerValue }, "component");
        }

        DomEventSubscription[] subscriptions = node.EventSubscriptions ?? Array.Empty<DomEventSubscription>();
        int eventIndex = 0;
        while (eventIndex < subscriptions.Length)
        {
            DomEventSubscription? subscription = subscriptions[eventIndex];
            if (subscription == null)
            {
                throw new ArgumentException("Hydration event subscriptions cannot contain null.", "roots");
            }

            AddDomEventHydrationMarker(node.Id, subscription, events);
            eventIndex++;
        }

        if (!string.IsNullOrWhiteSpace(node.StyleScope))
        {
            AddSortedHydrationMarker(styles, new UiValueEntry { Key = node.Id, Value = CreateHydrationStringValue(node.StyleScope) }, "style");
        }

        DomNode[] children = node.Children ?? Array.Empty<DomNode>();
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            DomNode? child = children[childIndex];
            if (child == null)
            {
                throw new ArgumentException("Hydration node children cannot contain null.", "roots");
            }

            AddDomHydrationMarkers(child, nodes, components, events, styles);
            childIndex++;
        }
    }

    internal void AddDomEventHydrationMarker(string nodeId, DomEventSubscription subscription, List<UiValueEntry> events)
    {
        if (string.IsNullOrWhiteSpace(subscription.Id))
        {
            throw new ArgumentException("Hydration event subscriptions require stable identifiers.", "roots");
        }

        UiValue value = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                CreateHydrationValueEntry("eventName", CreateHydrationStringValue(subscription.EventName)),
                CreateHydrationValueEntry("handlerId", CreateHydrationStringValue(subscription.HandlerId)),
                CreateHydrationValueEntry("kind", CreateHydrationIntegerValue((int)subscription.Kind)),
                CreateHydrationValueEntry("nodeId", CreateHydrationStringValue(nodeId)),
                CreateHydrationValueEntry("phase", CreateHydrationIntegerValue((int)subscription.Phase))
            }
        };
        AddSortedHydrationMarker(events, new UiValueEntry { Key = subscription.Id, Value = value }, "event");
    }

    internal void AddPageHydrationMarkers(UiPage page, List<UiValueEntry> components, List<UiValueEntry> events, List<UiValueEntry> bindings)
    {
        AddViewHydrationMarkers(page.ShellView, components, events, bindings);
        AddViewHydrationMarkers(page.ContentView, components, events, bindings);
        AddViewHydrationMarkers(page.NotFoundView, components, events, bindings);
        AddViewHydrationMarkers(page.ErrorView, components, events, bindings);
        AddViewHydrationMarkers(page.LoadingView, components, events, bindings);
    }

    internal void AddViewHydrationMarkers(UiView? view, List<UiValueEntry> components, List<UiValueEntry> events, List<UiValueEntry> bindings)
    {
        if (view == null)
        {
            return;
        }

        UiComponentContext? context = view.ComponentContext;
        string componentId = context == null ? view.Id : context.ComponentId;
        string typeId = context == null ? view.ComponentTypeId ?? string.Empty : context.TypeId;
        if (!string.IsNullOrWhiteSpace(componentId) && !string.IsNullOrWhiteSpace(typeId))
        {
            string stateScopeId = context == null ? view.LocalStateScopeId ?? string.Empty : context.StateScopeId;
            UiValue value = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    CreateHydrationValueEntry("nodeId", CreateHydrationStringValue(view.Id)),
                    CreateHydrationValueEntry("stateScopeId", CreateHydrationStringValue(stateScopeId)),
                    CreateHydrationValueEntry("typeId", CreateHydrationStringValue(typeId))
                }
            };
            AddSortedHydrationMarker(components, new UiValueEntry { Key = componentId, Value = value }, "component");
        }

        AddUiNodeHydrationMarkers(view.ContentRoot, components, events, bindings);
    }

    internal void AddUiNodeHydrationMarkers(UiNode? node, List<UiValueEntry> components, List<UiValueEntry> events, List<UiValueEntry> bindings)
    {
        if (node == null)
        {
            return;
        }

        string nodeId = node.Identity == null ? string.Empty : node.Identity.StableId;
        if (node.Kind == UiNodeKind.Component && !string.IsNullOrWhiteSpace(nodeId))
        {
            UiValue componentValue = new UiValue
            {
                Kind = UiValueKind.Object,
                ObjectValue = new UiValueEntry[]
                {
                    CreateHydrationValueEntry("nodeId", CreateHydrationStringValue(nodeId)),
                    CreateHydrationValueEntry("stateScopeId", CreateHydrationStringValue(string.Empty)),
                    CreateHydrationValueEntry("typeId", CreateHydrationStringValue(node.Name))
                }
            };
            AddSortedHydrationMarker(components, new UiValueEntry { Key = nodeId, Value = componentValue }, "component");
        }

        UiEventBinding[] eventBindings = node.EventBindings ?? Array.Empty<UiEventBinding>();
        int eventIndex = 0;
        while (eventIndex < eventBindings.Length)
        {
            UiEventBinding? eventBinding = eventBindings[eventIndex];
            if (eventBinding == null)
            {
                throw new ArgumentException("Hydration event bindings cannot contain null.", "application");
            }

            AddModelEventHydrationMarker(nodeId, eventBinding, events);
            eventIndex++;
        }

        UiBinding[] dataBindings = node.Bindings ?? Array.Empty<UiBinding>();
        int bindingIndex = 0;
        while (bindingIndex < dataBindings.Length)
        {
            UiBinding? binding = dataBindings[bindingIndex];
            if (binding == null)
            {
                throw new ArgumentException("Hydration data bindings cannot contain null.", "application");
            }

            AddBindingHydrationMarker(nodeId, binding, bindings);
            bindingIndex++;
        }

        UiNode[] children = node.Children ?? Array.Empty<UiNode>();
        int childIndex = 0;
        while (childIndex < children.Length)
        {
            AddUiNodeHydrationMarkers(children[childIndex], components, events, bindings);
            childIndex++;
        }

        UiSlot[] slots = node.Slots ?? Array.Empty<UiSlot>();
        int slotIndex = 0;
        while (slotIndex < slots.Length)
        {
            UiSlot? slot = slots[slotIndex];
            if (slot != null)
            {
                UiNode[] content = slot.Content ?? Array.Empty<UiNode>();
                int contentIndex = 0;
                while (contentIndex < content.Length)
                {
                    AddUiNodeHydrationMarkers(content[contentIndex], components, events, bindings);
                    contentIndex++;
                }
            }

            slotIndex++;
        }

        UiConditional? conditional = node.Conditional;
        if (conditional != null)
        {
            UiNode[] selected = conditional.Condition ? conditional.WhenTrue : conditional.WhenFalse;
            int selectedIndex = 0;
            while (selectedIndex < selected.Length)
            {
                AddUiNodeHydrationMarkers(selected[selectedIndex], components, events, bindings);
                selectedIndex++;
            }
        }
    }

    internal void AddModelEventHydrationMarker(string nodeId, UiEventBinding binding, List<UiValueEntry> events)
    {
        if (string.IsNullOrWhiteSpace(binding.Id))
        {
            throw new ArgumentException("Hydration event bindings require stable identifiers.", "application");
        }

        UiValue value = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                CreateHydrationValueEntry("handlerId", CreateHydrationStringValue(binding.HandlerId)),
                CreateHydrationValueEntry("kind", CreateHydrationIntegerValue((int)binding.Kind)),
                CreateHydrationValueEntry("nodeId", CreateHydrationStringValue(nodeId)),
                CreateHydrationValueEntry("phase", CreateHydrationIntegerValue((int)binding.Phase))
            }
        };
        AddSortedHydrationMarker(events, new UiValueEntry { Key = binding.Id, Value = value }, "event");
    }

    internal void AddBindingHydrationMarker(string nodeId, UiBinding binding, List<UiValueEntry> bindings)
    {
        if (string.IsNullOrWhiteSpace(binding.Id))
        {
            throw new ArgumentException("Hydration data bindings require stable identifiers.", "application");
        }

        string targetNodeId = string.IsNullOrWhiteSpace(binding.TargetNodeId) ? nodeId : binding.TargetNodeId;
        UiValue value = new UiValue
        {
            Kind = UiValueKind.Object,
            ObjectValue = new UiValueEntry[]
            {
                CreateHydrationValueEntry("mode", CreateHydrationIntegerValue((int)binding.Mode)),
                CreateHydrationValueEntry("path", CreateHydrationStringValue(binding.Path)),
                CreateHydrationValueEntry("stateId", CreateHydrationStringValue(binding.StateId)),
                CreateHydrationValueEntry("target", CreateHydrationStringValue(binding.Target)),
                CreateHydrationValueEntry("targetNodeId", CreateHydrationStringValue(targetNodeId)),
                CreateHydrationValueEntry("trigger", CreateHydrationIntegerValue((int)binding.Trigger))
            }
        };
        AddSortedHydrationMarker(bindings, new UiValueEntry { Key = binding.Id, Value = value }, "binding");
    }

    internal void AddSortedHydrationMarker(List<UiValueEntry> markers, UiValueEntry marker, string category)
    {
        if (string.IsNullOrWhiteSpace(marker.Key))
        {
            throw new ArgumentException("Hydration " + category + " markers require stable identifiers.", category);
        }

        int index = 0;
        while (index < markers.Count)
        {
            int comparison = string.CompareOrdinal(markers[index].Key, marker.Key);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                markers.Insert(index, marker);
                return;
            }

            index++;
        }

        markers.Add(marker);
    }

    internal UiValueEntry[] CopyAndSortHydrationMarkers(UiValueEntry[]? source, string category)
    {
        UiValueEntry[] values = source ?? Array.Empty<UiValueEntry>();
        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < values.Length)
        {
            UiValueEntry? entry = values[index];
            if (entry == null)
            {
                throw new ArgumentException("Hydration " + category + " markers cannot contain null.", category);
            }

            UiValue copy = ValueJsonCodec.CloneValue(entry.Value, 64, 10000, 1048576, 1048576);
            AddSortedHydrationMarker(result, new UiValueEntry { Key = entry.Key, Value = copy }, category);
            index++;
        }

        return result.ToArray();
    }

    internal void ApplyNavigationHydrationMarkers(UiHydrationManifest result, UiNavigationResult? navigation)
    {
        if (navigation == null)
        {
            return;
        }

        result.RouteUrl = navigation.CanonicalUrl.Length == 0 ? null : navigation.CanonicalUrl;
        object? routeCopy = new ModelNormalizer().CopyValue(navigation.RouteData);
        result.RouteData = routeCopy == null ? null : (UiRouteData)routeCopy;
        result.HistoryState = navigation.State == null ? null : ValueJsonCodec.CloneValue(navigation.State, 64, 10000, 1048576, 1048576);
        if (navigation.Generation > 0)
        {
            result.NavigationGeneration = navigation.Generation;
        }

        UiHistoryEntry[] history = navigation.History ?? Array.Empty<UiHistoryEntry>();
        if (navigation.HistoryIndex >= 0 && navigation.HistoryIndex < history.Length)
        {
            result.HistoryEntryId = history[navigation.HistoryIndex].Id;
        }
    }

    internal UiValueEntry CreateHydrationValueEntry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    internal UiValue CreateHydrationStringValue(string? value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value ?? string.Empty };
    }

    internal UiValue CreateHydrationIntegerValue(int value)
    {
        return new UiValue { Kind = UiValueKind.Integer, IntegerValue = value };
    }

    internal bool IsValidHydrationMode(UiHydrationMode mode)
    {
        return mode == UiHydrationMode.Exact || mode == UiHydrationMode.Repair || mode == UiHydrationMode.Replace;
    }

    internal DomMutation[] CopyHydrationMutations(DomMutation[]? source)
    {
        DomMutation[] values = source ?? Array.Empty<DomMutation>();
        object? copied = new ModelNormalizer().CopyValue(values);
        if (copied == null)
        {
            return Array.Empty<DomMutation>();
        }

        return (DomMutation[])copied;
    }

    internal UiDiagnostic[] CopyHydrationDiagnostics(UiDiagnostic[]? source)
    {
        UiDiagnostic[] values = source ?? Array.Empty<UiDiagnostic>();
        UiDiagnostic[] result = new UiDiagnostic[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            UiDiagnostic? diagnostic = values[index];
            if (diagnostic == null)
            {
                throw new ArgumentException("Hydration diagnostics cannot contain null.", "diagnostics");
            }

            result[index] = new UiDiagnostic
            {
                Severity = diagnostic.Severity,
                Code = diagnostic.Code,
                Message = diagnostic.Message,
                Location = diagnostic.Location
            };
            index++;
        }

        return result;
    }

    internal UiDiagnostic[] AppendHydrationDiagnostic(UiDiagnostic[] source, string code, string message)
    {
        UiDiagnostic[] result = new UiDiagnostic[source.Length + 1];
        Array.Copy(source, result, source.Length);
        result[source.Length] = new UiDiagnostic
        {
            Severity = UiDiagnosticSeverity.Error,
            Code = code,
            Message = message,
            Location = "/hydration"
        };
        return result;
    }
}
