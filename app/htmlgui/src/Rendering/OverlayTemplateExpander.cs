using System;
using System.Collections.Generic;

namespace htmlgui;

internal class OverlayTemplateExpander
{
    internal bool IsDialogControlName(string name)
    {
        switch (name)
        {
            case "dialog":
            case "modal-dialog":
            case "non-modal-dialog":
            case "alert-dialog":
            case "confirm-dialog":
            case "prompt-dialog":
            case "sheet":
            case "drawer":
                return true;
            default:
                return false;
        }
    }

    internal bool IsPopoverControlName(string name)
    {
        return name == "popover";
    }

    internal bool IsTooltipControlName(string name)
    {
        return name == "tooltip";
    }

    internal UiDialog PrepareDialog(UiNode node)
    {
        UiDialog? dialog = node.Dialog;
        if (dialog == null || string.IsNullOrWhiteSpace(dialog.Key) || string.IsNullOrWhiteSpace(dialog.Title) || dialog.Content == null || dialog.Actions == null || dialog.State == null)
        {
            throw CreateRenderException(node, "Dialogs require a key, title, content, actions, and overlay state.");
        }

        GetDialogKindToken(dialog.Kind);
        ValidateOverlayState(node, dialog.State);
        if (dialog.InitialFocusId != null && string.IsNullOrWhiteSpace(dialog.InitialFocusId))
        {
            throw CreateRenderException(node, "A supplied initial-focus identifier cannot be empty.");
        }

        int index = 0;
        while (index < dialog.Actions.Length)
        {
            if (dialog.Actions[index] == null)
            {
                throw CreateRenderException(node, "Dialog actions cannot be null.");
            }

            index++;
        }

        return dialog;
    }

    internal UiPopover PreparePopover(UiNode node)
    {
        UiPopover? popover = node.Popover;
        if (popover == null || string.IsNullOrWhiteSpace(popover.Key) || popover.Anchor == null || popover.Content == null || popover.State == null)
        {
            throw CreateRenderException(node, "Popovers require a key, anchor, content, and overlay state.");
        }

        ValidateAnchor(node, popover.Anchor);
        ValidateOverlayState(node, popover.State);
        return popover;
    }

    internal UiTooltip PrepareTooltip(UiNode node)
    {
        UiTooltip? tooltip = node.Tooltip;
        if (tooltip == null || string.IsNullOrWhiteSpace(tooltip.Key) || string.IsNullOrWhiteSpace(tooltip.Text) || tooltip.Anchor == null || tooltip.State == null)
        {
            throw CreateRenderException(node, "Tooltips require a key, text, anchor, and overlay state.");
        }

        ValidateAnchor(node, tooltip.Anchor);
        ValidateOverlayState(node, tooltip.State);
        if (tooltip.HasDelayMilliseconds && tooltip.DelayMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException("delayMilliseconds", tooltip.DelayMilliseconds, "Tooltip delays cannot be negative.");
        }

        if (tooltip.State.IsModal || tooltip.State.HasBackdrop || tooltip.State.TrapFocus)
        {
            throw CreateRenderException(node, "Tooltips cannot be modal, own a backdrop, or trap focus.");
        }

        return tooltip;
    }

    internal DomNode[] ExpandDialogActions(UiNode node, UiDialog dialog)
    {
        DomNode[] result = new DomNode[dialog.Actions.Length];
        ButtonTemplateExpander expander = new ButtonTemplateExpander();
        int index = 0;
        while (index < dialog.Actions.Length)
        {
            UiNode actionNode = CreateActionNode(node, dialog, dialog.Actions[index], index);
            result[index] = expander.ExpandButton(actionNode, Array.Empty<DomNode>());
            index++;
        }

        return result;
    }

