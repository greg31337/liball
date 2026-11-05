using System;
using System.Collections.Generic;

namespace htmlgui;

internal class MenuTemplateExpander
{
    internal bool IsMenuControlName(string name)
    {
        switch (name)
        {
            case "menu":
            case "menu-bar":
            case "menubar":
            case "popup-menu":
            case "context-menu":
            case "menu-button":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandMenu(UiNode node)
    {
        UiMenu menu = RequireMenu(node);
        HashSet<UiMenu> ancestors = new HashSet<UiMenu>();
        ValidateMenu(node, menu, menu.Key, ancestors);
        if (node.Name == "menu-button")
        {
            return CreateMenuButton(node, menu);
        }

        bool menuBar = node.Name == "menu-bar" || node.Name == "menubar";
        string kind = "popup";
        if (menuBar)
        {
            kind = "bar";
        }
        else if (node.Name == "context-menu")
        {
            kind = "context";
        }

        return CreateMenuElement(node, menu, menu.Key, kind, menuBar, true);
    }

    internal UiMenu RequireMenu(UiNode node)
    {
        UiMenu? menu = node.Menu;
        if (menu == null)
        {
            throw CreateRenderException(node, "A menu control requires a menu description.");
        }

        if (string.IsNullOrWhiteSpace(menu.Key))
        {
            throw CreateRenderException(node, "Menus require a non-empty stable key.");
        }

        return menu;
    }

    internal void ValidateMenu(UiNode node, UiMenu menu, string path, HashSet<UiMenu> ancestors)
    {
        if (string.IsNullOrWhiteSpace(menu.Key) || menu.Items == null || menu.TypeAhead == null || menu.TypeAhead.Buffer == null)
        {
            throw CreateRenderException(node, "Menus require keys, items, and type-ahead state.");
        }

        ValidateOrientation(menu.Orientation);
        if (menu.TypeAhead.Generation < 0)
        {
            throw CreateRenderException(node, "Menu type-ahead generations cannot be negative.");
        }

        if (menu.RestoreFocusId != null && string.IsNullOrWhiteSpace(menu.RestoreFocusId))
        {
            throw CreateRenderException(node, "A supplied menu focus-restore identifier cannot be empty.");
        }

        if (!ancestors.Add(menu))
        {
            throw CreateRenderException(node, "Menu graphs cannot contain cycles.");
        }

        int index = 0;
        while (index < menu.Items.Length)
        {
            UiMenuItem? item = menu.Items[index];
            if (item == null || string.IsNullOrWhiteSpace(item.Key))
            {
                throw CreateRenderException(node, "Menu items require non-empty stable keys.");
            }

            int earlierIndex = 0;
            while (earlierIndex < index)
            {
                if (menu.Items[earlierIndex].Key == item.Key)
                {
                    throw CreateRenderException(node, "Menu item keys must be unique within a menu.");
                }

                earlierIndex++;
            }

            ValidateMenuItem(node, item);
            if (item.Submenu != null)
            {
                ValidateMenu(node, item.Submenu, path + "/" + item.Key + "/" + item.Submenu.Key, ancestors);
            }

            index++;
        }

        if (menu.ActiveItemKey != null)
        {
            int activeIndex = FindItemIndex(menu.Items, menu.ActiveItemKey);
            if (activeIndex < 0 || !IsFocusableItem(menu.Items[activeIndex]))
            {
                throw CreateRenderException(node, "The active menu key must identify a visible enabled menu item.");
            }
        }

        if (menu.TypeAhead.MatchedKey != null)
        {
            int matchIndex = FindItemIndex(menu.Items, menu.TypeAhead.MatchedKey);
            if (matchIndex < 0 || !IsFocusableItem(menu.Items[matchIndex]))
            {
                throw CreateRenderException(node, "The type-ahead match must identify a visible enabled menu item.");
            }
        }

        ancestors.Remove(menu);
    }

    internal void ValidateMenuItem(UiNode node, UiMenuItem item)
    {
        ValidateMenuItemKind(item.Kind);
        if (item.Kind != UiMenuItemKind.Separator && string.IsNullOrWhiteSpace(item.Label))
        {
            throw CreateRenderException(node, "Actionable menu items require labels.");
        }

        if (item.CommandId != null && string.IsNullOrWhiteSpace(item.CommandId))
        {
            throw CreateRenderException(node, "A supplied menu command identifier cannot be empty.");
        }

        if (item.IconKey != null && string.IsNullOrWhiteSpace(item.IconKey))
        {
            throw CreateRenderException(node, "A supplied menu icon key cannot be empty.");
        }

        if (item.Accelerator != null && string.IsNullOrWhiteSpace(item.Accelerator))
        {
            throw CreateRenderException(node, "A supplied menu accelerator cannot be empty.");
        }

        if (item.Mnemonic != null && (item.Mnemonic.Length != 1 || string.IsNullOrWhiteSpace(item.Mnemonic)))
        {
            throw CreateRenderException(node, "A menu mnemonic must be one visible character.");
        }

        if (item.Kind == UiMenuItemKind.Checkbox || item.Kind == UiMenuItemKind.Radio)
        {
            if (!item.HasCheckState)
            {
                throw CreateRenderException(node, "Checkbox and radio menu items require check state.");
            }

            ValidateCheckState(item.CheckState);
            if (item.Kind == UiMenuItemKind.Radio && item.CheckState == UiCommandCheckState.Mixed)
            {
                throw CreateRenderException(node, "Radio menu items cannot have mixed check state.");
            }
        }

        if (item.Kind == UiMenuItemKind.Submenu)
        {
            if (item.Submenu == null)
            {
                throw CreateRenderException(node, "Submenu items require nested menu descriptions.");
            }
        }
        else if (item.Submenu != null)
        {
            throw CreateRenderException(node, "Only submenu items can contain nested menus.");
        }
    }

    internal DomNode CreateMenuButton(UiNode node, UiMenu menu)
    {
        if (string.IsNullOrWhiteSpace(menu.Label))
        {
            throw CreateRenderException(node, "Menu buttons require a non-empty accessible label.");
        }

        string path = menu.Key;
        string popupId = CreateSyntheticId(node, "menu", path);
        List<DomAttribute> triggerAttributes = new List<DomAttribute>();
        triggerAttributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = popupId });
        triggerAttributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(menu.IsOpen) });
        triggerAttributes.Add(new DomAttribute { Name = AriaAttributes.HasPopup, Value = "menu" });
        triggerAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-button-trigger" });
        triggerAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "menu-key", Value = menu.Key });
        triggerAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        AddTriggerState(node, triggerAttributes);
        SortAttributes(triggerAttributes);
        DomNode trigger = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$trigger",
            Id = CreateSyntheticId(node, "trigger", path),
            Name = HtmlElements.Button,
            Attributes = triggerAttributes.ToArray(),
            Children = new DomNode[] { CreateTextNode(node, "$trigger-label", "trigger-label", path, menu.Label) },
            Version = GetNodeVersion(node)
        };
        DomNode popup = CreateMenuElement(node, menu, path, "button-popup", false, false);
        List<DomAttribute> containerAttributes = new List<DomAttribute>();
        containerAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-button" });
        AddNodeState(node, containerAttributes);
        SortAttributes(containerAttributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = containerAttributes.ToArray(),
            Children = new DomNode[] { trigger, popup },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateMenuElement(UiNode node, UiMenu menu, string path, string kind, bool menuBar, bool useNodeIdentity)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu htmlgui-menu-" + kind });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dismiss-escape", Value = FormatBoolean(menu.DismissOnEscape) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dismiss-outside", Value = FormatBoolean(menu.DismissOnOutsideClick) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "menu-key", Value = menu.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "menu-kind", Value = kind });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "open", Value = FormatBoolean(menu.IsOpen) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-buffer", Value = menu.TypeAhead.Buffer });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-generation", Value = InvariantFormatter.FormatInt32(menu.TypeAhead.Generation) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-ignore-case", Value = FormatBoolean(menu.TypeAhead.IgnoreCase) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "wrap-focus", Value = FormatBoolean(menu.WrapFocus) });
        attributes.Add(new DomAttribute { Name = AriaAttributes.Orientation, Value = GetOrientationToken(menu.Orientation) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = menuBar ? AriaRoles.MenuBar : AriaRoles.Menu });
        if (menu.Label != null && menu.Label.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = menu.Label });
        }

        if (menu.RestoreFocusId != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "restore-focus", Value = menu.RestoreFocusId });
        }

        if (menu.TypeAhead.MatchedKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-match", Value = menu.TypeAhead.MatchedKey });
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "typeahead-match-id", Value = CreateSyntheticId(node, "item", path + "/" + menu.TypeAhead.MatchedKey) });
        }

        if (!menuBar && !menu.IsOpen)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (useNodeIdentity)
        {
            AddNodeState(node, attributes);
        }

        SortAttributes(attributes);
        DomNode[] children = CreateMenuItems(node, menu, path);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = useNodeIdentity ? GetNodeKey(node) : menu.Key,
            Id = useNodeIdentity ? GetNodeId(node) : CreateSyntheticId(node, "menu", path),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] CreateMenuItems(UiNode node, UiMenu menu, string path)
    {
        DomNode[] result = new DomNode[menu.Items.Length];
        string? focusKey = GetFocusKey(menu);
        int index = 0;
        while (index < menu.Items.Length)
        {
            UiMenuItem item = menu.Items[index];
            string itemPath = path + "/" + item.Key;
            result[index] = CreateMenuItemNode(node, item, itemPath, focusKey == item.Key);
            index++;
        }

        return result;
    }

    internal DomNode CreateMenuItemNode(UiNode node, UiMenuItem item, string path, bool focused)
    {
        if (item.Kind == UiMenuItemKind.Separator)
        {
            List<DomAttribute> separatorAttributes = new List<DomAttribute>();
            separatorAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-separator" });
            separatorAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Separator });
            if (!item.IsVisible)
            {
                separatorAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
            }

            SortAttributes(separatorAttributes);
            return new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = item.Key,
                Id = CreateSyntheticId(node, "item", path),
                Name = HtmlElements.Division,
                Attributes = separatorAttributes.ToArray(),
                Version = GetNodeVersion(node)
            };
        }

        DomNode action = CreateMenuAction(node, item, path, focused);
        if (item.Submenu == null)
        {
            return action;
        }

        string submenuPath = path + "/" + item.Submenu.Key;
        DomNode submenu = CreateMenuElement(node, item.Submenu, submenuPath, "submenu", false, false);
        List<DomAttribute> wrapperAttributes = new List<DomAttribute>();
        wrapperAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-submenu-owner" });
        wrapperAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.None });
        if (!item.IsVisible)
        {
            wrapperAttributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        SortAttributes(wrapperAttributes);
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Key,
            Id = CreateSyntheticId(node, "owner", path),
            Name = HtmlElements.Division,
            Attributes = wrapperAttributes.ToArray(),
            Children = new DomNode[] { action, submenu },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateMenuAction(UiNode node, UiMenuItem item, string path, bool focused)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-item htmlgui-menu-item-" + GetItemKindToken(item.Kind) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "item-key", Value = item.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = GetItemRole(item.Kind) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = focused ? "0" : "-1" });
        if (item.CommandId != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "command-id", Value = item.CommandId });
        }

        if (item.IconKey != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "icon-key", Value = item.IconKey });
        }

        if (item.Accelerator != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "accelerator", Value = item.Accelerator });
        }

        if (item.Mnemonic != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "mnemonic", Value = item.Mnemonic });
        }

        if (!item.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (!item.IsVisible)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (item.Kind == UiMenuItemKind.Checkbox || item.Kind == UiMenuItemKind.Radio)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Checked, Value = FormatCheckState(item.CheckState) });
        }

        if (item.Kind == UiMenuItemKind.Submenu && item.Submenu != null)
        {
            string submenuPath = path + "/" + item.Submenu.Key;
            attributes.Add(new DomAttribute { Name = AriaAttributes.Controls, Value = CreateSyntheticId(node, "menu", submenuPath) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(item.Submenu.IsOpen) });
            attributes.Add(new DomAttribute { Name = AriaAttributes.HasPopup, Value = "menu" });
        }

        SortAttributes(attributes);
        List<DomNode> children = new List<DomNode>();
        if (item.IconKey != null)
        {
            List<DomAttribute> iconAttributes = new List<DomAttribute>();
            iconAttributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
            iconAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-menu-icon" });
            iconAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "icon", Value = item.IconKey });
            SortAttributes(iconAttributes);
            children.Add(new DomNode
            {
                Kind = DomNodeKind.Element,
                Key = "$icon",
                Id = CreateSyntheticId(node, "icon", path),
                Name = HtmlElements.Span,
                Attributes = iconAttributes.ToArray(),
                Version = GetNodeVersion(node)
            });
        }

        children.Add(CreateTextElement(node, "$label", "label", path, item.Label, "htmlgui-menu-label"));
        if (item.Accelerator != null)
        {
            children.Add(CreateTextElement(node, "$accelerator", "accelerator", path, item.Accelerator, "htmlgui-menu-accelerator"));
        }

        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = item.Submenu == null ? item.Key : "$action",
            Id = CreateSyntheticId(node, "item", path),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children.ToArray(),
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextElement(UiNode node, string key, string suffix, string path, string text, string className)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix, path),
            Name = HtmlElements.Span,
            Attributes = new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = className } },
            Children = new DomNode[] { CreateTextNode(node, "$text", suffix + "-text", path, text) },
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateTextNode(UiNode node, string key, string suffix, string path, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix, path),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal string? GetFocusKey(UiMenu menu)
    {
        if (menu.ActiveItemKey != null)
        {
            return menu.ActiveItemKey;
        }

        int index = 0;
        while (index < menu.Items.Length)
        {
            if (IsFocusableItem(menu.Items[index]))
            {
                return menu.Items[index].Key;
            }

            index++;
        }

        return null;
    }

    internal bool IsFocusableItem(UiMenuItem item)
    {
        return item.Kind != UiMenuItemKind.Separator && item.IsVisible && item.IsEnabled;
    }

    internal int FindItemIndex(UiMenuItem[] items, string key)
    {
        int index = 0;
        while (index < items.Length)
        {
            if (items[index].Key == key)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal void AddTriggerState(UiNode node, List<DomAttribute> attributes)
    {
        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }

        string tabIndex = "0";
        if (node.State != null && node.State.HasTabIndex)
        {
            tabIndex = InvariantFormatter.FormatInt32(node.State.TabIndex);
        }

        attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = tabIndex });
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

    internal void ValidateMenuItemKind(UiMenuItemKind kind)
    {
        switch (kind)
        {
            case UiMenuItemKind.Command:
            case UiMenuItemKind.Checkbox:
            case UiMenuItemKind.Radio:
            case UiMenuItemKind.Submenu:
            case UiMenuItemKind.Separator:
                return;
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The menu item kind is not supported.");
        }
    }

    internal void ValidateCheckState(UiCommandCheckState state)
    {
        switch (state)
        {
            case UiCommandCheckState.Unchecked:
            case UiCommandCheckState.Checked:
            case UiCommandCheckState.Mixed:
                return;
            default:
                throw new ArgumentOutOfRangeException("checkState", state, "The menu check state is not supported.");
        }
    }

    internal void ValidateOrientation(UiOrientation orientation)
    {
        switch (orientation)
        {
            case UiOrientation.Horizontal:
            case UiOrientation.Vertical:
                return;
            default:
                throw new ArgumentOutOfRangeException("orientation", orientation, "The menu orientation is not supported.");
        }
    }

    internal string GetItemRole(UiMenuItemKind kind)
    {
        switch (kind)
        {
            case UiMenuItemKind.Checkbox:
                return AriaRoles.MenuItemCheckbox;
            case UiMenuItemKind.Radio:
                return AriaRoles.MenuItemRadio;
            default:
                return AriaRoles.MenuItem;
        }
    }

    internal string GetItemKindToken(UiMenuItemKind kind)
    {
        switch (kind)
        {
            case UiMenuItemKind.Command:
                return "command";
            case UiMenuItemKind.Checkbox:
                return "checkbox";
            case UiMenuItemKind.Radio:
                return "radio";
            case UiMenuItemKind.Submenu:
                return "submenu";
            default:
                return "separator";
        }
    }

    internal string GetOrientationToken(UiOrientation orientation)
    {
        ValidateOrientation(orientation);
        if (orientation == UiOrientation.Horizontal)
        {
            return "horizontal";
        }

        return "vertical";
    }

    internal string FormatCheckState(UiCommandCheckState state)
    {
        ValidateCheckState(state);
        if (state == UiCommandCheckState.Checked)
        {
            return "true";
        }

        if (state == UiCommandCheckState.Mixed)
        {
            return "mixed";
        }

        return "false";
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
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "menu", suffix, identity });
        return "ui-menu-" + token.Substring(0, 20);
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
