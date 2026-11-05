using System;
using System.Collections.Generic;

namespace htmlgui;

internal class NavigationTemplateExpander
{
    internal bool IsToolbarControlName(string name)
    {
        return name == "toolbar" || name == "tool-bar";
    }

    internal bool IsNavigationControlName(string name)
    {
        return name == "breadcrumb" || name == "breadcrumbs" || name == "navigation-rail" || name == "rail" || name == "navigation-sidebar" || name == "sidebar";
    }

    internal bool IsPaginationControlName(string name)
    {
        return name == "pagination" || name == "pager";
    }

    internal DomNode ExpandToolbar(UiNode node)
    {
        UiToolbar? toolbar = node.Toolbar;
        if (toolbar == null || string.IsNullOrWhiteSpace(toolbar.Key) || toolbar.Tools == null)
        {
            throw CreateRenderException(node, "Toolbars require a key and tool collection.");
        }

        ValidateOrientation(toolbar.Orientation);
        ValidateTools(node, toolbar);
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-toolbar" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "toolbar-key", Value = toolbar.Key });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Orientation, Value = GetOrientationToken(toolbar.Orientation) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ToolBar });
        if (toolbar.Label != null && toolbar.Label.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = toolbar.Label });
        }

        AddNodeState(node, attributes);
        SortAttributes(attributes);
        List<DomNode> children = CreateToolbarChildren(node, toolbar);
        if (toolbar.HasOverflow)
        {
            children.Add(CreateOverflowTool(node, toolbar));
        }

        return CreateRoot(node, attributes, children.ToArray());
    }

    internal void ValidateTools(UiNode node, UiToolbar toolbar)
    {
        int index = 0;
        while (index < toolbar.Tools.Length)
        {
            UiTool? tool = toolbar.Tools[index];
            if (tool == null || string.IsNullOrWhiteSpace(tool.Key) || tool.Label == null || string.IsNullOrWhiteSpace(tool.Kind))
            {
                throw CreateRenderException(node, "Toolbar tools require keys, kinds, and labels.");
            }

            ValidateToolKind(tool.Kind);
            if (tool.CommandId != null && string.IsNullOrWhiteSpace(tool.CommandId))
            {
                throw CreateRenderException(node, "A supplied toolbar command identifier cannot be empty.");
            }

            if (tool.GroupKey != null && string.IsNullOrWhiteSpace(tool.GroupKey))
            {
                throw CreateRenderException(node, "A supplied toolbar group key cannot be empty.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (toolbar.Tools[earlierIndex].Key == tool.Key)
                {
                    throw CreateRenderException(node, "Toolbar tool keys must be unique.");
                }

                if (tool.GroupKey != null && toolbar.Tools[earlierIndex].GroupKey == tool.GroupKey && earlierIndex + 1 < index && toolbar.Tools[earlierIndex + 1].GroupKey != tool.GroupKey)
                {
                    throw CreateRenderException(node, "Toolbar groups must be contiguous.");
                }

                earlierIndex++;
            }

            ValidateToolPayload(node, tool);
            index++;
        }

        if (toolbar.ActiveToolKey != null)
        {
            int activeIndex = FindToolIndex(toolbar.Tools, toolbar.ActiveToolKey);
            if (activeIndex < 0 || !IsFocusableTool(toolbar.Tools[activeIndex]))
            {
                throw CreateRenderException(node, "The active toolbar key must identify a visible enabled tool.");
            }
        }
    }

    internal void ValidateToolPayload(UiNode node, UiTool tool)
    {
        if (tool.Kind == "input" && tool.Input == null)
        {
            throw CreateRenderException(node, "Input tools require input descriptions.");
        }

        if (tool.Kind == "choice" && tool.Choice == null)
        {
            throw CreateRenderException(node, "Choice tools require choice descriptions.");
        }

        if (tool.Input != null && (tool.Input.Value == null || string.IsNullOrWhiteSpace(tool.Input.Name)))
        {
            throw CreateRenderException(node, "Toolbar inputs require names and portable values.");
        }

        if (tool.Choice != null && (tool.Choice.Value == null || string.IsNullOrWhiteSpace(tool.Choice.Key) || string.IsNullOrWhiteSpace(tool.Choice.Label)))
        {
            throw CreateRenderException(node, "Toolbar choices require keys, labels, and portable values.");
        }
    }

    internal List<DomNode> CreateToolbarChildren(UiNode node, UiToolbar toolbar)
    {
        List<DomNode> result = new List<DomNode>();
        string? focusKey = toolbar.ActiveToolKey;
        if (focusKey == null)
        {
            focusKey = FindFirstToolKey(toolbar.Tools);
        }

        int index = 0;
        while (index < toolbar.Tools.Length)
        {
            UiTool tool = toolbar.Tools[index];
            if (tool.GroupKey == null)
            {
                result.Add(CreateTool(node, tool, focusKey == tool.Key));
                index++;
            }
            else
            {
                string groupKey = tool.GroupKey;
                List<DomNode> grouped = new List<DomNode>();
                while (index < toolbar.Tools.Length && toolbar.Tools[index].GroupKey == groupKey)
                {
                    grouped.Add(CreateTool(node, toolbar.Tools[index], focusKey == toolbar.Tools[index].Key));
                    index++;
                }

                List<DomAttribute> groupAttributes = new List<DomAttribute>();
                groupAttributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = groupKey });
                groupAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-toolbar-group" });
                groupAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "group-key", Value = groupKey });
                groupAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Group });
                SortAttributes(groupAttributes);
                result.Add(new DomNode
                {
                    Kind = DomNodeKind.Element,
                    Key = "$group-" + groupKey,
                    Id = CreateSyntheticId(node, "group", groupKey),
                    Name = HtmlElements.Division,
                    Attributes = groupAttributes.ToArray(),
                    Children = grouped.ToArray(),
                    Version = GetNodeVersion(node)
                });
            }
        }

        return result;
    }

    internal DomNode CreateTool(UiNode node, UiTool tool, bool focused)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-tool htmlgui-tool-" + tool.Kind });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tool-key", Value = tool.Key });
        if (tool.CommandId != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "command-id", Value = tool.CommandId });
        }

        if (!tool.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (tool.Kind == "separator")
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Separator });
            SortAttributes(attributes);
            return CreateElement(node, tool.Key, "tool", tool.Key, HtmlElements.Division, attributes, Array.Empty<DomNode>());
        }

        string name = HtmlElements.Button;
        if (tool.Kind == "input")
        {
            name = HtmlElements.Input;
            UiInput input = tool.Input ?? new UiInput();
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = tool.Label });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Name, Value = input.Name });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Text });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Value, Value = FormatValue(input.Value) });
        }
        else
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = focused ? "0" : "-1" });
        if (!tool.IsEnabled || (node.State != null && !node.State.IsEnabled))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        if (tool.Kind == "toggle")
        {
            bool pressed = tool.Button != null && tool.Button.HasPressed && tool.Button.IsPressed;
            attributes.Add(new DomAttribute { Name = AriaAttributes.Pressed, Value = FormatBoolean(pressed) });
        }

        SortAttributes(attributes);
        DomNode[] children = Array.Empty<DomNode>();
        if (name != HtmlElements.Input)
        {
            string label = tool.Label;
            if (tool.Kind == "choice" && tool.Choice != null)
            {
                label = tool.Choice.Label;
                attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "choice-key", Value = tool.Choice.Key });
                SortAttributes(attributes);
            }

            children = new DomNode[] { CreateText(node, "$label", "tool-label", tool.Key, label) };
        }

        return CreateElement(node, tool.Key, "tool", tool.Key, name, attributes, children);
    }

    internal DomNode CreateOverflowTool(UiNode node, UiToolbar toolbar)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.HasPopup, Value = "menu" });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = "More tools" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-toolbar-overflow" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "overflow", Value = toolbar.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = "-1" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        SortAttributes(attributes);
        return CreateElement(node, "$overflow", "overflow", toolbar.Key, HtmlElements.Button, attributes, new DomNode[] { CreateText(node, "$text", "overflow-text", toolbar.Key, "More") });
    }

    internal DomNode ExpandNavigation(UiNode node)
    {
        UiBreadcrumb? navigation = node.Breadcrumb;
        if (navigation == null || string.IsNullOrWhiteSpace(navigation.Key) || navigation.Items == null || string.IsNullOrWhiteSpace(navigation.Label))
        {
            throw CreateRenderException(node, "Navigation controls require keys, labels, and item collections.");
        }

        bool breadcrumb = node.Name == "breadcrumb" || node.Name == "breadcrumbs";
        ValidateNavigationItems(node, navigation);
        List<DomAttribute> attributes = new List<DomAttribute>();
        string kind = breadcrumb ? "breadcrumb" : (node.Name == "rail" || node.Name == "navigation-rail" ? "rail" : "sidebar");
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = navigation.Label });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-navigation htmlgui-navigation-" + kind });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "navigation-key", Value = navigation.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "navigation-kind", Value = kind });
        AddNodeState(node, attributes);
        SortAttributes(attributes);
        DomNode[] items = new DomNode[navigation.Items.Length];
        int index = 0;
        while (index < navigation.Items.Length)
        {
            bool current = navigation.HasCurrentIndex && navigation.CurrentIndex == index;
            items[index] = CreateNavigationItem(node, navigation.Items[index], current, kind);
            index++;
        }

        DomNode list = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$items",
            Id = CreateSyntheticId(node, "navigation-items", navigation.Key),
            Name = breadcrumb ? HtmlElements.OrderedList : HtmlElements.UnorderedList,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-navigation-items" } },
            Children = items,
            Version = GetNodeVersion(node)
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Navigation,
            Attributes = attributes.ToArray(),
            Children = new DomNode[] { list },
            Version = GetNodeVersion(node)
        };
    }

    internal void ValidateNavigationItems(UiNode node, UiBreadcrumb navigation)
    {
        if (navigation.HasCurrentIndex && (navigation.CurrentIndex < 0 || navigation.CurrentIndex >= navigation.Items.Length))
        {
            throw CreateRenderException(node, "The current navigation index must identify an item.");
        }

        int index = 0;
        while (index < navigation.Items.Length)
        {
            UiChoice? item = navigation.Items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key) || string.IsNullOrWhiteSpace(item.Label) || item.Value == null)
            {
                throw CreateRenderException(node, "Navigation items require keys, labels, and portable values.");
            }

            if (item.Url != null && string.IsNullOrWhiteSpace(item.Url))
            {
                throw CreateRenderException(node, "A supplied navigation URL cannot be empty.");
            }

            if (item.Url != null && !HtmlSecurityPolicy.IsSafeHyperlinkUrl(item.Url))
            {
                throw CreateRenderException(node, "A supplied navigation URL is malformed or uses an unsafe scheme.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (navigation.Items[earlierIndex].Key == item.Key)
                {
                    throw CreateRenderException(node, "Navigation item keys must be unique.");
                }

                earlierIndex++;
            }

            ValueJsonCodec.ValidateValue(item.Value, 64, 10000, 1048576, 1048576);
            index++;
        }
    }

    internal DomNode CreateNavigationItem(UiNode node, UiChoice item, bool current, string kind)
    {
        List<DomAttribute> contentAttributes = new List<DomAttribute>();
        contentAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-navigation-link" });
        contentAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "item-key", Value = item.Key });
        if (item.Url != null)
        {
            contentAttributes.Add(new DomAttribute { Name = HtmlAttributes.HyperlinkReference, Value = HtmlSecurityPolicy.NormalizeHyperlinkUrl(item.Url) });
        }

        if (current || item.IsSelected)
        {
            contentAttributes.Add(new DomAttribute { Name = AriaAttributes.Current, Value = kind == "breadcrumb" ? "page" : "true" });
        }

        if (item.IsDisabled)
        {
            contentAttributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        SortAttributes(contentAttributes);
        DomNode content = CreateElement(node, "$link", "navigation-link", item.Key, item.Url == null ? HtmlElements.Span : HtmlElements.Anchor, contentAttributes, new DomNode[] { CreateText(node, "$text", "navigation-text", item.Key, item.Label) });
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Key,
            Id = CreateSyntheticId(node, "navigation-item", item.Key),
            Name = HtmlElements.ListItem,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-navigation-item" } },
            Children = new DomNode[] { content },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode ExpandPagination(UiNode node)
    {
        UiPagination? pagination = node.Pagination;
        if (pagination == null || string.IsNullOrWhiteSpace(pagination.Key))
        {
            throw CreateRenderException(node, "Pagination controls require a non-empty key.");
        }

        ValidatePagination(node, pagination);
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = "Pagination" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-pagination" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-index", Value = InvariantFormatter.FormatInt32(pagination.PageIndex) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-size", Value = InvariantFormatter.FormatInt32(pagination.PageSize) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "pagination-key", Value = pagination.Key });
        if (pagination.HasPageCount)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "page-count", Value = InvariantFormatter.FormatInt32(pagination.PageCount) });
        }

        AddNodeState(node, attributes);
        SortAttributes(attributes);
        List<DomNode> children = new List<DomNode>();
        children.Add(CreatePageButton(node, pagination, "$previous", "Previous", pagination.PageIndex - 1, pagination.HasPrevious, false));
        if (pagination.HasPageCount)
        {
            int pageIndex = 0;
            while (pageIndex < pagination.PageCount)
            {
                children.Add(CreatePageButton(node, pagination, "$page-" + InvariantFormatter.FormatInt32(pageIndex), InvariantFormatter.FormatInt32(pageIndex + 1), pageIndex, true, pageIndex == pagination.PageIndex));
                pageIndex++;
            }
        }
        else
        {
            children.Add(CreatePageButton(node, pagination, "$current", InvariantFormatter.FormatInt32(pagination.PageIndex + 1), pagination.PageIndex, false, true));
        }

        children.Add(CreatePageButton(node, pagination, "$next", "Next", pagination.PageIndex + 1, pagination.HasNext, false));
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Navigation,
            Attributes = attributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal void ValidatePagination(UiNode node, UiPagination pagination)
    {
        if (pagination.PageIndex < 0 || pagination.PageSize < 1)
        {
            throw CreateRenderException(node, "Pagination indices must be non-negative and page sizes positive.");
        }

        if (pagination.HasPageCount && (pagination.PageCount < 1 || pagination.PageIndex >= pagination.PageCount))
        {
            throw CreateRenderException(node, "The current page must fit within the known page count.");
        }

        if ((!pagination.HasPrevious && pagination.PageIndex > 0) || (pagination.HasPrevious && pagination.PageIndex == 0))
        {
            throw CreateRenderException(node, "Previous-page state must agree with the current page index.");
        }

        if (pagination.HasPageCount && pagination.HasNext == (pagination.PageIndex + 1 >= pagination.PageCount))
        {
            throw CreateRenderException(node, "Next-page state must agree with the known page count.");
        }
    }

    internal DomNode CreatePageButton(UiNode node, UiPagination pagination, string key, string label, int target, bool enabled, bool current)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = label });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-page-button" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "target-page", Value = InvariantFormatter.FormatInt32(target) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        if (current)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Current, Value = "page" });
        }

        if (!enabled || current)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(attributes);
        return CreateElement(node, key, "page", pagination.Key + "/" + key, HtmlElements.Button, attributes, new DomNode[] { CreateText(node, "$text", "page-text", pagination.Key + "/" + key, label) });
    }

    internal void ValidateToolKind(string kind)
    {
        switch (kind)
        {
            case "button":
            case "command":
            case "toggle":
            case "separator":
            case "input":
            case "choice":
                return;
            default:
                throw new ArgumentException("The toolbar tool kind is not supported.", "kind");
        }
    }

    internal bool IsFocusableTool(UiTool tool)
    {
        return tool.Kind != "separator" && tool.IsEnabled && tool.IsVisible;
    }

    internal int FindToolIndex(UiTool[] tools, string key)
    {
        int index = 0;
        while (index < tools.Length)
        {
            if (tools[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal string? FindFirstToolKey(UiTool[] tools)
    {
        int index = 0;
        while (index < tools.Length)
        {
            if (IsFocusableTool(tools[index]))
            {
                return tools[index].Key;
            }

            index++;
        }

        return null;
    }

    internal void ValidateOrientation(UiOrientation orientation)
    {
        if (orientation != UiOrientation.Horizontal && orientation != UiOrientation.Vertical)
        {
            throw new ArgumentOutOfRangeException("orientation", orientation, "The orientation is not supported.");
        }
    }

    internal string GetOrientationToken(UiOrientation orientation)
    {
        ValidateOrientation(orientation);
        return orientation == UiOrientation.Vertical ? "vertical" : "horizontal";
    }

    internal string FormatValue(UiValue value)
    {
        ValueJsonCodec.ValidateValue(value, 64, 10000, 1048576, 1048576);
        return new DocumentComposer().FormatMetadataValue(value);
    }

    internal DomNode CreateRoot(UiNode node, List<DomAttribute> attributes, DomNode[] children)
    {
        return new DomNode { Kind = DomNodeKind.Element, Key = GetNodeKey(node), Id = GetNodeId(node), Name = HtmlElements.Division, Attributes = attributes.ToArray(), Children = children, Version = GetNodeVersion(node) };
    }

    internal DomNode CreateElement(UiNode node, string key, string suffix, string identity, string name, List<DomAttribute> attributes, DomNode[] children)
    {
        return new DomNode { Kind = DomNodeKind.Element, Key = key, Id = CreateSyntheticId(node, suffix, identity), Name = name, Attributes = attributes.ToArray(), Children = children, Version = GetNodeVersion(node) };
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string identity, string text)
    {
        return new DomNode { Kind = DomNodeKind.Text, Key = key, Id = CreateSyntheticId(node, suffix, identity), Text = text, Version = GetNodeVersion(node) };
    }

    internal void AddNodeState(UiNode node, List<DomAttribute> attributes)
    {
        if (node.State != null && node.State.IsBusy) attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        if (node.State != null && !node.State.IsEnabled) attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        if (node.State != null && !node.State.IsVisible) attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
    }

    internal string FormatBoolean(bool value)
    {
        return value ? "true" : "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "navigation", suffix, identity });
        return "ui-navigation-" + token.Substring(0, 20);
    }

    internal string GetNodeKey(UiNode node)
    {
        if (node.Identity == null) return string.Empty;
        if (node.Identity.AuthorKey.Length > 0) return node.Identity.AuthorKey;
        return node.Identity.SiblingIdentity;
    }

    internal string GetNodeId(UiNode node)
    {
        if (node.Identity == null) return string.Empty;
        return node.Identity.StableId;
    }

    internal int GetNodeVersion(UiNode node)
    {
        if (node.State == null) return 0;
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
        if (nodeId.Length == 0) nodeId = "unknown";
        return new UiRenderException(message, nodeId, "dom");
    }
}