    internal DomNode ExpandDialog(UiNode node, UiDialog dialog, DomNode[] content, DomNode[] actions)
    {
        if (content.Length != dialog.Content.Length || actions.Length != dialog.Actions.Length)
        {
            throw CreateRenderException(node, "Expanded dialog content and actions must align with the dialog model.");
        }

        bool modal = IsDialogModal(dialog);
        string kind = GetDialogKindToken(dialog.Kind);
        string titleId = CreateSyntheticId(node, "dialog-title", dialog.Key);
        List<DomAttribute> dialogAttributes = new List<DomAttribute>();
        dialogAttributes.Add(new DomAttribute { Name = AriaAttributes.LabelledBy, Value = titleId });
        dialogAttributes.Add(new DomAttribute { Name = AriaAttributes.Modal, Value = FormatBoolean(modal) });
        dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-dialog htmlgui-dialog-" + kind });
        dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dialog-key", Value = dialog.Key });
        dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dialog-kind", Value = kind });
        dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = GetDialogRole(dialog.Kind) });
        if (dialog.State.IsOpen)
        {
            dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.Open, Value = string.Empty, IsBoolean = true });
        }

        if (dialog.InitialFocusId != null)
        {
            dialogAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "initial-focus", Value = dialog.InitialFocusId });
        }

        SortAttributes(dialogAttributes);
        DomNode title = CreateElement(node, "$title", "dialog-title", dialog.Key, HtmlElements.Heading2, new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-dialog-title" } }, new DomNode[] { CreateText(node, "$text", "dialog-title-text", dialog.Key, dialog.Title) });
        DomNode body = CreateElement(node, "$body", "dialog-body", dialog.Key, HtmlElements.Division, new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-dialog-body" } }, content);
        DomNode actionBar = CreateElement(node, "$actions", "dialog-actions", dialog.Key, HtmlElements.Division, new DomAttribute[] { new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-dialog-actions" }, new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Group } }, actions);
        DomNode dialogNode = CreateElement(node, "$dialog", "dialog", dialog.Key, HtmlElements.Dialog, dialogAttributes.ToArray(), new DomNode[] { title, body, actionBar });
        List<DomNode> children = new List<DomNode>();
        AddBackdrop(node, dialog.State, dialog.Key, children);
        children.Add(dialogNode);
        List<DomAttribute> rootAttributes = CreateOverlayAttributes(node, dialog.State, modal, "dialog");
        rootAttributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dialog-kind", Value = kind });
        SortAttributes(rootAttributes);
        return CreateRoot(node, rootAttributes, children.ToArray());
    }

    internal DomNode ExpandPopover(UiNode node, UiPopover popover, DomNode[] content)
    {
        if (content.Length != popover.Content.Length)
        {
            throw CreateRenderException(node, "Expanded popover content must align with the popover model.");
        }

        List<DomAttribute> attributes = CreateOverlayAttributes(node, popover.State, popover.State.IsModal, "popover");
        attributes.Add(new DomAttribute { Name = AriaAttributes.Modal, Value = FormatBoolean(popover.State.IsModal) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "popover-key", Value = popover.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Popover, Value = "manual" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.Dialog });
        AddAnchorAttributes(popover.Anchor, attributes);
        SortAttributes(attributes);
        List<DomNode> children = new List<DomNode>();
        AddBackdrop(node, popover.State, popover.Key, children);
        int index = 0;
        while (index < content.Length)
        {
            children.Add(content[index]);
            index++;
        }

        return CreateRoot(node, attributes, children.ToArray());
    }

    internal DomNode ExpandTooltip(UiNode node, UiTooltip tooltip)
    {
        List<DomAttribute> attributes = CreateOverlayAttributes(node, tooltip.State, false, "tooltip");
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "tooltip-key", Value = tooltip.Key });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Role, Value = AriaRoles.ToolTip });
        if (tooltip.HasDelayMilliseconds)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "delay-milliseconds", Value = InvariantFormatter.FormatInt32(tooltip.DelayMilliseconds) });
        }

        AddAnchorAttributes(tooltip.Anchor, attributes);
        SortAttributes(attributes);
        return CreateRoot(node, attributes, new DomNode[] { CreateText(node, "$text", "tooltip-text", tooltip.Key, tooltip.Text) });
    }

    internal UiNode CreateActionNode(UiNode node, UiDialog dialog, UiButton action, int index)
    {
        string indexText = InvariantFormatter.FormatInt32(index);
        string actionIdentity = dialog.Key + "/" + indexText;
        UiNodeState state = new UiNodeState();
        state.Version = GetNodeVersion(node);
        if (node.State != null)
        {
            state.IsEnabled = node.State.IsEnabled;
            state.IsVisible = node.State.IsVisible;
            state.IsBusy = node.State.IsBusy;
        }

        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "button",
            Button = action,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "$action-" + indexText,
                SiblingIdentity = "$action-" + indexText,
                StableId = CreateSyntheticId(node, "dialog-action", actionIdentity)
            },
            State = state
        };
    }

    internal List<DomAttribute> CreateOverlayAttributes(UiNode node, UiOverlayState state, bool modal, string kind)
    {
        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-overlay htmlgui-overlay-" + kind });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "backdrop", Value = FormatBoolean(state.HasBackdrop) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dismiss-escape", Value = FormatBoolean(state.DismissOnEscape) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dismiss-outside", Value = FormatBoolean(state.DismissOnOutsideClick) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "modal", Value = FormatBoolean(modal) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "open", Value = FormatBoolean(state.IsOpen) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "stack-order", Value = InvariantFormatter.FormatInt32(state.StackOrder) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "trap-focus", Value = FormatBoolean(state.TrapFocus) });
        if (modal && state.IsOpen)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "inert-background", Value = "true" });
        }

        if (state.RestoreFocusId != null)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "restore-focus", Value = state.RestoreFocusId });
        }

        if (!state.IsOpen || (node.State != null && !node.State.IsVisible))
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.Hidden, Value = string.Empty, IsBoolean = true });
        }

        if (node.State != null && !node.State.IsEnabled)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Disabled, Value = "true" });
        }

        if (node.State != null && node.State.IsBusy)
        {
            attributes.Add(new DomAttribute { Name = AriaAttributes.Busy, Value = "true" });
        }

        return attributes;
    }

    internal void AddAnchorAttributes(UiAnchor anchor, List<DomAttribute> attributes)
    {
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "allow-flip", Value = FormatBoolean(anchor.AllowFlip) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "allow-shift", Value = FormatBoolean(anchor.AllowShift) });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "anchor-target", Value = anchor.TargetId });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "placement", Value = anchor.Placement });
        LayoutExpander layoutExpander = new LayoutExpander();
        string offset = layoutExpander.FormatOptionalLength(anchor.Offset);
        string crossOffset = layoutExpander.FormatOptionalLength(anchor.CrossOffset);
        if (offset.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "offset", Value = offset });
        }

        if (crossOffset.Length > 0)
        {
            attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "cross-offset", Value = crossOffset });
        }
    }

    internal void AddBackdrop(UiNode node, UiOverlayState state, string key, List<DomNode> children)
    {
        if (!state.HasBackdrop)
        {
            return;
        }

        List<DomAttribute> attributes = new List<DomAttribute>();
        attributes.Add(new DomAttribute { Name = AriaAttributes.Hidden, Value = "true" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.Class, Value = "htmlgui-overlay-backdrop" });
        attributes.Add(new DomAttribute { Name = HtmlAttributes.DataPrefix + "dismiss-overlay", Value = FormatBoolean(state.DismissOnOutsideClick) });
        SortAttributes(attributes);
        children.Add(CreateElement(node, "$backdrop", "backdrop", key, HtmlElements.Division, attributes.ToArray(), Array.Empty<DomNode>()));
    }

    internal void ValidateOverlayState(UiNode node, UiOverlayState state)
    {
        if (state.StackOrder < 0)
        {
            throw new ArgumentOutOfRangeException("stackOrder", state.StackOrder, "Overlay stack order cannot be negative.");
        }

        if (state.RestoreFocusId != null && string.IsNullOrWhiteSpace(state.RestoreFocusId))
        {
            throw CreateRenderException(node, "A supplied focus-restoration identifier cannot be empty.");
        }
    }

    internal void ValidateAnchor(UiNode node, UiAnchor anchor)
    {
        if (string.IsNullOrWhiteSpace(anchor.TargetId) || anchor.Offset == null || anchor.CrossOffset == null)
        {
            throw CreateRenderException(node, "Anchors require a target and both offset descriptions.");
        }

        switch (anchor.Placement)
        {
            case "top":
            case "top-start":
            case "top-end":
            case "right":
            case "right-start":
            case "right-end":
            case "bottom":
            case "bottom-start":
            case "bottom-end":
            case "left":
            case "left-start":
            case "left-end":
                break;
            default:
                throw new ArgumentException("The anchor placement is not supported.", "placement");
        }

        LayoutExpander layoutExpander = new LayoutExpander();
        layoutExpander.FormatOptionalLength(anchor.Offset);
        layoutExpander.FormatOptionalLength(anchor.CrossOffset);
    }

    internal bool IsDialogModal(UiDialog dialog)
    {
        switch (dialog.Kind)
        {
            case UiDialogKind.NonModal:
                return false;
            case UiDialogKind.Sheet:
            case UiDialogKind.Drawer:
                return dialog.State.IsModal;
            case UiDialogKind.Modal:
            case UiDialogKind.Alert:
            case UiDialogKind.Confirm:
            case UiDialogKind.Prompt:
                return true;
            default:
                throw new ArgumentOutOfRangeException("kind", dialog.Kind, "The dialog kind is not supported.");
        }
    }

    internal string GetDialogKindToken(UiDialogKind kind)
    {
        switch (kind)
        {
            case UiDialogKind.Modal:
                return "modal";
            case UiDialogKind.NonModal:
                return "non-modal";
            case UiDialogKind.Alert:
                return "alert";
            case UiDialogKind.Confirm:
                return "confirm";
            case UiDialogKind.Prompt:
                return "prompt";
            case UiDialogKind.Sheet:
                return "sheet";
            case UiDialogKind.Drawer:
                return "drawer";
            default:
                throw new ArgumentOutOfRangeException("kind", kind, "The dialog kind is not supported.");
        }
    }

    internal string GetDialogRole(UiDialogKind kind)
    {
        if (kind == UiDialogKind.Alert || kind == UiDialogKind.Confirm)
        {
            return AriaRoles.AlertDialog;
        }

        return AriaRoles.Dialog;
    }

    internal DomNode CreateRoot(UiNode node, List<DomAttribute> attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = GetNodeKey(node),
            Id = GetNodeId(node),
            Name = HtmlElements.Division,
            Attributes = attributes.ToArray(),
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateElement(UiNode node, string key, string suffix, string identity, string name, DomAttribute[] attributes, DomNode[] children)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Element,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Name = name,
            Attributes = attributes,
            Children = children,
            Version = GetNodeVersion(node)
        };
    }

    internal DomNode CreateText(UiNode node, string key, string suffix, string identity, string value)
    {
        return new DomNode
        {
            Kind = DomNodeKind.Text,
            Key = key,
            Id = CreateSyntheticId(node, suffix, identity),
            Text = value,
            Version = GetNodeVersion(node)
        };
    }

    internal string CreateSyntheticId(UiNode node, string suffix, string identity)
    {
        string token = StableHash.HashParts(new string[] { GetNodeId(node), GetNodeKey(node), "overlay", suffix, identity });
        return "ui-overlay-" + token.Substring(0, 20);
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

    internal string FormatBoolean(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
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
