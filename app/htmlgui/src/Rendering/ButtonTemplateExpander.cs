using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ButtonTemplateExpander
{
    internal DomNode ExpandButton(UiNode node, DomNode[] logicalChildren)
    {
        UiButton? button = node.Button;
        if (button == null)
        {
            throw CreateRenderException(node, "A button control requires a button description.");
        }

        if (string.IsNullOrWhiteSpace(button.Label))
        {
            throw CreateRenderException(node, "Buttons require a non-empty accessible label.");
        }

        switch (button.Kind)
        {
            case UiButtonKind.Push:
                return ExpandOrdinaryButton(node, button, "push", HtmlInputTypes.Button, logicalChildren, false);
            case UiButtonKind.Submit:
                return ExpandOrdinaryButton(node, button, "submit", HtmlInputTypes.Submit, logicalChildren, false);
            case UiButtonKind.Reset:
                return ExpandOrdinaryButton(node, button, "reset", HtmlInputTypes.Reset, logicalChildren, false);
            case UiButtonKind.Toggle:
                return ExpandOrdinaryButton(node, button, "toggle", HtmlInputTypes.Button, logicalChildren, true);
            case UiButtonKind.Split:
                return ExpandSplitButton(node, button, logicalChildren);
            case UiButtonKind.Icon:
                return ExpandIconButton(node, button, logicalChildren);
            case UiButtonKind.Command:
                return ExpandCommandButton(node, button, logicalChildren);
            default:
                throw new ArgumentOutOfRangeException("kind", button.Kind, "The button kind is not supported.");
        }
    }

    internal bool IsButtonControlName(string name)
    {
        switch (name)
        {
            case "button":
            case "push-button":
            case "submit-button":
            case "reset-button":
            case "toggle-button":
            case "split-button":
            case "icon-button":
            case "command-button":
                return true;
            default:
                return false;
        }
    }

    internal DomNode ExpandOrdinaryButton(UiNode node, UiButton button, string kindToken, string type, DomNode[] logicalChildren, bool forcePressed)
    {
        List<DomAttribute> attributes = CreateButtonAttributes(node, button, kindToken, type, forcePressed);
        return CreateRootButton(node, attributes, CreateVisibleContent(node, button, logicalChildren));
    }

    internal DomNode ExpandIconButton(UiNode node, UiButton button, DomNode[] logicalChildren)
    {
        if (button.IconKey == null || string.IsNullOrWhiteSpace(button.IconKey))
        {
            throw CreateRenderException(node, "An icon button requires a non-empty icon key.");
        }

        List<DomAttribute> attributes = CreateButtonAttributes(node, button, "icon", HtmlInputTypes.Button, false);
        attributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = button.Label });
        SortAttributes(attributes);
        DomNode[] children = PrependChild(CreateIcon(node, button.IconKey, "$icon", "icon"), logicalChildren);
        return CreateRootButton(node, attributes, children);
    }

    internal DomNode ExpandCommandButton(UiNode node, UiButton button, DomNode[] logicalChildren)
    {
        if (button.CommandId == null || string.IsNullOrWhiteSpace(button.CommandId))
        {
            throw CreateRenderException(node, "A command button requires a non-empty command identifier.");
        }

        List<DomAttribute> attributes = CreateButtonAttributes(node, button, "command", HtmlInputTypes.Button, false);
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "command-id", Value = button.CommandId });
        SortAttributes(attributes);
        return CreateRootButton(node, attributes, CreateVisibleContent(node, button, logicalChildren));
    }

    internal DomNode ExpandSplitButton(UiNode node, UiButton button, DomNode[] logicalChildren)
    {
        UiMenu? menu = button.Menu;
        if (menu == null || string.IsNullOrWhiteSpace(menu.Key))
        {
            throw CreateRenderException(node, "A split button requires a menu with a non-empty key.");
        }

        bool enabled = IsEnabled(node, button);
        List<DomAttribute> primaryAttributes = new List<DomAttribute>();
        primaryAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-split-primary" });
        primaryAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        AddNativeDisabled(primaryAttributes, enabled);
        if (HasPressed(node, button))
        {
            primaryAttributes.Add(new DomAttribute { Name = AriaAttributes.Pressed, Value = FormatBoolean(IsPressed(node, button)) });
        }

        AddTabIndex(node, primaryAttributes);
        SortAttributes(primaryAttributes);
        DomNode primary = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$split-primary",
            Id = CreateSyntheticId(node, "split-primary"),
            Name = HtmlElements.Button,
            Attributes = primaryAttributes.ToArray(),
            Children = CreateVisibleContent(node, button, logicalChildren),
            Version = GetNodeVersion(node)
        };

        List<DomAttribute> toggleAttributes = new List<DomAttribute>();
        toggleAttributes.Add(new DomAttribute { Name = AriaAttributes.Expanded, Value = FormatBoolean(menu.IsOpen) });
        toggleAttributes.Add(new DomAttribute { Name = AriaAttributes.HasPopup, Value = "menu" });
        toggleAttributes.Add(new DomAttribute { Name = AriaAttributes.Label, Value = button.Label + " menu" });
        toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-split-toggle" });
        toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "menu-key", Value = menu.Key });
        toggleAttributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = HtmlInputTypes.Button });
        AddNativeDisabled(toggleAttributes, enabled);
        AddTabIndex(node, toggleAttributes);
        SortAttributes(toggleAttributes);
        DomNode toggle = new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = "$split-toggle",
            Id = CreateSyntheticId(node, "split-toggle"),
            Name = HtmlElements.Button,
            Attributes = toggleAttributes.ToArray(),
            Children = new DomNode[] { CreateText(node, "$split-toggle-text", "split-toggle-text", "▾") },
            Version = GetNodeVersion(node)
        };

        List<DomAttribute> containerAttributes = new List<DomAttribute>();
        containerAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-split-button" });
        containerAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Group });
        AddContainerState(node, button, containerAttributes);
        SortAttributes(containerAttributes);
        return CreateElement(node, HtmlElements.Division, containerAttributes.ToArray(), new DomNode[] { primary, toggle });
    }

    internal List<DomAttribute> CreateButtonAttributes(UiNode node, UiButton button, string kindToken, string type, bool forcePressed)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-button htmlgui-button-" + kindToken });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Type, Value = type });
        AddInteractiveState(node, button, attributes, forcePressed);
        SortAttributes(attributes);
        return attributes;
    }

    internal void AddInteractiveState(UiNode node, UiButton button, List<DomAttribute> attributes, bool forcePressed)
    {
        AddNativeDisabled(attributes, IsEnabled(node, button));
        if (!IsVisible(node, button))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (IsBusy(node, button))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        if (forcePressed || HasPressed(node, button))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Pressed, Value = FormatBoolean(IsPressed(node, button)) });
        }

        AddTabIndex(node, attributes);
    }

    internal void AddContainerState(UiNode node, UiButton button, List<DomAttribute> attributes)
    {
        if (!IsVisible(node, button))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (IsBusy(node, button))
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }
    }

    internal void AddNativeDisabled(List<DomAttribute> attributes, bool enabled)
    {
        if (!enabled)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Disabled, Value = string.Empty, IsBoolean = true });
        }
    }

    internal void AddTabIndex(UiNode node, List<DomAttribute> attributes)
    {
        if (node.State != null && node.State.HasTabIndex)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.TabIndex, Value = InvariantFormatter.FormatInt32(node.State.TabIndex) });
        }
    }

    internal DomNode[] CreateVisibleContent(UiNode node, UiButton button, DomNode[] logicalChildren)
    {
        List<DomNode> children = new List<DomNode>();
        if (button.IconKey != null && button.IconKey.Length > 0)
        {
            children.Add(CreateIcon(node, button.IconKey, "$button-icon", "button-icon"));
        }

        children.Add(CreateText(node, "$button-label", "button-label", button.Label));
        int index = 0;
        while (index < logicalChildren.Length)
        {
            children.Add(logicalChildren[index]);
            index++;
        }

        return children.ToArray();
    }

    internal DomNode CreateIcon(UiNode node, string iconKey, string key, string suffix)
    {
        DomAttribute[] attributes = new DomAttribute[]
        {
            new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" },
            new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-button-icon" },
            new DomAttribute { Name = HtmlAttributes.DataPrefix + "icon-key", Value = iconKey }
        };
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix),
            Name = HtmlElements.Span,
            Attributes = attributes,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string text)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix),
            Text = text,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode[] PrependChild(DomNode child, DomNode[] logicalChildren)
    {
        DomNode[] result = new DomNode[logicalChildren.Length + 1];
        result[0] = child;
        Array.Copy(logicalChildren, 0, result, 1, logicalChildren.Length);
        return result;
    }

    internal DomNode CreateRootButton(UiNode node, List<DomAttribute> attributes, DomNode[] children)
    {
        return CreateElement(node, HtmlElements.Button, attributes.ToArray(), children);
    }

    internal DomNode CreateElement(UiNode node, string name, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = name,
            Attributes = attributes,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal bool IsEnabled(UiNode node, UiButton button)
    {
        if (!button.IsEnabled)
        {
            return false;
        }

        return node.State == null || node.State.IsEnabled;
    }

    internal bool IsVisible(UiNode node, UiButton button)
    {
        if (!button.IsVisible)
        {
            return false;
        }

        return node.State == null || node.State.IsVisible;
    }

    internal bool IsBusy(UiNode node, UiButton button)
    {
        if (button.IsBusy)
        {
            return true;
        }

        return node.State != null && node.State.IsBusy;
    }

    internal bool HasPressed(UiNode node, UiButton button)
    {
        if (button.HasPressed)
        {
            return true;
        }

        return node.State != null && node.State.HasPressed;
    }

    internal bool IsPressed(UiNode node, UiButton button)
    {
        if (button.HasPressed)
        {
            return button.IsPressed;
        }

        return node.State != null && node.State.HasPressed && node.State.IsPressed;
    }

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }

    internal string CreateSyntheticId(UiNode node, string suffix)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "button", suffix });
        return "ui-button-" + token.Substring(0, 20);
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
