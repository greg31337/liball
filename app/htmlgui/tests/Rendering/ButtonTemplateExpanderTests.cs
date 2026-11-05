using System;
using htmlgui;

namespace htmlgui.Tests;

public class ButtonTemplateExpanderTests
{
    [TestCase(UiButtonKind.Push, HtmlInputTypes.Button, "htmlgui-button htmlgui-button-push")]
    [TestCase(UiButtonKind.Submit, HtmlInputTypes.Submit, "htmlgui-button htmlgui-button-submit")]
    [TestCase(UiButtonKind.Reset, HtmlInputTypes.Reset, "htmlgui-button htmlgui-button-reset")]
    public void ExpandsPushSubmitAndResetButtonsWithNativeTypes(UiButtonKind kind, string type, string className)
    {
        UiNode node = ButtonControl(kind, "Continue");
        node.State.Version = 6;
        node.Children = new UiNode[] { TextNode("hint", "hint-id", " now") };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Kind, Is.EqualTo(DomNodeKind.Element));
            Assert.That(result.Key, Is.EqualTo("button"));
            Assert.That(result.Id, Is.EqualTo("button-id"));
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(result, HtmlAttributes.Type), Is.EqualTo(type));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo(className));
            Assert.That(result.Version, Is.EqualTo(6));
            Assert.That(result.Children[0].Text, Is.EqualTo("Continue"));
            Assert.That(result.Children[1].Text, Is.EqualTo(" now"));
        });
    }

    [Test]
    public void ToggleButtonsAlwaysProjectPressedStateWithButtonPrecedence()
    {
        UiNode defaultToggle = ButtonControl(UiButtonKind.Toggle, "Pin");
        UiNode nodeStateToggle = ButtonControl(UiButtonKind.Toggle, "Pin");
        nodeStateToggle.State.HasPressed = true;
        nodeStateToggle.State.IsPressed = true;
        UiNode buttonStateToggle = ButtonControl(UiButtonKind.Toggle, "Pin");
        buttonStateToggle.State.HasPressed = true;
        buttonStateToggle.State.IsPressed = true;
        buttonStateToggle.Button!.HasPressed = true;
        buttonStateToggle.Button.IsPressed = false;
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedDefault = expander.ExpandNode(defaultToggle);
        DomNode expandedNodeState = expander.ExpandNode(nodeStateToggle);
        DomNode expandedButtonState = expander.ExpandNode(buttonStateToggle);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(expandedDefault, AriaAttributes.Pressed), Is.EqualTo("false"));
            Assert.That(AttributeValue(expandedNodeState, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(AttributeValue(expandedButtonState, AriaAttributes.Pressed), Is.EqualTo("false"));
        });
    }

    [Test]
    public void ProjectsButtonAndNodeInteractiveStateConservatively()
    {
        UiNode nodeState = ButtonControl(UiButtonKind.Push, "Node state");
        nodeState.State.IsEnabled = false;
        nodeState.State.IsVisible = false;
        nodeState.State.IsBusy = true;
        nodeState.State.HasTabIndex = true;
        nodeState.State.TabIndex = -1;
        nodeState.State.HasPressed = true;
        nodeState.State.IsPressed = true;
        UiNode buttonState = ButtonControl(UiButtonKind.Push, "Button state");
        buttonState.Button!.IsEnabled = false;
        buttonState.Button.IsVisible = false;
        buttonState.Button.IsBusy = true;
        SemanticExpander expander = new SemanticExpander();

        DomNode expandedNodeState = expander.ExpandNode(nodeState);
        DomNode expandedButtonState = expander.ExpandNode(buttonState);

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(expandedNodeState, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(expandedNodeState, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(expandedNodeState, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(AttributeValue(expandedNodeState, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(expandedNodeState, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(FindAttribute(expandedButtonState, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(expandedButtonState, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(expandedButtonState, AriaAttributes.Busy), Is.EqualTo("true"));
        });
    }

    [Test]
    public void ExpandsIconOnlyButtonWithAccessibleLabelAndDecorativeIcon()
    {
        UiNode node = ButtonControl(UiButtonKind.Icon, "Close");
        node.Button!.IconKey = "close";
        node.Children = new UiNode[] { TextNode("shortcut", "shortcut-id", "Esc") };

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Close"));
            Assert.That(AttributeValue(result, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Button));
            Assert.That(result.Children, Has.Length.EqualTo(2));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(AttributeValue(result.Children[0], "data-icon-key"), Is.EqualTo("close"));
            Assert.That(AttributeValue(result.Children[0], AriaAttributes.Hidden), Is.EqualTo("true"));
            Assert.That(result.Children[1].Text, Is.EqualTo("Esc"));
        });
    }

    [Test]
    public void ExpandsCommandButtonWithDispatchIdentifierAndOptionalIcon()
    {
        UiNode node = ButtonControl(UiButtonKind.Command, "Save");
        node.Button!.CommandId = "save-document";
        node.Button.IconKey = "disk";

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, "data-command-id"), Is.EqualTo("save-document"));
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-button htmlgui-button-command"));
            Assert.That(result.Children[0].Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(AttributeValue(result.Children[0], "data-icon-key"), Is.EqualTo("disk"));
            Assert.That(result.Children[1].Text, Is.EqualTo("Save"));
        });
    }

    [Test]
    public void ProjectsOptionalIconsExplicitPressedStateAndCommandChanges()
    {
        UiNode push = ButtonControl(UiButtonKind.Push, "Refresh");
        push.Button!.IconKey = "refresh";
        push.Button.HasPressed = true;
        push.Button.IsPressed = false;
        UiNode command = ButtonControl(UiButtonKind.Command, "Archive");
        command.Button!.CommandId = "archive-current";
        SemanticExpander expander = new SemanticExpander();

        DomNode pushResult = expander.ExpandNode(push);
        DomNode originalCommand = expander.ExpandNode(command);
        command.Button.CommandId = "archive-all";
        DomNode changedCommand = expander.ExpandNode(command);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(pushResult, AriaAttributes.Pressed), Is.EqualTo("false"));
            Assert.That(pushResult.Children, Has.Length.EqualTo(2));
            Assert.That(AttributeValue(pushResult.Children[0], "data-icon-key"), Is.EqualTo("refresh"));
            Assert.That(pushResult.Children[1].Text, Is.EqualTo("Refresh"));
            Assert.That(originalCommand.Children, Has.Length.EqualTo(1));
            Assert.That(originalCommand.Children[0].Text, Is.EqualTo("Archive"));
            Assert.That(AttributeValue(originalCommand, "data-command-id"), Is.EqualTo("archive-current"));
            Assert.That(AttributeValue(changedCommand, "data-command-id"), Is.EqualTo("archive-all"));
            Assert.That(changedCommand.Id, Is.EqualTo(originalCommand.Id));
        });
    }

    [Test]
    public void ExpandsSplitButtonWithStablePrimaryAndMenuToggleSemantics()
    {
        UiNode node = ButtonControl(UiButtonKind.Split, "Export");
        node.Button!.IconKey = "download";
        node.Button.Menu = new UiMenu { Key = "export-menu" };
        node.State.HasTabIndex = true;
        node.State.TabIndex = 2;
        node.State.HasPressed = true;
        node.State.IsPressed = true;
        SemanticExpander expander = new SemanticExpander();

        DomNode closed = expander.ExpandNode(node);
        node.Button.Menu.IsOpen = true;
        DomNode open = expander.ExpandNode(node);
        DomNode closedPrimary = closed.Children[0];
        DomNode closedToggle = closed.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(closed.Name, Is.EqualTo(HtmlElements.Division));
            Assert.That(AttributeValue(closed, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Group));
            Assert.That(AttributeValue(closed, HtmlAttributes.Class), Is.EqualTo("htmlgui-split-button"));
            Assert.That(closedPrimary.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(closedPrimary, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Button));
            Assert.That(AttributeValue(closedPrimary, HtmlAttributes.TabIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(closedPrimary, AriaAttributes.Pressed), Is.EqualTo("true"));
            Assert.That(closedPrimary.Children[0].Name, Is.EqualTo(HtmlElements.Span));
            Assert.That(closedPrimary.Children[1].Text, Is.EqualTo("Export"));
            Assert.That(closedToggle.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(closedToggle, AriaAttributes.HasPopup), Is.EqualTo("menu"));
            Assert.That(AttributeValue(closedToggle, AriaAttributes.Expanded), Is.EqualTo("false"));
            Assert.That(AttributeValue(closedToggle, AriaAttributes.Label), Is.EqualTo("Export menu"));
            Assert.That(AttributeValue(closedToggle, "data-menu-key"), Is.EqualTo("export-menu"));
            Assert.That(AttributeValue(closedToggle, HtmlAttributes.TabIndex), Is.EqualTo("2"));
            Assert.That(AttributeValue(open.Children[1], AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(open.Children[0].Id, Is.EqualTo(closedPrimary.Id));
            Assert.That(open.Children[1].Id, Is.EqualTo(closedToggle.Id));
            Assert.That(open.Children[0], Is.Not.SameAs(closedPrimary));
        });
    }

    [Test]
    public void SplitButtonProjectsDisabledHiddenAndBusyStateToTheCorrectElements()
    {
        UiNode node = ButtonControl(UiButtonKind.Split, "Actions");
        node.Button!.Menu = new UiMenu { Key = "actions" };
        node.Button.IsEnabled = false;
        node.Button.IsVisible = false;
        node.Button.IsBusy = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(FindAttribute(result.Children[0], HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(FindAttribute(result.Children[1], HtmlAttributes.Disabled).IsBoolean, Is.True);
        });
    }

    [Test]
    public void RejectsMissingPayloadLabelsIdentifiersMenusAndUndefinedKinds()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "button",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode missingLabel = ButtonControl(UiButtonKind.Push, " ");
        UiNode missingIcon = ButtonControl(UiButtonKind.Icon, "Icon");
        UiNode missingCommand = ButtonControl(UiButtonKind.Command, "Command");
        UiNode missingMenu = ButtonControl(UiButtonKind.Split, "Split");
        UiNode emptyMenuKey = ButtonControl(UiButtonKind.Split, "Split");
        emptyMenuKey.Button!.Menu = new UiMenu { Key = " " };
        UiNode undefined = ButtonControl((UiButtonKind)999, "Undefined");
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingLabel));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingIcon));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingCommand));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingMenu));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(emptyMenuKey));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(undefined))!.ParamName, Is.EqualTo("kind"));
        });
    }

    [Test]
    public void RepeatedExpansionIsFreshStableAndDoesNotMutateButtonModels()
    {
        UiNode node = ButtonControl(UiButtonKind.Command, "Original");
        node.Button!.CommandId = "original-command";
        UiButton sourceButton = node.Button;
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        DomNode second = expander.ExpandNode(node);
        first.Attributes[0].Value = "changed";
        first.Children[0].Text = "Changed output";

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.SameAs(second));
            Assert.That(first.Attributes, Is.Not.SameAs(second.Attributes));
            Assert.That(first.Children, Is.Not.SameAs(second.Children));
            Assert.That(first.Children[0], Is.Not.SameAs(second.Children[0]));
            Assert.That(first.Children[0].Id, Is.EqualTo(second.Children[0].Id));
            Assert.That(sourceButton.Label, Is.EqualTo("Original"));
            Assert.That(sourceButton.CommandId, Is.EqualTo("original-command"));
            Assert.That(second.Children[0].Text, Is.EqualTo("Original"));
        });
    }

    private static UiNode ButtonControl(UiButtonKind kind, string label)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Identity = new UiNodeIdentity { AuthorKey = "button", SiblingIdentity = "button", StableId = "button-id" },
            Button = new UiButton { Kind = kind, Label = label }
        };
    }

    private static UiNode TextNode(string key, string id, string text)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Text,
            Identity = new UiNodeIdentity { AuthorKey = key, SiblingIdentity = key, StableId = id },
            Text = text
        };
    }

    private static DomAttribute FindAttribute(DomNode node, string name)
    {
        int index = 0;
        while (index < node.Attributes.Length)
        {
            if (node.Attributes[index].Name == name)
            {
                return node.Attributes[index];
            }

            index++;
        }

        throw new AssertionException("Attribute was not found: " + name);
    }

    private static string AttributeValue(DomNode node, string name)
    {
        return FindAttribute(node, name).Value;
    }
}
