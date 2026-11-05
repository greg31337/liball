using System;
using System.Collections.Generic;

namespace htmlgui;

internal class TabsTemplateExpander
{
    internal bool IsTabsControlName(string name)
    {
        switch (name)
        {
            case "tabs":
            case "tab-view":
            case "stepper":
                return true;
            default:
                return false;
        }
    }

    internal int PrepareTabs(UiNode node)
    {
        UiTabs tabs = RequireTabs(node);
        ValidateTabs(node, tabs);
        return FindSelectedIndex(tabs);
    }

    internal DomNode ExpandTabs(UiNode node, DomNode?[] panels, int selectedIndex)
    {
        UiTabs tabs = RequireTabs(node);
        if (panels.Length != tabs.Tabs.Length)
        {
            throw CreateRenderException(node, "Expanded tab panels must align with tabs.");
        }

        if (node.Name == "stepper")
        {
            return ExpandStepper(node, tabs, panels, selectedIndex);
        }

        return ExpandTabSet(node, tabs, panels, selectedIndex);
    }

    internal UiTabs RequireTabs(UiNode node)
    {
        UiTabs? tabs = node.Tabs;
        if (tabs == null)
        {
            throw CreateRenderException(node, "A tabs control requires a tabs description.");
        }

        if (string.IsNullOrWhiteSpace(tabs.Key))
        {
            throw CreateRenderException(node, "Tab sets require a non-empty stable key.");
        }

        return tabs;
    }

    internal void ValidateTabs(UiNode node, UiTabs tabs)
    {
        if (tabs.Tabs == null)
        {
            throw CreateRenderException(node, "Tabs cannot be null.");
        }

        ValidateOrientation(tabs.Orientation);
        int selectedCount = 0;
        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            UiTab? tab = tabs.Tabs[index];
            if (tab == null || string.IsNullOrWhiteSpace(tab.Key) || string.IsNullOrWhiteSpace(tab.Label))
            {
                throw CreateRenderException(node, "Tabs require stable keys and labels.");
            }

            if (tab.IconKey != null && string.IsNullOrWhiteSpace(tab.IconKey))
            {
                throw CreateRenderException(node, "A supplied tab icon key cannot be empty.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (tabs.Tabs[earlierIndex].Key == tab.Key)
                {
                    throw CreateRenderException(node, "Tab keys must be unique.");
                }

                earlierIndex++;
            }

            if (tab.IsSelected)
            {
                selectedCount++;
            }

            index++;
        }

        if (selectedCount > 1)
        {
            throw CreateRenderException(node, "At most one tab can be selected.");
        }

        if (tabs.ActiveTabKey != null)
        {
            int activeIndex = FindTabIndex(tabs.Tabs, tabs.ActiveTabKey);
            if (activeIndex < 0 || tabs.Tabs[activeIndex].IsDisabled)
            {
                throw CreateRenderException(node, "The active tab key must identify an enabled tab.");
            }

            if (selectedCount == 1 && !tabs.Tabs[activeIndex].IsSelected)
            {
                throw CreateRenderException(node, "Active and selected tab state must agree.");
            }
        }

        int selectedIndex = FindSelectedIndex(tabs);
        if (selectedIndex >= 0 && tabs.Tabs[selectedIndex].IsDisabled)
        {
            throw CreateRenderException(node, "A selected tab cannot be disabled.");
        }
    }

