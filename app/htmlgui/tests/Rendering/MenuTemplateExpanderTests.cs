using System;
using htmlgui;

namespace htmlgui.Tests;

public class MenuTemplateExpanderTests
{
    [Test]
    public void ExpandsMenuBarItemKindsPresentationSubmenusAndRovingFocus()
    {
        UiMenuItem open = Command("open", "Open", "file.open");
        open.IconKey = "folder-open";
        open.Accelerator = "Ctrl+O";
        open.Mnemonic = "O";
        UiMenuItem autosave = Command("autosave", "Autosave", "file.autosave");
        autosave.Kind = UiMenuItemKind.Checkbox;
        autosave.HasCheckState = true;
        autosave.CheckState = UiCommandCheckState.Mixed;
        UiMenuItem mode = Command("mode", "Mode", "mode.select");
        mode.Kind = UiMenuItemKind.Radio;
        mode.HasCheckState = true;
        mode.CheckState = UiCommandCheckState.Checked;
        UiMenuItem separator = new UiMenuItem { Kind = UiMenuItemKind.Separator, Key = "separator" };
        UiMenu recent = new UiMenu
        {
            Key = "recent",
            Label = "Recent files",
            Items = new UiMenuItem[] { Command("project", "Project", "recent.project") },
            IsOpen = true
        };
        UiMenuItem recentOwner = new UiMenuItem { Kind = UiMenuItemKind.Submenu, Key = "recent-owner", Label = "Recent", Submenu = recent };
        UiNode node = MenuControl("menu-bar", new UiMenuItem[] { open, autosave, mode, separator, recentOwner });
        node.Menu!.Label = "Application";
        node.Menu.Orientation = UiOrientation.Horizontal;
        node.Menu.ActiveItemKey = "mode";
        node.Menu.TypeAhead = new UiTypeAheadState { Buffer = "mo", MatchedKey = "mode", Generation = 3, IgnoreCase = false };
        node.State.Version = 6;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode openResult = result.Children[0];
        DomNode checkResult = result.Children[1];
        DomNode radioResult = result.Children[2];
        DomNode separatorResult = result.Children[3];
        DomNode submenuOwner = result.Children[4];
        DomNode submenuAction = submenuOwner.Children[0];
        DomNode submenu = submenuOwner.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.MenuBar));
            Assert.That(AttributeValue(result, AriaAttributes.Orientation), Is.EqualTo("horizontal"));
            Assert.That(AttributeValue(result, AriaAttributes.Label), Is.EqualTo("Application"));
            Assert.That(AttributeValue(result, "data-typeahead-buffer"), Is.EqualTo("mo"));
            Assert.That(AttributeValue(result, "data-typeahead-generation"), Is.EqualTo("3"));
            Assert.That(AttributeValue(result, "data-typeahead-ignore-case"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-typeahead-match-id"), Is.EqualTo(radioResult.Id));
            Assert.That(AttributeValue(openResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.MenuItem));
            Assert.That(AttributeValue(openResult, "data-command-id"), Is.EqualTo("file.open"));
            Assert.That(AttributeValue(openResult, "data-icon-key"), Is.EqualTo("folder-open"));
            Assert.That(AttributeValue(openResult, "data-accelerator"), Is.EqualTo("Ctrl+O"));
            Assert.That(AttributeValue(openResult, "data-mnemonic"), Is.EqualTo("O"));
            Assert.That(openResult.Children[0].Attributes[0].Name, Is.EqualTo(AriaAttributes.Hidden));
            Assert.That(openResult.Children[1].Children[0].Text, Is.EqualTo("Open"));
            Assert.That(openResult.Children[2].Children[0].Text, Is.EqualTo("Ctrl+O"));
            Assert.That(AttributeValue(openResult, HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(checkResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.MenuItemCheckbox));
            Assert.That(AttributeValue(checkResult, AriaAttributes.Checked), Is.EqualTo("mixed"));
            Assert.That(AttributeValue(radioResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.MenuItemRadio));
            Assert.That(AttributeValue(radioResult, AriaAttributes.Checked), Is.EqualTo("true"));
            Assert.That(AttributeValue(radioResult, HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(separatorResult, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Separator));
            Assert.That(AttributeValue(submenuOwner, HtmlAttributes.Role), Is.EqualTo(AriaRoles.None));
            Assert.That(AttributeValue(submenuAction, AriaAttributes.HasPopup), Is.EqualTo("menu"));
            Assert.That(AttributeValue(submenuAction, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(submenuAction, AriaAttributes.Controls), Is.EqualTo(submenu.Id));
            Assert.That(AttributeValue(submenu, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Menu));
            Assert.That(HasAttribute(submenu, HtmlAttributes.Hidden), Is.False);
            Assert.That(submenu.Children[0].Children[0].Children[0].Text, Is.EqualTo("Project"));
            Assert.That(submenuOwner.Version, Is.EqualTo(6));
        });
    }

    [TestCase("menu", "popup")]
    [TestCase("popup-menu", "popup")]
    [TestCase("context-menu", "context")]
    public void ExpandsClosedPopupAndContextMenusWithDismissalState(string name, string kind)
    {
        UiNode node = MenuControl(name, new UiMenuItem[] { Command("copy", "Copy", "edit.copy") });
        node.Menu!.Items[0].CommandId = null;
        node.Menu.Label = "Actions";
        node.Menu.IsOpen = false;
        node.Menu.WrapFocus = false;
        node.Menu.DismissOnEscape = false;
        node.Menu.DismissOnOutsideClick = false;
        node.Menu.RestoreFocusId = "invoker";
        node.State.IsBusy = true;

        DomNode result = new SemanticExpander().ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Menu));
            Assert.That(AttributeValue(result, "data-menu-kind"), Is.EqualTo(kind));
            Assert.That(AttributeValue(result, "data-open"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-wrap-focus"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-dismiss-escape"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-dismiss-outside"), Is.EqualTo("false"));
            Assert.That(AttributeValue(result, "data-restore-focus"), Is.EqualTo("invoker"));
            Assert.That(AttributeValue(result, AriaAttributes.Busy), Is.EqualTo("true"));
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(result.Children, Has.Length.EqualTo(1));
            Assert.That(result.Children[0].Key, Is.EqualTo("copy"));
        });
    }

    [Test]
    public void ExpandsMenuButtonTriggerAndControlledPopup()
    {
        UiNode node = MenuControl("menu-button", new UiMenuItem[] { Command("save", "Save", "file.save") });
        node.Menu!.Label = "File actions";
        node.Menu.IsOpen = true;
        node.State.IsEnabled = false;
        node.State.IsVisible = false;
        node.State.HasTabIndex = true;
        node.State.TabIndex = 2;

        DomNode result = new SemanticExpander().ExpandNode(node);
        DomNode trigger = result.Children[0];
        DomNode popup = result.Children[1];

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(result, HtmlAttributes.Class), Is.EqualTo("htmlgui-menu-button"));
            Assert.That(AttributeValue(result, AriaAttributes.Disabled), Is.EqualTo("true"));
            Assert.That(FindAttribute(result, HtmlAttributes.Hidden).IsBoolean, Is.True);
            Assert.That(trigger.Name, Is.EqualTo(HtmlElements.Button));
            Assert.That(AttributeValue(trigger, HtmlAttributes.Type), Is.EqualTo(HtmlInputTypes.Button));
            Assert.That(AttributeValue(trigger, AriaAttributes.HasPopup), Is.EqualTo("menu"));
            Assert.That(AttributeValue(trigger, AriaAttributes.Expanded), Is.EqualTo("true"));
            Assert.That(AttributeValue(trigger, AriaAttributes.Controls), Is.EqualTo(popup.Id));
            Assert.That(AttributeValue(trigger, HtmlAttributes.TabIndex), Is.EqualTo("2"));
            Assert.That(FindAttribute(trigger, HtmlAttributes.Disabled).IsBoolean, Is.True);
            Assert.That(trigger.Children[0].Text, Is.EqualTo("File actions"));
            Assert.That(AttributeValue(popup, HtmlAttributes.Role), Is.EqualTo(AriaRoles.Menu));
            Assert.That(AttributeValue(popup, "data-menu-kind"), Is.EqualTo("button-popup"));
            Assert.That(HasAttribute(popup, HtmlAttributes.Hidden), Is.False);
        });
    }

    [Test]
    public void StableItemIdentitiesSurviveReorderingAndSubmenuVisibilityChanges()
    {
        UiMenu submenu = new UiMenu { Key = "nested", Items = new UiMenuItem[] { Command("child", "Child", "child") } };
        UiMenuItem owner = new UiMenuItem { Kind = UiMenuItemKind.Submenu, Key = "owner", Label = "Owner", Submenu = submenu };
        UiMenuItem command = Command("command", "Command", "command");
        UiNode node = MenuControl("menu-bar", new UiMenuItem[] { owner, command });
        SemanticExpander expander = new SemanticExpander();

        DomNode first = expander.ExpandNode(node);
        string ownerId = first.Children[0].Id;
        string commandId = first.Children[1].Id;
        Assert.That(FindAttribute(first.Children[0].Children[1], HtmlAttributes.Hidden).IsBoolean, Is.True);
        submenu.IsOpen = true;
        node.Menu!.Items = new UiMenuItem[] { command, owner };
        DomNode reordered = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(reordered.Children[0].Id, Is.EqualTo(commandId));
            Assert.That(reordered.Children[1].Id, Is.EqualTo(ownerId));
            Assert.That(HasAttribute(reordered.Children[1].Children[1], HtmlAttributes.Hidden), Is.False);
            Assert.That(reordered.Children[1].Children[1].Children[0].Id, Is.EqualTo(first.Children[0].Children[1].Children[0].Id));
            Assert.That(node.Menu.Items[0], Is.SameAs(command));
            Assert.That(owner.Submenu, Is.SameAs(submenu));
        });
    }

    [Test]
    public void CommandAcceleratorAndActiveStateChangesPreserveMenuItemIdentity()
    {
        UiMenuItem open = Command("open", "Open", "file.open");
        open.Accelerator = "Ctrl+O";
        UiMenuItem save = Command("save", "Save", "file.save");
        save.Accelerator = "Ctrl+S";
        UiNode node = MenuControl("menu-bar", new UiMenuItem[] { open, save });
        node.Menu!.ActiveItemKey = "open";
        SemanticExpander expander = new SemanticExpander();

        DomNode original = expander.ExpandNode(node);
        string openId = original.Children[0].Id;
        string saveId = original.Children[1].Id;
        node.Menu.ActiveItemKey = "save";
        save.CommandId = "document.save";
        save.Accelerator = "Meta+S";
        DomNode changed = expander.ExpandNode(node);

        Assert.Multiple(() =>
        {
            Assert.That(AttributeValue(original.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(AttributeValue(original.Children[1], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(changed.Children[0], HtmlAttributes.TabIndex), Is.EqualTo("-1"));
            Assert.That(AttributeValue(changed.Children[1], HtmlAttributes.TabIndex), Is.EqualTo("0"));
            Assert.That(changed.Children[0].Id, Is.EqualTo(openId));
            Assert.That(changed.Children[1].Id, Is.EqualTo(saveId));
            Assert.That(AttributeValue(changed.Children[1], "data-command-id"), Is.EqualTo("document.save"));
            Assert.That(AttributeValue(changed.Children[1], "data-accelerator"), Is.EqualTo("Meta+S"));
            Assert.That(changed.Children[1].Children[1].Children[0].Text, Is.EqualTo("Meta+S"));
            Assert.That(AttributeValue(original.Children[1], "data-command-id"), Is.EqualTo("file.save"));
            Assert.That(original.Children[1].Children[1].Children[0].Text, Is.EqualTo("Ctrl+S"));
        });
    }

    [Test]
    public void RejectsMalformedMenusItemsAndRelationships()
    {
        UiNode missingPayload = new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = "menu-bar",
            Identity = new UiNodeIdentity { AuthorKey = "missing", StableId = "missing-id" }
        };
        UiNode blankKey = MenuControl("menu", Array.Empty<UiMenuItem>());
        blankKey.Menu!.Key = " ";
        UiNode nullItems = MenuControl("menu", Array.Empty<UiMenuItem>());
        nullItems.Menu!.Items = null!;
        UiNode duplicate = MenuControl("menu", new UiMenuItem[] { Command("same", "One", "one"), Command("same", "Two", "two") });
        UiMenuItem invalidKindItem = Command("invalid", "Invalid", "invalid");
        invalidKindItem.Kind = (UiMenuItemKind)999;
        UiNode invalidKind = MenuControl("menu", new UiMenuItem[] { invalidKindItem });
        UiNode blankCommand = MenuControl("menu", new UiMenuItem[] { new UiMenuItem { Kind = UiMenuItemKind.Command, Key = "command", Label = "Command", CommandId = " " } });
        UiNode missingCheck = MenuControl("menu", new UiMenuItem[] { Command("check", "Check", "check") });
        missingCheck.Menu!.Items[0].Kind = UiMenuItemKind.Checkbox;
        UiMenuItem mixedRadioItem = Command("radio", "Radio", "radio");
        mixedRadioItem.Kind = UiMenuItemKind.Radio;
        mixedRadioItem.HasCheckState = true;
        mixedRadioItem.CheckState = UiCommandCheckState.Mixed;
        UiNode mixedRadio = MenuControl("menu", new UiMenuItem[] { mixedRadioItem });
        UiNode missingSubmenu = MenuControl("menu", new UiMenuItem[] { new UiMenuItem { Kind = UiMenuItemKind.Submenu, Key = "submenu", Label = "Submenu" } });
        UiMenuItem extraSubmenuItem = Command("command", "Command", "command");
        extraSubmenuItem.Submenu = new UiMenu { Key = "extra" };
        UiNode extraSubmenu = MenuControl("menu", new UiMenuItem[] { extraSubmenuItem });
        UiNode badMnemonic = MenuControl("menu", new UiMenuItem[] { Command("command", "Command", "command") });
        badMnemonic.Menu!.Items[0].Mnemonic = "CC";
        UiMenu cyclicMenu = new UiMenu { Key = "cycle" };
        cyclicMenu.Items = new UiMenuItem[] { new UiMenuItem { Kind = UiMenuItemKind.Submenu, Key = "owner", Label = "Owner", Submenu = cyclicMenu } };
        UiNode cycle = MenuControl("menu", Array.Empty<UiMenuItem>());
        cycle.Menu = cyclicMenu;
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.That(Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingPayload))!.NodeId, Is.EqualTo("missing-id"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankKey));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(nullItems));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(duplicate));
            Assert.That(Assert.Throws<ArgumentOutOfRangeException>(() => expander.ExpandNode(invalidKind))!.ParamName, Is.EqualTo("kind"));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(blankCommand));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingCheck));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(mixedRadio));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(missingSubmenu));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(extraSubmenu));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badMnemonic));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(cycle));
        });
    }

    [Test]
    public void RejectsInvalidActiveTypeAheadDismissalAndButtonState()
    {
        UiMenuItem hidden = Command("hidden", "Hidden", "hidden");
        hidden.IsVisible = false;
        UiNode hiddenActive = MenuControl("menu", new UiMenuItem[] { hidden });
        hiddenActive.Menu!.ActiveItemKey = "hidden";
        UiNode separatorMatch = MenuControl("menu", new UiMenuItem[] { new UiMenuItem { Kind = UiMenuItemKind.Separator, Key = "separator" } });
        separatorMatch.Menu!.TypeAhead.MatchedKey = "separator";
        UiNode negativeGeneration = MenuControl("menu", Array.Empty<UiMenuItem>());
        negativeGeneration.Menu!.TypeAhead.Generation = -1;
        UiNode badRestore = MenuControl("menu", Array.Empty<UiMenuItem>());
        badRestore.Menu!.RestoreFocusId = " ";
        UiNode unnamedButton = MenuControl("menu-button", Array.Empty<UiMenuItem>());
        SemanticExpander expander = new SemanticExpander();

        Assert.Multiple(() =>
        {
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(hiddenActive));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(separatorMatch));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(negativeGeneration));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(badRestore));
            Assert.Throws<UiRenderException>(() => expander.ExpandNode(unnamedButton));
        });
    }

    private static UiNode MenuControl(string name, UiMenuItem[] items)
    {
        return new UiNode
        {
            Kind = UiNodeKind.Control,
            Name = name,
            Identity = new UiNodeIdentity
            {
                AuthorKey = "menu-node",
                SiblingIdentity = "menu-node",
                StableId = "menu-node-id"
            },
            Menu = new UiMenu { Key = "main", Items = items }
        };
    }

    private static UiMenuItem Command(string key, string label, string commandId)
    {
        return new UiMenuItem { Kind = UiMenuItemKind.Command, Key = key, Label = label, CommandId = commandId };
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
}
