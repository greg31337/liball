using System;
using htmlgui;

namespace htmlgui.Tests;

public class OverlayTemplateExpanderTests
{
    [Test]
    public void ExpandsModalDialogContentActionsBackdropFocusAndDismissalPolicy()
    {
        UiNode node = Control("modal-dialog");
        node.Dialog = new UiDialog
        {
            Kind = UiDialogKind.Modal,
            Key = "delete-dialog",
            Title = "Delete item",
            Content = new UiNode[] { TextNode("message", "This cannot be undone.") },
            Actions = new UiButton[]
            {
                new UiButton { Kind = UiButtonKind.Command, Label = "Delete", CommandId = "item.delete" },
                new UiButton { Kind = UiButtonKind.Push, Label = "Cancel" }
            },
            State = new UiOverlayState
            {
                IsOpen = true,
                HasBackdrop = true,
                DismissOnEscape = false,
                DismissOnOutsideClick = true,
                TrapFocus = true,
                RestoreFocusId = "delete-trigger",
                StackOrder = 4
            },
            InitialFocusId = "cancel-button"
        };
        node.State.IsBusy = true;
        node.State.Version = 7;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode backdrop = result.Children[0];
        DomNode dialog = result.Children[1];
        DomNode title = dialog.Children[0];
        DomNode body = dialog.Children[1];
        DomNode actions = dialog.Children[2];

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(result, "data-open"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-modal"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-backdrop"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-dismiss-escape"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-dismiss-outside"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-trap-focus"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-restore-focus"), Is.EqualTo("delete-trigger"));
            Assert.That(AttributeValue(result, "data-stack-order"), Is.EqualTo("4"));
            Assert.That(AttributeValue(result, "data-inert-background"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(HasAttribute(result, HtmlAttributes.Hidden), Is.False);
            Assert.That(AttributeValue(backdrop, AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(AttributeValue(backdrop, "data-dismiss-overlay"), Is.EqualTo("true"));
            Assert.That(dialog.Name, Is.EqualTo(HtmlElements.Dialog));
            Assert.That(HasAttribute(dialog, HtmlAttributes.Role), Is.False);
            Assert.That(AttributeValue(dialog, AriaAttributes.Modal), Is.EqualTo("true"));
            Assert.That(AttributeValue(dialog, AriaAttributes.LabelledBy), Is.EqualTo(title.Id));
            Assert.That(AttributeValue(dialog, "data-initial-focus"), Is.EqualTo("cancel-button"));
            Assert.That(FindAttribute(dialog, HtmlAttributes.Open).IsBoolean, Is.True);
            Assert.That(title.Name, Is.EqualTo(HtmlElements.Heading2));
            Assert.That(title.Children[0].Text, Is.EqualTo("Delete item"));
            Assert.That(body.Children[0].Text, Is.EqualTo("This cannot be undone."));
            Assert.That(AttributeValue(actions, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Group));
            Assert.That(AttributeValue(actions.Children[0], "data-command-id"), Is.EqualTo("item.delete"));
            Assert.That(actions.Children[0].Children[0].Text, Is.EqualTo("Delete"));
            Assert.That(actions.Children[1].Children[0].Text, Is.EqualTo("Cancel"));
            Assert.That(result.Version, Is.EqualTo(7));
            Assert.That(dialog.Version, Is.EqualTo(7));
            Assert.That(actions.Children[0].Version, Is.EqualTo(7));
        });
    }

    [TestCase(UiDialogKind.Modal, "modal", "", "true")]
    [TestCase(UiDialogKind.NonModal, "non-modal", "", "false")]
    [TestCase(UiDialogKind.Alert, "alert", AriaRoles.AlertDialog, "true")]
    [TestCase(UiDialogKind.Confirm, "confirm", AriaRoles.AlertDialog, "true")]
    [TestCase(UiDialogKind.Prompt, "prompt", "", "true")]
    [TestCase(UiDialogKind.Sheet, "sheet", "", "false")]
    [TestCase(UiDialogKind.Drawer, "drawer", "", "false")]
    public void ExpandsEveryDialogKind(UiDialogKind dialogKind, string token, string role, string modal)
    {
        UiNode node = Control("dialog");
        node.Dialog = new UiDialog
        {
            Kind = dialogKind,
            Key = "kind-dialog",
            Title = "Dialog kind",
            State = new UiOverlayState { IsOpen = true }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode dialog = result.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-dialog-kind"), Is.EqualTo(token));
            Assert.That(AttributeValue(result, "data-modal"), Is.EqualTo(modal));
            Assert.That(AttributeValue(dialog, "data-dialog-kind"), Is.EqualTo(token));
            Assert.That(OptionalAttributeValue(dialog, HtmlAttributes.Role), Is.EqualTo(role));
            Assert.That(AttributeValue(dialog, AriaAttributes.Modal), Is.EqualTo(modal));
        });
    }