    internal int FindSelectedIndex(UiTabs tabs)
    {
        if (tabs.ActiveTabKey != null)
        {
            return FindTabIndex(tabs.Tabs, tabs.ActiveTabKey);
        }

        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            if (tabs.Tabs[index].IsSelected)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal DomNode ExpandTabSet(UiNode node, UiTabs tabs, DomNode?[] panels, int selectedIndex)
    {
        List<DomAttribute> rootAttributes = new List<DomAttribute>();
        rootAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tabs" });
        rootAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "automatic-activation", Value = FormatBoolean(tabs.AutomaticActivation) });
        rootAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tabs-key", Value = tabs.Key });
        AddNodeState(node, rootAttributes);
        SortAttributes(rootAttributes);
        List<DomNode> children = new List<DomNode>();
        children.Add(CreateTabList(node, tabs, selectedIndex));
        if (tabs.HasOverflow)
        {
            children.Add(CreateOverflowButton(node, tabs));
        }

        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            children.Add(CreatePanel(node, tabs.Tabs[index], panels[index], index == selectedIndex));
            index++;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = rootAttributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTabList(UiNode node, UiTabs tabs, int selectedIndex)
    {
        DomNode[] children = new DomNode[tabs.Tabs.Length];
        int focusIndex = selectedIndex;
        if (focusIndex < 0)
        {
            focusIndex = FindFirstEnabledIndex(tabs.Tabs);
        }

        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            children[index] = CreateTabOwner(node, tabs, tabs.Tabs[index], index == selectedIndex, index == focusIndex);
            index++;
        }

        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-list" });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Orientation, Value = GetOrientationToken(tabs.Orientation) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.TabList });
        if (tabs.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorder", Value = "true" });
        }

        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$tab-list",
            Id = CreateSyntheticId(node, "tab-list", tabs.Key),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTabOwner(UiNode node, UiTabs tabs, UiTab tab, bool selected, bool focused)
    {
        DomNode action = CreateTabAction(node, tabs, tab, selected, focused);
        if (!tab.CanClose)
        {
            return action;
        }

        List<DomAttribute> closeAttributes = new List<DomAttribute>();
        closeAttributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = "Close " + tab.Label });
        closeAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-close" });
        closeAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "close-tab", Value = tab.Key });
        closeAttributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
        closeAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        if (tab.IsDisabled)
        {
            closeAttributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(closeAttributes);
        DomNode close = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$close",
            Id = CreateSyntheticId(node, "close", tab.Key),
            Name = HtmlElements.Button,
            Attributes = closeAttributes.ToArray(),
            Children = new DomNode[] { CreateText(node, "$close-text", "close-text", tab.Key, "×") },
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = tab.Key,
            Id = CreateSyntheticId(node, "owner", tab.Key),
            Name = HtmlElements.Division,
            Attributes = new DomAttribute[]
            {
                new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-owner" },
                new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.None }
            },
            Children = new DomNode[] { action, close },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTabAction(UiNode node, UiTabs tabs, UiTab tab, bool selected, bool focused)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = CreateSyntheticId(node, "panel", tab.Key) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Selected, Value = FormatBoolean(selected) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tab-key", Value = tab.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Tab });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = focused ? "0" : "-1" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        if (tab.IsDisabled || (node.State != null && !node.State.IsEnabled))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        if (tabs.CanReorder)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorderable", Value = "true" });
            attributes.Add(new DomAttribute { Name = "draggable", Value = "true" });
        }

        SortAttributes(attributes);
        List<DomNode> children = new List<DomNode>();
        if (tab.IconKey != null)
        {
            children.Add(CreateIcon(node, tab));
        }

        children.Add(CreateTextElement(node, "$label", "label", tab.Key, tab.Label, "htmlgui-tab-label"));
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = tab.CanClose ? "$tab" : tab.Key,
            Id = CreateSyntheticId(node, "tab", tab.Key),
            Name = HtmlElements.Button,
            Attributes = attributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateIcon(UiNode node, UiTab tab)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-icon" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "icon", Value = tab.IconKey ?? string.Empty });
        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$icon",
            Id = CreateSyntheticId(node, "icon", tab.Key),
            Name = HtmlElements.Span,
            Attributes = attributes.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreatePanel(UiNode node, UiTab tab, DomNode? panel, bool selected)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = CreateSyntheticId(node, "tab", tab.Key) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-panel" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "lazy", Value = FormatBoolean(tab.IsLazy) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "materialized", Value = FormatBoolean(panel != null) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tab-key", Value = tab.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.TabPanel });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "0" });
        if (!selected)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        DomNode[] children = Array.Empty<DomNode>();
        if (panel != null)
        {
            children = new DomNode[] { panel };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$panel-" + tab.Key,
            Id = CreateSyntheticId(node, "panel", tab.Key),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateOverflowButton(UiNode node, UiTabs tabs)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.HasPopup, Value = "menu" });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = "More tabs" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tab-overflow" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "overflow", Value = tabs.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        SortAttributes(attributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$overflow",
            Id = CreateSyntheticId(node, "overflow", tabs.Key),
            Name = HtmlElements.Button,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { CreateText(node, "$overflow-text", "overflow-text", tabs.Key, "More") },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode ExpandStepper(UiNode node, UiTabs tabs, DomNode?[] panels, int selectedIndex)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-stepper" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "stepper-key", Value = tabs.Key });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Orientation, Value = GetOrientationToken(tabs.Orientation) });
        AddNodeState(node, attributes);
        SortAttributes(attributes);
        List<DomNode> children = new List<DomNode>();
        DomNode[] steps = new DomNode[tabs.Tabs.Length];
        int index = 0;
        while (index < tabs.Tabs.Length)
        {
            steps[index] = CreateStep(node, tabs, tabs.Tabs[index], index, selectedIndex);
            index++;
        }

        children.Add(new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$steps",
            Id = CreateSyntheticId(node, "steps", tabs.Key),
            Name = HtmlElements.OrderedList,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-step-list" } },
            Children = steps,
            Version = GetNodeVersion(node)
        });
        index = 0;
        while (index < tabs.Tabs.Length)
        {
            children.Add(CreateStepPanel(node, tabs.Tabs[index], panels[index], index == selectedIndex));
            index++;
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateStep(UiNode node, UiTabs tabs, UiTab tab, int index, int selectedIndex)
    {
        List<DomAttribute> buttonAttributes = new List<DomAttribute>();
        buttonAttributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = CreateSyntheticId(node, "step-panel", tab.Key) });
        buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-step" });
        buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "step-key", Value = tab.Key });
        buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = index == selectedIndex ? "0" : "-1" });
        buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        if (index == selectedIndex)
        {
            buttonAttributes.Add(new DomAttribute { Name = AriaAttributes.Current, Value = "step" });
        }
        else if (selectedIndex >= 0 && index < selectedIndex)
        {
            buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "complete", Value = "true" });
        }

        if (tab.IsDisabled)
        {
            buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        if (tabs.CanReorder)
        {
            buttonAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "reorderable", Value = "true" });
            buttonAttributes.Add(new DomAttribute { Name = "draggable", Value = "true" });
        }

        SortAttributes(buttonAttributes);
        DomNode button = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$step",
            Id = CreateSyntheticId(node, "step", tab.Key),
            Name = HtmlElements.Button,
            Attributes = buttonAttributes.ToArray(),
            Children = new DomNode[] { CreateTextElement(node, "$label", "step-label", tab.Key, tab.Label, "htmlgui-step-label") },
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = tab.Key,
            Id = CreateSyntheticId(node, "step-item", tab.Key),
            Name = HtmlElements.ListItem,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-step-item" } },
            Children = new DomNode[] { button },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateStepPanel(UiNode node, UiTab tab, DomNode? panel, bool selected)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = CreateSyntheticId(node, "step", tab.Key) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-step-panel" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "lazy", Value = FormatBoolean(tab.IsLazy) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "materialized", Value = FormatBoolean(panel != null) });
        if (!selected)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        DomNode[] children = Array.Empty<DomNode>();
        if (panel != null)
        {
            children = new DomNode[] { panel };
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$step-panel-" + tab.Key,
            Id = CreateSyntheticId(node, "step-panel", tab.Key),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal int FindTabIndex(UiTab[] tabs, string key)
    {
        int index = 0;
        while (index < tabs.Length)
        {
            if (tabs[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal int FindFirstEnabledIndex(UiTab[] tabs)
    {
        int index = 0;
        while (index < tabs.Length)
        {
            if (!tabs[index].IsDisabled)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void ValidateOrientation(UiOrientation orientation)
    {
        switch (orientation)
        {
            case UiOrientation.Horizontal:
            case UiOrientation.Vertical:
                return;
            default:
                throw new ArgumentOutOfRangeException("orientation", orientation, "The tab orientation is not supported.");
        }
    }

    internal string GetOrientationToken(UiOrientation orientation)
    {
        ValidateOrientation(orientation);
        if (orientation == UiOrientation.Vertical)
        {
            return "vertical";
        }

        return "horizontal";
    }

    internal void AddNodeState(UiNode node, List<DomAttribute> attributes)
    {
        if (node.State != null && node.State.IsBusy)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (node.State != null && !node.State.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }
    }

    internal DomNode CreateTextElement(UiNode node, string key, string suffix, string identity, string text, string className)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            Children = new DomNode[] { CreateText(node, "$text", suffix + "-text", identity, text) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string identity, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "tabs", suffix, identity });
        return "ui-tabs-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        if (node.Identity.AuthorKey.Length > 0)
        {
            return node.Identity.AuthorKey;
        }

        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null)
        {
            return string.Empty;
        }

        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null)
        {
            return 0;
        }

        return node.State.Version;
    }

    internal void SortAttributes(List<DomAttribute> attributes)
    {
        int index = 1;
        while (index < attributes.Count)
        {
            DomAttribute candidate = attributes[index];
            int insertionIndex = index - 1;
            while (insertionIndex >= 0 && string.CompareOrdinal(attributes[insertionIndex].Name, candidate.Name) > 0)
            {
                attributes[insertionIndex + 1] = attributes[insertionIndex];
                insertionIndex--;
            }

            attributes[insertionIndex + 1] = candidate;
            index++;
        }
    }

    internal UiRenderException CreateRenderException(UiNode node, string message)
    {
        string nodeId = GetNodeId(node);
        if (nodeId.Length == 0)
        {
            nodeId = "unknown";
        }

        return new UiRenderException(message, nodeId, "dom");
    }
}