    [Test]
    public void ModalIntentAndInertBackgroundDependOnKindAndOpenState()
    {
        UiNode closedModal = DialogControl();
        closedModal.Dialog!.Kind = UiDialogKind.Modal;
        closedModal.Dialog.State.IsOpen = false;
        UiNode openNonModal = DialogControl();
        openNonModal.Dialog!.Kind = UiDialogKind.NonModal;
        openNonModal.Dialog.State.IsOpen = true;
        openNonModal.Dialog.State.IsModal = true;
        UiNode modalSheet = DialogControl();
        modalSheet.Dialog!.Kind = UiDialogKind.Sheet;
        modalSheet.Dialog.State.IsOpen = true;
        modalSheet.Dialog.State.IsModal = true;
        modalSheet.Dialog.State.HasBackdrop = true;
        SemanticExpander expander = new SemanticExpander();

        DomNode closedModalResult = expander.ExpandNode(closedModal);
        DomNode openNonModalResult = expander.ExpandNode(openNonModal);
        DomNode modalSheetResult = expander.ExpandNode(modalSheet);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(closedModalResult, "data-modal"), Is.EqualTo("true"));
            Assert.That(HasAttribute(closedModalResult, "data-inert-background"), Is.False);
            Assert.That(HasAttribute(closedModalResult, HtmlAttributes.Hidden), Is.True);
            Assert.That(AttributeValue(openNonModalResult, "data-modal"), Is.EqualTo("false"));
            Assert.That(HasAttribute(openNonModalResult, "data-inert-background"), Is.False);
            Assert.That(AttributeValue(openNonModalResult.Children[0], AriaAttributes.Modal), Is.EqualTo("false"));
            Assert.That(AttributeValue(modalSheetResult, "data-modal"), Is.EqualTo("true"));
            Assert.That(AttributeValue(modalSheetResult, "data-inert-background"), Is.EqualTo("true"));
            Assert.That(AttributeValue(modalSheetResult.Children[0], "data-dismiss-overlay"), Is.EqualTo("true"));
            Assert.That(AttributeValue(modalSheetResult.Children[1], AriaAttributes.Modal), Is.EqualTo("true"));
        });
    }

    [Test]
    public void ExpandsPromptInputAndConfirmActions()
    {
        UiNode prompt = Control("prompt-dialog");
        prompt.Dialog = new UiDialog
        {
            Kind = UiDialogKind.Prompt,
            Key = "rename",
            Title = "Rename item",
            Content = new UiNode[] { InputNode("new-name", "name") },
            Actions = new UiButton[]
            {
                new UiButton { Kind = UiButtonKind.Push, Label = "Rename" },
                new UiButton { Kind = UiButtonKind.Push, Label = "Cancel" }
            },
            State = new UiOverlayState { IsOpen = true }
        };

        DomNode result = new SemanticExpander().ExpandNode(prompt);
        DomNode dialog = result.Children[0];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(dialog, "data-dialog-kind"), Is.EqualTo("prompt"));
            Assert.That(dialog.Children[1].Children[0].Name, Is.EqualTo(HtmlElements.Input));
            Assert.That(AttributeValue(dialog.Children[1].Children[0], HtmlAttributes.Name), Is.EqualTo("name"));
            Assert.That(dialog.Children[2].Children, Has.Length.EqualTo(2));
            Assert.That(dialog.Children[2].Children[0].Children[0].Text, Is.EqualTo("Rename"));
        });
    }

    [Test]
    public void ExpandsAnchoredModalPopoverWithCollisionAndBackdropState()
    {
        UiNode node = Control("popover");
        node.Popover = new UiPopover
        {
            Key = "account-menu",
            Anchor = new UiAnchor
            {
                TargetId = "account-button",
                Placement = "top-end",
                Offset = Length(8, UiLengthUnit.Pixel),
                CrossOffset = Length(0.5, UiLengthUnit.Rem),
                AllowFlip = false,
                AllowShift = true
            },
            Content = new UiNode[] { TextNode("account-content", "Account options") },
            State = new UiOverlayState
            {
                IsOpen = true,
                IsModal = true,
                HasBackdrop = true,
                DismissOnEscape = true,
                DismissOnOutsideClick = false,
                TrapFocus = true,
                StackOrder = 2
            }
        };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Popover), Is.EqualTo("manual"));
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Dialog));
            Assert.That(AttributeValue(result, AriaAttributes.Modal), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-anchor-target"), Is.EqualTo("account-button"));
            Assert.That(AttributeValue(result, "data-placement"), Is.EqualTo("top-end"));
            Assert.That(AttributeValue(result, "data-offset"), Is.EqualTo("8px"));
            Assert.That(AttributeValue(result, "data-cross-offset"), Is.EqualTo("0.5rem"));
            Assert.That(AttributeValue(result, "data-allow-flip"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-allow-shift"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result, "data-inert-background"), Is.EqualTo("true"));
            Assert.That(AttributeValue(result.Children[0], "data-dismiss-overlay"), Is.EqualTo("false"));
            Assert.That(result.Children[1].Text, Is.EqualTo("Account options"));
        });
    }

    [Test]
    public void ProjectsStackDismissalAndFocusRestoreForDialogPopoverAndTooltip()
    {
        UiNode dialog = DialogControl();
        dialog.Dialog!.State = new UiOverlayState
        {
            IsOpen = true,
            DismissOnEscape = true,
            DismissOnOutsideClick = false,
            RestoreFocusId = "dialog-trigger",
            StackOrder = 1
        };
        UiNode popover = PopoverControl();
        popover.Popover!.State = new UiOverlayState
        {
            IsOpen = true,
            DismissOnEscape = false,
            DismissOnOutsideClick = true,
            RestoreFocusId = "popover-trigger",
            StackOrder = 2
        };
        UiNode tooltip = TooltipControl();
        tooltip.Tooltip!.State = new UiOverlayState
        {
            IsOpen = true,
            DismissOnEscape = true,
            DismissOnOutsideClick = true,
            RestoreFocusId = "tooltip-trigger",
            StackOrder = 3
        };
        tooltip.State.IsEnabled = false;
        SemanticExpander expander = new SemanticExpander();

        DomNode dialogResult = expander.ExpandNode(dialog);
        DomNode popoverResult = expander.ExpandNode(popover);
        DomNode tooltipResult = expander.ExpandNode(tooltip);

        Assert.Multiple(() =>
        {
            AssertOverlayState(dialogResult, "1", "true", "false", "dialog-trigger");
            AssertOverlayState(popoverResult, "2", "false", "true", "popover-trigger");
            AssertOverlayState(tooltipResult, "3", "true", "true", "tooltip-trigger");
            Assert.That(AttributeValue(tooltipResult, AriaAttributes.Disabled), Is.EqualTo("true"));
        });
    }

    [Test]
    public void AnchorLossIsRejectedAndRestoringTheTargetPreservesOverlayIdentity()
    {
        UiNode popover = PopoverControl();
        popover.Popover!.State.IsOpen = true;
        UiNode tooltip = TooltipControl();
        tooltip.Tooltip!.State.IsOpen = true;
        SemanticExpander expander = new SemanticExpander();

        DomNode originalPopover = expander.ExpandNode(popover);
        DomNode originalTooltip = expander.ExpandNode(tooltip);
        popover.Popover.Anchor.TargetId = string.Empty;
        tooltip.Tooltip.Anchor.TargetId = " ";

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(popover));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(tooltip));
        });

        popover.Popover.Anchor.TargetId = "replacement-popover-target";
        tooltip.Tooltip.Anchor.TargetId = "replacement-tooltip-target";
        DomNode restoredPopover = expander.ExpandNode(popover);
        DomNode restoredTooltip = expander.ExpandNode(tooltip);

        Assert.Multiple(() =>
        {
            Assert.That(restoredPopover.Id, Is.EqualTo(originalPopover.Id));
            Assert.That(restoredTooltip.Id, Is.EqualTo(originalTooltip.Id));
            Assert.That(AttributeValue(restoredPopover, "data-anchor-target"), Is.EqualTo("replacement-popover-target"));
            Assert.That(AttributeValue(restoredTooltip, "data-anchor-target"), Is.EqualTo("replacement-tooltip-target"));
        });
    }

    [Test]
    public void ExpandsTooltipTimingVisibilityAndStableIdentity()
    {
        UiNode node = Control("tooltip");
        node.Tooltip = new UiTooltip
        {
            Key = "save-help",
            Text = "Save changes",
            Anchor = new UiAnchor { TargetId = "save-button", Placement = "right" },
            State = new UiOverlayState(),
            HasDelayMilliseconds = true,
            DelayMilliseconds = 250
        };
        SemanticExpander expander = new SemanticExpander();

        DomNode closed = expander.ExpandNode(node);
        string textId = closed.Children[0].Id;
        node.Tooltip.State.IsOpen = true;
        DomNode open = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(closed, HtmlAttributes.Role), Is.EqualTo(AriaRoles.ToolTip));
            Assert.That(AttributeValue(closed, "data-delay-milliseconds"), Is.EqualTo("250"));
            Assert.That(AttributeValue(closed, "data-anchor-target"), Is.EqualTo("save-button"));
            Assert.That(AttributeValue(closed, "data-placement"), Is.EqualTo("right"));
            Assert.That(HasAttribute(closed, HtmlAttributes.Hidden), Is.True);
            Assert.That(HasAttribute(open, HtmlAttributes.Hidden), Is.False);
            Assert.That(open.Children[0].Text, Is.EqualTo("Save changes"));
            Assert.That(open.Children[0].Id, Is.EqualTo(textId));
            Assert.That(open.Id, Is.EqualTo(closed.Id));
        });
    }

    [Test]
    public void RejectsMalformedDialogPopoverTooltipAndOverlayState()
    {
        UiNode missingDialog = Control("alert-dialog");
        UiNode blankDialog = DialogControl();
        blankDialog.Dialog!.Key = " ";
        UiNode invalidKind = DialogControl();
        invalidKind.Dialog!.Kind = (UiDialogKind)99;
        UiNode nullContent = DialogControl();
        nullContent.Dialog!.Content = null!;
        UiNode nullAction = DialogControl();
        nullAction.Dialog!.Actions = new UiButton[] { null! };
        UiNode badAction = DialogControl();
        badAction.Dialog!.Actions = new UiButton[] { new UiButton() };
        UiNode blankInitialFocus = DialogControl();
        blankInitialFocus.Dialog!.InitialFocusId = " ";
        UiNode negativeStack = DialogControl();
        negativeStack.Dialog!.State.StackOrder = -1;
        UiNode missingPopover = Control("popover");
        UiNode badAnchor = PopoverControl();
        badAnchor.Popover!.Anchor.TargetId = " ";
        UiNode badPlacement = PopoverControl();
        badPlacement.Popover!.Anchor.Placement = "center";
        UiNode missingTooltip = Control("tooltip");
        UiNode negativeDelay = TooltipControl();
        negativeDelay.Tooltip!.HasDelayMilliseconds = true;
        negativeDelay.Tooltip.DelayMilliseconds = -1;
        UiNode modalTooltip = TooltipControl();
        modalTooltip.Tooltip!.State.IsModal = true;
        UiNode blankRestore = PopoverControl();
        blankRestore.Popover!.State.RestoreFocusId = " ";
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingDialog));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankDialog));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidKind));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullContent));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullAction));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badAction));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankInitialFocus));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(negativeStack));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPopover));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badAnchor));
            Assert.Throws<ArgumentException>(() => expander.ExpandNode(badPlacement));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingTooltip));
            Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(negativeDelay));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(modalTooltip));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankRestore));
        });
    }

    private static UiNode DialogControl()
    {
        UiNode node = Control("dialog");
        node.Dialog = new UiDialog { Key = "dialog", Title = "Dialog" };
        return node;
    }

    private static UiNode PopoverControl()
    {
        UiNode node = Control("popover");
        node.Popover = new UiPopover { Key = "popover", Anchor = new UiAnchor { TargetId = "target" } };
        return node;
    }

    private static UiNode TooltipControl()
    {
        UiNode node = Control("tooltip");
        node.Tooltip = new UiTooltip { Key = "tooltip", Text = "Tooltip", Anchor = new UiAnchor { TargetId = "target" } };
        return node;
    }

    private static UiNode Control(string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = name + "-node",
                SiblingIdentity = name + "-node",
                StableId = name + "-node-id"
            }
        };
    }

    private static UiNode TextNode(string key, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Text = text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" }
        };
    }

    private static UiNode InputNode(string key, string name)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "text-input",
            Input = new UiInput { Kind = UiInputKind.Text, Name = name, Value = new UiValue { Kind = UiValueKind.String, StringValue = string.Empty } },
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = key + "-id" }
        };
    }

    private static UiLength Length(double value, UiLengthUnit unit)
    {
        return new UiLength { HasValue = true, Value = value, Unit = unit };
    }

    private static bool HasAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return attribute;
            }
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }

    private static string OptionalAttributeValue(DomNode node, string name)
    {
        foreach (DomAttribute attribute in node.Attributes)
        {
            if (attribute.Name == name)
            {
                return attribute.Value;
            }
        }

        return string.Empty;
    }

    private static void AssertOverlayState(DomNode node, string stackOrder, string dismissEscape, string dismissOutside, string restoreFocusId)
    {
        Assert.That(AttributeValue(node, "data-stack-order"), Is.EqualTo(stackOrder));
        Assert.That(AttributeValue(node, "data-dismiss-escape"), Is.EqualTo(dismissEscape));
        Assert.That(AttributeValue(node, "data-dismiss-outside"), Is.EqualTo(dismissOutside));
        Assert.That(AttributeValue(node, "data-restore-focus"), Is.EqualTo(restoreFocusId));
    }
}
